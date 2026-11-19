# Economy implementation queue for Claude

Updated 2026-11-19 after the user decision round. The original ECON-01–03 have been marked complete by the implementing model, and ECON-04 is in progress. Preserve that historical progress; revision-2 acceptance requires the new ECON-03R migration gate. See [DECISIONS.md](DECISIONS.md) for the current approved rules. Earlier baseline contracts are historical where they conflict.

## Claude execution instructions

Read START_HERE.md, DECISIONS.md, HANDOFF.md, GDD.md, TASKS.md, this file and [the research](reference/wardogs-economy-study.md). Execute the first unchecked ECON task whose dependencies are complete. Mark it `[~]`, implement only that task, run its acceptance checks and relevant regressions, update this file and HANDOFF.md, and commit a task handoff. When the user authorizes working through the queue, continue automatically to the next eligible task; otherwise stop after the requested task. Commit only task-owned changes as required by HANDOFF.md; never stage unrelated work. If a task needs multiple sessions, leave it `[~]` and record remaining work. Do not mark a task complete based only on a mock UI or simulated server result.

Every task is [CLAUDE/GPT]. Order is intentional. Aircraft and equipment unlock progression are now in scope through the added tasks. Do not add unrelated art or a new game mode. Letter-suffixed tasks are executed where placed below, after their stated dependencies; existing task IDs remain stable. Gameplay prices, amounts, ranges, timers, limits and multipliers belong in data assets. Names below are suggested types; adapt to existing architecture without creating duplicate systems. Create Unity `.meta` files through Unity and preserve asset references.

## Existing code and constraints

Paths below are relative to `Mudfront/Assets/_Mudfront/`:

| Existing code | Integration constraint |
|---|---|
| `Scripts/Runtime/Health.cs` | Damage receives only attacker team; `Died` has no attacker identity. `Revived` currently fires on full reset/respawn, so it cannot be treated as a paid teammate revive. |
| `Scripts/Runtime/Shooter.cs` | One hitscan rifle, automatic reload with unlimited reserve. Must connect equipped items, finite ammunition and authoritative damage attribution. Locally modified at planning time; preserve those edits. |
| `Scripts/Runtime/Respawner.cs` | Immediately schedules respawn on death. No downed/bleedout state or per-life kit lifecycle. |
| `Scripts/Runtime/Vehicle.cs` | One driver, unrestricted entry, automatic free respawn. No passengers, buyer identity or permissions. Purchased vehicles need a different lifecycle. |
| `Scripts/Runtime/ThirdPersonController.cs` | E directly enters/exits vehicles; controller owns gravity and current driver input. Centralize interactions and prevent UI input leakage. |
| `Scripts/Runtime/CaptureZone.cs`, `TeamMember.cs` | Zone scans static members and health. Downed players must stop counting as active participants. Preserve existing capture/contest rules. |
| `Scripts/Runtime/MatchManager.cs` | Team scores and winner only; no complete round reset or economy lifecycle. |
| `Scripts/Runtime/SliceHud.cs` | Immediate-mode debug HUD, no shop. |
| `Scripts/Editor/SliceSceneBuilder.cs`, `SaltmarshGreybox.cs` | Generated scenes must receive stations/pads through their builders, not only hand edits. |
| `Tests/PlayMode/SliceGameplayTests.cs`, `MovementFeelTests.cs` | Preserve movement, shooting, capture, respawn and legacy map-vehicle behavior, updating expectations only for intentional documented behavior. |

`Packages/manifest.json` has Input System, uGUI and test framework; Fish-Net is absent. Current gameplay is an offline prototype. An authority interface is useful but is not multiplayer validation.

## Proposed first-pass tuning

These are Mudfront playtest defaults, not WARDOGS values. ECON-02 creates editable data; ECON-25 tunes it.

| Setting | Initial value/rule |
|---|---|
| Account creation | 10,000 credits once per account; balance persists across matches, sessions, disconnects and server restarts; no recurring match/death grant |
| Free kit | Claim starter rifle + 60 reserve rounds from a trader once per life; no automatic paid kit, stacking, selling or repeat claims |
| Purchases | Rifle 250; sidearm 100; 30 reserve rounds 30; bandage 50; parachute 150; repair tool 150; transport jeep 600 |
| Kill / damage assist | 150 / up to 75 proportional to effective enemy damage in last 15 seconds; once per victim life |
| Revive / healing | 150 per completed eligible revive; 1 per actual teammate HP restored, up to 100 per recipient life |
| Objective | 20 per 10 seconds of living, on-foot participation in the active zone; eligible action multiplier 1.5, applied once |
| Passenger trip | 100 per distinct delivered teammate after at least 75 m net displacement and 10 seconds onboard; origin outside zone, exit alive inside zone |
| Repair | 1 per actual eligible HP restored, up to 150 per asset life |
| Spot assist | 50 per marked enemy killed by another teammate inside a 15-second mark window |
| Construction / resupply | 50 per completed useful buildable; 1 per accepted round supplied, up to 60 per recipient life |
| Asset destruction | 200 per enemy vehicle; 50 per enemy completed buildable, scaled by construction completion |

