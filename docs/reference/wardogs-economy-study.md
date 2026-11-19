# WARDOGS economy reference for Mudfront

Researched 2026-11-19. Public documentation research; no hands-on test or inspection of WARDOGS code/assets. This is a mechanics reference, not a claim that Mudfront already implements these features.

## Sources and confidence

1. [Team17 official WARDOGS overview](https://www.team17.com/games/wardogs): confirms a $10,000 initial balance, buying equipment and vehicles each life, rewards for reviving, transporting friendlies and controlling objectives, money persisting between matches, and extra cash in the Hot Zone.
2. [Official Steam store description](https://store.steampowered.com/app/1867240/WARDOGS/): corroborates the core loop and describes logistics, construction and support gameplay. Early Access mechanics and tuning can change.
3. [Community Field Guide: game tips](https://wardogsgame.tools/text/tips): reports vendor categories, a free rifle, repurchasing the previous kit, bandaging, equipment-assisted revives, spotting assists, parachutes, repair/refuel, and locked purchased vehicles with owner-controlled access. This is a community reproduction of game text, not independently verified developer documentation. Its wording varies between squad, teammates and everyone; do not infer exact enemy access rules.

Exact current payout tables, access permissions and item behavior are not established by these sources. Do not copy internet payout tables as authoritative tuning. Mudfront's choices below are original implementation proposals.

## Lessons to apply

Useful actions should fund another useful life. Support and transport need visible payouts comparable to combat, and useful equipment needs a complete gameplay implementation. A shop entry alone does not deliver that loop. Death creates spending decisions; a successful revive preserves an existing kit. A free functional loadout prevents bankruptcy from blocking participation.

Purchasing a vehicle includes delivery and control: spawn somewhere usable, identify its buyer, start locked, explain the lock, and let the buyer change access. Transport requires passenger seats and a meaningful completed trip, not a reward for pressing the enter button.

## Mudfront adaptation (user decisions, revision 2)

| Area | Mudfront decision for this work |
|---|---|
| Persistence | Authoritative account cash persists across matches/sessions; 10,000 starting credits once. Seasons/gold deferred. |
| Starting funds | No recurring allowance. Claim free starter gear from a friendly trader once per life. |
| Shopping | Physical stations at HQs and friendly completed FOBs. Ground/aircraft orders require suitable configured delivery pads. |
| Roles | Equipment-defined roles and persistent unlock requirements; anyone can buy unlocked building/medical gear. No selected-class gate. |
| Required purchases | Guns/ammo, bandages, large medical tools, parachutes, repair tools, armour, grenades, attachments, ground transport and specified helicopters. |
| Vehicle access | OwnerOnly on delivery; buyer can WorldUnlock for everyone INCLUDING enemies or relock. Intact vehicles default to public unlock on buyer final death; configurable alternative preserves locks. |
| Rewards | Combat, support, objective, transport, repair, spotting, construction, resupply and asset destruction, plus capped/time-limited reviver/driver follow-up kill bonuses. |
| Aircraft | Transport fuel/damage/repair/refuelling, door-gunner variant, attack helicopter with front gunner/rockets and aircraft flares. |
| Zone bonuses | Configurable existing Control Zone bonus. Smaller Hot Zone deferred. |
| Later | Seasons, gold/cosmetics, tips/bounties, squad permissions, supply pallets and looting/trading are not included by this decision round. |

The user's later answers supersede earlier Mudfront-specific match resets and class restrictions. [DECISIONS.md](../DECISIONS.md) separates approved behavior from adjustable defaults. Do not treat these Mudfront decisions as verified WARDOGS behavior.

Additional research during the question round: [official developer announcements](https://steamcommunity.com/app/1867240/announcements/) describe role progression and unlock tracks. [PC Gamer's hands-on report](https://www.pcgamer.com/games/fps/believe-the-hype-for-wardogs-possibly-the-next-great-multiplayer-fps/) describes equipment-defined roles rather than formal selected classes. Exact current unlock tables and payouts remain version-dependent. Use original data and implementation.

## Counting rules

Track valid actions as well as cash: per-match kills, assists, revives, HP restored, passengers delivered, repairs, objective time, construction, resupply, spotting assists and assets destroyed. Expose earned/spent/net alongside counts. Apply configured caps to payouts without silently losing valid statistics. Rejected, repeated or fabricated events affect neither counts nor cash.

Prevent easy farming: no self rewards, no friendly kills, no overheal payout, no rewards for damage deliberately caused by the same beneficiary, no repeated seat-hop payouts, no repeated spotting of an unchanged target, no full rewards for destroying an unfinished construction ghost. Attribute contributions to stable player IDs and a unique life/action/asset ID. Server time, health, distance and actual inventory changes decide validity.

## Planning artifacts

The execution order, acceptance checks and starter tuning are in [ECONOMY_TASKS.md](../ECONOMY_TASKS.md). That queue includes the approved persistent economy, equipment unlocks and ground/air vehicle scope. [START_HERE.md](../START_HERE.md) is the AI job entry point.
