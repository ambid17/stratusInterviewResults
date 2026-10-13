# Interview Rules

Welcome. This is an AI-collaboration coding interview. The hiring team
wants to see *how* you work with AI coding tools — not just the final
code. Use whatever AI agent, IDE, and tools you would normally use.

## Time-box

Your interview is time-boxed. The exact duration is filled in by your
interviewer in this section. **`60`** minutes.

## Before the session

Have these installed and working ahead of time. Setup time counts against
your time-box, and we would rather you spent it on the problem.

- .NET 8 SDK — `dotnet --version`
- Node.js 18+ — `node --version`
- Unity 2022.3 LTS, installed via Unity Hub, and this project already added
  to Hub so the first import is done

Opening a Unity project for the first time can take several minutes. Do it
before the clock starts.

## During the interview

- The brief for what to build is in [README.md](README.md). Your
  interviewer will point you to the right place if needed.
- Use your normal AI coding workflow. Talk to your AI agent the way you
  would on any work day.
- **Verify the AI's output.** Run tests. Read diffs. Check edge cases.
  Unity is a place where confident-sounding AI output is often subtly wrong;
  we are interested in whether you notice.
- It's fine to abandon an approach. It's fine to ask the AI to back out
  changes. It's fine to push back on the AI's suggestions.
- Work across the whole repo. The task deliberately spans the Unity client,
  the API, and the web dashboard; picking one and ignoring the rest is a
  scope decision, and we would rather hear you make it out loud than find it
  in the diff.
- If you finish early, polish — refactor, add tests, write docs.

## Finishing

1. Run `./end.sh` from this repo's root. It stages an optional
   `interview.log` (if your AI tool produced one) into a single git
   commit.
2. Push the repo to the URL your interviewer provided:
   ```
   git push origin HEAD
   ```
3. Share the URL with your interviewer.

## What we're observing

See `RUBRIC_OVERVIEW.md` for a plain-language summary of the dimensions
the proctor observes during the session. The hiring manager reads the
proctor's report alongside the rest of the interview signal. There is no
numerical score — the report is narrative observations and raw
measurements, and a human makes the decision.

## Questions

If anything is unclear about the rules or the brief — ask
your interviewer. There are no trick questions and no hidden criteria.
