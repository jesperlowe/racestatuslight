# Nordschleife LED Lamp for Home Assistant

Project for a WS2812B LED strip shaped as Nürburgring Nordschleife. The lamp is now an ESPHome/Home Assistant device: SimHub sends race state to a Home Assistant webhook, and Home Assistant forwards the state to the ESP32 lamp over the ESPHome native API.

No custom SimHub plugin is required, and the lamp no longer needs to stay connected to the SimHub PC by USB serial after flashing.

## Architecture

```text
SimHub telemetry/action
        |
        | HTTP POST /api/webhook/<secret webhook id>
        v
Home Assistant automation + script
        |
        | ESPHome native API service
        v
ESP32 + WS2812B Nordschleife lamp
```

## Hardware list

- ESP32 development board.
- WS2812B LED strip, 60 LEDs by default.
- External regulated 5 V power supply sized for the LED count.
- 470 ohm resistor for the data line.
- 1000 uF electrolytic capacitor across LED power.
- USB cable for first ESPHome flash.

## Wiring

- PSU `+5V` -> LED `5V`.
- PSU `GND` -> LED `GND`.
- ESP32 `GND` -> PSU `GND`.
- ESP32 `GPIO18` -> 470 ohm -> LED `DIN`.
- 1000 uF capacitor between LED `5V` and `GND`.

See `docs/wiring.md` for details and power notes.

## Data contract

Home Assistant expects a JSON payload with mode and section:

```json
{
  "mode": "R",
  "section": 7
}
```

Modes are `R` for Race, `Q` for Qualify, `P` for Practice/fallback, and `O` for Offline. Sections are `0..11`. See `docs/protocol.md` for the Nordschleife section table and curl examples.

## Install the lamp firmware

1. Install the ESPHome add-on in Home Assistant, or install the ESPHome CLI.
2. Copy `esphome/nordschleife-led-lamp.yaml` into your ESPHome configuration folder.
3. Add the required secrets shown in `docs/setup-home-assistant.md`.
4. Flash the ESP32 once over USB.
5. Adopt or add the device in Home Assistant.

## Configure Home Assistant

1. Copy `home-assistant/packages/nordschleife_led_lamp.yaml` into your Home Assistant packages folder.
2. Enable packages if your Home Assistant configuration does not already load them.
3. Add `nordschleife_simhub_webhook_id` to `secrets.yaml`.
4. Restart Home Assistant or reload YAML automations/scripts/helpers.

Full steps are in `docs/setup-home-assistant.md`.

## SimHub integration without a plugin

Configure SimHub using a built-in HTTP/web request action, a no-code output feature, or a small external helper to POST this payload to Home Assistant:

```text
http://homeassistant.local:8123/api/webhook/<nordschleife_simhub_webhook_id>
```

```json
{"mode":"R","section":7}
```

The exact SimHub UI depends on your installed SimHub version and game profile. The important part is that SimHub should calculate or expose the current session mode and lap-progress section, then POST the JSON to the Home Assistant webhook. This repository no longer contains or requires a SimHub SDK plugin.

## Manual test

After Home Assistant is configured, test from any machine on the same network:

```bash
curl -X POST "http://homeassistant.local:8123/api/webhook/YOUR_WEBHOOK_ID" \
  -H "Content-Type: application/json" \
  -d '{"mode":"R","section":0}'
```

Expected behavior:

- `{"mode":"R","section":0}` -> red track, section 0 blue.
- `{"mode":"Q","section":5}` -> yellow track, section 5 blue.
- `{"mode":"P","section":11}` -> green track, section 11 blue.
- `{"mode":"O","section":0}` -> dim white/offline track, section 0 blue.
