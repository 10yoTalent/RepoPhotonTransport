using System;
using System.Collections.Generic;
using System.Reflection;
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Realtime;

namespace RepoPhotonTransport
{
    [HarmonyPatch(typeof(LoadBalancingClient), nameof(LoadBalancingClient.ConnectUsingSettings))]
    internal static class ConnectUsingSettingsPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(LoadBalancingClient __instance, AppSettings appSettings)
        {
            Transport.ApplyTo(appSettings, "ConnectUsingSettings");
            Transport.ForcePeer(__instance, "ConnectUsingSettings");
        }
    }

    [HarmonyPatch]
    internal static class ConnectPathPatches
    {
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var t = typeof(LoadBalancingClient);

            yield return AccessTools.Method(t, nameof(LoadBalancingClient.ConnectToNameServer));
            yield return AccessTools.Method(t, nameof(LoadBalancingClient.ConnectToMasterServer));
            yield return AccessTools.Method(t, nameof(LoadBalancingClient.ConnectToRegionMaster), new[] { typeof(string) });
            yield return AccessTools.Method(t, nameof(LoadBalancingClient.ReconnectToMaster));
            yield return AccessTools.Method(t, nameof(LoadBalancingClient.ReconnectAndRejoin));
            yield return AccessTools.Method(t, nameof(LoadBalancingClient.Connect), Type.EmptyTypes);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(LoadBalancingClient __instance, MethodBase __originalMethod)
        {
            Transport.ApplyToPunSettings(__originalMethod.Name);
            Transport.ForcePeer(__instance, __originalMethod.Name);
        }
    }

    [HarmonyPatch]
    internal static class ConnectDiagnosticsPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(PhotonPeer),
                nameof(PhotonPeer.Connect),
                new[] { typeof(string), typeof(string), typeof(string), typeof(object), typeof(object) });
        }

        [HarmonyPostfix]
        private static void Postfix(PhotonPeer __instance, string __0, bool __result)
        {
            if (Plugin.Verbose == null || !Plugin.Verbose.Value) return;

            var protocol = __instance == null ? "?" : __instance.TransportProtocol.ToString();
            var socket = __instance?.SocketImplementation?.Name ?? "?";

            Plugin.Log.LogInfo(
                $"[Photon] connect -> {__0} | transport={protocol} socket={socket} attempt-started={__result}");
        }
    }
}
