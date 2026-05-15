# Wiring

## Hardware list

- Arduino Nano or Arduino Pro Micro.
- WS2812B strip, 60 LEDs by default.
- External regulated 5 V PSU sized for your LED count.
- 470 ohm resistor on the data line.
- 1000 uF electrolytic capacitor across LED power.
- USB cable for Arduino/SimHub serial connection.

## Connections

| From | To | Notes |
| --- | --- | --- |
| PSU `+5V` | LED strip `5V` | Do not power a long LED strip from the Arduino 5 V pin. |
| PSU `GND` | LED strip `GND` | Shared power ground. |
| Arduino `GND` | PSU `GND` | Required so the data signal has a common reference. |
| Arduino `D6` | 470 ohm resistor -> LED `DIN` | Keep the resistor close to the first LED if possible. |
| 1000 uF capacitor `+` | LED `5V` | Observe polarity. |
| 1000 uF capacitor `-` | LED `GND` | Observe polarity. |

## Power note

A 60 LED WS2812B strip can draw up to about 3.6 A at full white. This project uses brightness `80` and mostly saturated colors, but use a PSU with safe headroom.
