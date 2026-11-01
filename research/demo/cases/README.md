# Demo / evaluation cases

Three scripted incidents, each submitted through the live app against a real Meta Model API
key, covering the three evidence conditions called for in the build spec: clear, conflicting,
and incomplete evidence. These are the cases to run live in the demo video and the source of
the evaluation numbers in the main README.

All inputs here are synthetic - no real customer, production, or personal data.

## Case 1 - clear evidence: "Checkout pods OOMKilled"

- **Title:** Checkout pods OOMKilled - steady memory climb
- **Severity:** high
- **Report:** text describing a 20-30 minute OOMKilled restart cycle, flat traffic, and an
  unrelated deploy 18 hours prior.
- **Image:** `case1-memory-graph.png` - a synthetic Grafana-style panel showing a sawtooth
  memory curve climbing to a 2GB limit line and dropping on each restart.
- **Audio:** `case1-voice-note.wav` - a synthesized voice note from "the on-call engineer"
  describing the same symptoms, generated with `say` and converted to mono 16kHz PCM WAV.
- **What it's meant to show:** all three evidence modalities (text, image, audio) agree, and
  the model's facts cite all three source IDs where they overlap. No contradictions, no
  unsupported actions, verifier passes.

## Case 2 - conflicting evidence: "Suspected DB replica lag"

- **Title:** Suspected DB replica lag causing stale reads
- **Severity:** medium
- **Report:** operator suspects a replica is behind, but says they manually checked it and it
  looked healthy.
- **Image:** `case2-replica-dashboard.png` - a synthetic monitoring panel showing the same
  replica as `DEGRADED`, with a climbing lag graph and repeated connection-reset errors.
- **Audio:** `case2-voice-note.wav` - a synthesized voice note repeating the operator's "looks
  healthy to me" claim from the text report.
- **What it's meant to show:** the operator's own account (text + voice) directly contradicts
  the monitoring screenshot. This is the "verifiability moment" - the model should surface the
  contradiction as an unknown/question rather than silently picking a side or averaging the two
  into a false, confident answer.

## Case 3 - incomplete evidence: "Something weird with checkout"

- **Title:** Something weird with checkout
- **Severity:** low
- **Report:** text only, deliberately vague - "a couple" of customers, no timestamp, no error
  messages, dashboards not yet checked. No image or audio attached.
- **What it's meant to show:** with almost nothing to go on, the model should produce
  low-confidence hypotheses, several explicit unknowns, and only fact-finding actions (check
  dashboards, pull logs) rather than inventing a root cause.

## Running these cases yourself

```bash
curl -X POST http://localhost:5288/api/incidents \
  -F "title=Checkout pods OOMKilled - steady memory climb" \
  -F "severity=high" \
  -F "report=Checkout pods have been restarting every 20-30 minutes since 09:15 UTC due to OOMKilled events. Memory usage climbs steadily from 200MB to the 2GB limit over each cycle. Last deploy was 18 hours ago (unrelated feature flag change). Request rate has been flat all morning per our load balancer dashboard - this does not look like a traffic-driven spike." \
  -F "image=@demo/cases/case1-memory-graph.png;type=image/png" \
  -F "audio=@demo/cases/case1-voice-note.wav;type=audio/wav"
```

Swap in `case2-*` / `case3-*` and the corresponding title/severity/report from above for the
other two cases (case 3 has no image/audio - omit those `-F` flags).