Use integer credits and deterministic rounding after a single zone multiplier. Apply per-event caps after the multiplier; health/asset-life budgets are final credited totals. Healing/repair/resupply caps are shared across providers for each recipient/asset life. Specify any additional caps and overflow limits in the data schema. Counts stay separate from match points. No reward payout outside an active round.

## Ordered tasks

### [x] ECON-01 — Baseline and implementation contract
Depends on: none.

- Re-read current source and local instructions; capture existing modified files, installed Unity version and actual tests. Identify stable scene/prefab creation paths and record baseline results in `docs/verification/economy_report.md`.
- Document authority commands/events, player identity, item fulfillment and lifecycle boundaries. Original baseline used match-only money; this has been superseded by DECISIONS.md. ECON-02 and ECON-03A reconcile the contract without rerunning completed baseline work.
- Acceptance: report lists real baseline outcomes and any blockers; no gameplay changed or user changes overwritten. Do not infer that old reports still pass.

### [x] ECON-02 — Data assets and catalog validation
Depends on: 01.

- Add item, reward, station and vehicle definitions plus economy configuration. Include stable IDs, category, price, unlock requirements, equipment compatibility, stacks/slots, behavior/prefab reference and gameplay parameters.
- Reconcile current in-progress data against DECISIONS.md: remove mandatory class gates and match-reset grants; support persistent account money, unlock tracks, WorldUnlocked vehicle access, configurable owner-death policy, support reward windows/caps, armour, grenades, attachments and aircraft. Preserve unrelated work. Update the baseline implementation contract with a dated amendment.
- Supply the trader-claimed free kit and required purchases from the tuning table. Add validation for duplicate IDs, negative/overflowing values, missing behavior/prefabs and incompatible slots. All aircraft prices, unlock fees/thresholds, fuel/ammo/flare costs and armour/grenade/attachment parameters must have explicit editable defaults before those products are enabled. Unimplemented products stay hidden/unavailable.
- Acceptance: invalid assets fail clear validation; editing prices/rewards needs no source changes; the catalog has no purchasable placeholder behavior.

### [x] ECON-03 — Identity, wallet, ledger and action statistics
Depends on: 02.

- Introduce stable player/match/life IDs and an authoritative wallet service usable by an offline authority. Never use TeamId, display name or scene instance ID as player identity.
- Add checked integer credit/debit, transaction IDs, reward event IDs, reason codes, balances, earned/spent totals and per-action statistics. Deduplicate retries and retain records until the relevant match/life cannot replay.
- Grant 10,000 once at account creation. Keep account balance and durable idempotency records across matches; reset match/life statistics only. Bind reconnects to account identity; never trust an arbitrary client-supplied identity.
- Acceptance: new match entry never resets the wallet or repeats the initial grant. Duplicate grants/rewards/debits, insufficient funds, negative amounts and overflow cannot change money incorrectly. Two players on the same team have separate wallets. Statistics and money reconcile.

### [x] ECON-03R — Reconcile completed data/wallet work with revision 2
Depends on: 03.

- Review the implementation and evidence from completed ECON-02/03; their original match-only/class-gated acceptance does not prove revision 2. Keep their completed historical status and migrate their code/data rather than duplicating systems.
- Replace per-match wallet resets/seed grants with persistent-account identity and a one-time 10,000 seed. Remove mandatory class gates, add data for unlock prerequisites, WorldUnlocked access, owner-final-death policy, follow-up budgets and all approved equipment/aircraft categories. Keep unimplemented products hidden.
- Amend outdated implementation contracts and tests; preserve unrelated gameplay and another worker's in-progress Fish-Net work. Coordinate sequentially before editing overlapping files.
- Acceptance: updated data/ledger tests prove a new match preserves balance, seed grants cannot repeat, all specialist categories can be represented without class restrictions, and legacy behavior is explicitly superseded. Do not advance ECON-04 acceptance until this task and ECON-03A pass.

### [x] ECON-03A — Durable account wallet and recovery
Depends on: 03R.

