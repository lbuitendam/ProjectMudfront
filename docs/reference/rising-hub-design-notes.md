# Rising Hub / Battlefield Heroes – Design Notes

These are study notes on how the game is put together. They describe the patterns in our own words and contain no copied code or assets.

## 1. Architecture

| Layer | What it is | Lesson for our game |
|---|---|---|
| Launcher (`rising.exe`) | Qt/QML app: login, news, starts the game, Discord presence | Keep the launcher separate from the game |
| Engine (`bfheroes.exe`, `RendDX9.dll`) | Refractor 2 (Battlefield 2 engine), DirectX 9 | We use Godot instead |
| Revival patch (`Rising.dll`) | Hooks the old client to the new servers | Not needed |
| Game rules | **Python 2.3 scripts** (`python/bf2/*.py`) | Gameplay lives in scripts, so the engine stays generic |
| Content setup | `.con` text files | Data-driven: objects and settings are defined as data |
| Content | zip archives split **_client** (art/sound) and **_server** (logic/collision) | The server only loads what it needs |
| Menus | Flash (`SwiffPlayer.dll`, `Menu/FlashMenu`) | UI is separate from the game code |
| Web pages | Embedded browser (`EasyWeb.dll`, `html/`) | Used for the store and account pages |

## 2. Core gameplay setup (from Init/GameLogicInit)
- **2 teams**: "National" vs "Royal" (shown as "British" in the config).
- **3 classes (kits)**: Commando (stealth/sniper/knife), Soldier (all-rounder/healer), Gunner (heavy MG/rockets). Female variants exist but are commented out.
- **Tickets**: 250 per team; tickets drain while you hold fewer points and when your players die. Ticket parameters can be changed per map.
- Supply drops from the sky, with a set height and how long they stay.
- Network optimisations: simplified networking for soldiers and simple objects.

## 3. Ability system (AbilitiesDB)
- 22 abilities, each just an **ID + name**: bandage, heal area, stealth, sprint, stomp, mark target, see enemies, grenade spam, MG frenzy, incendiary ammo, explosive keg/trap, wrench (repair), etc.
- **Levels via name suffixes** (`_lvl2` … `_lvl5`), all pointing back to one base ability, so upgrades are separate data entries rather than new code.
- Abilities are often **tied to a weapon type** (e.g. frenzy only with an MG).
- The file is **auto-generated** by a converter from spreadsheets. Designers edit tables, and a tool exports the scripts.

**For us:** an `Ability` resource (id, cooldown, duration, level, effect) + a table/CSV → generated data.

## 4. Weapons (WeaponsDB, ~139 KB)
- Template: id, asset id, name, **type**, and 3 flags: **which class may use it**.
- 9 types: SMG, anti-tank, shotgun, MG, sniper, pistol, grenade, knife, TNT.
- Lots of **skin/variant entries** (gold, skin2, promo versions), so cosmetics are just separate weapon entries.

## 5. Progression and economy
- **XP → rank**: rank-up when XP exceeds the cap for the current rank; leftover XP carries over to the next rank.
- **Currencies**: Valor Points (per level-up, server setting), **Hero Points every 2 levels**, plus PP/credits from medals.
- **Missions** (MissionsDB): chains like "Combat Mastery I → II → III", each with criteria (score X, kills/assists Y, damage Z), filters (infantry/vehicle), reset modes (global / per round), a reward and a prerequisite mission. Daily missions exist too.
- Medals, ELO, end-of-round stats and a snapshot that gets uploaded to the backend (`stats/`).
- Server **caps** that switch XP/VP/HP/mission progress on or off (e.g. not on unranked servers).

**For us:** a server-authoritative stats module + mission table (criteria + filters + reward + prerequisite).

## 6. Maps
~43 levels, grouped into **~20 base maps with variants** (day/night/snow/"doom"), i.e. one layout reused with different lighting and weather. Cheap content multiplier.

## 7. What's NOT in BFH
**No base building and no buy menu during a match.** Those come from Wardogs-style games, so we design them ourselves:
- Currency per kill/capture/round time.
- Buy menu = table (item, cost, category, class restriction, unlock rank), same pattern as WeaponsDB.
- Base building = ghost preview → placement check → server confirms → spawn, with an HP/upgrade level per structure.

## 8. Main takeaways
1. Generic engine + **scripted rules** + **data tables** for everything.
2. Abilities, weapons, missions and skins are all **data entries**, generated from tables.
3. **Server-authoritative** for score, XP and currency.
4. Client/server content split.
5. Map variants for cheap extra content.
