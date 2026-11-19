# Approved economy decisions — revision 2

Updated 2026-11-19 from the user's question-round answers. These rules supersede the original ECON plan and the match-only contract recorded during ECON-01. Implementation status stays in ECONOMY_TASKS.md; this file records product decisions, not completed features.

## Confirmed by the user

| Area | Approved behavior |
|---|---|
| Wallet | Persistent gameplay cash, one-time 10,000 starting balance per account. No recurring match or life allowance. Persist earnings/spending immediately through leaving, reconnecting and later sessions. No real-money cash purchases. |
| Seasons and gold | Later. Do not implement seasonal wipes, gold exchanges or cosmetics as part of this queue. |
| Traders | Physical buy stations at main faction bases and friendly completed FOBs. Free starter equipment is claimed from the trader, not automatically granted on spawn. |
| Roles | Equipment defines the role. Anyone may buy specialist equipment once unlocked; no mandatory selected classes or Engineer-only construction. Building tools and large medical kits provide those specialist functions. This supersedes the earlier class-restriction answer. |
| Death | Final death loses carried equipment; a successful revive preserves it. Wallet and account unlocks survive death. |
| Vehicle purchase | Spawn locked to the buyer at a valid delivery pad. Only the buyer controls access settings. |
| Unlocking | Public unlock permits everyone, including enemies. Being unlocked does not transfer ownership. |
| Buyer death | Keep intact vehicles in-world and unlock them for everyone on final death. This must be configurable so retaining ownership/lock settings can be selected later. Downing is not final death. |
| Support follow-up | Revivers and transport drivers receive subsequent kill bonuses from helped teammates, with a time limit and payout cap. |
| Required equipment | Guns, finite ammunition, bandages, parachutes, repair tools, armour, grenades and working weapon attachments. Include a large medical kit as specialist equipment. |
| Ground transport | Purchased usable vehicles with passenger seats, locks, damage and repair. |
| Aircraft | Transport helicopter with fuel, damage, repair and refuelling; armed transport variant with door gunner/ammo; attack helicopter with front gunner and rockets; aircraft flares. Passenger exits/parachutes and owner locks must work. |
| Zone | Keep the existing Control Zone for now. Smaller moving Hot Zone is deferred. |

## Implementation defaults (adjustable, not separately confirmed or WARDOGS facts)

- Free starter rifle and 60 reserve rounds once per life from a friendly trader; slow basic revive available to everyone. No resale/stacking of free gear.
- Role tracks: Assault, Recon, Medical, Support, Driver and Pilot. Actions earn role XP; configured XP prerequisites permit a one-time cash unlock, then each physical item is purchased per life. No seasonal reset. Starter essentials are unlocked immediately. Exact thresholds, unlock fees and aircraft prices are tuning data, not invented claims about WARDOGS.
- Persistent account authority is required. Begin with a durable repository implementation and authenticated identity, then choose the deployed account/backend provider in its task. A client save file must never be trusted for online cash. No shared cross-server wallet until one canonical authority can prevent concurrent spending.
- Follow-up bonus starting proposal: 25 credits per eligible teammate kill, 120-second window, maximum 150 credits per provider/recipient life, plus a shared 150-credit budget across support providers for each recipient life. Downing/revive cannot reset this budget. All values are editable data.
- Existing Control Zone reward multiplier remains configurable (initial 1.5). Do not add Hot Zone scoring or change capture rules implicitly.
- Vehicle owner disconnect retains the earlier 60-second reconnect grace and then despawns without refund. This is a disconnect policy, separate from the confirmed final-death unlock policy. No despawn while the pilot connection could still be recovered without a defined safe occupant cleanup.
- Countermeasure task includes a minimal testable heat-seeking anti-air threat so flares have actual behavior. A complete ground anti-air weapon catalog, aircraft variants beyond those listed, maps and realistic flight simulation require separate scope decisions.
- Match changes clear match vehicles and carried equipment; persistent money and unlocks remain. Public vehicle use by enemies never qualifies as friendly transport/support income.

## Reading and migration

Read [START_HERE.md](START_HERE.md), then follow [ECONOMY_TASKS.md](ECONOMY_TASKS.md). The implementing model has completed the original ECON-01–03 and started ECON-04. Preserve those historical statuses; new ECON-03R and ECON-03A gates migrate the old data/wallet contract to revision 2 before ECON-04 acceptance. No task is marked done by this document update.

Research is descriptive; these user-approved choices control Mudfront. Build original code, names and assets rather than importing WARDOGS content.