- Implement an account repository with atomic durable transactions, account/ledger schema versions, unique idempotency keys and one-time seed grant. Bind PlayerId to persistent AccountId; retain MatchId/LifeId for activity validation.
- Define the canonical authority for shared balances. Local development can use a server-side database, but a client-controlled save is never online authority. Document backup/recovery and production identity/provider choice before exposing public cross-server persistence.
- Commit debit/delivery intent and reward receipt durably before acknowledging success. Recover interrupted transactions deterministically, refund or finish pending delivery exactly once, and handle storage failure without inventing spendable cash.
- Acceptance: earn/spend, leave immediately, restart server, reconnect and join a new match: committed balance persists. Duplicate account creation never re-seeds. Crash/retry around commit and simultaneous sessions cannot double-spend; old-match actions remain invalid even though the wallet persists.

### [~] ECON-04 — Fish-Net foundation and authority boundary
Depends on: 03A.

- Install and pin Fish-Net following its current official documentation. Record version and setup. Build a minimal dedicated-server + clients scene with connection-bound identity, team assignment and server-owned match/player state.
- Establish command/result replication for wallet snapshots, transaction results and action notifications. Sender identity comes from the authenticated connection. Clients request actions; they never submit trusted prices, balance changes, reward amounts or arbitrary owners.
- Create reusable server validation/rate limiting and a real multi-process test harness. Support offline play through the same domain services.
- Acceptance: dedicated server + two clients show isolated wallets; forged identity and direct reward requests are rejected. Headless startup works. Mark network-dependent tasks blocked if this cannot run; local service tests do not satisfy this gate.

### [ ] ECON-05 — Combat authority and attribution
Depends on: 04.

- Add authoritative damage context: attacker ID/team/life, victim ID/life, source and effective damage. Keep environment damage distinct and avoid attributing vehicles/buildables as infantry kills.
- Connect shooter requests to server checks for equipped weapon, ammo, fire cadence, living state and plausible shot origin. Server resolves hits against authoritative player positions; add the minimum server-validated movement needed for reliable range/hit checks.
- Maintain a bounded contribution window and emit one confirmed terminal kill per life. Define disconnect, suicide, friendly-fire and simultaneous-hit outcomes.
- Acceptance: two clients agree on damage; client damage/position spoofing cannot mint rewards. Repeated fatal hits count once; friendly/environment damage pays no enemy-kill reward. Regression: close-cover shooting still blocks shots.

### [ ] ECON-06 — Inventory, equipment and purchase fulfillment contract
Depends on: 05.

- Add authoritative per-life inventory, unlock-validated compatible equipment slots, stack limits, finite reserve ammo and item use/cancel commands. Replicate owner inventory and visible equipped state appropriately.
- Define validate/reserve/fulfill/commit/rollback hooks shared by equipment and vehicle purchases. Use immutable item IDs; do not trust a client inventory snapshot.
- Acceptance: full/incompatible inventory fails without changing money; equips persist correctly across a reconnect in the same life; duplicated use commands consume once. No paid item is lost on fulfillment failure.

### [ ] ECON-06A — Equipment-defined roles and persistent unlocks
Depends on: 06, 03A.

- Replace mandatory selected-class gates with configurable role XP tracks, equipment prerequisites and compatible inventory slots. Any account can unlock medical/building/pilot equipment; using one role does not prohibit another.
- Add authoritative eligible action XP, data-defined thresholds, one-time unlock fees and persistent unlock records. Basic rifle/ammo/bandages and slow revive remain accessible from the start. Large medical kits, advanced tools and aircraft have explicit editable unlock requirements.
- Expose unlock state, requirement and separate unlock price versus per-life purchase price. Denied unlocks/purchases never charge. Existing class concepts may remain presentation tags, not hidden access gates.
- Acceptance: one player unlocks and uses both construction and medical equipment without class selection. XP and unlocks survive restart/new match, repeated unlock requests charge once, and the server rejects fabricated XP and unmet prerequisites.

### [ ] ECON-07 — Bought weapons and ammunition work
Depends on: 06A.

- Connect rifle/sidearm data and equipment to actual firing, damage, cadence, magazine size, reload and HUD. Preserve existing aim/cover fixes.
- Replace unlimited reserve reloads with real inventory ammunition. Define partial reload behavior and switching restrictions; purchases cannot silently overwrite another paid item.
- Acceptance: purchase via the fulfillment service, equip each weapon and damage a target on the server. Shoot/reload to exhaustion; buying ammo restores only the purchased amount. Repeated equips/reloads do not duplicate ammo.

### [ ] ECON-07A — Working weapon attachments
Depends on: 07.

- Implement attachment slots and compatibility data; provide at least a sight, recoil-affecting grip and suppressor with actual aiming/handling/audio effects. Paid attachments belong to an item instance and are lost on final death.
- Validate installation at an authorized trader; incompatible installs fail without debit or replacement loss. Store effective stats authoritatively and show the effect before buying.
- Acceptance: purchase/install each attachment, aim/fire and observe its defined effect across clients; switching/reconnecting cannot stack modifiers. Unsupported combinations cannot be bought.

