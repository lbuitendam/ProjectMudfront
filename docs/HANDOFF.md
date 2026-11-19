# Handoff & AI rules – Project Mudfront

**Every AI (Claude, GPT, free models) reads this first, then GDD.md and TASKS.md.**

## 1. Rules
1. Work on **one task** from TASKS.md or its linked active execution queue. For economy work use ECONOMY_TASKS.md in dependency order. Set it to `[~]` when you start and `[x]` only after acceptance checks pass.
2. Stay within the decisions in GDD.md. Want to change something? Propose it, don't just do it.
3. **No assets/code from other games** (BFH, Heroes of Valor, WARDOGS). Everything is made new. Reference docs describe style/systems only.
4. Small changes, then a Git commit with a clear message.
5. Gameplay values (prices, damage, cooldowns) go in **data files**, not hard-coded.
6. Server-authoritative thinking: money, hits and building are decided by the server.
7. Free models: only tasks marked [FREE], and follow an existing example exactly.

## 2. Structure
```
G:\dev\ProjectMudfront\
  docs\        GDD, ROADMAP, TASKS, HANDOFF, reference\
  art\         palette, concepts, Blender sources
  tools\blender\  Python scripts that generate models
  Mudfront\    Unity project (to be created)
```

## 3. Status (update at the end of every session)
- **Last update:** 2026-11-19
- **Phase:** 2–3 (offline prototype + first map grey-box)
- **Done:** test slice (Slice_Test) + Map 01 Saltmarsh Point grey-box, built and validated in batch mode; 10/10 play mode tests pass (capture/contest/score, shooting, respawn, vehicle enter/drive/exit/destroy/respawn, door clearance, run speed). Reports + screenshots: docs/verification/.
- **Rebuild/verify (Unity closed):** `Unity.exe -batchmode -projectPath Mudfront -executeMethod Mudfront.EditorTools.SaltmarshGreybox.BuildAndValidate` (also `SliceSceneBuilder.BuildAndValidate`); tests: `-runTests -testPlatform PlayMode`. In the editor: menu **Mudfront**.
- **Economy planning:** WARDOGS public-source research is in `reference/wardogs-economy-study.md`; 25 sequential Claude tasks with acceptance criteria and a starting prompt are in `ECONOMY_TASKS.md`. No economy code implemented in the planning session. Start with ECON-01, verify the current baseline, and preserve existing local edits (including Shooter.cs). GDD now includes requested purchased ground vehicles and owner locks; money still resets per match.
- **Next:** execute the economy queue one task at a time. Art direction and grey-box polish remain separate backlog work; multiplayer library (Fish-Net) not installed yet.
- **Open questions:** final faction names; servers; Muddogs green has weak contrast on grass (1.26:1).
