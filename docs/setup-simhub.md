# SimHub Setup

## Reference notes

The plugin follows SimHub's official SDK guidance by using the core plugin lifecycle only. SimHub's official wiki says SDK demo projects are installed under `C:\Program Files (x86)\SimHub\PluginSdk`, including `User.PluginSdkDemo`, and warns that undocumented components can break with SimHub updates.

SimHub's Custom Serial Devices page is useful as a fallback/no-code approach: it documents ASCII messages, selectable update frequency, changes-only updates, automatic reconnect, and the free-version 10 Hz limit. This project uses the same robust idea but keeps the calculation in a tiny plugin and LED control on Arduino.

## Install the plugin

1. Install SimHub.
2. Install Visual Studio 2022 or later with .NET Framework 4.8 targeting pack.
3. Open `simhub-plugin/NordschleifeLedPlugin.csproj`.
4. Build in Release mode.
5. Copy `User.NordschleifeLedPlugin.dll` from the build output to the plugin location used by your SimHub SDK workflow.
6. Restart SimHub.
7. If SimHub asks to activate the plugin, accept/enable it.

## Select COM port

1. Connect the Arduino by USB.
2. In Windows Device Manager or Arduino IDE, note the COM port, for example `COM5`.
3. Start SimHub once so the settings file is created:

```text
%APPDATA%\SimHub\NordschleifeLedPlugin\settings.json
```

4. Edit the settings file:

```json
{
  "ComPort": "COM5",
  "BaudRate": 115200,
  "LedSections": 12,
  "UpdateIntervalMs": 100,
  "ForceUpdate": false
}
```

5. Restart SimHub or reload the plugin.

## Expected behavior

- Race session: command starts with `R`, base color red.
- Qualify session: command starts with `Q`, base color yellow.
- Practice/test/hotlap/unknown: command starts with `P`, base color green.
- Active Nordschleife section is blue.
- If mode and section do not change, the plugin does not repeat the same command unless `ForceUpdate` is `true`.

## Manual serial test before SimHub

Use Arduino IDE Serial Monitor at `115200` baud with newline enabled:

```text
R,0
Q,5
P,11
```

If these work, the Arduino and LED wiring are correct and remaining issues are usually COM-port selection or SimHub plugin installation.
