# Stratus Field Link

## Interview documents

Read these before you start:

- [PRIVACY_RELEASE.md](PRIVACY_RELEASE.md) — sign before running `./start.sh`
- [INTERVIEW_RULES.md](INTERVIEW_RULES.md) — recording workflow, time-box, and setup notes
- [PROCESS.md](PROCESS.md) — short write-up template to complete after the session
- [RUBRIC_OVERVIEW.md](RUBRIC_OVERVIEW.md) — what the proctor observes

## What you're building

This starter is the thin end of Stratus Field Link: a **Unity field app** a foreman
uses to sketch a pipe run on the jobsite, a **.NET API** it publishes to, and a
**Vue + TypeScript** dashboard the shop reviews it on. All three are in this repo.

Your job is to close the loop — get a sketch from the tablet into the shop's
dashboard, and make it behave when the network doesn't.

The domain is intentionally small:

- A **sketch** is a run of pipe or duct: an ordered list of **nodes** in job-site
  coordinates (feet), plus a diameter, a material, and a package
- Nodes have `x`/`z` on the floor plane and `y` for elevation — a **riser** or a
  **drop** is a change in `y`
- A sketch moves `Draft → Published → UnderReview → Approved | Rejected`
- `Draft` only ever exists on the tablet. The server's first sight of a sketch is
  the publish

No MEP experience is needed. That paragraph is the whole domain.

## Time-box

**60 minutes**

You are not expected to finish everything below. Ship a coherent slice and say
what you cut.

## What the starter provides

- **.NET 8 API** with an in-memory repository seeded from JSON — no database to install
- **Unity 2022.3 LTS project** under `unity/StratusFieldLink`, with EditMode and
  PlayMode test assemblies already wired up
- **Vue 3 + TypeScript** shop dashboard
- **~150 field sketches** in `src/Data/sketches.seed.json`, loaded on API startup
- A stubbed auth endpoint, a dashboard endpoint, and a green test suite on both sides
- Optional project hook configs for Claude Code, Cursor, and Codex that can append
  structured events to `interview.log`

> **Heads up:** parts of this starter were generated with AI and lightly reviewed.
> It runs, but it has rough edges — exactly the kind you'd inherit on a real team.
> Treat it as a codebase you've just been handed, not as a reference you should trust.

## Getting started

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- Unity 2022.3 LTS (via Unity Hub)

No database installation is required.

### 1) Run the API

```bash
cd src/StratusFieldLink.Api
dotnet restore
dotnet run
```

The API listens on `http://localhost:5072` and auto-seeds from
`src/Data/sketches.seed.json`. Check it:

```bash
curl http://localhost:5072/api/health
curl http://localhost:5072/api/dashboard
```

### 2) Run the shop dashboard

```bash
cd src/stratus-field-link-web
npm install
npm run dev
```

Vite serves on `http://localhost:5173` and proxies `/api` to the API.

### 3) Open the Unity project

Add `unity/StratusFieldLink` in Unity Hub and open it with **2022.3 LTS**.

There is no `.unity` scene checked in — scene assets are merge-hostile blobs and
this starter would rather you could read the setup. Build the scene from the menu:

**Stratus → Build Field Sketch Scene**

That writes `Assets/Scenes/FieldSketch.unity` and opens it. Press **Play**.

```
click       place a node at the working elevation
backspace   undo the last node
R / F       raise / lower the working elevation (this is how you sketch a riser)
N           start a new sketch
```

The scene is built in code by `SketchSceneFactory` — the same method the PlayMode
tests call, so what they exercise is what ships.

### 4) Run the tests

**API:**

```bash
dotnet test
```

**Unity:** `Window → General → Test Runner`, then run **EditMode** and **PlayMode**.

Both suites are green on a fresh clone. You are expected to add your own.

## The build

### 1. Close the publish loop

The Unity client can sketch a run but cannot get it to the shop.

- Add `POST /api/sketches` — you decide the contract, the validation, the status
  code, and what a duplicate publish does. `SketchValidator` is already written and
  tested; wire it up.
- Make `PublishService.PublishRoutine` do something sane with the answer. Today
  anything that isn't a 2xx is logged and dropped.
