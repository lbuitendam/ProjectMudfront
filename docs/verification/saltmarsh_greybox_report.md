# Saltmarsh Point grey-box – verification
Generated 2026-11-18 14:26 (Unity 6000.3.28f1). Targets: docs/maps/MAP_01_Saltmarsh_Point.md, docs/TARGETS.md

## HQs
- PASS HQ_Tidewatch_HarbourQuay: ground 1,8 m (above sea), 190 m to marsh centre, nearest zone **Net Sheds** 86 m
- PASS HQ_Ironclads_CliffFort: ground 22,5 m (above sea), 240 m to marsh centre, nearest zone **Beacon Mill** 110 m
- PASS HQ_Muddogs_DuneFarm: ground 9,6 m (above sea), 215 m to marsh centre, nearest zone **Dune Battery** 93 m
- HQ_Tidewatch_HarbourQuay ↔ HQ_Ironclads_CliffFort: 368 m
- HQ_Tidewatch_HarbourQuay ↔ HQ_Muddogs_DuneFarm: 339 m
- HQ_Ironclads_CliffFort ↔ HQ_Muddogs_DuneFarm: 413 m

## Zones (neighbour spacing target 90–130 m)
- PASS Beacon Mill: ground 22,6 m, radius 8 m, nearest other zone 130 m
- PASS Marsh Boardwalk: ground 1,5 m, radius 10 m, nearest other zone 92 m
- PASS Net Sheds: ground 3,7 m, radius 8 m, nearest other zone 134 m
- PASS Sunken Barge: ground 5,5 m, radius 9 m, nearest other zone 92 m
- PASS Dune Battery: ground 6,0 m, radius 9 m, nearest other zone 125 m

## Infantry routes (target HQ→nearest zone ≤ 25 s sprint at 8 m/s; max slope ≤ 40°)
- PASS `Tidewatch→Net Sheds (quay)`: 84 m → **10,5 s** sprint (15 s run), max slope 4°, clear
- PASS `Tidewatch→Marsh (boardwalk)`: 180 m → **22,5 s** sprint (33 s run), max slope 9°, clear
- PASS `Tidewatch→Marsh (mudflat flank)`: 279 m → **34,9 s** sprint (51 s run), max slope 14°, clear
- PASS `Ironclads→Beacon Mill (cliff path)`: 93 m → **11,7 s** sprint (17 s run), max slope 2°, clear
- PASS `Ironclads→Marsh (cliff descent)`: 243 m → **30,4 s** sprint (44 s run), max slope 38°, clear
- PASS `Muddogs→Dune Battery (trench)`: 90 m → **11,2 s** sprint (16 s run), max slope 7°, clear
- PASS `Muddogs→Marsh (dune trail)`: 206 m → **25,7 s** sprint (37 s run), max slope 9°, clear

## Vehicle ring road
- PASS ring road 1078 m (lap ≈ 77 s at 14 m/s), steepest average segment slope 11°
- PASS HQ_Tidewatch_HarbourQuay → nearest zone by vehicle ≈ 89 m ≈ 6,4 s (target ≤ 12 s)
- PASS HQ_Ironclads_CliffFort → nearest zone by vehicle ≈ 112 m ≈ 8,0 s (target ≤ 12 s)
- PASS HQ_Muddogs_DuneFarm → nearest zone by vehicle ≈ 96 m ≈ 6,8 s (target ≤ 12 s)

Screenshots: `saltmarsh_overview.png`, `saltmarsh_harbour.png`, `saltmarsh_mill.png`.

**Result: all checks passed.**
