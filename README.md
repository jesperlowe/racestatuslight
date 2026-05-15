# Nordschleife LED Lamp for SimHub

Project for a WS2812B LED strip shaped as Nürburgring Nordschleife. SimHub determines session mode and active track section, then sends compact ASCII serial commands to an Arduino Nano/Pro Micro. The Arduino renders the LED strip.

## Hardware list

- Arduino Nano or Arduino Pro Micro.
- WS2812B LED strip, 60 LEDs by default.
- External regulated 5 V power supply sized for the LED count.
- 470 ohm resistor for the data line.
- 1000 uF electrolytic capacitor across LED power.
- USB cable and hookup wire.

## Wiring

- PSU `+5V` -> LED `5V`.
- PSU `GND` -> LED `GND`.
- Arduino `GND` -> PSU `GND`.
- Arduino `D6` -> 470 ohm -> LED `DIN`.
- 1000 uF capacitor between LED `5V` and `GND`.

See `docs/wiring.md` for details and power notes.

## Serial protocol

The protocol is one ASCII line:

```text
<MODE>,<SECTION>\n
```

Examples:

```text
R,7
Q,3
P,10
```

Modes are `R` for Race, `Q` for Qualify, and `P` for Practice/fallback. Sections are `0..11`. See `docs/protocol.md` for the Nordschleife section table.

## Upload Arduino sketch

1. Install Arduino IDE.
2. Install the `FastLED` library.
3. Open `arduino/NordschleifeLedLamp.ino`.
4. Select Arduino Nano or Pro Micro board.
5. Select the Arduino COM port.
6. Upload.

## Serial Monitor tests

Open Serial Monitor at `115200` baud with newline enabled and send:

```text
R,0
Q,5
P,11
```

Expected behavior:

- `R,0` -> red track, section 0 blue.
- `Q,5` -> yellow track, section 5 blue.
- `P,11` -> green track, section 11 blue.

## Install SimHub plugin

1. Install SimHub and Visual Studio 2022 or later.
2. Open `simhub-plugin/NordschleifeLedPlugin.csproj`.
3. Build the plugin against your SimHub installation.
4. Copy `User.NordschleifeLedPlugin.dll` to the plugin location used by your SimHub SDK workflow.
5. Restart SimHub and enable the plugin if prompted.

## Choose COM port

1. Connect the Arduino by USB.
2. Find the port in Windows Device Manager or Arduino IDE, for example `COM5`.
3. Start SimHub once so this file is created:

```text
%APPDATA%\SimHub\NordschleifeLedPlugin\settings.json
```

4. Edit `ComPort` and keep `BaudRate` at `115200`:

```json
{
  "ComPort": "COM5",
  "BaudRate": 115200,
  "LedSections": 12,
  "UpdateIntervalMs": 100,
  "ForceUpdate": false
}
```

## Design notes

The SimHub plugin is intentionally simple and uses only core plugin lifecycle methods from official SDK examples. It reads session/progress defensively because RaceRoom, Assetto Corsa, and SimHub versions may expose different property names. It sends only changed mode/section values by default, which works well with SimHub's 10 Hz free-version limit.
