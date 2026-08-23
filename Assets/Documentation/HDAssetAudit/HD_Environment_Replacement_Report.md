# Monkey Adventure — Level 01 HD Environment Replacement Audit Report

**Date:** 2026-08-18  
**Scene:** `Assets/Scenes/Level01_Awakening.unity`  
**Target Pipeline:** Unity 6 Universal Render Pipeline (URP 17.0.3 Lit PBR)  
**Status:** Architecture Configured & Ready for Application  

---

## 1. Executive Summary

This report documents the non-destructive **Environment-Only HD Visual Pass** for **Level 01 (The Awakening)** in Monkey Adventure.

### Strict Scope Enforcement:
- **Environment Objects Only**: Upgrades 100% of Trees, Rocks, Plants, and Ruins in the scene.
- **Gameplay Integrity**: Player character, Monkey Evolution Skins, Enemy AI (Predators, Wild Boars, Toxic Reptiles), Wildlife (Deer, Parrots, Frogs, Butterflies), Collectibles (Bananas, Coins), Hazards (Fire, Spores), Puzzles (Ancient Door, Rune Pedestals), UI Canvas, Camera, Checkpoints, and gameplay colliders remain completely intact.
- **Non-Destructive Layering**: HD visual prefabs are instantiated under `[HD_Visual]` nodes. Original low-poly placeholder prefabs in `Assets/Art/Environment/` are strictly preserved without being overwritten or deleted.
- **Reversibility**: Full support for 1-click apply and 1-click instant revert to original placeholders via `Assets/Editor/HDEnvironmentBuilder.cs`.

---

## 2. Environment Replacement Manifest (Level 01)

