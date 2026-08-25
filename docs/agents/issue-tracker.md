# Issue tracker: Local Markdown

Issues and PRDs for this repo live as markdown files in `.scratch/`.

## Conventions

- One feature per directory: `.scratch/<feature-slug>/`
- The PRD is `.scratch/<feature-slug>/PRD.md`
- Tickets are `.scratch/<feature-slug>/issues/<NN>-<slug>.md`, numbered from `01`
- **Kind** near the top: `lock` (wayfinder decision) or `build` (tracer-bullet implementation). When a pack already has lock tickets, `/to-tickets` continues from the next number and writes **build** files. Do not reuse a lock number for a build ticket.
- Triage state is recorded as a `Status:` line near the top of each issue file (see `triage-labels.md` for the role strings)
- Comments and conversation history append to the bottom of the file under a `## Comments` heading

When a pack has a **map** and a **PRD**, the map is the index for **both** lock tickets and later build tickets. **Decisions so far** holds lock gists. **Frontier** lists every build ticket (number, name, status). Open locks still appear by scanning `issues/` as below.

## When a skill says "publish to the issue tracker"

Create a new file under `.scratch/<feature-slug>/` (creating the directory if needed).

## When a skill says "fetch the relevant ticket"

Read the file at the referenced path. The user will normally pass the path or the issue number directly.

## Wayfinding operations

Used by `/wayfinder`. The **map** is a file with one **child** file per ticket.

- **Map**: `.scratch/<effort>/map.md` — the Notes / Decisions-so-far / Fog body.
- **Child ticket**: `.scratch/<effort>/issues/NN-<slug>.md`, numbered from `01`, with the question in the body. A `Kind: lock` line plus a `Type:` line (`research`/`prototype`/`grilling`/`task`); a `Status:` line records `open`/`claimed`/`resolved`.
- **Blocking**: a `Blocked by: NN, NN` line near the top (empty if none). A ticket is unblocked when every file it lists is `resolved`.
- **Frontier**: during wayfinding, scan `.scratch/<effort>/issues/` for files that are open, unblocked, and unclaimed; first by number wins. After `/to-tickets`, the map **Frontier** table is the build index.
- **Claim**: set `Status: claimed` and save before any work.
- **Resolve**: append the answer under an `## Answer` heading, set `Status: resolved`, then append a context pointer (gist + link) to the map's Decisions-so-far in `map.md`.

### Figma URLs on UI map tickets

When creating or updating a **map child ticket** (or the map Notes) that involves UI — design-to-code, visual chrome, layout, empty states, overlays, Operator dashboard surfaces, etc. — **always attach full Figma URLs**, not node ids or frame names alone.

- Prefer a `## Figma` section near the top of the ticket with one bullet per relevant frame.
- Use share links that include `node-id`, e.g. `https://www.figma.com/design/<fileKey>/…?node-id=3438-40498&m=dev`.
- Keep node ids in prose only as secondary labels (`3438:40498`); agents must be able to open Figma MCP from the URL alone.
- Also list the same URLs on the map under Notes when the destination is UI-shaped, so the map remains the index.
- Backend-only / non-UI tickets may omit Figma unless a screen is needed for acceptance.
