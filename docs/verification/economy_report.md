# Economy verification report

Running log of actual results for the ECONOMY_TASKS.md queue. Only outcomes actually observed are recorded here.

## Scope amendment — 2026-11-19, revision 2

The ECON-01 results below remain historical evidence; they were not rerun by the documentation revision. The original implementation contract's match-only persistence, class gates and match-scoped identity are superseded by `docs/DECISIONS.md`. Use persistent AccountId, a one-time 10,000 seed, durable authoritative transactions/unlocks, and match/life IDs only for activity validation. Unlocked vehicles admit enemies; final buyer death defaults intact vehicles to public access. Added jobs cover support bonuses and equipment/aircraft expansion. Original ECON-02/03 were completed concurrently under the old contract; ECON-03R now migrates those data/ledger rules and ECON-03A verifies durable persistence before ECON-04 acceptance. No new gameplay or test pass is claimed by this amendment.

## ECON-01 — Baseline and implementation contract (2026-11-19)

### Environment
- Unity editor: 6000.3.28f1 (8f082d2fe476), at `C:\Program Files\Unity\Hub\Editor\6000.3.28f1`.
- Packages: Input System 1.20.1, uGUI 2.0.0, Test Framework 1.6.0, URP 17.3.0, AI Navigation 2.0.16, Multiplayer Center 1.0.1. **Fish-Net is not installed.**
- `AGENTS.md` (referenced by the session prompt) **does not exist** in the repo. HANDOFF.md §1 serves as the agent rules.

### Pre-existing local changes (preserved, not owned by the economy work)
- `Mudfront/Assets/_Mudfront/Scripts/Runtime/Shooter.cs` (modified): muzzle origin, barrel obstruction raycast, `ShotFired`/`ReloadStarted` events, `Shots` counter, no firing while in a vehicle, URP unlit tracer.
- Untracked: `.vs/`, `MudfrontAssetLibrary/`, `SeasideSkirmish/`, `Tests/PlayMode/MovementFeelTests.cs.meta`, `art/exports/SM_House_Townhouse_A.blend`, `art/exports/_townhouse_a.png`, `tools/blender/townhouse_a.py`.
- None of these are staged or committed by economy tasks.

### Tests present
- `Tests/PlayMode/SliceGameplayTests.cs`: 11 tests (incl. door clearance x2, team tint) (capture single/contested/outside radius, shooting enemy vs teammate, respawn, vehicle enter/drive/exit, vehicle destroy/respawn, run speed).
- `Tests/PlayMode/MovementFeelTests.cs`: 10 tests (accel, decel, sprint rules, crouch, headroom, jump height, coyote, buffered jump, aim zoom/1st person, camera wall pull-in). Commit e5a4684 says these were "not yet run".
- No EditMode test assembly.

### Baseline run
First attempt (2026-11-19 16:41) exited 198: "No valid Unity Editor license found" (Hub signed out). The user signed in.

Rerun 2026-11-19 16:57, same command: **21/21 PlayMode passed**, 0 failed, 20 s. This includes the 10 movement-feel tests and the local Shooter.cs edits. This is the economy baseline.

### Stable creation paths
- Scenes come from editor builders: `SliceSceneBuilder.BuildAndValidate` → `Scenes/Slice_Test.unity`, `SaltmarshGreybox.BuildAndValidate` → `Scenes/Map01_Saltmarsh_Greybox.unity`. Shop stations and vehicle pads must be added in these builders, not by hand edits.
- No prefabs exist yet. Players/vehicles are assembled in code by builders and tests.

## Implementation contract

### Persistence
Money is **match-only**. One entry grant per player per match. The balance survives death and reconnect inside that match. It resets exactly once when a new match starts. There is no bank, no premium currency and no progression.

### Identity
- `PlayerId`: server-assigned and stable for the match. Offline, the local authority assigns it. Online, it is bound to the authenticated connection, and reconnect maps back through a server-issued session token. TeamId, display name and instance IDs are never identity.
- `MatchId`: new per match. `LifeId`: new on each spawn after final death. A revive keeps the same LifeId. `AssetId`: per spawned vehicle or buildable life.

