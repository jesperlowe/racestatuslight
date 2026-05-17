# Home Assistant Protocol

SimHub no longer talks directly to the lamp. SimHub sends a small JSON payload to Home Assistant, then Home Assistant calls the ESPHome service exposed by the lamp.

## HTTP webhook request

```http
POST /api/webhook/<nordschleife_simhub_webhook_id>
Content-Type: application/json
```

```json
{
  "mode": "R",
  "section": 7
}
```

The webhook ID is stored in Home Assistant `secrets.yaml` as `nordschleife_simhub_webhook_id`.

## Mode

| Mode | Meaning | Base color |
| --- | --- | --- |
| `R` | Race | Red |
| `Q` | Qualify / Qualification | Yellow |
| `P` | Practice / test / hotlap / fallback | Green |
| `O` | Offline / idle | Dim white |

Unknown values are normalized to `P` by the Home Assistant script. The ESPHome lamp also falls back to dim white for non-race values it does not recognize.

## Section

`SECTION` is an integer from `0` to `11` for the default 12 Nordschleife sections.

| Section | Track area |
| --- | --- |
| 0 | T13 / Hatzenbach |
| 1 | Hocheichen / Quiddelbacher Höhe |
| 2 | Flugplatz |
| 3 | Schwedenkreuz / Aremberg |
| 4 | Fuchsröhre |
| 5 | Adenauer Forst / Metzgesfeld |
| 6 | Kallenhard / Wehrseifen |
| 7 | Ex-Mühle / Bergwerk |
| 8 | Kesselchen / Klostertal |
| 9 | Karussell |
| 10 | Hohe Acht / Wippermann / Brünnchen |
| 11 | Pflanzgarten / Schwalbenschwanz / Döttinger Höhe / Galgenkopf |

## Mapping logic

Use linear lap progress if SimHub exposes normalized lap progress:

```text
section = floor(trackProgress * 12)
```

Clamp the result to `0..11`. If lap progress is unavailable, use a sector fallback or send section `0`.

## ESPHome service

Home Assistant forwards normalized values to the ESPHome native API service created by the lamp firmware:

```yaml
service: esphome.nordschleife_led_lamp_set_race_status
data:
  mode: R
  section: 7
```

## Curl examples

```bash
curl -X POST "http://homeassistant.local:8123/api/webhook/YOUR_WEBHOOK_ID" \
  -H "Content-Type: application/json" \
  -d '{"mode":"R","section":0}'

curl -X POST "http://homeassistant.local:8123/api/webhook/YOUR_WEBHOOK_ID" \
  -H "Content-Type: application/json" \
  -d '{"mode":"Q","section":5}'

curl -X POST "http://homeassistant.local:8123/api/webhook/YOUR_WEBHOOK_ID" \
  -H "Content-Type: application/json" \
  -d '{"mode":"P","section":11}'
```

## 60 LED default table

```cpp
sectionStart = {0,5,10,15,20,25,30,35,40,45,50,55}
sectionEnd   = {4,9,14,19,24,29,34,39,44,49,54,59}
```
