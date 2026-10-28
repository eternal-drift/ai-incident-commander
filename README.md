# AI Incident Commander

A multimodal incident-response workspace. Feed it an operator report, a photo or screenshot, and an optional voice note - it comes back with a timeline, confidence-scored hypotheses, and a set of proposed actions, each one tied to the evidence that backs it up. Nothing gets executed automatically; a human has to approve, edit, or reject every action.

Built for the Meta Global AI Developer Hackathon.

## Why this exists

When something goes wrong, the hard part usually isn't gathering evidence - it's deciding what to do with it fast, without just trusting a confident-sounding guess. This project tries to keep an AI model useful in that loop while making sure every recommendation can be traced back to something real: a fact, a timestamp, an uploaded image, a line in a transcript.

## Status

Early build. This README will fill in as the pieces land - model wiring, the verifier, the demo scenarios, and real evaluation results instead of placeholders.

## What's here so far

- [ ] Incident intake form
- [ ] Meta model integration (model TBD)
- [ ] Image/voice evidence extraction
- [ ] Verifier (schema + evidence-reference checks)
- [ ] Command board UI
- [ ] Approve/reject/edit flow + audit log
- [ ] Seeded demo cases
- [ ] Evaluation results

## Setup

Coming once the stack is chosen and the first slice runs end to end.

## Safety notes

This is decision support, not an authoritative system. It will not contact emergency services, control equipment, or take any real-world action - it only proposes actions for a human to approve. It's scoped to non-medical, non-political operational incidents. All demo data is synthetic.

## License

TBD.
