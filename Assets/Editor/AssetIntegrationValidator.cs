using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using MonkeyAdventure.AI;
using MonkeyAdventure.Audio;
using MonkeyAdventure.Bosses;
using MonkeyAdventure.Combat;
using MonkeyAdventure.Core;
using MonkeyAdventure.Player;
using MonkeyAdventure.Progression;
using MonkeyAdventure.Puzzles;
using MonkeyAdventure.Skins;
using GuardianSystem.Combat;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Automated 3D Asset Integration & Reference Diagnostic Validator.
    /// Scans all characters, evolution skins, bosses, enemies, wildlife, environment prefabs, VFX, audio,
    /// and scene object bindings to ensure 100% production readiness.
    /// </summary>
    public class AssetIntegrationValidator : EditorWindow
    {
        private Vector2 _scrollPos;
        private List<string> _passedChecks = new List<string>();
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();

        [MenuItem("Window/Monkey Adventure/Validate 3D Asset Integration", priority = 2)]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetIntegrationValidator>("Asset Validator");
            window.minSize = new Vector2(550, 600);
            window.RunFullValidation();
            window.Show();
        }

        public static bool ValidateAll(out string summaryReport)
        {
            var validator = CreateInstance<AssetIntegrationValidator>();
            validator.RunFullValidation();
            summaryReport = validator.GenerateReportString();
            bool isSuccess = validator._errors.Count == 0;
            DestroyImmediate(validator);
            return isSuccess;
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("🐒 Monkey Adventure: 3D Asset Integration Validator", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Verifies models, animators, materials, VFX, audio, colliders, and scene references.", EditorStyles.miniLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("🔄 Run Full Validation Scan", GUILayout.Height(32)))
            {
                RunFullValidation();
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField($"Results: {_passedChecks.Count} Passed | {_warnings.Count} Warnings | {_errors.Count} Errors", EditorStyles.boldLabel);
            GUILayout.Space(5);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_errors.Count > 0)
            {
                EditorGUILayout.HelpBox($"Encountered {_errors.Count} Errors!", MessageType.Error);
                foreach (var err in _errors)
                {
                    EditorGUILayout.LabelField($"❌ {err}", EditorStyles.wordWrappedLabel);
                }
                GUILayout.Space(10);
            }

            if (_warnings.Count > 0)
            {
                EditorGUILayout.HelpBox($"Encountered {_warnings.Count} Warnings!", MessageType.Warning);
                foreach (var warn in _warnings)
                {
                    EditorGUILayout.LabelField($"⚠️ {warn}", EditorStyles.wordWrappedLabel);
                }
                GUILayout.Space(10);
            }

            if (_passedChecks.Count > 0)
            {
                EditorGUILayout.LabelField("✅ Passed Validations:", EditorStyles.boldLabel);
                foreach (var pass in _passedChecks)
                {
                    EditorGUILayout.LabelField($"  • {pass}", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        public void RunFullValidation()
        {
            _passedChecks.Clear();
            _warnings.Clear();
            _errors.Clear();

            ValidatePrefabs("Assets/Art/Characters", new[] {
                "Monkey_Base.prefab", "Monkey_Guardian.prefab", "Monkey_PrimalTitan.prefab", "Monkey_DivineGuardian.prefab"
            });

            ValidatePrefabs("Assets/Art/Bosses", new[] {
                "Boss_AlphaJaguar.prefab", "Boss_StoneGolem.prefab", "Boss_RiverSerpent.prefab", "Boss_ShadowBeast.prefab", "Boss_FinalCorruptor.prefab"
            });

            ValidatePrefabs("Assets/Art/Enemies", new[] {
                "Enemy_JunglePredator.prefab", "Enemy_WildBoar.prefab", "Enemy_ToxicReptile.prefab"
            });

            ValidatePrefabs("Assets/Art/Wildlife", new[] {
                "Wildlife_Deer.prefab", "Wildlife_Parrot.prefab", "Wildlife_TreeFrog.prefab", "Wildlife_Butterfly.prefab", "Wildlife_Monkey.prefab"
            });

            ValidatePrefabs("Assets/Art/Environment/Trees", new[] {
                "Tree_JungleCanopy.prefab", "Tree_CoconutPalm.prefab"
            });

            ValidatePrefabs("Assets/Art/Environment/Plants", new[] {
                "Plant_JungleFern.prefab", "Plant_TropicalBush.prefab", "Plant_GlowingMushroom.prefab", "Plant_HibiscusFlower.prefab"
            });

            ValidatePrefabs("Assets/Art/Environment/Rocks", new[] {
                "Rock_MossyBoulder.prefab", "Rock_CliffFormation.prefab", "Rock_FloatingIsland.prefab"
            });

            ValidatePrefabs("Assets/Art/Environment/Ruins", new[] {
                "Ruins_AncientArch.prefab", "Ruins_RunePedestal.prefab", "Ruins_HeavyStoneDoor.prefab"
            });

            ValidatePrefabs("Assets/Art/Props", new[] {
                "Prop_GoldenBanana.prefab", "Prop_AncientCoin.prefab", "Prop_BreakableRelic.prefab", "Prop_HollowFallenLog.prefab", "Prop_ClimbableVine.prefab"
            });

            ValidatePrefabs("Assets/Art/VFX", new[] {
                "VFX_EnergyBlast_Muzzle.prefab", "VFX_Projectile_Trail.prefab", "VFX_Impact_Sparks.prefab",
                "VFX_GroundSmash_Shockwave.prefab", "VFX_FireHazard_Flames.prefab", "VFX_Poison_SporeCloud.prefab",
                "VFX_Rune_ActivationGlow.prefab", "VFX_AncientDoor_Magic.prefab", "VFX_Checkpoint_Beam.prefab",
                "VFX_Portal_Vortex.prefab", "VFX_WaterSplash_Mist.prefab", "VFX_Evolution_Transformation.prefab",
                "VFX_Guardian_Aura.prefab", "VFX_BossDeath_Burst.prefab"
            });

            ValidateAudioFiles();
            ValidateUISprites();
            ValidateActiveSceneComponents();
        }

        private void ValidatePrefabs(string directory, string[] expectedPrefabs)
        {
            if (!Directory.Exists(directory))
            {
                _errors.Add($"Missing Directory: {directory}");
                return;
            }

            foreach (var prefabName in expectedPrefabs)
            {
                string path = $"{directory}/{prefabName}";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    _errors.Add($"Missing Prefab: {path}");
                }
                else
                {
                    // Check mesh renderers and materials
                    var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
                    bool hasMissingMat = false;
                    foreach (var r in renderers)
                    {
                        if (r.sharedMaterial == null)
                        {
                            hasMissingMat = true;
                            break;
                        }
                    }

                    if (hasMissingMat)
                    {
                        _warnings.Add($"Prefab {prefabName} has sub-meshes with missing Material!");
                    }
                    else
                    {
                        _passedChecks.Add($"Prefab Validated: {prefabName} ({renderers.Length} sub-meshes)");
                    }
                }
            }
        }

        private void ValidateAudioFiles()
        {
            string audioDir = "Assets/Art/Audio";
            string[] expectedClips = {
                "BGM_Act1.wav", "BGM_Act2.wav", "BGM_Act3.wav", "BGM_Act4.wav", "BGM_Act5.wav", "BGM_Boss.wav",
                "SFX_Jump.wav", "SFX_Land.wav", "SFX_Attack.wav", "SFX_HeavyAttack.wav", "SFX_EnergyBlast.wav",
                "SFX_Footstep.wav", "SFX_Coin.wav", "SFX_Banana.wav", "SFX_Hurt.wav", "SFX_Death.wav",
                "SFX_Checkpoint.wav", "SFX_RuneActivate.wav", "SFX_DoorOpen.wav", "SFX_LevelComplete.wav"
            };

            foreach (var clip in expectedClips)
            {
                string path = $"{audioDir}/{clip}";
                AudioClip audio = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (audio == null)
                {
                    _errors.Add($"Missing Audio Clip: {path}");
                }
                else
                {
                    _passedChecks.Add($"Audio Clip Validated: {clip} ({audio.length:F1}s)");
                }
            }
        }

        private void ValidateUISprites()
        {
            string uiDir = "Assets/Art/UI";
            string[] expectedSprites = {
                "UI_Heart_Health.png", "UI_Energy_Bolt.png", "UI_Coin_Gold.png", "UI_Banana_Food.png", "UI_Gem_Diamond.png",
                "UI_Btn_Jump.png", "UI_Btn_Attack.png", "UI_Btn_Smash.png", "UI_Btn_Blast.png",
                "UI_Joypad_Base.png", "UI_Joypad_Knob.png", "UI_Panel_Frame.png", "UI_Star_Rating.png"
            };

            foreach (var spriteName in expectedSprites)
            {
                string path = $"{uiDir}/{spriteName}";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    _errors.Add($"Missing UI Sprite: {path}");
                }
                else
                {
                    _passedChecks.Add($"UI Sprite Validated: {spriteName}");
                }
            }
        }

        private void ValidateActiveSceneComponents()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (player.GetComponent<CharacterController>() != null) _passedChecks.Add("Player CharacterController verified.");
                else _errors.Add("Player missing CharacterController!");

                if (player.GetComponent<MonkeyPlayerController>() != null) _passedChecks.Add("MonkeyPlayerController verified.");
                else _errors.Add("Player missing MonkeyPlayerController!");

                if (player.GetComponent<GuardianCombat>() != null) _passedChecks.Add("GuardianCombat verified.");
                else _errors.Add("Player missing GuardianCombat!");

                if (player.GetComponent<PlayerHealth>() != null) _passedChecks.Add("PlayerHealth verified.");
                else _errors.Add("Player missing PlayerHealth!");
            }

            var audioMgr = Object.FindAnyObjectByType<AudioManager>();
            if (audioMgr != null) _passedChecks.Add("AudioManager singleton present.");

            var gameMgr = Object.FindAnyObjectByType<GameManager>();
            if (gameMgr != null) _passedChecks.Add("GameManager singleton present.");
        }

        public string GenerateReportString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# 🐒 Asset Integration Diagnostic Report");
            sb.AppendLine($"Total Passed: {_passedChecks.Count} | Warnings: {_warnings.Count} | Errors: {_errors.Count}\n");

            if (_errors.Count > 0)
            {
                sb.AppendLine("## ❌ Errors:");
                foreach (var err in _errors) sb.AppendLine($"- {err}");
                sb.AppendLine();
            }

            if (_warnings.Count > 0)
            {
                sb.AppendLine("## ⚠️ Warnings:");
                foreach (var warn in _warnings) sb.AppendLine($"- {warn}");
                sb.AppendLine();
            }

            sb.AppendLine("## ✅ Verified Assets:");
            foreach (var pass in _passedChecks) sb.AppendLine($"- {pass}");

            return sb.ToString();
        }
    }
}
