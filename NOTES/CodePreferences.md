# Code Preferences — Godot Tower Defense

> **Applies only to:** `ben0kay/godot-tower-defense` (Godot 4 .NET / C#, 2D).
> **Default development branch:** `dev`; `main` is stable and is updated only when the owner explicitly requests it.
> **Start here:** Read the [root README](../README.md) for the game's direction and current structure, then the owning feature README/notes and relevant code. Reference repositories are examples, not permission to import their architectures.

These preferences guide both AI-assisted and manual development. **Keep the existing Tower Defense game modular, approachable and performance-conscious.** Favour a simple, understandable solution over a large framework. The near-term priority is a polished, playable Tower Defense game; the README's possible future multi-genre integration is **not** authorization for premature refactoring.

## Branch workflow (2026-10-26 — preserved from `dev`)

- **Default development target: `dev`** for all future code, asset, test, and documentation commits/pushes in `ben0kay/godot-tower-defense`, unless I explicitly request another branch.
- Keep `main` as the stable branch. Do **not** commit, push, merge, or change GitHub's default branch to `main` as part of regular development jobs unless I explicitly ask.
- Read and write files using the explicit GitHub ref `dev` (connector calls without a ref may silently read `main`). Fetch `dev`'s current HEAD before composing commits and advance `dev` with a non-force update.
- Only merge `dev` into `main` when I request a merge/release. Changes should generally be one commit per job, as specified below.

## Before every AI task

1. Confirm the **repository, branch, requested scope and actual files** before editing. Read the root README, this file and the relevant feature documentation; check the current code rather than trusting older plans alone. If an `AGENTS.md` exists, read it as well.
2. Identify the feature's current owner and reuse existing shared systems, scene patterns and configuration instead of building parallel implementations.
3. Keep the change **focused**. Preserve unrelated work, authored levels, balance and visuals unless the task explicitly includes them. Don't add speculative features or refactor unrelated systems.
4. Prefer **one Git commit per authorized job**, not many small commits. Push authorized work by default unless the owner says not to; report the commit SHA and changed files. Never force-reset, force-push or overwrite unrelated changes.
5. Follow the **minimal validation policy below**. Don't let testing or broad cleanup expand a small task.

## Validation and testing — IMPORTANT OWNER PREFERENCE

**The owner handles extensive testing in the Godot editor. AI agents must not spend lots of time running tests or repeatedly validating work unless the owner explicitly requests it.**

- **Default:** Inspect the relevant edits/diff and do a brief source sanity check. For C# changes, **one basic `dotnet build` if readily available and practical** is enough in most cases; for focused, risky changes, a small directly relevant smoke check may help.
- **Do not run large regression suites, repeated build/test cycles, extended simulations, prolonged headless Godot runs, visual acceptance passes or performance benchmarks by default.** Ask or leave those to the owner unless explicitly authorized.
- For **documentation-only, configuration-only or very small localized** changes, a focused file/diff review is usually sufficient; no game build is needed just to prove a Markdown edit.
- When the environment lacks Godot/.NET or checks would take disproportionate time, **stop at source review**. Do not install tooling or launch a major troubleshooting effort just to validate.
- **Don't claim something was tested or works if it wasn't.** Report what changed, which lightweight checks (if any) ran, what could not be verified and exactly what the owner should inspect in Godot.
- **This policy overrides generic instructions elsewhere in old project notes that suggest extensive test coverage.** Specific testing instructions from the owner for a particular task take precedence.

The goal is **good, careful implementation with minimal AI validation overhead**, followed by the owner's own gameplay, visual and FPS testing. Never disguise unverified results as accepted.

## Coding style and readability

- Put a short explanatory comment at the **top of each new script** describing its responsibility.
- Prefer comments **above methods/functions**, with a `// =========================================================` divider for easier navigation in Godot/VS Code.
- Use C# `#region` / `#endregion` for substantial related groups when it improves navigation, not around every trivial method.
- Prefer **moderately vertically compact**, readable C#. Keep code simple, avoid gratuitous abstraction and don't sacrifice clarity for fewer lines.
- Use descriptive names, small focused methods and shared helpers where duplication would otherwise accumulate. Reuse common logic such as staggered enemy checks rather than rebuilding it per tower/enemy.
- Group related settings with typed structs/resources and Godot Inspector `[ExportGroup]` / appropriate export attributes. Keep data that designers frequently tweak out of buried hard-coded values.
- Respect the existing repository's naming and uppercase top-level folder conventions. Do not rename whole directory trees simply to match another project.

## Folder ownership, scenes and configuration

- Keep **major feature folders** documented with a short `README.md` explaining their responsibility, key files, data flow, configurable settings and limitations. Read the existing docs before changes and update them when behaviour changes.
- Prefer a **feature-level master config** for frequently tuned settings, using existing `.tres` resources and typed C# `Resource` definitions where appropriate. Reuse the project's current config layout rather than inventing a second master config or relocating working settings.
- Keep systems contained and modular. Reusable plain `Node` helpers/container nodes are welcome to keep scenes tidy, but **container-only nodes do not need processing scripts**. Avoid unnecessary autoloads, central managers and deep scene-tree coupling.
- Prefer explicit signals, interfaces, callbacks or well-defined services over cross-feature hard-coded `GetNode` paths. Compose reusable systems in levels/scenes instead of hiding everything in one enormous controller.
- Keep **authored definitions/configuration** distinct from **per-instance runtime state**, and separate gameplay outcomes from purely cosmetic feedback.
- Use the existing owners as your guide: `COMBAT/` for shared combat rules, `TOWERS/` for tower behaviour and tower-specific assets, `ENEMIES/` for enemies, `WORLD/` for world/navigation, `LEVELS/` for mission composition, `ECONOMY/` for economy, `UI/` for menus/HUD, `FEEDBACK/` for shared visual feedback, `SYSTEMS/` for cross-cutting systems, `DEBUG/` for development tools, and `TESTS/` for checks. Inspect the feature README for finer ownership; **do not move an existing subsystem merely because this overview mentions another folder**.
- Keep existing levels and their launch/test paths independently usable. Do not couple reusable logic to one level, one tower type or one debug scene.
- Future idle/empire or shooter integration is **planning only**. Don't add speculative cross-game managers, move the Tower Defense UI, or change saving/economy contracts without a real authorized task.

## Performance-first implementation rules

**Before coding, ask:** *What wakes this feature? What does it do while active? What stops it? What does it cost while idle? How does that cost scale as waves get larger?*

- **Inactive means dormant.** Don't add `_Process` / `_PhysicsProcess` by default when an event, signal, timer or explicit callback will do. Use `SetProcess(false)`, `SetPhysicsProcess(false)`, emitter shutdown and deliberate wake/sleep transitions when suitable.
- If an effect is finished, disabled, has zero intensity or no valid work, it should not keep scanning, drawing, redrawing, animating or rewriting GPU properties. Preserve a clear event/config path to wake it again **without idle polling**.
- Enemy movement, target acquisition, projectiles, towers, shields, construction drones, pathfinding and visual effects must be designed for **many active instances**, not just one. Prefer shared bounded work, staggered checks, reusable caches/pools and batching where they simplify real hot paths.
- Avoid repeated scene-tree searches, unnecessary resource loads, per-frame allocations and full-map scans. Cache or index data when justified, and invalidate it correctly when towers/terrain/navigation change.
- Use shared renderers/buffers for high-count cosmetic effects when appropriate instead of thousands of independently updating nodes. Keep visuals separate from authoritative damage, economy and navigation state.
- Avoid expensive work on level load, wave start, large spawn batches, building placement and teardown. Budget or spread non-urgent work when needed; do not introduce delayed feedback or visible pop-in just to reduce spikes.
- Keep UI updates event-driven where possible; don't rebuild Inspector/HUD layouts every frame when their inputs haven't changed.
- Prefer **measured improvement** over assumed optimization. Describe FPS/CPU/GPU or memory results only when actually measured, including approximate test conditions. The owner handles longer profiling by default under the validation policy above.

## Godot 4 C# and gameplay reliability

- Match the Godot/.NET API available to this repository. Don't invent API calls, signals or thread-safety assumptions.
- Use valid C# enumeration (`foreach (var item in items)`) and conventional indexed loops (`for (int i = 0; i < count; i++)`); inspect new syntax briefly before committing.
- Keep Godot scene-tree mutations on permitted threads. If background work is ever introduced, bound and cancel it safely, and don't apply obsolete results after a scene/level unloads.
- Preserve resource paths, existing scene references, signals, exported properties and stable IDs where saves, authored content or tests depend on them.
- Aim for configurable tower/enemy variants using existing shared systems rather than hard-coding behaviour to the Battle Tower, Mars terrain, Level001 or another current example.
- Respect active/inactive lifecycle, pause, level transitions, destruction and cleanup. Timers, drone effects, temporary projectiles and callbacks must not continue consuming work after their owner is gone.

## Documentation, plans and AI handoff

- Check relevant feature Markdown and `NOTES/IMPLEMENTATION_BACKLOG.md` / `NOTES/WORK.md` when the task touches planned phases. **Older notes may be superseded**: distinguish source-implemented behaviour from plans, outdated instructions and unverified claims.
- For substantial changes, prefer a few independently reviewable phases with **scope, affected owners, expected behaviour, config knobs, performance considerations and owner acceptance checks**. Do not implement future phases without authorization.
- Use clear status language: **Planned / In Progress / Source Implemented / Awaiting Owner Validation / Accepted**. A commit, successful build or headless test does not equal gameplay/visual acceptance.
- Update the owning feature README and relevant backlog/status notes after implementing an authorized feature. Correct outdated contradictions when directly relevant, without turning a small job into a full documentation audit.
- If there is a useful recurring fix or performance lesson, document it in the existing appropriate notes. Don't create elaborate new process files unnecessarily.
- When reporting back, be concise: **what changed, config/paths to inspect, commit SHA, lightweight checks performed and any owner-side testing left**. Never imply Copilot, Codex, regular Chat or the owner ran checks they did not run.

## Reference projects

Other isometric 2D/3D and Godot repositories may offer organization, reusable ideas and implementation lessons. **Adapt useful patterns; do not blindly copy systems, folder names, third-party addons, 3D world architecture or branching rules.** This project's actual files and Tower Defense mechanics are the source of truth.

**Bottom line:** Keep the 2D Tower Defense game easy to work on, modular, expandable and fast **without overengineering**. Implement the requested feature, perform only lightweight proportional checks, push one focused commit, and let the owner handle extensive Godot testing.
