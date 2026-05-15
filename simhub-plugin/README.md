# Nordschleife LED SimHub Plugin

C# SimHub plugin that sends simple ASCII commands to an Arduino-controlled WS2812B Nordschleife lamp.

## Why this architecture?

SimHub's official SDK page says the plugin examples are installed with SimHub under `C:\Program Files (x86)\SimHub\PluginSdk`, and warns that the SDK covers the core shown in examples while undocumented internals can break after updates. This plugin therefore keeps the SimHub side small: it reads GameData defensively, calculates mode/section, and sends `<MODE>,<SECTION>\n` over a normal serial port.

SimHub's Custom Serial Devices page confirms that serial messages are ASCII, update messages can be sent at a selected frequency or on changes only, and the free version is limited to 10 Hz. The default plugin update interval is therefore 100 ms.

## Features

- Session mapping:
  - `race` -> `R`
  - `qual` or `qualification` -> `Q`
  - `practice`, `test`, `hotlap`, missing, or unknown -> `P`
- 12 configurable LED sections by default.
- Lap progress mapping: `floor(trackProgress * sections)`, clamped to `0..sections-1`.
- Fallback to sector index, otherwise section `0`.
- Sends only when mode or section changes unless `ForceUpdate` is enabled.
- Reconnects if the serial port is missing or disconnected.
- Publishes/logs the latest message and debug text where the SimHub build exposes a compatible property/log API.

## Files

- `NordschleifeLedPlugin.cs` - plugin source.
- `NordschleifeLedPlugin.csproj` - Visual Studio/MSBuild project for .NET Framework 4.8.

## Build

1. Install SimHub and Visual Studio 2022 or later.
2. Copy or open this folder near the SDK examples in `C:\Program Files (x86)\SimHub\PluginSdk`.
3. Open `NordschleifeLedPlugin.csproj`.
4. If needed, set the MSBuild property `SimHubInstallPath` to your SimHub install directory.
5. Build the project.
6. Copy `bin\Release\net48\User.NordschleifeLedPlugin.dll` to SimHub's plugin/addon folder as used by your SimHub SDK workflow.
7. Restart SimHub and enable the plugin if prompted.

## Configure COM port

On first run the plugin creates:

```text
%APPDATA%\SimHub\NordschleifeLedPlugin\settings.json
```

Example:

```json
{
  "ComPort": "COM3",
  "BaudRate": 115200,
  "LedSections": 12,
  "UpdateIntervalMs": 100,
  "ForceUpdate": false
}
```

Set `ComPort` to the Arduino port shown in Windows Device Manager or Arduino IDE. Keep baudrate at `115200` unless you also change `SERIAL_BAUD` in the sketch.

## Debugging

The plugin records:

- `LastSentMessage` - latest serial payload, for example `R,7`.
- `LastDebug` - selected telemetry source and fallback details.

Because SimHub property names vary between RaceRoom, Assetto Corsa, and SimHub versions, the source code tries several common names and ignores missing values instead of throwing.

## Protocol

Every command is one ASCII line:

```text
<MODE>,<SECTION>\n
```

Examples:

```text
R,7
Q,3
P,10
```

See `../docs/protocol.md` for the full protocol and section table.