### [ ] ECON-07B — Armour with real damage protection
Depends on: 07A.

- Implement purchasable helmet/body armour, covered hit regions, mitigation and durability using data. Handle damage on the server and display remaining protection. Explicitly define whether replacing an equipped piece requires a free slot; never silently discard paid armour.
- Acceptance: identical validated hits demonstrate unarmoured versus protected damage, uncovered hits bypass protection, depleted armour stops protecting, and a replayed hit never consumes durability twice. Armour is retained on revive and lost on final death.

### [ ] ECON-07C — Fragmentation and smoke grenades
Depends on: 07B.

- Implement purchased throwable inventory, equip/throw/fuse/cancel behavior, authoritative spawning and bounded area damage with cover checks. Provide frag and smoke; smoke must obscure vision consistently for clients rather than merely add a shop icon.
- Integrate explosion damage attribution and self/friendly damage provenance so resulting heals/revives cannot farm cash. Parameters and stock limits live in data.
- Acceptance: one throw consumes one grenade; retry produces one projectile; blast range/cover work. Smoke duration/coverage match across clients, late join is correct, and death/menu input cannot throw phantom grenades.

### [ ] ECON-08 — Downed, revive, final death and kit lifecycle
Depends on: 07C.

- Separate Alive, Downed and Dead states; add bleedout, give-up and teammate hold-to-revive. Everyone can perform a slow basic revive; purchased medical tools improve speed through data-defined behavior, not a selected class.
- Server validates range, line of sight, team, reviver state, target state and uninterrupted duration. Downed players cannot fire, shop, drive or score zone presence. Respawn begins only after final death.
- Successful revive retains inventory; final death clears the paid kit and opens a new life with one free-kit entitlement. Preserve wallet; cancel obsolete timers and transactions.
- Acceptance: two competing revivers produce one revive; respawn reset never pays as revive. Cancellation, bleedout, disconnect and death-in-vehicle clean up safely. Match end cancels pending activity.

### [ ] ECON-09 — Bandage use and effective healing
Depends on: 08.

- Implement purchased bandages on self and injured teammates: equip, target, use duration, cancellation, actual HP restoration, stack consumption and feedback. Normal bandages cannot resurrect.
- Emit effective teammate-heal events with damage provenance; self-heal works but pays nothing. Reward eligibility excludes damage caused by the healer/recipient or friendly fire.
- Acceptance: injured target heals by the configured amount, limited by missing HP; exactly one bandage consumed on success. Full-health/invalid/interrupted uses produce no heal reward or accidental consumption.

### [ ] ECON-09A — Large medical kit and specialist revive tools
Depends on: 09, 06A.

- Add an unlockable large medical kit with meaningful extra healing capacity and an equipment-based faster revive tool. Data defines charges, restoration, durations and compatibility; any eligible account can buy/use them.
- Reuse the existing validated heal/revive service and reward provenance. Tool use consumes actual stock; no separate unlimited medical-class healing route.
- Acceptance: trader purchase equips the correct tool; injured teammates recover real HP and valid revives complete faster than the baseline. Full-health, cancelled, duplicate and invalid uses cannot waste/duplicate stock or mint cash.

### [ ] ECON-10 — Parachute and fall behavior
Depends on: 08.

- Add a traversal slot and purchased parachute behavior integrated with controller gravity, descent, steering and server movement validation. Add configurable fall damage so equipped and unequipped falls have observable outcomes.
- Default: one charge per life, consumed on successful deployment; allow cancel/cut and landing cleanup, no deployment on ground or inside a vehicle. Include a safe height test platform reachable without aircraft.
- Acceptance: equipped player can deploy, descend slowly, steer and land safely from the test height; unequipped player cannot deploy and takes configured fall damage. Death/landing/reset clears state on all clients.

### [ ] ECON-11 — Combat, support and objective payouts
Depends on: 09A, 10.

- Route confirmed kills, effective damage assists, completed revives, effective heals and zone participation through the reward policy into ledger/statistics.
- Snapshot event location/time on the server; use victim location for combat, recipient location for support, and current player position for objective ticks. Apply the zone multiplier once. Require living on-foot objective participation; no base/empty-zone owner income.
- Prevent damage/heal loops and repeated revive farming: configure a recipient-life revive payout cooldown and budget; exclude downs caused by friendly/self damage. Real actions still appear in statistics when a cap suppresses cash.
- Acceptance: deterministic payout tests cover damage shares, duplicate events, zone boundaries, caps, dead/downed players, inactive rounds and fabricated events. Revive and healing can fund a cheap support kit.