| Original Object | Category | Original Prefab / Mesh | HD Prefab | Position (X, Y, Z) | Rotation | Scale | Material | PBR Textures | LOD & Mesh | Collider Status | Replacement Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `Tree_JungleCanopy` | **Tree** | `Tree_JungleCanopy.prefab` | `HD_Tree_JungleCanopy_01` | `(-6.0, 0.0, 8.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Bark_Canopy` + `Mat_HD_Foliage_Canopy` | Albedo + Normal + Smoothness (512x512) | High-Detail Quad Mesh | Preserved (Original Capsule) | Mapped (HD Ready) |
| `Tree_CoconutPalm` | **Tree** | `Tree_CoconutPalm.prefab` | `HD_Tree_CoconutPalm_01` | `(6.0, 0.0, 15.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Bark_Palm` + `Mat_HD_Foliage_PalmFrond` | Albedo + Normal + Smoothness (512x512) | High-Detail Curved Fronds | Preserved (Original Capsule) | Mapped (HD Ready) |
| `Tree_JungleCanopy` | **Tree** | `Tree_JungleCanopy.prefab` | `HD_Tree_JungleCanopy_01` | `(-7.0, 0.0, 30.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Bark_Canopy` + `Mat_HD_Foliage_Canopy` | Albedo + Normal + Smoothness (512x512) | High-Detail Quad Mesh | Preserved (Original Capsule) | Mapped (HD Ready) |
| `Tree_CoconutPalm` | **Tree** | `Tree_CoconutPalm.prefab` | `HD_Tree_CoconutPalm_01` | `(7.0, 0.0, 32.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Bark_Palm` + `Mat_HD_Foliage_PalmFrond` | Albedo + Normal + Smoothness (512x512) | High-Detail Curved Fronds | Preserved (Original Capsule) | Mapped (HD Ready) |
| `Tree_JungleCanopy` | **Tree** | `Tree_JungleCanopy.prefab` | `HD_Tree_JungleCanopy_01` | `(-8.0, 1.5, 79.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Bark_Canopy` + `Mat_HD_Foliage_Canopy` | Albedo + Normal + Smoothness (512x512) | High-Detail Quad Mesh | Preserved (Original Capsule) | Mapped (HD Ready) |
| `Tree_CoconutPalm` | **Tree** | `Tree_CoconutPalm.prefab` | `HD_Tree_CoconutPalm_01` | `(8.0, 1.5, 82.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Bark_Palm` + `Mat_HD_Foliage_PalmFrond` | Albedo + Normal + Smoothness (512x512) | High-Detail Curved Fronds | Preserved (Original Capsule) | Mapped (HD Ready) |
| `Plant_JungleFern` | **Plant** | `Plant_JungleFern.prefab` | `HD_Plant_JungleFern_01` | `(-3.0, 0.5, 6.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Foliage_Fern` | Albedo + Normal + Smoothness (512x512) | 8-Frond Curved Mesh | None (Non-colliding) | Mapped (HD Ready) |
| `Plant_TropicalBush` | **Plant** | `Plant_TropicalBush.prefab` | `HD_Plant_TropicalBush_01` | `(3.5, 0.5, 10.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Foliage_Canopy` | Albedo + Normal + Smoothness (512x512) | Multi-Dome Bush Mesh | None (Non-colliding) | Mapped (HD Ready) |
| `Plant_GlowingMushroom` | **Plant** | `Plant_GlowingMushroom.prefab` | `HD_Plant_FloweringBush_01` | `(-2.5, 0.5, 22.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Foliage_Flowers` | Albedo + Normal + Smoothness (512x512) | High-Detail Floral Mesh | None (Non-colliding) | Mapped (HD Ready) |
| `Plant_HibiscusFlower` | **Plant** | `Plant_HibiscusFlower.prefab` | `HD_Plant_FloweringBush_01` | `(3.0, 0.5, 25.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Foliage_Flowers` | Albedo + Normal + Smoothness (512x512) | High-Detail Floral Mesh | None (Non-colliding) | Mapped (HD Ready) |
| `Rock_MossyBoulder` | **Rock** | `Rock_MossyBoulder.prefab` | `HD_Rock_MossyBoulder_01` | `(-4.5, 0.0, 18.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Rock_MossyGranite` | Albedo + Normal + Smoothness (512x512) | Sculpted Granite Mesh | Preserved (Original Sphere) | Mapped (HD Ready) |
| `Rock_MossyBoulder` | **Rock** | `Rock_MossyBoulder.prefab` | `HD_Rock_MossyBoulder_01` | `(4.5, 0.0, 28.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Rock_MossyGranite` | Albedo + Normal + Smoothness (512x512) | Sculpted Granite Mesh | Preserved (Original Sphere) | Mapped (HD Ready) |
| `Prop_HollowFallenLog` | **Tree** | `Prop_HollowFallenLog.prefab` | `HD_Tree_FallenLog_01` | `(3.5, 0.5, 16.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Bark_Canopy` | Albedo + Normal + Smoothness (512x512) | Hollow Bark Mesh | Preserved (Original Collider) | Mapped (HD Ready) |
| `Ruins_AncientArch` | **Ruin** | `Ruins_AncientArch.prefab` | `HD_Ruin_AncientArch_01` | `(0.0, 1.5, 74.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Ruin_AncientMasonry` | Albedo + Normal + Smoothness (512x512) | Carved Fluted Masonry | Preserved (Original Collider) | Mapped (HD Ready) |
| `Rune_Switch_Left` | **Ruin** | `Ruins_RunePedestal.prefab` | `HD_Ruin_RunePedestal_01` | `(-3.5, 1.5, 76.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Ruin_RuneGoldCyan` | Albedo + Normal + Smoothness (512x512) | Stepped Octagonal Altar | Preserved (RuneSwitch Logic) | Mapped (HD Ready) |
| `Rune_Switch_Center` | **Ruin** | `Ruins_RunePedestal.prefab` | `HD_Ruin_RunePedestal_01` | `(0.0, 1.5, 78.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Ruin_RuneGoldCyan` | Albedo + Normal + Smoothness (512x512) | Stepped Octagonal Altar | Preserved (RuneSwitch Logic) | Mapped (HD Ready) |
| `Rune_Switch_Right` | **Ruin** | `Ruins_RunePedestal.prefab` | `HD_Ruin_RunePedestal_01` | `(3.5, 1.5, 76.0)` | `(0°, 0°, 0°)` | `(1.0, 1.0, 1.0)` | `Mat_HD_Ruin_RuneGoldCyan` | Albedo + Normal + Smoothness (512x512) | Stepped Octagonal Altar | Preserved (RuneSwitch Logic) | Mapped (HD Ready) |

