# Economy verification report

Running log of actual results for the ECONOMY_TASKS.md queue. Only outcomes actually observed are recorded here.

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
- `Tests/PlayMode/SliceGameplayTests.cs`: 8 tests (capture single/contested/outside radius, shooting enemy vs teammate, respawn, vehicle enter/drive/exit, vehicle destroy/respawn, run speed).
- `Tests/PlayMode/MovementFeelTests.cs`: 10 tests (accel, decel, sprint rules, crouch, headroom, jump height, coyote, buffered jump, aim zoom/1st person, camera wall pull-in). Commit e5a4684 says these were "not yet run".
- No EditMode test assembly.

### Baseline run: BLOCKED
Command: `Unity.exe -batchmode -projectPath Mudfront -runTests -testPlatform PlayMode -testResults <scratch>/baseline_playmode.xml`

Result: exit code **198** before compiling or running any test:
```
[Licensing::Module] Error: Access token is unavailable; failed to update
[Licensing::Client] Error: Code 404 ... Found 0 entitlement groups and 0 free entitlements
No valid Unity Editor license found. Please activate your license.
```
No test results were produced. The older `playmode_results.xml` (10/10) is **not** taken as the current baseline. Shooter.cs edits and the 10 movement tests are unverified.

**Needed from the user:** sign in to Unity Hub (and activate the Personal license if asked). After that, rerun the command above. Every later task needs Unity for compiles, `.meta` creation and tests, so the queue stops here.

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
