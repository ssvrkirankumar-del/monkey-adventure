# 🐒 Monkey Adventure — 3D Asset Requirements & Licensing Specification

**Project:** Monkey Adventure  
**Target Platform:** Unity 6 (`6000.5.8f1`) + Universal Render Pipeline (URP 17.0.3) + Android Mobile (Vulkan/GLES3)  
**Art Direction:** Stylized 3D Premium Low-Poly Jungle (Rich color palettes, readable silhouettes, mobile-optimized draw calls).  

---

## 🎨 Asset Taxonomy & Licensing Standards

All 3D models, textures, animations, VFX, and audio assets integrated into Monkey Adventure adhere to strict commercial and mobile performance guidelines:
- **Commercial Use Allowed:** 100% legally clear, procedurally synthesized or open-source CC0/MIT/Unity standard licenses. Zero pirated or unauthorized third-party content.
- **Original IP:** Character designs for the Monkey protagonist, Guardian forms, and Act Bosses are completely original forest-guardian entities, avoiding copyright infringement.
- **URP Lit Shading:** Fully compatible with Universal Render Pipeline Forward+ rendering path and Single-Pass Instanced draw calls.
- **Mobile Budgets:** Low draw call counts (<80 per frame), low poly budgets per entity (<2,500 tris for heroes, <1,200 tris for enemies/wildlife), and uncompressed 16-bit 22kHz audio.

---

## 📦 Required Asset Catalog

### 1. Characters & Evolution Forms
| Entity Name | Poly Count Target | Texture / Material | Required Animations | Source & License |
| :--- | :--- | :--- | :--- | :--- |
| **Base Young Monkey** | ~1,450 tris | `Mat_MonkeyFur`, `Mat_MonkeySkin` | Idle, Walk, Run, Jump, Fall, Land, Attack, Heavy Attack, Hurt, Death, Climb, Victory | Procedural Low-Poly (Original IP / CC0) |
| **Guardian Monkey** | ~1,850 tris | `Mat_MonkeyFur`, `Mat_GuardianGold` | Idle, Walk, Run, Jump, Attack, Smash, Energy Blast, Hurt, Death | Procedural Low-Poly (Original IP / CC0) |
| **Primal Titan Gorilla** | ~2,400 tris | `Mat_TitanFur`, `Mat_MonkeySkin` | Heavy Idle, Stomp Walk, Charge Run, Ground Slam, Roar, Hurt, Death | Procedural Low-Poly (Original IP / CC0) |
| **Divine Forest Guardian** | ~2,100 tris | `Mat_MonkeyFur`, `Mat_DivineCyan` | Floating Idle, Glide, Celestial Halo Orbit, Divine Blast, Invulnerable Aura | Procedural Low-Poly (Original IP / CC0) |

---

