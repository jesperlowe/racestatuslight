# SimHub Setup Without a Custom Plugin

This project no longer uses a SimHub SDK plugin. SimHub should send race state to Home Assistant, and Home Assistant forwards that state to the ESPHome lamp.

## Target endpoint

Configure SimHub, a SimHub no-code output, or a local helper to POST JSON to your Home Assistant webhook:

```text
http://homeassistant.local:8123/api/webhook/YOUR_WEBHOOK_ID
```

Use the webhook ID from Home Assistant `secrets.yaml`:

```yaml
nordschleife_simhub_webhook_id: "replace-with-a-long-random-string"
```

## Payload

```json
{
  "mode": "R",
  "section": 7
}
```

- `mode`: `R` for race, `Q` for qualify, `P` for practice/fallback, or `O` for offline.
- `section`: active Nordschleife section from `0` to `11`.

## Section calculation

If your SimHub profile exposes normalized lap progress as `0.0..1.0`, calculate:

```text
section = floor(trackProgress * 12)
```

Clamp the result to `0..11`. If lap progress is unavailable, use an available sector value as an approximate fallback or send `0`.

## Manual verification

Before configuring SimHub, verify Home Assistant and the lamp with curl:

```bash
curl -X POST "http://homeassistant.local:8123/api/webhook/YOUR_WEBHOOK_ID" \
  -H "Content-Type: application/json" \
  -d '{"mode":"R","section":0}'
```

If the curl test works, any remaining work is only mapping SimHub session/progress values into the same JSON payload.
