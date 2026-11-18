# Handoff & AI rules â€“ Project Mudfront

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
- **Phase:** 0 â€“ Setup
- **Done:** palette (`art/palette/`), Blender style helpers + 5 test-model script, gitignore/LFS, design decisions, GDD, roadmap, tasks, reference notes
- **Next:** [YOU] finish installing Unity 6.3 LTS + Blender, create URP project in `Mudfront\`. Then run `blender -b -P tools/blender/build_test_models.py` (scripts written, not yet run/tested).
- **Open questions:** final faction names; servers (dedicated vs player-hosted)
