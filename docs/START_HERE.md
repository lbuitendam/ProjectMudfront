# AI job entry point

Use this folder as the shared job board for Claude, Codex and other coding models.

1. Read repository/session AGENTS instructions, then [HANDOFF.md](HANDOFF.md).
2. Read [DECISIONS.md](DECISIONS.md) (current rules) and [GDD.md](GDD.md).
3. Open [ECONOMY_TASKS.md](ECONOMY_TASKS.md) for executable jobs and acceptance checks. [ROADMAP.md](ROADMAP.md) groups the milestones; [TASKS.md](TASKS.md) links the wider backlog.
4. Resume an in-progress job when it belongs to this session, or claim the first unchecked job with completed dependencies. Letter-suffixed jobs appear in execution order alongside the original IDs. Do not assume numeric sort is execution order.
5. Set `[~]`, implement one job, verify, record evidence, update handoff, set `[x]` only when complete, and commit only task-owned files. Continue sequentially if the user authorized the whole queue.

## Current handoff

While this documentation update was being prepared, the implementing model completed the original ECON-02/03 and started ECON-04. Those completions used the earlier scope. Keep their historical evidence and statuses; complete the new **ECON-03R** migration gate and **ECON-03A** persistence task before accepting ECON-04. Preserve current networking work. Read live statuses before acting; this snapshot is not a task claim. Do not edit another live worker's files concurrently; coordinate via handoff before overlapping implementation.

The old plan's match resets, locked classes, teammate-only unlocking and aircraft deferral are superseded. Existing verification logs are evidence of the old baseline, not proof that the expanded scope is implemented.

## Implementation prompt

```text
Work in G:\dev\ProjectMudfront. Read docs/START_HERE.md and follow its linked instructions, especially docs/DECISIONS.md revision 2 and docs/ECONOMY_TASKS.md.

Work through the economy queue one job at a time in document/dependency order. Resume the current in-progress job if it belongs to this session; do not overwrite another active worker. Preserve existing local work and completed statuses. Complete ECON-03R and ECON-03A to reconcile earlier work with revision 2 before accepting ECON-04.

For each job: mark busy, implement the full scope, run acceptance checks and relevant regressions, fix failures, update verification and HANDOFF, mark done only with evidence, and commit only task-owned files. Then automatically continue to the next eligible job. Stop only for a genuine blocker requiring user action, or when the queue is complete. If interrupted, leave a precise handoff.

Use data-driven values, authenticated account identity, persistent authoritative money, and server-validated gameplay. A shop listing is not a working item. Offline mocks do not prove multiplayer correctness. Never silently substitute the old match-only/class-restricted design.
```