### 2. Boss Encounters (Acts 1 to 5)
| Boss Name | Level / Act | Poly Count | Gameplay Script | Visual Characteristics |
| :--- | :--- | :--- | :--- | :--- |
| **Alpha Jaguar** | Level 10 (Act 1) | ~1,800 tris | [`AlphaJaguarBoss.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/Bosses/AlphaJaguarBoss.cs) | Muscular feline predator, glowing amber back-stripes, fangs, pounce attack. |
| **Ancient Stone Golem** | Level 20 (Act 2) | ~2,600 tris | [`StoneGolemBoss.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/Bosses/StoneGolemBoss.cs) | Segmented basalt monolith, moss shoulder boulders, glowing cyan rune core. |
| **River Serpent** | Level 30 (Act 3) | ~2,200 tris | [`RiverSerpentBoss.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/Bosses/RiverSerpentBoss.cs) | Multi-segmented aquatic leviathan, radiant fin crests, tidal wave attacks. |
| **Shadow Beast** | Level 40 (Act 4) | ~1,950 tris | [`ShadowBeastBoss.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/Bosses/ShadowBeastBoss.cs) | Phantom quadruped predator shrouded in dark purple ether and shadowy horns. |
| **Final Corruptor** | Level 50 (Act 5) | ~3,200 tris | [`FinalBossCorruptor.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/Bosses/FinalBossCorruptor.cs) | Floating titan entity with crystalline spires, void tentacles, corrupted energy eye. |

---

### 3. Normal Enemies & Ambient Wildlife
| Category | Asset Name | Poly Count | AI Controller | Behavior |
| :--- | :--- | :--- | :--- | :--- |
| **Enemy** | Jungle Predator | ~950 tris | [`EnemyAI.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/AI/EnemyAI.cs) | Waypoint patrol, line-of-sight chase, melee bite. |
| **Enemy** | Wild Boar | ~1,100 tris | [`EnemyAI.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/AI/EnemyAI.cs) | Stomp patrol, aggressive tusk charge attack. |
| **Enemy** | Toxic Spitting Reptile | ~850 tris | [`EnemyAI.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/AI/EnemyAI.cs) | Frilled neck intimidation, ranged toxic projectile. |
| **Wildlife** | Forest Stag / Deer | ~780 tris | [`WildlifeAI.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/AI/WildlifeAI.cs) | Peaceful grazing, wanders within home radius, flees on player approach. |
| **Wildlife** | Tropical Parrot / Toucan | ~320 tris | [`WildlifeAI.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/AI/WildlifeAI.cs) | Perched idle head-bobbing, flutter wings. |
| **Wildlife** | Jungle Tree Frog | ~240 tris | [`WildlifeAI.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/AI/WildlifeAI.cs) | Idle breathing, parabolic hop leaps across terrain. |
| **Wildlife** | Tropical Butterfly | ~110 tris | [`WildlifeAI.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/AI/WildlifeAI.cs) | Harmonic Lissajous curve hovering around flowers and foliage. |
| **Wildlife** | Small Marmoset Companion | ~650 tris | [`WildlifeAI.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/AI/WildlifeAI.cs) | Playful hops, climbing logs, chattering idle. |

---

### 4. Jungle Environment & Ruins
| Environment Asset | Folder Location | Material Binding | Collider Type |
| :--- | :--- | :--- | :--- |
| **Giant Jungle Canopy Tree** | `Assets/Art/Environment/Trees/` | `Mat_JungleWood`, `Mat_JungleLeaves` | Capsule Collider |
| **Coconut Palm Tree** | `Assets/Art/Environment/Trees/` | `Mat_JungleWood`, `Mat_PalmLeaves` | Capsule Collider |
| **Jungle Ferns & Tropical Bushes** | `Assets/Art/Environment/Plants/` | `Mat_JungleLeaves` | None (Foliage Deco) |
| **Bioluminescent Mushroom** | `Assets/Art/Environment/Plants/` | `Mat_JungleWood`, `Mat_DivineCyan` | Sphere Trigger (Toxic/Buff) |
| **Mossy Boulders & Cliffs** | `Assets/Art/Environment/Rocks/` | `Mat_MossyRock` | Mesh / Sphere / Box Collider |
| **Floating Island** | `Assets/Art/Environment/Rocks/` | `Mat_MossyRock` | Box Collider + [`FloatingIsland.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/Environment/FloatingIsland.cs) |
| **Ancient Inscribed Stone Arch** | `Assets/Art/Environment/Ruins/` | `Mat_AncientStone` | Box Colliders |
| **Rune Pedestal** | `Assets/Art/Environment/Ruins/` | `Mat_AncientStone`, `Mat_RuneActiveGlow` | Box Collider + [`RuneSwitch.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/Puzzles/RuneSwitch.cs) |
| **Heavy Stone Door** | `Assets/Art/Environment/Ruins/` | `Mat_AncientStone`, `Mat_RuneActiveGlow` | Box Collider + [`AncientDoor.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/Puzzles/AncientDoor.cs) |
| **Hollow Fallen Log** | `Assets/Art/Props/` | `Mat_JungleWood` | Capsule Collider |
| **Climbable Jungle Vine** | `Assets/Art/Props/` | `Mat_JungleLeaves` | Capsule Trigger + [`VineClimb.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/Mechanics/VineClimb.cs) |
| **Golden Banana & Ancient Coin** | `Assets/Art/Props/` | `Mat_GuardianGold` | Sphere Trigger + [`CollectibleItem.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/Collectibles/CollectibleItem.cs) |
| **Breakable Celestial Relic** | `Assets/Art/Props/` | `Mat_AncientStone`, `Mat_DivineCyan` | Box Collider + [`BreakableRelic.cs`](file:///d:/gemini%20AI/monkey%20adventure/Assets/Scripts/Combat/BreakableRelic.cs) |

---

### 5. Mobile Particle VFX (14 Prefabs)
* Particle budgets clamped to 15–50 particles per burst.
* GPU-friendly Billboard render modes with transparent additive blending.
* Prefab Catalog:
  - `VFX_EnergyBlast_Muzzle.prefab`
  - `VFX_Projectile_Trail.prefab`
  - `VFX_Impact_Sparks.prefab`
  - `VFX_GroundSmash_Shockwave.prefab`
  - `VFX_FireHazard_Flames.prefab`
  - `VFX_Poison_SporeCloud.prefab`
  - `VFX_Rune_ActivationGlow.prefab`
  - `VFX_AncientDoor_Magic.prefab`
  - `VFX_Checkpoint_Beam.prefab`
  - `VFX_Portal_Vortex.prefab`
  - `VFX_WaterSplash_Mist.prefab`
  - `VFX_Evolution_Transformation.prefab`
  - `VFX_Guardian_Aura.prefab`
  - `VFX_BossDeath_Burst.prefab`

---

### 6. Synthesized Audio Assets (26 WAV Clips)
* **6 BGM Tracks:** `BGM_Act1.wav` (Tropical Marimba), `BGM_Act2.wav` (Misty Bamboo Minor), `BGM_Act3.wav` (River Cascades), `BGM_Act4.wav` (Dark Forest Ambient), `BGM_Act5.wav` (Celestial Temple Tension), `BGM_Boss.wav` (Intense Boss Battle Drums).
* **20 SFX Clips:** `SFX_Jump`, `SFX_Land`, `SFX_Attack`, `SFX_HeavyAttack`, `SFX_EnergyBlast`, `SFX_Footstep`, `SFX_Coin`, `SFX_Banana`, `SFX_Hurt`, `SFX_Death`, `SFX_Checkpoint`, `SFX_RuneActivate`, `SFX_DoorOpen`, `SFX_LevelComplete`, `SFX_EnemyHit`, `SFX_BossRoar`, `SFX_UIClick`, `SFX_WaterSplash`, `SFX_FireCrackle`, `SFX_PoisonBubble`.
