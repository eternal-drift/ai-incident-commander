# Demo data — copy/paste into the intake form

Exact text for each of the three scripted cases (see `README.md` in this folder for what each
one is meant to demonstrate). Paste these directly into the running app at
`http://localhost:5173` - no editing needed. File attachments are in this same folder.

---

## Case 1 — clear evidence

**Title:**
```
Checkout pods OOMKilled - steady memory climb
```

**Severity:** `high`

**What's happening:**
```
Checkout pods have been restarting every 20-30 minutes since 09:15 UTC due to OOMKilled events. Memory usage climbs steadily from 200MB to the 2GB limit over each cycle. Last deploy was 18 hours ago (unrelated feature flag change). Request rate has been flat all morning per our load balancer dashboard - this does not look like a traffic-driven spike.
```

**Image evidence:** `case1-memory-graph.png`
**Audio evidence:** `case1-voice-note.wav`

---

## Case 2 — conflicting evidence

**Title:**
```
Suspected DB replica lag causing stale reads
```

**Severity:** `medium`

**What's happening:**
```
Customers are reporting stale data on the dashboard - numbers that should have updated an hour ago are still showing old values. We think db-replica-02 might be falling behind. I manually checked the replica a few minutes ago and it looked healthy to me, fully caught up, no errors in the logs.
```

**Image evidence:** `case2-replica-dashboard.png`
**Audio evidence:** `case2-voice-note.wav`

---

## Case 3 — incomplete evidence

**Title:**
```
Something weird with checkout
```

**Severity:** `low`

**What's happening:**
```
A couple of customers messaged support saying checkout felt slow earlier today. Not sure when exactly, no error messages, haven't checked dashboards yet.
```

**Image evidence:** none
**Audio evidence:** none

---

## What to point out live, per case

- **Case 1:** facts cite all three evidence IDs where they overlap (text + image + audio all
  agree); verifier passes; no unknowns block the plan.
- **Case 2:** the model should surface the operator/dashboard contradiction as an explicit
  unknown rather than picking a side - this is the "verifiability moment," don't skip narrating
  it even if the exact wording differs slightly run to run.
- **Case 3:** low-confidence hypotheses, several explicit unknowns, and only fact-finding
  actions (check dashboards, pull logs) - no invented root cause despite almost no input.
