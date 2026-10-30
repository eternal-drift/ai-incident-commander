# AI Incident Commander

A multimodal incident-response workspace. Feed it an operator report, a photo or screenshot, and an optional voice note - it comes back with a timeline, confidence-scored hypotheses, and a set of proposed actions, each one tied to the evidence that backs it up. Nothing gets executed automatically; a human has to approve, edit, or reject every action.

Built for the Meta Global AI Developer Hackathon.

## Why this exists

When something goes wrong, the hard part usually isn't gathering evidence - it's deciding what to do with it fast, without just trusting a confident-sounding guess. This project tries to keep an AI model useful in that loop while making sure every recommendation can be traced back to something real: a fact, a timestamp, an uploaded image, a line in a transcript.

## Status

Core loop runs end to end against a live Meta Model API key: text intake, image and audio evidence extraction, plan generation, the deterministic verifier, and human approve/reject decisions all work. A few things are still open before this is demo-ready - see the checklist below.

## What's here so far

- [x] Incident intake form
- [x] Meta model integration (text plan generation, verified against a live key)
- [x] Image evidence extraction (vision description via Muse Spark)
- [x] Audio evidence extraction (transcription via Muse Voice Transcribe)
- [x] Verifier (schema + evidence-reference checks)
- [x] Command board UI
- [x] Approve/reject flow (backend also supports edit; no UI for it yet)
- [x] Audit log (backend endpoint; no UI for it yet)
- [ ] File upload UI for image/audio evidence (backend + API client support it; the intake form has no file inputs yet)
- [ ] Edit-action UI (backend supports `edited` decisions; only Approve/Reject buttons exist)
- [ ] Audit log UI (backend serves the full trail; nothing in the frontend displays it yet)
- [ ] Seeded demo cases
- [ ] Evaluation results (clear/conflicting/incomplete evidence cases)

## Stack

- **Backend:** .NET 8/10 ASP.NET Core Web API, EF Core + SQLite.
- **Frontend:** React + TypeScript + Vite.
- **Model:** Meta Model API - `muse-spark-1.3` for plan generation and image understanding (`backend/Services/MetaModelClient.cs`, `MetaVisionClient.cs`), `muse-voice-transcribe-1.0` for audio (`MetaVoiceClient.cs`). Verified end to end against a live key.

## Setup

### Backend

```bash
export META_MODEL_API_KEY="your-key-here"
cd backend
dotnet run
```

The backend listens on `http://localhost:5288` (fixed by `Properties/launchSettings.json`).
Without `META_MODEL_API_KEY` set, the API returns a clear error instead of a fabricated plan.
SQLite data file is `backend/incidents.db` (gitignored).

### Frontend

```bash
cd frontend
cp .env.example .env   # sets VITE_API_BASE=http://localhost:5288
npm install
npm run dev
```

Open the printed localhost URL (`http://localhost:5173`), fill in the intake form, and submit.

### Tests

```bash
cd backend.Tests
dotnet test
```

17 unit tests covering the deterministic verifier (evidence grounding, unknown-id blocking,
the forced `requiresApproval` override, and the keyword-opposite contradiction check) and the
evidence storage layer (file size/type limits, filename sanitization against path traversal).
No live API key needed - these don't call the Meta Model API.

### Docker

```bash
export META_MODEL_API_KEY="your-key-here"
docker compose up --build
```

Backend on `http://localhost:5288`, frontend on `http://localhost:5173` (deliberately mapped to
5173 on the host so the browser's `Origin` header matches the backend's CORS allowlist).
SQLite data and uploaded evidence persist in named Docker volumes across restarts.

## Design notes / known limitations

Short version of the non-obvious calls made so far, so nobody has to reverse-engineer them from the diff:

- **Stack is C#/.NET + React, not the all-TypeScript setup you might expect from a typical hackathon stack.** Picked to match existing backend/distributed-systems/incident-management experience rather than learning a new stack under a 4-day clock. Meta model calls are plain server-side HTTPS/JSON, so there's no technical reason it had to be Python or Node.
- **The Meta Model API integration (chat completions for text/vision, the dedicated `/v1/asr/transcribe` endpoint for audio) is verified against a live key**, including real image description and real speech transcription. Model IDs are pinned to specific versions (`muse-spark-1.3`, `muse-voice-transcribe-1.0`) rather than unversioned aliases.
- **The verifier is deterministic-only right now.** It checks that every fact/hypothesis/action cites a real evidence or fact id, forces `requiresApproval = true` on every action no matter what the model says, and blocks on missing references. A second, model-assisted pass for semantic contradiction detection (e.g. two facts that disagree without an id mismatch) is deferred to Day 2/3 per the build plan - the spec only requires evidence grounding, not full contradiction reasoning, for the MVP.
- **Persistence is one `Incident` row with the plan stored as a JSON blob column**, not a normalized schema with separate tables for facts/hypotheses/actions. Fine for a demo-scale SQLite database with a handful of seeded incidents; would need to change for anything long-lived or queryable at scale.
- **Audit log exists on the backend but has no UI yet.** Every incident creation, plan generation, evidence processing, and approve/reject/edit decision is appended as an immutable `AuditEntry` row (see `AuditLogger.cs`), retrievable via `GET /api/incidents/{id}/audit`. The plan JSON itself is still overwritten per decision - the audit trail is what preserves history, not the plan blob.

## Evaluation

Three scripted cases covering clear, conflicting, and incomplete evidence - see
[`demo/cases/README.md`](demo/cases/README.md) for the exact inputs and how to re-run them.
Each was submitted through the real running app against a live Meta Model API key; nothing
below is simulated or hand-written.

| Case | Evidence condition | Evidence grounding | Unsupported actions | Contradiction handling | All actions require approval | Verifier |
|---|---|---|---|---|---|---|
| 1 - Checkout OOMKilled | Clear, all modalities agree | 10/10 (100%) | 0 | n/a - no conflict present | yes | pass |
| 2 - Suspected DB replica lag | Conflicting (operator says healthy, dashboard shows degraded) | 11/11 (100%) | 0 | Flagged as an explicit unknown (U1) instead of silently picking a side | yes | pass |
| 3 - "Something weird with checkout" | Incomplete (vague report, no files) | 9/9 (100%) | 0 | n/a - no conflict present | yes | pass |

"Evidence grounding" here means every fact cites a real evidence ID, every hypothesis cites a
real fact ID, and every action cites a real fact ID - counted directly from the plan JSON each
case actually returned, not estimated.

**Limitations observed:**
- The verifier is deterministic-only (schema + ID-reference checks). It would not catch a
  case where the model cited a real fact ID but the citation was semantically wrong - that
  would need a second model-assisted pass, which is out of scope for this submission.
- Case 2's contradiction handling depends on the model noticing the conflict unprompted; there
  is no code-level contradiction detector forcing it. It worked in this run, but isn't
  guaranteed on every phrasing of a conflicting case.
- Latency: text-only plan generation (case 3) returns in a few seconds; cases with an image and
  audio attachment (1 and 2) take longer end-to-end because of the extra sequential vision and
  transcription calls before plan generation starts.

## Safety notes

This is decision support, not an authoritative system. It will not contact emergency services, control equipment, or take any real-world action - it only proposes actions for a human to approve. It's scoped to non-medical, non-political operational incidents. All demo data is synthetic.

## License

MIT - see [LICENSE](LICENSE).