### Authority
- One domain layer: `EconomyAuthority` (wallet and ledger), `InventoryAuthority`, `PurchaseService`, `RewardService`. Offline it is called directly. Online it is called only from server RPC handlers, so both share the same code.
- Clients send **requests** only: `RequestPurchase(itemId, stationId)`, `RequestUseItem(slot, target)`, `RequestRevive(targetPlayerId)`, `RequestSetVehicleLock(assetId, mode)`, `RequestFire(origin, dir, weaponSlot)`. Clients never send prices, amounts, owners or reward claims.
- The server emits: `WalletSnapshot(balance, earned, spent)`, `TransactionResult(txId, ok, reason)`, `RewardNotice(reason, amount, eventId)`, inventory snapshots and lock state.

### Ledger
Credits are integers. Each change has a transaction or reward-event ID and a reason code, and replayed IDs are no-ops. Debit fails when funds are insufficient. Arithmetic is checked for overflow. Statistics are counted separately from cash, and caps limit only the payout.

### Purchase lifecycle
validate (station, class, funds, slot, stack) → reserve funds → fulfill (inventory grant or vehicle spawn) → commit; if fulfillment fails → rollback with full refund. Item IDs are immutable data IDs.

### Life lifecycle
Alive → Downed → (Revived → Alive, same LifeId and kit) | Dead → respawn → new LifeId, paid kit cleared, one free-kit entitlement. Wallet untouched. `Health.Revived` (current full reset) is **not** a paid revive.

### Tuning defaults
As listed in ECONOMY_TASKS.md "Proposed first-pass tuning". These go into data assets in ECON-02 and are Mudfront proposals, not WARDOGS values.

### Known integration gaps (from the source review)
- `Health.Damage` takes only the attacker team. `Died` carries no attacker → ECON-05.
- `Respawner` respawns immediately with no downed state → ECON-08.
- `Vehicle` has one driver, open entry and a free auto-respawn → ECON-15/16/17. Legacy map vehicles keep their current behavior.
- `ThirdPersonController.Interact` (E) only handles vehicles → ECON-12 shared interaction routing.
- `MatchManager` has no reset or economy lifecycle → ECON-23.

## ECON-02 — Data assets and catalog validation (2026-11-19)

Added `Scripts/Runtime/Economy/`: `ItemDefinition`, `VehicleDefinition`, `StationDefinition`, `EconomyConfig` (wallet rules, zone multiplier, catalog, free kit, reward rules), `RewardRule`/`FreeKitEntry`, `ItemBehaviors` (behaviour keys and the set of implemented behaviours) and `CatalogValidator`. `Scripts/Editor/EconomyCatalogBuilder.cs` creates only the missing default assets under `Assets/_Mudfront/Data/Economy/`, so designer edits are never overwritten. It also validates the catalog and writes `economy_catalog_report.md`.

Defaults follow the tuning table. Additions that are not in the table, all Mudfront proposals: free-kit-only `rifle_basic` (25 dmg, same as the old Shooter), `rifle_service` 250, `ammo_pistol` 15 credits per 24 rounds (the sidearm needs ammo), weapon stats, bandage 40 HP over 2.5 s, max stack 3.

Availability: an item is offered only if it is `Purchasable` and its behaviour is in `ItemBehaviors.Implemented`. A vehicle also needs a prefab. The implemented set is empty, so **every product is currently hidden**. ECON-07+ add keys as gameplay lands.

Results:
- `EconomyCatalogBuilder.BuildAndValidate` (batch): exit 0, **VALID**.
- Real asset edit `Item_pistol.asset` Price → -5 (no source change), then validate: **exit 1**, error reported. Restored → exit 0. Prices and rewards are editable data.
- PlayMode: **36/36 passed** (21 baseline + 15 new `EconomyCatalogTests`: duplicate ID, negative price, price > max balance, missing/unknown behaviour, incompatible slot, missing ammo link, stack overflow, capped reward without cap, duplicate reward reason, vehicle without prefab hidden, unimplemented item hidden, free-kit-only never sold, shipped catalog valid with expected prices).

