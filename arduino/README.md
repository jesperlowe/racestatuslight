# Nordschleife LED Lamp Arduino Sketch

Arduino sketch for a WS2812B LED strip shaped like the Nürburgring Nordschleife. It receives ASCII commands from SimHub and renders the whole track in the session color with the active section in blue.

## Hardware list

- Arduino Nano or Arduino Pro Micro.
- WS2812B LED strip, 60 LEDs by default.
- 5 V power supply sized for your LED count.
- 470 ohm resistor for the data line.
- 1000 uF capacitor rated for at least 6.3 V, preferably 10 V or more.
- USB cable and hookup wire.

## Wiring

- PSU `+5V` -> LED `5V`.
- PSU `GND` -> LED `GND`.
- Arduino `GND` -> PSU `GND`.
- Arduino `D6` -> 470 ohm resistor -> LED `DIN`.
- 1000 uF capacitor between LED `5V` and `GND`, observing capacitor polarity.

## Upload sketch

1. Install Arduino IDE.
2. Install the `FastLED` library through Library Manager.
3. Open `NordschleifeLedLamp.ino`.
4. Select board: Arduino Nano or Arduino Leonardo/Pro Micro depending on your hardware.
5. Select the correct COM port.
6. Upload.

## Test with Serial Monitor

Open Serial Monitor at `115200` baud and set line ending to `Newline` or `Both NL & CR`. Send:

```text
R,0
Q,5
P,11
```

Expected output:

- `R,0` -> red base, section 0 blue.
- `Q,5` -> yellow base, section 5 blue.
- `P,11` -> green base, section 11 blue.

## Custom LED layout

Defaults are configured near the top of the sketch:

```cpp
#define NUM_LEDS 60
#define LED_SECTIONS 12
```

For a custom track shape, update `NUM_LEDS`, `LED_SECTIONS`, `sectionStart[]`, and `sectionEnd[]`. Each section is inclusive, so start `35` and end `39` lights LEDs 35, 36, 37, 38, and 39.