### [ ] ECON-12 — Base stations and shared interaction routing
Depends on: 11.

- Create reusable equipment-station and delivery-pad prefabs for all three faction HQs. Introduce a shared interaction selector so E can choose a station, teammate or vehicle without multiple simultaneous actions.
- Server checks station existence/activity, team, distance, line of sight and living state on open and purchase. A remembered open menu is not authorization. FOB station support uses the same component when FOBs arrive.
- Acceptance: each HQ has a reachable friendly station and marked pad; enemy/out-of-range/destroyed stations fail. Regenerating both prototype scenes keeps stations. Nearby vehicle and station interaction is unambiguous.

### [ ] ECON-13 — Atomic purchase service
Depends on: 12.

- Connect requests containing station ID, item ID, quantity and request ID to server catalog, eligibility, wallet and fulfillment. Reserve capacity and funds, fulfill once, then commit; rollback all changes on failure.
- Return explicit result codes and authoritative balance/inventory. Replay returns the previous result. Reject reused IDs with different payloads, spam, malformed quantities and purchases from an expired life/match.
- Acceptance: simultaneous purchases never overspend; duplicated/delayed requests create one debit and one delivery. Disconnect/death during fulfillment leaves no orphan grants or missing credits. Failure injection proves rollback.

### [ ] ECON-14 — Buy menu, inventory feedback and rebuy
Depends on: 13.

- Build an actual runtime menu using installed Unity UI tooling: Weapons, Ammo, Medical, Armour, Attachments, Utility and Vehicles; role/unlock progress and equipment compatibility, prices, balance, quantity, inventory occupancy, selected item behavior and purchase result. Include the free kit.
- Unsupported products remain unavailable. Retain the previous life loadout as a purchase list, not free inventory; rebuy revalidates current eligibility and affordability. Use an atomic equipment basket; vehicles require a separate explicit order.
- Block firing, aiming, movement, vehicle controls and world interactions while the menu owns input; restore cursor/input on close, death, range loss and disconnect. Do not pause the server.
- Acceptance: mouse/keyboard navigation at 1280x720 and 1920x1080, affordable/unaffordable states and repeated clicks work. Clicking Buy never fires a bullet. Rebuy charges current prices and cannot duplicate free claims.

### [ ] ECON-15 — Passenger seats and networked ground transport
Depends on: 14.

- Extend Vehicle/controller into explicit driver/passenger seats with authoritative occupancy, entry range, safe exit placement and driver-only input. Synchronize transform, seats and occupants with late joins.
- Keep the existing map-vehicle respawn policy available as a separate data setting. Provide a jeep prefab with at least one driver and two passenger seats; primitive art is acceptable.
- Acceptance: three clients ride together; simultaneous seat requests have one winner; passengers cannot steer. Death/destruction/disconnect eject or clear occupants safely; no exit inside a wall. Legacy vehicle tests remain meaningful.

### [ ] ECON-16 — Purchased vehicle delivery and lifecycle
Depends on: 15.

- Implement the vehicle fulfillment provider: reserve a valid pad and per-player/team vehicle quota, collision-check the footprint, create one server-spawned vehicle, attach buyer/team identity and commit payment only on successful delivery.
- Blocked pad, failed spawn or exhausted quota causes no charge/spawn. Purchased vehicles do not automatically respawn for free after destruction. Default quota: one live purchased vehicle per player; destruction frees it. All limits/timers are data.
- Acceptance: concurrent orders cannot overlap a pad; clear delivery marker identifies the buyer's vehicle. Destruction requires a new paid purchase. Scene reset/despawn removes ownership/quota reservations correctly.

### [ ] ECON-17 — Owner locks and unlock controls
Depends on: 16.

- Spawn purchased vehicles as OwnerOnly before any client can enter. Buyer can change to WorldUnlocked or relock through a nearby interaction; authenticate the sender and validate range/state on the server. WorldUnlocked permits teammates AND enemies; it does not transfer ownership or lock-control rights.
- Enforce access on every seat entry/switch and driver takeover. Locked mode denies everyone except the buyer; WorldUnlocked admits everyone. Relocking keeps current occupants until voluntary exit; no new unauthorized entry. Non-owner occupants cannot change permissions.
- On buyer FINAL death (not downing), leave an intact vehicle in-world and default its access to WorldUnlocked. Store ownership for audit/quota and later owner actions; make this a data policy selectable between KeepOwnershipAndLock and UnlockForEveryone. No refund or free replacement; test both policies.
- On buyer disconnect: clear their seat/input immediately, retain ownership/lock during a configurable 60-second reconnect grace, then despawn without refund; occupants get safe exits. Cancel cleanup if the authenticated buyer returns. No ownership theft via a new connection ID.
- Acceptance: owner enters a freshly spawned locked vehicle; teammate is denied until unlock; enemy enters only in WorldUnlocked; forged owner requests are always denied. Buyer final death defaults to public access and the alternative policy preserves locks. Relock, reconnect, late join and disconnect grace replicate consistently. UI shows owner and access state.

