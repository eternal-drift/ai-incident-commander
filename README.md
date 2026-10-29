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

## Design notes / known limitations

Short version of the non-obvious calls made so far, so nobody has to reverse-engineer them from the diff:

- **Stack is C#/.NET + React, not the all-TypeScript setup you might expect from a typical hackathon stack.** Picked to match existing backend/distributed-systems/incident-management experience rather than learning a new stack under a 4-day clock. Meta model calls are plain server-side HTTPS/JSON, so there's no technical reason it had to be Python or Node.
- **The Meta Model API integration (chat completions for text/vision, the dedicated `/v1/asr/transcribe` endpoint for audio) is verified against a live key**, including real image description and real speech transcription. Model IDs are pinned to specific versions (`muse-spark-1.3`, `muse-voice-transcribe-1.0`) rather than unversioned aliases.
- **The verifier is deterministic-only right now.** It checks that every fact/hypothesis/action cites a real evidence or fact id, forces `requiresApproval = true` on every action no matter what the model says, and blocks on missing references. A second, model-assisted pass for semantic contradiction detection (e.g. two facts that disagree without an id mismatch) is deferred to Day 2/3 per the build plan - the spec only requires evidence grounding, not full contradiction reasoning, for the MVP.
- **Persistence is one `Incident` row with the plan stored as a JSON blob column**, not a normalized schema with separate tables for facts/hypotheses/actions. Fine for a demo-scale SQLite database with a handful of seeded incidents; would need to change for anything long-lived or queryable at scale.
- **Audit log exists on the backend but has no UI yet.** Every incident creation, plan generation, evidence processing, and approve/reject/edit decision is appended as an immutable `AuditEntry` row (see `AuditLogger.cs`), retrievable via `GET /api/incidents/{id}/audit`. The plan JSON itself is still overwritten per decision - the audit trail is what preserves history, not the plan blob.

## Safety notes

This is decision support, not an authoritative system. It will not contact emergency services, control equipment, or take any real-world action - it only proposes actions for a human to approve. It's scoped to non-medical, non-political operational incidents. All demo data is synthetic.

## License

MIT - see [LICENSE](LICENSE).
