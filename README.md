# AI Incident Commander

A multimodal incident-response workspace. Feed it an operator report, a photo or screenshot, and an optional voice note - it comes back with a timeline, confidence-scored hypotheses, and a set of proposed actions, each one tied to the evidence that backs it up. Nothing gets executed automatically; a human has to approve, edit, or reject every action.

Built for the Meta Global AI Developer Hackathon.

## Why this exists

When something goes wrong, the hard part usually isn't gathering evidence - it's deciding what to do with it fast, without just trusting a confident-sounding guess. This project tries to keep an AI model useful in that loop while making sure every recommendation can be traced back to something real: a fact, a timestamp, an uploaded image, a line in a transcript.

## Status

Day 1 vertical slice is in. Intake, persistence, and the deterministic verifier all run end to end; the Meta model call is wired but needs a real API key to actually produce a plan.

## What's here so far

- [x] Incident intake form
- [x] Meta model integration (client wired, model TBD once credits are claimed)
- [ ] Image/voice evidence extraction
- [x] Verifier (schema + evidence-reference checks)
- [x] Command board UI
- [x] Approve/reject/edit flow
- [ ] Audit log
- [ ] Seeded demo cases
- [ ] Evaluation results

## Stack

- **Backend:** .NET 8/10 ASP.NET Core Web API, EF Core + SQLite.
- **Frontend:** React + TypeScript + Vite.
- **Model:** Meta Model API (see `backend/Services/MetaModelClient.cs`); the exact wire format is a best guess until we have a live key to test against.

## Setup

### Backend

```bash
cd backend
dotnet run --urls http://localhost:5080
```

Set `META_MODEL_API_KEY` in your environment before submitting an incident, otherwise the API
will return a clear error instead of a fabricated plan. SQLite data file is `backend/incidents.db`
(gitignored).

### Frontend

```bash
cd frontend
cp .env.example .env   # optional, defaults to http://localhost:5080
npm install
npm run dev
```

Open the printed localhost URL, fill in the intake form, and submit. Without a model key you'll
see the plan request fail with a 503-style error - that's expected until credits are set up.

## Design notes / known limitations

Short version of the non-obvious calls made so far, so nobody has to reverse-engineer them from the diff:

- **Stack is C#/.NET + React, not the all-TypeScript setup you might expect from a typical hackathon stack.** Picked to match existing backend/distributed-systems/incident-management experience rather than learning a new stack under a 4-day clock. Meta model calls are plain server-side HTTPS/JSON, so there's no technical reason it had to be Python or Node.
- **The Meta Model API request/response shape in `MetaModelClient.cs` is a best guess (OpenAI-compatible chat completions), not verified against a live key.** This is the first thing to double check once credits are claimed - if the real API differs, this is the one file that should need changing.
- **The verifier is deterministic-only right now.** It checks that every fact/hypothesis/action cites a real evidence or fact id, forces `requiresApproval = true` on every action no matter what the model says, and blocks on missing references. A second, model-assisted pass for semantic contradiction detection (e.g. two facts that disagree without an id mismatch) is deferred to Day 2/3 per the build plan - the spec only requires evidence grounding, not full contradiction reasoning, for the MVP.
- **Persistence is one `Incident` row with the plan stored as a JSON blob column**, not a normalized schema with separate tables for facts/hypotheses/actions. Fine for a demo-scale SQLite database with a handful of seeded incidents; would need to change for anything long-lived or queryable at scale.
- **No audit log yet.** The plan JSON currently gets overwritten on each approve/reject/edit rather than appending an immutable history entry. That's a Day 2 item, not an oversight - it's called out in the checklist above.

## Safety notes

This is decision support, not an authoritative system. It will not contact emergency services, control equipment, or take any real-world action - it only proposes actions for a human to approve. It's scoped to non-medical, non-political operational incidents. All demo data is synthetic.

## License

TBD.