## ECON-03 — Identity, wallet, ledger and action statistics (2026-11-19)

Added `PlayerRegistry` and `WalletLedger` (pure C#, no MonoBehaviour, so offline and server authorities share them).
- **Identity:** `PlayerId` is server-issued and sequential. Each player gets a private reconnect token per match. Reconnect maps only through that token, so a client-chosen ID is never trusted. `BeginNewMatch` rotates tokens and drops disconnected players. `LifeId` increases across the server session.
- **Ledger:**
  - Integer credits with a hard ceiling (`MaxBalance`, which clamps and returns `Capped`) and checked arithmetic.
  - Every mutation carries an event ID. A replay returns `Duplicate` with the original TxId.
  - Purchase request IDs are namespaced by match and player, so one client can't pre-empt or collide with another.
  - A refund needs a real, successful purchase, can't exceed it, and happens once.
  - `InsufficientFunds` isn't remembered, so a legitimate retry can still succeed. No reward is paid when `RoundActive` is false.
- **Statistics:** per-action counters with their own dedupe, separate from payouts.
- **Reconciliation:** `Reconciles()` checks balance = sum of ledger records = earned - spent.
- **Lifecycle:** the entry grant is paid once per match, and late joiners get it when they join. Wallets, records and replay sets reset exactly once per new MatchId. Replay records live for the whole match; protection against stale requests from a previous match is enforced by the network layer in ECON-04.

Results: PlayMode **50/50 passed** (36 previous + 14 new `WalletLedgerTests`: entry once, duplicate reward/debit, insufficient funds, negative/zero/forged entry amounts, overflow clamp, separate same-team wallets with colliding request IDs, refund rules, stat dedupe, no payout outside round, late join + new-match reset, reconnect keeps identity/balance, forged/stale token rejected, life ID uniqueness).

## Contract amendment — DECISIONS.md revision 2 (2026-11-19, ECON-03R)

The ECON-01 "Implementation contract" above is **superseded** where it conflicts:
- **Persistence:** money is persistent per **account**. A one-time `AccountSeedGrant` (10,000) is paid at account creation. There is no match, death or respawn grant, and no wallet reset on a new match. Only per-match action statistics reset.
- **Identity:** `PlayerRegistry.Join(authenticatedAccountId)` binds an AccountId to a stable PlayerId. A reconnect with the same account gets the same PlayerId, and a second simultaneous session is refused. The ECON-03 reconnect tokens are removed; identity comes from authentication (ECON-03A/04). MatchId and LifeId are kept for validating actions.
- **Stale actions:** rewards and debits carry the MatchId and are rejected (`StaleMatch`) if it isn't the current match. The wallet persists, but old-match actions don't.
- **Roles:** there are no class gates; `ClassMask`/`AllowedClasses` are removed. Items and vehicles have `UnlockTrack` + `UnlockXp` + `UnlockFee`. The fee is debited as `LedgerKind.Unlock`. Starter gear must have no unlock.
- **Trader starter kit:** the free kit is claimed at a trader once per life. It is not granted automatically.
- **Vehicles:** `VehicleLockMode {OwnerOnly, WorldUnlocked}`; WorldUnlocked admits enemies too. `OwnerFinalDeathPolicy` (default `UnlockForEveryone`, configurable to `RetainOwnerLock`). `OwnerDisconnectGrace` is 60 s.
- **Follow-up rewards:** `FollowUpKill` pays 25 within a 120 s window, capped at 150 per provider/recipient life, plus a 150 `SharedCap` across all providers.

## ECON-03R — Reconcile completed data/wallet work with revision 2 (2026-11-19)

The existing systems were migrated in place; nothing was rebuilt alongside them.
- **Data:** `EconomyConfig` has `DataVersion` (current 2), `AccountSeedGrant`, the vehicle lock/death/disconnect policies and `BasicReviveSeconds`.
  - New categories and slots: Armour, Grenade, Attachment.
  - New behaviour keys: large medkit, build tool, armour, frag/smoke grenade, attachment, and three helicopter variants.
  - Item fields for armour, grenade, attachment, medkit charges and revive multiplier. Vehicle fields for kind, gunner seats, fuel, mounted gun, rockets and flares, with prices for each.
  - `RewardRule` gains `SharedCap` and an XP track/amount.
- **Validator:** checks every new field: unlock consistency, per-category parameters, helicopter fuel and flares, armed and attack weapon requirements, follow-up window/caps, outdated DataVersion, and that starter gear has no unlock.
- **Builder:** now *migrates* the existing assets. It adds missing defaults to lists, sets the seed to 10,000 and DataVersion to 2, and adds XP to existing reward rules, without overwriting edited values.
  - New proposed defaults: `armour_vest` 200; `grenade_frag` 75 (Assault 100 XP + 300); `grenade_smoke` 50; `scope_rifle` 120 (Recon 100 XP + 300); `extmag_rifle` 100; `medkit_large` 300 (Medical 200 XP + 1,000, 3 charges, revive x0.4); `build_tool` 250 (Support 200 XP + 1,000).
  - Helicopters: `heli_transport` 2,500 (Pilot 0 XP + 2,000), `heli_armed` 4,000 (Pilot 300 XP + 3,000), `heli_attack` 6,000 (Pilot 800 XP + 5,000).
  - Fuel 0.5 per unit, mounted rounds 1, rockets 40, flares 25. There is also a new `hq_helipad` station.
  - All are tuning data, and **all products remain hidden** (no behaviour is implemented yet).
- **Ledger:** match reset and per-match grant removed. Wallets are keyed by AccountId. The seed is paid once per account. Purchase keys are `buy:{account}:{match}:{request}`. `LedgerKind.Unlock` is new. An `ILedgerStore` hook commits each record before it is acknowledged (a failed write returns `StorageFailure` and changes nothing), and `Restore()` rebuilds state from it for ECON-03A.
- **Tests:** ECON-03 tests rewritten. They now prove that a new match preserves the balance, the seed can't repeat, old-match actions are rejected, unlock fees count as spending, a second session is refused, and the earlier properties still hold. Catalog tests were added for every new rule. The shipped catalog has an item in every category, the three helicopter kinds, the default policies and follow-up values, and **no `AllowedClasses` field**, so the old behaviour is explicitly superseded.

Results:
- `BuildAndValidate`: exit 0, **VALID**. The migrated asset has `DataVersion: 2`, `AccountSeedGrant: 10000` and `MaxBalance: 10000000`.
- PlayMode: **57/57 passed** (21 baseline + 20 catalog + 16 ledger).

Limitations:
- Older item assets still have an orphan `AllowedClasses` line in their YAML. Unity ignores it and drops it the next time it reserializes the asset.
- The Pilot track has no XP source yet; ECON-19F adds pilot XP. The transport helicopter needs 0 XP so it can be unlocked.

## ECON-03A — Durable account wallet and recovery (2026-11-19)

**Canonical authority.** One server-side `FileLedgerStore` per account database. It is an append-only journal (`#mudfront-ledger schema=1` header, then one record per line as FNV-1a checksum plus JSON).
- The file is opened with `FileShare.None`, so only one authority process can hold a given database.
- Every `Commit` is `Flush(true)` (fsync) **before** the ledger acknowledges anything.
- `WalletLedger` serializes all mutations under a lock.
- PlayerId is a session handle bound to the persistent AccountId, from `PlayerRegistry.Join(authenticatedAccountId)`. MatchId and LifeId are kept for validating actions.
- After a restart, `PlayerRegistry.ResumeAfter` numbers matches above every persisted MatchId, so old-match actions stay invalid.

**Schema versions.** The ledger schema is in the journal header; the store refuses a newer schema. The catalog/account data version is `EconomyConfig.DataVersion`.

**Idempotency.** These keys are durable: seed `seed:{account}`, purchases `buy:{account}:{match}:{request}`, rewards `rw:{match}:{event}`, refunds `refund:{purchaseKey}` and delivery `done:{purchaseKey}`. They are rebuilt by `Restore()` on startup.

**Purchase intent.** The debit record *is* the durable intent, and delivery writes a `Fulfilled` record. On startup `AccountAuthority.Open` refunds every purchase or unlock that has neither a fulfilment nor a refund. Refund keys are unique, so running recovery again does nothing.

**Failure handling.**
- A failed write returns `StorageFailure`, changes nothing and isn't remembered, so a retry works exactly once.
- A torn final line (crash mid-write) was never acknowledged, so it is discarded and truncated.
- A complete line with a bad checksum is corruption: the server **refuses to start** and must be restored from backup, rather than inventing or losing cash.
- A refused file is released immediately. A handle leak here was found by the tests and fixed.

**Backup and recovery procedure.**
- `FileLedgerStore.Backup(dest)` copies a consistent journal while the server holds the lock. Schedule it periodically and before upgrades.
- To restore: stop the server, replace the journal with the newest good backup, and start. Startup recovery refunds any purchase that was in flight.
- Money committed after that backup is lost, so back up often and keep the journal on redundant storage.

**Production identity and provider: not decided, so cross-server persistence is not exposed.**
- Local development uses `FileLedgerStore` on the server host. The AccountId comes from the connection authenticator (ECON-04). A dev authenticator is acceptable locally.
- Before a public cross-server deployment, a decision is needed on (a) an identity provider (platform auth ticket, e.g. Steam or Unity Authentication, verified by the server) and (b) one canonical transactional database service shared by all game servers. Each server would call it instead of its own file.
- Until then, each server's database is separate and there is **no shared cross-server wallet** (DECISIONS.md). A client save file is never an authority.

**Results:**
- PlayMode: **67/67 passed** (57 previous + 10 `AccountPersistenceTests`):
  - Earn/spend, restart, reconnect and new match keep the balance.
  - No re-seed across restarts.
  - Old-match actions are invalid after a restart.
  - A crash between debit and delivery is refunded once, and recovery is idempotent.
  - Idempotency keys survive a restart.
  - A torn tail is discarded.
  - A corrupt record refuses startup.
  - A storage failure invents no cash and a retry succeeds once.
  - A second authority can't open the database.
  - 200 parallel debits on 10,000 give exactly 100 successes and a balance of 0, with no overdraw.
- **Real two-process restart** (`EconomyPersistenceProbe`, two separate batch Unity processes on one database):
  - Phase 1: seed, kill +150, rifle -250 delivered, jeep -600 not delivered; the process exits without closing the store; balance 9,300.
  - Phase 2 (new process): `refunds=1 seed=Duplicate balanceAfterRestart=9900 staleOldMatchDebit=StaleMatch balanceAfterNewMatch=9900 => PASS`.

**Limitations:**
- A match in which no money moved leaves no MatchId in the journal. Such a match has no money actions that could go stale, but a later networking task may want an explicit match-start record.
- "Simultaneous sessions" are covered by a single session per account (`AlreadyConnected`) and by serialized spending on one authority. Behaviour across server instances depends on the production database decision above.

## ECON-04 — Fish-Net foundation (in progress, 2026-11-19)

**Install (done):**
- Installed through the official Package Manager Git URL (fish-networking.gitbook.io, "Installing Fish-Networking"). Manifest entry: `"com.firstgeargames.fishnet": "https://github.com/FirstGearGames/FishNet.git?path=Assets/FishNet#4.7.3"`. Lock: . Git 2.55.
- **Why 4.7.3:** the newest official tag, 4.7.4, fails to compile on Unity 6000.3.28f1. Its bundled Yak transport reports CS0103/CS1061: `ClientSocket`/`ServerSocket` lack `IterateIncoming`, `SendToServer`, `SendToClient` and `OnLocalServerConnectionState`. 4.7.3 and 4.6.22 compile, and the user approved pinning 4.7.3.
- Fish-Net generated `Assets/DefaultPrefabObjects.asset` (its spawnable prefab list).
- Regression with 4.7.3: PlayMode **67/67 passed**.

**Remaining for ECON-04:** connection authenticator bound to AccountId; server-owned match/player state; wallet snapshot, transaction result and notification messages; server validation and rate limiting; headless dedicated server build; a real server process + 2 client processes test (isolated wallets, forged identity and direct reward requests rejected).