### [ ] ECON-18 — Qualified transport income
Depends on: 17.

- Track authoritative passenger trip sessions, driver contribution, origin, time, net displacement and exit/safe-delivery position. Use distinct passenger life IDs, not seat entries. No self, enemy, dead or driver-as-passenger rewards.
- Award once on an alive exit in the active zone after thresholds in the table; use delivery location for the zone multiplier. Driver switches end the qualifying segment and start a new one; no shared/double claim. Repeated loops use a configurable driver/passenger-life cooldown and reward cap.
- Acceptance: real base-to-zone ride pays driver once per eligible passenger; hopping seats, circling in place, starting in the zone, short rides, crashes, disconnects and passenger death pay nothing. Statistics show completed valid deliveries.

### [ ] ECON-18A — Capped follow-up rewards for revivers and drivers
Depends on: 18.

- Record server-validated support links only after an eligible revive/delivery. Teammate's later confirmed enemy kills can pay the original provider until the configured window expires or recipient final death, match end or cap ends eligibility.
- Implement individual and shared recipient-life budgets from DECISIONS.md; define deterministic splitting when several providers qualify. Revives/seat hopping cannot reset the recipient-life budget or stack duplicate links. No chains of bonuses from bonus events.
- Persist resulting wallet credits atomically, with source kill/support IDs. Separate base kill reward, spot assist and support bonus reasons in ledger/HUD.
- Acceptance: eligible later kills pay provider without reducing the killer's normal reward; expiry/caps/final death stop payment. Multiple providers stay within the shared budget. Retry/reconnect cannot duplicate a bonus or refresh its window.

### [ ] ECON-19 — Repair tool, vehicle damage and destruction rewards
Depends on: 18A.

- Implement purchased repair tool with charge inventory, held use, range, cancellation and actual HP restoration on friendly vehicles. Repair never resurrects a wreck. Reward only eligible damage from hostile/environment sources under the configured provenance rule.
- Pay destruction rewards once per enemy vehicle/asset life with authoritative attacker attribution. Keep infantry kills separate from asset destruction; reward passengers killed only if the combat system confirms their deaths.
- Acceptance: bought tool restores real HP and consumes charges; full-health/friendly damage repair loops earn nothing. Wrecks never heal back into free purchased vehicles. Duplicate destruction callbacks pay once.

### [ ] ECON-19A — Transport helicopter flight and delivery
Depends on: 19, 10, 06A.

- Add a usable server-spawned transport helicopter with pilot and passenger seats, lift, yaw, pitch/roll, landing controls, rotor collision envelope, health and validated flight input. Use original placeholder art as needed.
- Extend purchase delivery to aircraft-safe pads with full clearance/quota checks; apply the same persistent purchase, owner locks, public unlock and buyer-death policies as ground vehicles. Ensure the map/test area has room for safe flight without silently rebuilding the game map.
- Acceptance: purchase, enter, take off, fly a route, land, exit and re-enter in actual multiplayer. Passenger helicopter exits can deploy a purchased parachute; cockpit/rotor and spawn collisions do not strand players. Failure to deliver never charges.

### [ ] ECON-19B — Aircraft fuel, damage, repair and refuelling
Depends on: 19A.

- Add server-owned fuel capacity/consumption, engine state, fuel exhaustion, collision/weapon damage and repair integration. Refuelling at authorized HQ/FOB facilities transfers paid finite fuel; configure prices/rates and require a valid landing/service state.
- Show fuel/health and warnings. A wreck cannot be repaired/refuelled into a free replacement. Protect repair provenance and economic stock accounting against farming.
- Acceptance: fuel drains while operating, empty fuel affects flight, damage affects the defined flight capabilities, purchased repairs restore real eligible HP, and refuelling debits once and restores exactly the bought fuel. Interrupted/full-tank/remote requests cause no incorrect charge.

### [ ] ECON-19C — Armed transport and door gunner ammunition
Depends on: 19B, 07A.

