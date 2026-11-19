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

## Mudfront adaptation (proposed defaults, not WARDOGS facts)

| Area | Mudfront decision for this work |
|---|---|
| Persistence | Keep the GDD's match-only wallet. No cross-match bank, premium currency or account progression. |
| Starting funds | One configurable grant at match entry; no repeated death grant. Free basic rifle/ammo available each life. |
| Shopping | One equipment station and one vehicle delivery area at each of the three faction HQs; one shared menu is acceptable. Active friendly FOBs get an equipment station when that buildable exists; vehicle delivery requires an explicitly configured pad. |
| Loadout | Keep four classes and Engineer-only building. Validate class restrictions on the authority. |
| Required purchases | Working rifle, sidearm, reserve ammo, bandage, parachute, repair tool and a ground transport with passenger seats. Revive is available to every class; Medic is faster. |
| Vehicle access | OwnerOnly on purchase; buyer may select TeamUnlocked or relock. Enemies always denied. Squad mode waits for a real squad system. |
| Rewards | Kills, damage assists, revives, effective teammate heals, objective participation, completed passenger trips, effective repairs, spotting assists, construction, resupply and enemy asset destruction. |
| Zone bonuses | Configurable multiplier for eligible actions inside the existing active control zone. No new Hot Zone or match scoring rewrite in this work. |
| Later extensions | Squad permissions, aircraft, supply pallets, tips/revive bounties, support follow-up royalties, looting/trading, persistent bank, cosmetics. These need separate design/tasks. |

## Counting rules

Track valid actions as well as cash: per-match kills, assists, revives, HP restored, passengers delivered, repairs, objective time, construction, resupply, spotting assists and assets destroyed. Expose earned/spent/net alongside counts. Apply configured caps to payouts without silently losing valid statistics. Rejected, repeated or fabricated events affect neither counts nor cash.

Prevent easy farming: no self rewards, no friendly kills, no overheal payout, no rewards for damage deliberately caused by the same beneficiary, no repeated seat-hop payouts, no repeated spotting of an unchanged target, no full rewards for destroying an unfinished construction ghost. Attribute contributions to stable player IDs and a unique life/action/asset ID. Server time, health, distance and actual inventory changes decide validity.

## Planning artifacts

The execution order, acceptance checks and starter tuning are in [ECONOMY_TASKS.md](../ECONOMY_TASKS.md). That queue implements the requested ground-vehicle economy before expanding to the larger WARDOGS sandbox.
