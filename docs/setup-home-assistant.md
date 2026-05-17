# Home Assistant + ESPHome Setup

## 1. Prepare ESPHome secrets

Add these entries to your ESPHome `secrets.yaml`:

```yaml
wifi_ssid: "Your Wi-Fi SSID"
wifi_password: "Your Wi-Fi password"
fallback_ap_password: "ChangeThisFallbackPassword"
ota_password: "ChangeThisOtaPassword"
nordschleife_led_lamp_api_key: "PASTE_A_32_BYTE_BASE64_KEY_FROM_ESPHOME"
```

ESPHome can generate an API encryption key when you create a new device in the ESPHome dashboard. Reuse that generated value for `nordschleife_led_lamp_api_key`.

## 2. Flash the ESP32 lamp

1. Copy `esphome/nordschleife-led-lamp.yaml` to the ESPHome configuration directory.
2. Adjust these substitutions if needed:
   - `led_pin`: defaults to `GPIO18`.
   - `led_count`: defaults to `60`.
   - `led_sections`: defaults to `12`.
3. Flash the ESP32 by USB for the first install.
4. After Wi-Fi is working, future updates can use OTA.
5. Add the ESPHome device to Home Assistant if it is not discovered automatically.

## 3. Enable Home Assistant packages

If packages are not already enabled, add this to Home Assistant `configuration.yaml`:

```yaml
homeassistant:
  packages: !include_dir_named packages
```

Then copy `home-assistant/packages/nordschleife_led_lamp.yaml` into the `packages` folder.

## 4. Add the webhook secret

Add a long random webhook ID to Home Assistant `secrets.yaml`:

```yaml
nordschleife_simhub_webhook_id: "replace-with-a-long-random-string"
```

The package uses `local_only: true`, so the webhook is intended for SimHub or helpers running on your local network.

## 5. Reload Home Assistant YAML

Restart Home Assistant, or reload YAML automations, scripts, and helpers. Confirm these entities/services exist:

- `script.nordschleife_set_lamp`
- `automation.nordschleife_simhub_webhook_to_lamp`
- `input_select.nordschleife_session_mode`
- `input_number.nordschleife_active_section`
- `esphome.nordschleife_led_lamp_set_race_status`

## 6. Test without SimHub

Run this from a machine on the same network as Home Assistant:

```bash
curl -X POST "http://homeassistant.local:8123/api/webhook/YOUR_WEBHOOK_ID" \
  -H "Content-Type: application/json" \
  -d '{"mode":"R","section":0}'
```

Try additional states:

```bash
curl -X POST "http://homeassistant.local:8123/api/webhook/YOUR_WEBHOOK_ID" \
  -H "Content-Type: application/json" \
  -d '{"mode":"Q","section":5}'

curl -X POST "http://homeassistant.local:8123/api/webhook/YOUR_WEBHOOK_ID" \
  -H "Content-Type: application/json" \
  -d '{"mode":"P","section":11}'
```

## 7. Connect SimHub without a custom plugin

Use a SimHub built-in HTTP/web request action, a no-code output feature, or an external helper that reads SimHub data and sends HTTP. Send JSON to:

```text
http://homeassistant.local:8123/api/webhook/YOUR_WEBHOOK_ID
```

The payload must match this shape:

```json
{
  "mode": "R",
  "section": 7
}
```

Recommended update behavior:

- Send only when `mode` or `section` changes.
- Keep updates around 10 Hz or slower.
- Clamp `section` to `0..11` before sending.
- Use `R`, `Q`, or `P` for live sessions and `O` when SimHub is idle.