- Provide an armed transport variant with explicit gunner seats, bounded aim arcs, rate/heat or reload behavior, finite ammunition and server-resolved damage. Passenger/pilot input never controls an unauthorized gun.
- Vehicle ammo purchases at an authorized service station replenish actual stocks. Apply access validation to seat switches, including enemy occupants after public unlock, without transferring buyer control.
- Acceptance: a pilot flies while a second player fires a door gun at a valid target. Ammo exhausts, paid resupply restores only accepted stock, aim arcs/line of fire respect the airframe, and seat/input spoofing is rejected.

### [ ] ECON-19D — Attack helicopter with front gunner and rockets
Depends on: 19C.

- Implement a separately purchasable attack helicopter with pilot/front-gunner stations, working front weapon and finite rocket inventory. Define weapon control ownership and sight/aim limits in data; rockets have authoritative impact/explosion and attribution.
- Share aircraft delivery, fuel, repair, access and lifecycle services; no independent free-ammo or client-damage path. Document rocket arming distance and explosion cover behavior.
- Acceptance: two players fly and operate the front weapon/rockets; direct hits and splash affect real targets, ammunition is consumed once, and exhausted weapons require paid resupply. Occupied/blocked pads and unaffordable orders produce no delivery/debit.

### [ ] ECON-19E — Flares and functional heat-seeking threats
Depends on: 19D.

- Add finite aircraft flare charges, cooldown, authorized crew control, resupply cost and replicated effects. Implement a minimal server-owned heat-seeking missile/target-lock test threat so flare effectiveness is measurable; do not claim protection from unguided rockets.
- Configure lock acquisition, warning, line of sight, flare decoy interval/chance and missile retarget rules. Baseline should be deterministic/testable; no decorative-only countermeasure.
- Acceptance: threat locks/tracks an aircraft without flares; valid flare deployment changes targeting according to data. Empty charges/cooldown/spoofed crew commands do nothing, paid resupply restores charges once, and all clients see the same outcome.

### [ ] ECON-19F — Aircraft economy, crew attribution and lifecycle verification
Depends on: 19E, 18A.

- Extend qualified transport and follow-up rewards to pilot delivery, including safe parachute landings (award on confirmed alive landing in the zone, not just exit in midair). Stop trip credit on occupant death, enemy travel or unqualified flight.
- Attribute gunner kills to the actual operator; configure any pilot contribution share within a single capped crew budget. Apply asset destruction rewards once per aircraft life and infantry rewards only to confirmed occupants' deaths.
- Acceptance: ground and air transport share anti-farm rules; aircraft destruction, buyer death/public unlock, disconnect grace, late join, seat switching and round transitions preserve locks, stocks and persistent cash correctly. Include pilot/gunner/enemy scenarios in ECON-24.

### [ ] ECON-20 — Spotting and spot assists
Depends on: 19F.

- Add a functional binocular/recon item with configurable cost, range, line-of-sight validation, mark duration and teammate-visible markers. Respect unlock and compatibility rules; no selected class gate. No payout for repeatedly marking the same unchanged target.
- Pay one spotting assist per eligible enemy life when a different teammate confirms a kill within the active mark window; define multiple spotters using one configured shared assist budget.
- Acceptance: actual enemy is visible/marked to teammates; through-wall, self/friendly and expired marks fail. Kill with a valid mark produces the bounded assist, without turning a client marker into trusted reward evidence.

### [ ] ECON-21 — Construction, resupply and FOB shop foundation
Depends on: 20.

- Implement the minimal real buildable lifecycle needed for economy: placement gated by owned/unlocked building tools, not a selected Engineer class, authoritative cost/limits/collision checks, construction progress, damage, repair and destruction. Start with sandbags, ammo crate and FOB beacon; leave other art/content to their existing tasks.
- Construction income pays completed useful work under per-builder/match budgets; never exceed that object's nonrefundable construction spend. No dismantle refund loop. Resupply transfers finite purchased stock into a teammate's actual ammo deficit and pays once for accepted eligible stock, with recipient-life caps.
- FOB beacon enables a friendly respawn/station only while completed and alive; no vehicle category without a valid pad. Spawn at FOB still follows paid-kit/free-kit rules. Destroyed stations close menus and reject pending purchases.
- Acceptance: build -> complete -> buy/resupply -> damage/repair -> destroy works across clients. Ghosts, self-resupply, refill loops, friendly damage and repeated construction callbacks cannot print money. Enemy asset destruction pays in proportion to completed construction.

### [ ] ECON-22 — Economy HUD, scoreboard and player guidance
Depends on: 21.

- Show wallet, life/match earned/spent/net, concise reward notifications and counts for combat, revive/heal, transport, objective, repairs, spotting, construction/resupply and asset destruction. Separate credits from team match points.
- Show equipped ammo/consumables, bandage/parachute use controls, station location, purchased vehicle marker/lock and denial reasons. Coalesce frequent heal/repair ticks without hiding ledger entries.
- Acceptance: a support-only player can see why they earned cash; scoreboard includes all players with server-confirmed statistics. No duplicate notification on reconnect or transaction retry. Check readable UI at the two target resolutions.

