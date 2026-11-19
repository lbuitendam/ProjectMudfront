# Handoff & AI rules – Project Mudfront

**Every AI reads START_HERE.md, this file, DECISIONS.md, GDD.md and TASKS.md before claiming a job.**

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
- **Economy scope revision 2:** Read `DECISIONS.md` and `START_HERE.md`. User approved persistent account cash (10,000 once), trader-claimed free gear, equipment-defined roles/unlocks, public vehicle unlock including enemies, configurable buyer-final-death public unlock, capped/time-limited support follow-ups, armour/grenades/attachments/large medical kit and transport/armed/attack helicopters with fuel/service/flares. Hot Zone, seasons and gold stay later. Revised tasks are in `ECONOMY_TASKS.md`; complete ECON-03R/03A migration gates before accepting ECON-04; old match-only/class-gated contracts are historical. No gameplay implementation was performed in this documentation revision.
- **Economy queue:** ECON-01 `[x]`, ECON-02 `[x]` (catalog data + validator; all products hidden until their behaviour is implemented; 36/36 PlayMode). Rebuild/validate catalog: `-executeMethod Mudfront.EditorTools.EconomyCatalogBuilder.BuildAndValidate`. ECON-03 `[x]` (PlayerRegistry + WalletLedger; 50/50 PlayMode). ECON-03R `[x]` (revision-2 migration: persistent account seed 10,000, no class gates, unlock/vehicle-policy/follow-up/aircraft data; 57/57 PlayMode). ECON-03A `[x]` (FileLedgerStore journal: fsync before ack, exclusive authority, crash refunds, two-process restart PASS; 67/67 PlayMode; production identity/DB provider undecided, so no cross-server wallet). ECON-04 `[x]` (Fish-Net 4.7.3; Mudfront.Net: account authenticator, EconomyServer on the same domain services, rate limiter; headless server + 4 clients harness PASS via `tools/net/run_econ04_harness.sh` after `NetHarnessBuilder.BuildPlayer`; 69/69 PlayMode). Current: ECON-05 (combat authority and attribution).
- **Next:** execute the economy queue one task at a time. Art direction and grey-box polish remain separate backlog work; multiplayer library (Fish-Net) not installed yet.
- **Open questions:** final faction names; servers; Muddogs green has weak contrast on grass (1.26:1).