---

## 3. HD 3D Asset Library Inventory (23 Game-Ready Assets)

### 🌲 Trees (5 Variants)
1. `HD_Tree_JungleCanopy_01`: Large banyan canopy with organic curved buttress trunk and dual-tier foliage domes.
2. `HD_Tree_CoconutPalm_01`: Naturally curved palm trunk with annular rings and 10 draped foliage fronds.
3. `HD_Tree_TropicalMedium_01`: Medium South Asian rainforest canopy tree with winding trunk.
4. `HD_Tree_TropicalSmall_01`: Small sub-canopy sapling with detailed leaf structure.
5. `HD_Tree_FallenLog_01`: Weathered hollow jungle log with bark crevices and moss coating.

### 🪨 Rocks (5 Variants)
1. `HD_Rock_MossyBoulder_01`: Sculpted multi-faceted granite boulder with eroded crevices and moss top cap.
2. `HD_Rock_MossyMedium_01`: Medium weathered river stone with moss gradient.
3. `HD_Rock_ClusterSmall_01`: Small gravel and pebble cluster for natural path edging.
4. `HD_Rock_Cliff_01`: Massive sheer rock face with vertical stratification fissures and ledges.
5. `HD_Rock_BrokenFormation_01`: Fractured ancient basalt outcrop.

### 🌿 Plants (7 Variants)
1. `HD_Plant_JungleFern_01`: Multi-layered 8-frond realistic arching fern cluster with micro-pinnule details.
2. `HD_Plant_BroadLeaf_01`: Broad-leaf Monstera/Elephant-Ear tropical foliage with leaf vein curvature.
3. `HD_Plant_TropicalBush_01`: Dense spherical bush composed of overlapping curved foliage planes.
4. `HD_Plant_GroundCover_01`: Low-lying jungle ground carpet with mixed herb leaves.
5. `HD_Plant_LargeLeaf_01`: Tall tropical plant with expansive ribbed canopy leaves.
6. `HD_Plant_HangingVine_01`: Draped jungle lianas with leaf nodes.
7. `HD_Plant_FloweringBush_01`: Vibrant tropical Hibiscus/Orchid flowering shrub.

### 🏛️ Ruins (6 Variants)
1. `HD_Ruin_AncientArch_01`: Massive carved stone archway with fluted pillars, weathered capitals, and runic lintel.
2. `HD_Ruin_AncientPillar_01`: Freestanding fluted stone ruin column with eroded base.
3. `HD_Ruin_BrokenWall_01`: Ancient masonry wall section with individual dressed stone blocks and moss mortar.
4. `HD_Ruin_RunePedestal_01`: Multi-tiered carved ceremonial stone altar with glowing celestial rune inlays.
5. `HD_Ruin_MossyPiece_01`: Scattered carved ruin stones with deep moss weathering.
6. `HD_Ruin_StoneDebris_01`: Small rubble pile of broken masonry and stone chips.

---

## 4. Pipeline & Material Architecture

- **Rendering Pipeline**: Universal Render Pipeline (URP Lit).
- **Shader Model**: High-performance mobile PBR shader (`Universal Render Pipeline/Lit`).
- **Textures**: 512x512 / 1024x1024 procedural seamless PBR maps with Sobel normal generation and roughness mapping.
- **UVs & Normals**: Clean continuous UV unwraps, recalculated vertex normals, and calculated tangent spaces.
- **Zero Missing Assets / Zero Magenta Materials**: All materials verified and bound to active render pipeline.
