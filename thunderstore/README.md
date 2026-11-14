# REPO Photon Transport

Fixes `Disconnected from server: ClientTimeout` / `ExceptionOnConnect` in R.E.P.O when the default UDP connection to Photon does not work on your network.
Restarting the game does not help with this error — this mod does.

**English** · [Русский](#русский)

## What it does

Switches Photon from UDP to WebSocketSecure (TLS, port 443). The game still connects to the same official Photon servers. Nothing else is changed: no extra servers, no data collection.

You can play with people who don't have the mod. Install it only if you have the error.

## Check that it works

`BepInEx/LogOutput.log` should contain `transport=WebSocketSecure socket=PhotonClientWebSocket`.

## Still disconnecting?

Set `Protocol = Tcp` in `BepInEx/config/io.gedisu.repophotontransport.cfg` and restart. If TCP does not help either, the problem is outside the game and this mod cannot fix it.

## Credits

Port of [PeakPhotonTransport](https://github.com/Swerrio/PeakPhotonTransport) by Swerrio (MIT).

---

# Русский

Исправляет `Disconnected from server: ClientTimeout` / `ExceptionOnConnect` в R.E.P.O, когда стандартное UDP-подключение к Photon не работает в твоей сети.
Перезапуск игры от этой ошибки не помогает — помогает этот мод.

## Что делает

Переключает Photon с UDP на WebSocketSecure (TLS, порт 443). Игра подключается к тем же официальным серверам Photon. Больше ничего не меняется: никаких сторонних серверов, никакого сбора данных.

Играть можно с теми, у кого мода нет. Ставить нужно только тем, у кого ошибка.

## Проверить, что работает

В `BepInEx/LogOutput.log` должна быть строка `transport=WebSocketSecure socket=PhotonClientWebSocket`.

## Всё равно выкидывает?

Поставь `Protocol = Tcp` в `BepInEx/config/io.gedisu.repophotontransport.cfg` и перезапусти игру. Если и с TCP не подключается — проблема вне игры, и мод тут не поможет.

## Авторство

Порт [PeakPhotonTransport](https://github.com/Swerrio/PeakPhotonTransport) от Swerrio (MIT).
