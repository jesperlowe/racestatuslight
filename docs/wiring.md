# Wiring

## Hardware list

- ESP32 development board.
- WS2812B strip, 60 LEDs by default.
- External regulated 5 V PSU sized for your LED count.
- 470 ohm resistor on the data line.
- 1000 uF electrolytic capacitor across LED power.
- USB cable for initial ESPHome flashing.

## Connections

| From | To | Notes |
| --- | --- | --- |
| PSU `+5V` | LED strip `5V` | Do not power a long LED strip from the ESP32 5 V pin. |
| PSU `GND` | LED strip `GND` | Shared power ground. |
| ESP32 `GND` | PSU `GND` | Required so the data signal has a common reference. |
| ESP32 `GPIO18` | 470 ohm resistor -> LED `DIN` | Change `led_pin` in `esphome/nordschleife-led-lamp.yaml` if you use another GPIO. |
| 1000 uF capacitor `+` | LED `5V` | Observe polarity. |
| 1000 uF capacitor `-` | LED `GND` | Observe polarity. |

## Power note

A 60 LED WS2812B strip can draw up to about 3.6 A at full white. This project uses 80% brightness and mostly saturated colors, but use a PSU with safe headroom.

## Logic level note

Many short WS2812B strips work directly from an ESP32 data pin when the strip and ESP32 share ground. For longer wiring, unreliable first-pixel behavior, or a 5 V strip with strict data timing, add a 3.3 V to 5 V logic level shifter on the data line.
