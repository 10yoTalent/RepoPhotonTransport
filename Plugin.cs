using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;

namespace RepoPhotonTransport
{
    public enum TransportMode
    {
        Udp,
        Tcp,
        WebSocketSecure,
    }

    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "io.gedisu.repophotontransport";
        public const string Name = "REPO Photon Transport";
        public const string Version = "1.0.1";
        public const string Author = "gedisu";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<TransportMode> Mode;
        internal static ConfigEntry<bool> AllowFallback;
        internal static ConfigEntry<bool> Verbose;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind(
                "General", "Enabled", true,
                "Master switch. Turn off to leave Photon completely untouched (vanilla UDP behaviour).");

            Mode = Config.Bind(
                "Transport", "Protocol", TransportMode.WebSocketSecure,
                "Transport Photon should connect with.\n" +
                "WebSocketSecure - TLS on port 443.\n" +
                "Tcp - plain TCP on 4530/4531/4533.\n" +
                "Udp - Photon's default.");

            AllowFallback = Config.Bind(
                "Transport", "EnableProtocolFallback", true,
                "Let Photon switch to WebSocketSecure on its own if the chosen protocol fails to connect.\n" +
                "Photon's fallback only ever moves towards WebSocketSecure, never back to UDP, so this\n" +
                "cannot undo the fix. It does nothing when Protocol is already WebSocketSecure.");

            Verbose = Config.Bind(
                "Diagnostics", "VerboseLogging", true,
                "Log every Photon server hop with the transport actually used.\n" +
                "Useful when checking whether the fix took effect.");

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo(Enabled.Value
                ? $"Forcing Photon transport to {Transport.Desired} " +
                  $"(protocol fallback {(AllowFallback.Value ? "enabled" : "disabled")})."
                : "Disabled by config, Photon keeps its default transport.");
        }
    }

    internal static class Transport
    {
        internal static ConnectionProtocol Desired
        {
            get
            {
                switch (Plugin.Mode.Value)
                {
                    case TransportMode.Tcp: return ConnectionProtocol.Tcp;
                    case TransportMode.WebSocketSecure: return ConnectionProtocol.WebSocketSecure;
                    default: return ConnectionProtocol.Udp;
                }
            }
        }

        internal static bool Active => Plugin.Enabled != null && Plugin.Enabled.Value;

        internal static void ApplyTo(Photon.Realtime.AppSettings settings, string origin)
        {
            if (!Active || settings == null) return;

            var want = Desired;
            var changed = settings.Protocol != want || settings.Port != 0;

            settings.Protocol = want;
            settings.Port = 0;
            settings.EnableProtocolFallback = Plugin.AllowFallback.Value;

            if (changed && Plugin.Verbose.Value)
            {
                Plugin.Log.LogInfo($"[{origin}] AppSettings.Protocol -> {want} (port reset to protocol default)");
            }
        }

        internal static void ForcePeer(Photon.Realtime.LoadBalancingClient client, string origin)
        {
            if (!Active || client == null) return;

            // R.E.P.O (DataDirector.PhotonSetAppId) ставит AlternativeUdpPorts (27000-27002),
            // пока протокол ещё UDP. Для WSS/TCP эти порты неверны -> первая попытка рвётся.
            var ports = client.ServerPortOverrides;
            if (Desired != ConnectionProtocol.Udp &&
                (ports.NameServerPort != 0 || ports.MasterServerPort != 0 || ports.GameServerPort != 0))
            {
                client.ServerPortOverrides = default(Photon.Realtime.PhotonPortDefinition);
                if (Plugin.Verbose.Value)
                    Plugin.Log.LogInfo($"[{origin}] ServerPortOverrides cleared (were {ports.NameServerPort}/{ports.MasterServerPort}/{ports.GameServerPort})");
            }

            var peer = client.LoadBalancingPeer;
            if (peer == null) return;

            if (peer.PeerState != PeerStateValue.Disconnected) return;

            var want = Desired;
            if (peer.TransportProtocol != want)
            {
                peer.TransportProtocol = want;
                if (Plugin.Verbose.Value)
                {
                    Plugin.Log.LogInfo($"[{origin}] peer.TransportProtocol -> {want}");
                }
            }

            client.ExpectedProtocol = null;
            client.EnableProtocolFallback = Plugin.AllowFallback.Value;
        }

        internal static void ApplyToPunSettings(string origin)
        {
            if (!Active) return;

            try
            {
                var settings = Photon.Pun.PhotonNetwork.PhotonServerSettings;
                if (settings != null) ApplyTo(settings.AppSettings, origin);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"[{origin}] could not reach PhotonNetwork.PhotonServerSettings: {e.Message}");
            }
        }
    }
}