### [ ] ECON-23 — Round transitions, reconnect and cleanup
Depends on: 22.

- Add complete round start/end/reset integration: freeze spending/rewards at end, cancel uses, trips, revives and pending deliveries, show summary, clear match vehicles/inventories/contributions and start new match/life IDs once. Preserve durable account cash and unlock progression.
- Restore persistent account wallet and progression on authenticated reconnect. Inventory/vehicle reconnect state is current-match only. Define deterministic outcomes for disconnect during purchase, revive and trip; a new match never grants starting cash again.
- Acceptance: play two consecutive rounds with late join and reconnect; no old event or request can award/debit the new round. No leaked locks/seats/quotas, extra starting funds or retained paid kit after final death.

### [ ] ECON-24 — Multiplayer abuse and regression verification
Depends on: 23, 19F.

- Run EditMode policy/transaction tests, relevant PlayMode regressions and real dedicated-server scenarios with at least three clients (buyer, teammate, enemy), plus a second teammate where passenger capacity needs it.
- Exercise spoofed rewards/owners/prices, remote shopping, input spam, repeat IDs, packet delay/retry, insufficient funds, blocked pads, failed delivery, owner death/disconnect, late join and round reset.
- Report commands, versions, process topology, actual outcomes, test logs and screenshots in `docs/verification/economy_report.md`. Validate runtime prefabs/scenes and headless build; static source review alone is insufficient.
- Acceptance: all required mechanics have positive and negative cases with real results. Any unavailable multiplayer or Unity checks remain explicitly pending, and this task stays unchecked.

### [ ] ECON-25 — Balance pass and final Claude handoff
Depends on: 24.

- Play measured combat, medical, driver, builder and pilot sessions. Record income/spend per minute, lives funded, reward caps hit and vehicle losses. Tune only editable data; support roles must remain useful and bankruptcy must remain recoverable with the free kit.
- Walk the full acceptance scenario below; update GDD/HANDOFF/report and synchronize only completed overlapping tasks in TASKS.md. Record known issues and remaining expansion work.
- Acceptance: the scenario passes in actual multiplayer, station/menu controls are documented, no supported purchasable item lacks behavior, and the report separates executed checks from pending ones.

## Final playable acceptance scenario

1. Start a round with three factions. At every HQ, a friendly player can reach a station and see their own balance. An enemy cannot shop there.
2. Buy a rifle and ammo, equip, shoot, reload and exhaust ammunition. Buy and use bandages; buy a parachute and safely descend from the test platform. The purchases change the actual game state.
3. Buy/equip armour, attachments, frag/smoke grenades and large medical tools; verify protection, weapon effects, explosion/smoke and actual healing. Unlock gear with legitimate role XP and verify it survives a restart. Damage enemies and earn kill/assist credit; revive/heal teammates and earn support income. Observe correct counts, wallet, ledger and objective bonus. A revive keeps the kit; a final death removes it without resetting cash.
4. Buy a jeep. Exactly one charge and one delivered vehicle result. It starts locked; the buyer enters, a teammate is denied, and only the buyer can unlock it. After unlock, teammates AND enemies can ride; neither can change lock settings. Buyer final death leaves the intact vehicle publicly accessible under the default configurable policy.
5. Transport distinct teammates from base into the zone; meaningful completed rides pay once. Repair hostile damage with a purchased tool and confirm real restoration plus eligible income.
6. Purchase and fly transport, door-gun and attack helicopters; use fuel, refuel/repair, fire guns/rockets, use flares against a real tracking threat, and deliver passengers safely including parachute landings. Verify capped follow-up rewards after a delivered/revived teammate gets kills. Spot an enemy for a teammate's kill, build and supply a real FOB, and purchase at its friendly station. Destroy enemy assets and verify bounded payouts.
7. Repeat failed/duplicate/spoofed purchases and rewards, destroy the purchased jeep, reconnect the owner, then start another round. Money, kits, locks, statistics and match resets remain correct while cash and unlocks persist. Restart the authority and verify committed balances still exist.

## Copy/paste starting prompt for Claude

> Read `docs/START_HERE.md` and follow its implementation prompt. Resume the in-progress task before claiming another. Read DECISIONS.md revision 2 and complete ECON-03R/03A before accepting ECON-04: the original match-only/class-restricted plan is superseded. Execute tasks sequentially, preserve local work, verify acceptance, update status and handoff, commit task-owned changes and continue to the next eligible task when authorized. Do not claim offline checks prove multiplayer behavior.
