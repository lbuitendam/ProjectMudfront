# Handoff & AI rules – Project Mudfront

**Every AI (Claude, GPT, free models) reads this first, then GDD.md and TASKS.md.**

## 1. Rules
1. Work on **one task** from TASKS.md. Set it to `[~]` when you start and `[x]` when done.
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
- **Last update:** 2026-11-18
- **Phase:** 2–3 (offline prototype + first map grey-box)
- **Done:** test slice (Slice_Test) + Map 01 Saltmarsh Point grey-box, built and validated in batch mode; 10/10 play mode tests pass (capture/contest/score, shooting, respawn, vehicle enter/drive/exit/destroy/respawn, door clearance, run speed). Reports + screenshots: docs/verification/.
- **Rebuild/verify (Unity closed):** `Unity.exe -batchmode -projectPath Mudfront -executeMethod Mudfront.EditorTools.SaltmarshGreybox.BuildAndValidate` (also `SliceSceneBuilder.BuildAndValidate`); tests: `-runTests -testPlatform PlayMode`. In the editor: menu **Mudfront**.
- **Next:** art direction pass (models read as "Roblox"); grey-box polish (marsh/mud colours, water channels, cover on long routes); multiplayer library (Fish-Net) not installed yet.
- **Open questions:** final faction names; servers; Muddogs green has weak contrast on grass (1.26:1).