- A published sketch has to appear in `GET /api/sketches` and on the dashboard
  without restarting the API.

### 2. Survive a bad network

Foremen work in basements. `PublishQueue` is written and unit-tested; nothing
drains it. Decide what happens on no signal, on a 4xx, on a 5xx, and when the same
client-generated id arrives twice — then make the foreman able to tell which one
happened.

### 3. Add throughput (deliberately under-specified)

The shop wants to know how much work crews are publishing per day and whether
review is keeping up. Nobody has defined that number. Pick a definition, state your
assumptions in `PROCESS.md`, and move on.

### 4. Fix what's wrong

Support ticket, forwarded from the field lead this morning, unedited:

> Crew 4 says the app won't let them publish a vertical drop off the header on
> L3 — it tells them the run is too short even though it's a 12 foot drop. They've
> been sketching a fake elbow to get around it. Also the shop says our footage
> totals are coming in light on the multi-level packages and they've been adding
> a fudge factor. Probably unrelated?

Find it, explain it, fix it, and prove the fix with tests. Note that the API and
the Unity client each have their own copy of the measurement logic.

### Tests

Cover the publish path on both sides of the seam. On the Unity side we care that
you can write both **EditMode** unit tests and a **PlayMode** test that drives the
real components — `StubFieldLinkTransport` exists so you can do that without a
live server.

## Deliverables

1. **The code**, with clear run instructions if you changed how anything starts.
2. **`PROCESS.md`** — what AI tools you used, where AI output was wrong, how you
   validated what you kept, your throughput assumptions, and what you'd do next.
3. **Tests** covering the publish path and whatever you fixed.

## Ground rules

- **AI is encouraged.** Use your real workflow — Claude Code, Cursor, Codex, Copilot.
- **Timebox is real.** Ship a coherent slice; cut scope openly rather than running over.
- **Don't gold-plate.** This is not a production system. We're not grading polish.

## Recording the session

1. Sign [PRIVACY_RELEASE.md](PRIVACY_RELEASE.md)
2. Run `./start.sh` from the repo root — this records your terminal session
3. Work in your normal AI-assisted workflow
4. When finished, exit the recording shell and run `./end.sh`

`start.sh` and `end.sh` depend only on the terminal recording. If your AI tool
supports project hooks, the repo includes optional configs under `.claude/`,
`.cursor/`, and `.codex/` that may append structured events to `interview.log`.
Codex users may need to trust project hooks via `/hooks` before they run.

## Repository layout

```
src/StratusFieldLink.Api/          .NET 8 minimal API
  Domain/                          FieldSketch, RunGeometry, SketchValidator
  Infrastructure/                  in-memory repository + seed loading
  Api/                             endpoints and DTOs
src/StratusFieldLink.Api.Tests/    xUnit
src/stratus-field-link-web/        Vue 3 + TypeScript shop dashboard
src/Data/sketches.seed.json        ~150 seeded sketches
unity/StratusFieldLink/            Unity 2022.3 LTS project
  Assets/StratusFieldLink/Runtime/ geometry, models, transport, sketching, HUD
  Assets/StratusFieldLink/Editor/  scene builder menu item
  Assets/StratusFieldLink/Tests/   EditMode + PlayMode assemblies
scripts/generate-seed.mjs          regenerates the seed dataset
```

## Maintainer notes

To regenerate the seed dataset:

```bash
node scripts/generate-seed.mjs
```

This overwrites `src/Data/sketches.seed.json` with a deterministic ~150-sketch
dataset. Timestamps in the file are fixed for reproducibility and slid forward to
"now" as a block when the API loads them — see `SeedTimeRebaser`.

The Unity project is checked in without `.meta` files or a generated
`ProjectSettings/` beyond `ProjectVersion.txt`; Unity writes both on first open.
Assembly definition references are name-based rather than GUID-based so nothing
breaks when those GUIDs differ between clones. If you fork this to add assets,
open the project once and commit what Unity generates.

## Process

See [INTERVIEW_RULES.md](INTERVIEW_RULES.md) for the recording and interview workflow.
