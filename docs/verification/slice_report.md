# Slice verification report
Generated 2026-11-18 14:25 by `SliceSceneBuilder.Validate` (Unity 6000.3.28f1).

## 1. Asset imports
- PASS `SM_Char_Trooper_Placeholder` size 1,25×1,85×0,75 m, 6 materials, pivot→bottom +0,00, collider added in code
- PASS `SM_Cover_BarricadeHigh_4m` size 4,10×2,17×1,47 m, 2 materials, pivot→bottom +0,00, collider mesh
- PASS `SM_Cover_CrateStack` size 2,22×1,79×1,17 m, 2 materials, pivot→bottom +0,00, collider mesh
- PASS `SM_Cover_SandbagsLow_4m` size 4,05×1,06×0,79 m, 1 materials, pivot→bottom +0,00, collider mesh
- PASS `SM_Kit_CornerPost` size 0,75×3,41×0,75 m, 2 materials, pivot→bottom +0,00, collider mesh
- PASS `SM_Kit_Floor_4m` size 4,00×0,22×4,00 m, 2 materials, pivot→bottom +0,00, collider mesh
- PASS `SM_Kit_RoofSlope_4m` size 2,64×2,02×4,25 m, 2 materials, pivot→bottom +0,00, collider mesh
- PASS `SM_Kit_Wall_4m` size 4,05×3,23×0,58 m, 3 materials, pivot→bottom +0,00, collider mesh
- PASS `SM_Kit_WallDoor_4m` size 4,05×3,23×0,58 m, 3 materials, pivot→bottom +0,00, collider mesh
- PASS `SM_Kit_WallWindow_4m` size 4,06×3,23×0,59 m, 3 materials, pivot→bottom +0,00, collider mesh
- PASS `SM_Veh_Mudskipper` size 2,38×2,09×3,68 m, 6 materials, pivot→bottom +0,00, collider added in code

## 2. Controller clearance (capsule r=0.4, h=1.8)
- PASS `SM_Kit_WallDoor_4m`: capsule passes (expected pass)
- PASS `SM_Kit_Wall_4m`: capsule blocked (expected block)
- PASS `SM_Kit_WallWindow_4m`: capsule blocked (expected block)

## 3. Routes (sprint 8 m/s, run 5.5 m/s, vehicle ~14 m/s avg)
- PASS `Route_Infantry_West` length 77 m → 9,6 s sprint / 14,0 s run, clear
- PASS `Route_Infantry_East` length 95 m → 11,8 s sprint / 17,2 s run, clear
- PASS `Route_Vehicle_Lane` length 54 m → 3,9 s by vehicle, clear

## 4. Readability (WCAG-style luminance contrast, team colour vs background)
- Tidewatch vs grass: 1,77:1 
- Ironclads vs grass: 1,81:1 
- Muddogs vs grass: 1,26:1  ← weak, relies on silhouette/outline
- Tidewatch vs plaster: 3,96:1 
- Ironclads vs plaster: 4,04:1 
- Muddogs vs plaster: 2,82:1 
- Tidewatch vs dirt road: 1,47:1  ← weak, relies on silhouette/outline
- Ironclads vs dirt road: 1,50:1 
- Muddogs vs dirt road: 1,05:1  ← weak, relies on silhouette/outline

Screenshots from the gameplay camera: `shot_spawn.png`, `shot_zone.png`, `shot_tower.png`.

**Result: all checks passed.**
