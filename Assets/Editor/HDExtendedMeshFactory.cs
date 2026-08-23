using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Procedurally synthesizes high-detail 3D geometry, organic trunks, curved fronds,
    /// sculpted rock meshes, and ancient carved masonry for the HD Environment Pass.
    /// Saves optimized .asset meshes and sets up game-ready HD prefabs.
    /// </summary>
    public static class HDExtendedMeshFactory
    {
        public const string HD_ROOT = "Assets/Art/Environment/HD";
        public const string MESH_DIR = "Assets/Art/Environment/HD/Meshes";
        public const string TREE_DIR = "Assets/Art/Environment/HD/Trees";
        public const string ROCK_DIR = "Assets/Art/Environment/HD/Rocks";
        public const string PLANT_DIR = "Assets/Art/Environment/HD/Plants";
        public const string RUIN_DIR = "Assets/Art/Environment/HD/Ruins";

        public static void GenerateAllHDAssetsAndPrefabs()
        {
            HDPBRTextureFactory.GenerateAllHDPBRMaterials();

            GenerateHDTrees();
            GenerateHDRocks();
            GenerateHDPlants();
            GenerateHDRuins();

            AssetDatabase.SaveAssets();
            Debug.Log("[HDExtendedMeshFactory] All 23 HD Environment Assets & Prefabs successfully generated in Assets/Art/Environment/HD/!");
        }

        #region Tree Generation
        private static void GenerateHDTrees()
        {
            Material barkCanopy = HDPBRTextureFactory.GetMaterial("Mat_HD_Bark_Canopy");
            Material foliageCanopy = HDPBRTextureFactory.GetMaterial("Mat_HD_Foliage_Canopy");
            Material barkPalm = HDPBRTextureFactory.GetMaterial("Mat_HD_Bark_Palm");
            Material foliagePalm = HDPBRTextureFactory.GetMaterial("Mat_HD_Foliage_PalmFrond");

            // 1. Large Jungle Canopy Tree (HD_Tree_JungleCanopy_01)
            {
                GameObject root = new GameObject("HD_Tree_JungleCanopy_01");
                Mesh trunkMesh = CreateCurvedTrunkMesh("Mesh_HD_CanopyTrunk", 8.0f, 1.2f, 0.6f, 16, 12, 0.4f);
                SaveMeshAsset(trunkMesh, "Mesh_HD_CanopyTrunk");
                AttachMesh(root, trunkMesh, barkCanopy, "Trunk");

                // Foliage Clusters
                Mesh foliageCluster = CreateFoliageDomeMesh("Mesh_HD_CanopyDome", 4.5f, 2.8f, 12, 8);
                SaveMeshAsset(foliageCluster, "Mesh_HD_CanopyDome");

                GameObject c1 = AttachMesh(root, foliageCluster, foliageCanopy, "Foliage_Main");
                c1.transform.localPosition = new Vector3(0, 6.8f, 0);

                GameObject c2 = AttachMesh(root, foliageCluster, foliageCanopy, "Foliage_Left");
                c2.transform.localPosition = new Vector3(-2.2f, 5.8f, 0.8f);
                c2.transform.localScale = new Vector3(0.75f, 0.7f, 0.75f);

                GameObject c3 = AttachMesh(root, foliageCluster, foliageCanopy, "Foliage_Right");
                c3.transform.localPosition = new Vector3(2.0f, 6.0f, -0.6f);
                c3.transform.localScale = new Vector3(0.8f, 0.75f, 0.8f);

                SavePrefab(root, $"{TREE_DIR}/HD_Tree_JungleCanopy_01.prefab");
            }

            // 2. Coconut Palm (HD_Tree_CoconutPalm_01)
            {
                GameObject root = new GameObject("HD_Tree_CoconutPalm_01");
                Mesh palmTrunk = CreateCurvedTrunkMesh("Mesh_HD_PalmTrunk", 6.5f, 0.55f, 0.35f, 12, 16, 0.75f);
                SaveMeshAsset(palmTrunk, "Mesh_HD_PalmTrunk");
                AttachMesh(root, palmTrunk, barkPalm, "Trunk");

                Mesh frondMesh = CreateCurvedFrondMesh("Mesh_HD_PalmFrond", 3.2f, 0.7f, 8, 4);
                SaveMeshAsset(frondMesh, "Mesh_HD_PalmFrond");

                GameObject crown = new GameObject("Palm_Crown");
                crown.transform.SetParent(root.transform);
                crown.transform.localPosition = new Vector3(0.75f, 6.2f, 0);

                int frondCount = 10;
                for (int i = 0; i < frondCount; i++)
                {
                    float angle = i * (360f / frondCount);
                    GameObject f = AttachMesh(crown, frondMesh, foliagePalm, $"Frond_{i}");
                    f.transform.localRotation = Quaternion.Euler(20f + (i % 2) * 12f, angle, 0);
                }

                SavePrefab(root, $"{TREE_DIR}/HD_Tree_CoconutPalm_01.prefab");
            }

            // 3. Medium Tropical Tree (HD_Tree_TropicalMedium_01)
            {
                GameObject root = new GameObject("HD_Tree_TropicalMedium_01");
                Mesh medTrunk = CreateCurvedTrunkMesh("Mesh_HD_MedTrunk", 5.5f, 0.8f, 0.45f, 12, 10, 0.3f);
                SaveMeshAsset(medTrunk, "Mesh_HD_MedTrunk");
                AttachMesh(root, medTrunk, barkCanopy, "Trunk");

                Mesh medFoliage = CreateFoliageDomeMesh("Mesh_HD_MedFoliage", 3.2f, 2.2f, 10, 6);
                SaveMeshAsset(medFoliage, "Mesh_HD_MedFoliage");

                GameObject f1 = AttachMesh(root, medFoliage, foliageCanopy, "Foliage_Center");
                f1.transform.localPosition = new Vector3(0, 4.8f, 0);

                GameObject f2 = AttachMesh(root, medFoliage, foliageCanopy, "Foliage_Side");
                f2.transform.localPosition = new Vector3(1.2f, 4.2f, 0.4f);
                f2.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                SavePrefab(root, $"{TREE_DIR}/HD_Tree_TropicalMedium_01.prefab");
            }

            // 4. Small Tropical Tree (HD_Tree_TropicalSmall_01)
            {
                GameObject root = new GameObject("HD_Tree_TropicalSmall_01");
                Mesh smallTrunk = CreateCurvedTrunkMesh("Mesh_HD_SmallTrunk", 3.6f, 0.5f, 0.28f, 10, 8, 0.2f);
                SaveMeshAsset(smallTrunk, "Mesh_HD_SmallTrunk");
                AttachMesh(root, smallTrunk, barkCanopy, "Trunk");

                Mesh smallFoliage = CreateFoliageDomeMesh("Mesh_HD_SmallFoliage", 2.2f, 1.6f, 8, 5);
                SaveMeshAsset(smallFoliage, "Mesh_HD_SmallFoliage");

                GameObject f = AttachMesh(root, smallFoliage, foliageCanopy, "Foliage");
                f.transform.localPosition = new Vector3(0, 3.2f, 0);

                SavePrefab(root, $"{TREE_DIR}/HD_Tree_TropicalSmall_01.prefab");
            }

            // 5. Fallen Tree / Log (HD_Tree_FallenLog_01)
            {
                GameObject root = new GameObject("HD_Tree_FallenLog_01");
                Mesh logMesh = CreateHollowLogMesh("Mesh_HD_FallenLog", 4.8f, 0.65f, 0.45f, 14, 8);
                SaveMeshAsset(logMesh, "Mesh_HD_FallenLog");
                AttachMesh(root, logMesh, barkCanopy, "Log_Mesh");
                SavePrefab(root, $"{TREE_DIR}/HD_Tree_FallenLog_01.prefab");
            }
        }
        #endregion

        #region Rock Generation
        private static void GenerateHDRocks()
        {
            Material rockGranite = HDPBRTextureFactory.GetMaterial("Mat_HD_Rock_MossyGranite");
            Material rockCliff = HDPBRTextureFactory.GetMaterial("Mat_HD_Rock_CliffBasalt");

            // 1. Large Mossy Boulder (HD_Rock_MossyBoulder_01)
            {
                GameObject root = new GameObject("HD_Rock_MossyBoulder_01");
                Mesh boulderMesh = CreateSculptedRockMesh("Mesh_HD_MossyBoulder", new Vector3(2.2f, 1.8f, 2.0f), 12, 10, 0.35f);
                SaveMeshAsset(boulderMesh, "Mesh_HD_MossyBoulder");
                AttachMesh(root, boulderMesh, rockGranite, "Rock_Mesh");
                SavePrefab(root, $"{ROCK_DIR}/HD_Rock_MossyBoulder_01.prefab");
            }

            // 2. Medium Mossy Rock (HD_Rock_MossyMedium_01)
            {
                GameObject root = new GameObject("HD_Rock_MossyMedium_01");
                Mesh medMesh = CreateSculptedRockMesh("Mesh_HD_MossyMed", new Vector3(1.3f, 1.0f, 1.2f), 10, 8, 0.28f);
                SaveMeshAsset(medMesh, "Mesh_HD_MossyMed");
                AttachMesh(root, medMesh, rockGranite, "Rock_Mesh");
                SavePrefab(root, $"{ROCK_DIR}/HD_Rock_MossyMedium_01.prefab");
            }

            // 3. Small Rock Cluster (HD_Rock_ClusterSmall_01)
            {
                GameObject root = new GameObject("HD_Rock_ClusterSmall_01");
                Mesh clusterMesh = CreateRockClusterMesh("Mesh_HD_RockCluster", 4, 1.4f);
                SaveMeshAsset(clusterMesh, "Mesh_HD_RockCluster");
                AttachMesh(root, clusterMesh, rockGranite, "Cluster_Mesh");
                SavePrefab(root, $"{ROCK_DIR}/HD_Rock_ClusterSmall_01.prefab");
            }

            // 4. Cliff Rock (HD_Rock_Cliff_01)
            {
                GameObject root = new GameObject("HD_Rock_Cliff_01");
                Mesh cliffMesh = CreateCliffFaceMesh("Mesh_HD_CliffFace", 5.0f, 6.0f, 3.0f, 10, 12);
                SaveMeshAsset(cliffMesh, "Mesh_HD_CliffFace");
                AttachMesh(root, cliffMesh, rockCliff, "Cliff_Mesh");
                SavePrefab(root, $"{ROCK_DIR}/HD_Rock_Cliff_01.prefab");
            }

            // 5. Broken Stone / Rock Formation (HD_Rock_BrokenFormation_01)
            {
                GameObject root = new GameObject("HD_Rock_BrokenFormation_01");
                Mesh brokenMesh = CreateSculptedRockMesh("Mesh_HD_BrokenFormation", new Vector3(3.2f, 2.4f, 2.6f), 12, 10, 0.45f);
                SaveMeshAsset(brokenMesh, "Mesh_HD_BrokenFormation");
                AttachMesh(root, brokenMesh, rockCliff, "Formation_Mesh");
                SavePrefab(root, $"{ROCK_DIR}/HD_Rock_BrokenFormation_01.prefab");
            }
        }
        #endregion

        #region Plant Generation
        private static void GenerateHDPlants()
        {
            Material fernMat = HDPBRTextureFactory.GetMaterial("Mat_HD_Foliage_Fern");
            Material broadLeafMat = HDPBRTextureFactory.GetMaterial("Mat_HD_Foliage_BroadLeaf");
            Material bushMat = HDPBRTextureFactory.GetMaterial("Mat_HD_Foliage_Canopy");
            Material flowerMat = HDPBRTextureFactory.GetMaterial("Mat_HD_Foliage_Flowers");

            // 1. Tropical Fern (HD_Plant_JungleFern_01)
            {
                GameObject root = new GameObject("HD_Plant_JungleFern_01");
                Mesh fernFrond = CreateCurvedFrondMesh("Mesh_HD_FernFrond", 1.4f, 0.45f, 6, 3);
                SaveMeshAsset(fernFrond, "Mesh_HD_FernFrond");

                int frondCount = 8;
                for (int i = 0; i < frondCount; i++)
                {
                    float angle = i * (360f / frondCount);
                    GameObject f = AttachMesh(root, fernFrond, fernMat, $"Frond_{i}");
                    f.transform.localRotation = Quaternion.Euler(-30f + (i % 2) * 8f, angle, 0);
                    f.transform.localScale = Vector3.one * (0.85f + (i % 3) * 0.1f);
                }
                SavePrefab(root, $"{PLANT_DIR}/HD_Plant_JungleFern_01.prefab");
            }

            // 2. Broad-leaf Jungle Plant (HD_Plant_BroadLeaf_01)
            {
                GameObject root = new GameObject("HD_Plant_BroadLeaf_01");
                Mesh broadLeafMesh = CreateBroadLeafBladeMesh("Mesh_HD_BroadLeafBlade", 1.6f, 0.7f, 6);
                SaveMeshAsset(broadLeafMesh, "Mesh_HD_BroadLeafBlade");

                int leafCount = 6;
                for (int i = 0; i < leafCount; i++)
                {
                    float angle = i * (360f / leafCount);
                    GameObject l = AttachMesh(root, broadLeafMesh, broadLeafMat, $"Leaf_{i}");
                    l.transform.localRotation = Quaternion.Euler(-25f + (i % 2) * 10f, angle, 0);
                }
                SavePrefab(root, $"{PLANT_DIR}/HD_Plant_BroadLeaf_01.prefab");
            }

            // 3. Tropical Bush (HD_Plant_TropicalBush_01)
            {
                GameObject root = new GameObject("HD_Plant_TropicalBush_01");
                Mesh bushMesh = CreateFoliageDomeMesh("Mesh_HD_TropicalBush", 1.8f, 1.3f, 8, 6);
                SaveMeshAsset(bushMesh, "Mesh_HD_TropicalBush");
                AttachMesh(root, bushMesh, bushMat, "Bush_Mesh");
                SavePrefab(root, $"{PLANT_DIR}/HD_Plant_TropicalBush_01.prefab");
            }

            // 4. Ground Cover (HD_Plant_GroundCover_01)
            {
                GameObject root = new GameObject("HD_Plant_GroundCover_01");
                Mesh groundCoverMesh = CreateGroundCoverPatchMesh("Mesh_HD_GroundCover", 1.5f, 8);
                SaveMeshAsset(groundCoverMesh, "Mesh_HD_GroundCover");
                AttachMesh(root, groundCoverMesh, fernMat, "GroundCover_Mesh");
                SavePrefab(root, $"{PLANT_DIR}/HD_Plant_GroundCover_01.prefab");
            }

            // 5. Large Leaf Plant (HD_Plant_LargeLeaf_01)
            {
                GameObject root = new GameObject("HD_Plant_LargeLeaf_01");
                Mesh bigLeafMesh = CreateBroadLeafBladeMesh("Mesh_HD_LargeLeafBlade", 2.2f, 0.95f, 8);
                SaveMeshAsset(bigLeafMesh, "Mesh_HD_LargeLeafBlade");

                int count = 5;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * (360f / count);
                    GameObject l = AttachMesh(root, bigLeafMesh, broadLeafMat, $"BigLeaf_{i}");
                    l.transform.localRotation = Quaternion.Euler(-18f + (i % 2) * 10f, angle, 0);
                    l.transform.localScale = Vector3.one * (0.9f + (i % 2) * 0.2f);
                }
                SavePrefab(root, $"{PLANT_DIR}/HD_Plant_LargeLeaf_01.prefab");
            }

            // 6. Hanging / Vine Plant (HD_Plant_HangingVine_01)
            {
                GameObject root = new GameObject("HD_Plant_HangingVine_01");
                Mesh vineMesh = CreateHangingVineMesh("Mesh_HD_HangingVine", 3.0f, 0.12f, 16);
                SaveMeshAsset(vineMesh, "Mesh_HD_HangingVine");
                AttachMesh(root, vineMesh, broadLeafMat, "Vine_Mesh");
                SavePrefab(root, $"{PLANT_DIR}/HD_Plant_HangingVine_01.prefab");
            }

            // 7. Small Flowering Jungle Plant (HD_Plant_FloweringBush_01)
            {
                GameObject root = new GameObject("HD_Plant_FloweringBush_01");
                Mesh bushMesh = CreateFoliageDomeMesh("Mesh_HD_FlowerBushBase", 1.4f, 1.0f, 8, 6);
                SaveMeshAsset(bushMesh, "Mesh_HD_FlowerBushBase");
                AttachMesh(root, bushMesh, bushMat, "Bush_Base");

                Mesh flowerMesh = CreateFlowerPetalsMesh("Mesh_HD_FlowerPetals", 0.35f);
                SaveMeshAsset(flowerMesh, "Mesh_HD_FlowerPetals");

                Vector3[] flowerOffsets = {
                    new Vector3(0, 0.95f, 0.1f),
                    new Vector3(0.45f, 0.8f, 0.25f),
                    new Vector3(-0.4f, 0.75f, -0.2f),
                    new Vector3(0.2f, 0.85f, -0.4f)
                };

                for (int i = 0; i < flowerOffsets.Length; i++)
                {
                    GameObject fl = AttachMesh(root, flowerMesh, flowerMat, $"Flower_{i}");
                    fl.transform.localPosition = flowerOffsets[i];
                    fl.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-20f, 20f), UnityEngine.Random.Range(0f, 360f), 0);
                }

                SavePrefab(root, $"{PLANT_DIR}/HD_Plant_FloweringBush_01.prefab");
            }
        }
        #endregion

        #region Ruins Generation
        private static void GenerateHDRuins()
        {
            Material masonryMat = HDPBRTextureFactory.GetMaterial("Mat_HD_Ruin_AncientMasonry");
            Material runeMat = HDPBRTextureFactory.GetMaterial("Mat_HD_Ruin_RuneGoldCyan");

            // 1. Ancient Stone Arch (HD_Ruin_AncientArch_01)
            {
                GameObject root = new GameObject("HD_Ruin_AncientArch_01");

                Mesh pillarMesh = CreateFlutedPillarMesh("Mesh_HD_ArchPillar", 5.2f, 0.75f, 8);
                SaveMeshAsset(pillarMesh, "Mesh_HD_ArchPillar");

                Mesh lintelMesh = CreateCarvedLintelMesh("Mesh_HD_ArchLintel", 5.6f, 0.8f, 1.0f);
                SaveMeshAsset(lintelMesh, "Mesh_HD_ArchLintel");

                GameObject pL = AttachMesh(root, pillarMesh, masonryMat, "Pillar_Left");
                pL.transform.localPosition = new Vector3(-2.2f, 0, 0);

                GameObject pR = AttachMesh(root, pillarMesh, masonryMat, "Pillar_Right");
                pR.transform.localPosition = new Vector3(2.2f, 0, 0);

                GameObject lintel = AttachMesh(root, lintelMesh, masonryMat, "Lintel");
                lintel.transform.localPosition = new Vector3(0, 5.2f, 0);

                SavePrefab(root, $"{RUIN_DIR}/HD_Ruin_AncientArch_01.prefab");
            }

            // 2. Ancient Stone Pillar (HD_Ruin_AncientPillar_01)
            {
                GameObject root = new GameObject("HD_Ruin_AncientPillar_01");
                Mesh pillarMesh = CreateFlutedPillarMesh("Mesh_HD_SoloPillar", 4.5f, 0.7f, 8);
                SaveMeshAsset(pillarMesh, "Mesh_HD_SoloPillar");
                AttachMesh(root, pillarMesh, masonryMat, "Pillar_Mesh");
                SavePrefab(root, $"{RUIN_DIR}/HD_Ruin_AncientPillar_01.prefab");
            }

            // 3. Broken Stone Wall (HD_Ruin_BrokenWall_01)
            {
                GameObject root = new GameObject("HD_Ruin_BrokenWall_01");
                Mesh wallMesh = CreateBrokenMasonryWallMesh("Mesh_HD_BrokenWall", 4.5f, 2.8f, 0.8f);
                SaveMeshAsset(wallMesh, "Mesh_HD_BrokenWall");
                AttachMesh(root, wallMesh, masonryMat, "Wall_Mesh");
                SavePrefab(root, $"{RUIN_DIR}/HD_Ruin_BrokenWall_01.prefab");
            }

            // 4. Rune / Stone Pedestal (HD_Ruin_RunePedestal_01)
            {
                GameObject root = new GameObject("HD_Ruin_RunePedestal_01");
                Mesh pedestalMesh = CreateRunePedestalMesh("Mesh_HD_RunePedestal", 1.1f, 0.95f);
                SaveMeshAsset(pedestalMesh, "Mesh_HD_RunePedestal");
                AttachMesh(root, pedestalMesh, masonryMat, "Pedestal_Base");

                Mesh gemMesh = CreateSculptedRockMesh("Mesh_HD_RuneGem", new Vector3(0.45f, 0.45f, 0.45f), 8, 6, 0.1f);
                SaveMeshAsset(gemMesh, "Mesh_HD_RuneGem");
                GameObject gem = AttachMesh(root, gemMesh, runeMat, "RuneGem");
                gem.transform.localPosition = new Vector3(0, 1.15f, 0);

                SavePrefab(root, $"{RUIN_DIR}/HD_Ruin_RunePedestal_01.prefab");
            }

            // 5. Moss-covered Ruin Pieces (HD_Ruin_MossyPiece_01)
            {
                GameObject root = new GameObject("HD_Ruin_MossyPiece_01");
                Mesh pieceMesh = CreateSculptedRockMesh("Mesh_HD_RuinPiece", new Vector3(1.8f, 1.2f, 1.4f), 10, 8, 0.3f);
                SaveMeshAsset(pieceMesh, "Mesh_HD_RuinPiece");
                AttachMesh(root, pieceMesh, masonryMat, "Ruin_Piece");
                SavePrefab(root, $"{RUIN_DIR}/HD_Ruin_MossyPiece_01.prefab");
            }

            // 6. Small Stone Debris (HD_Ruin_StoneDebris_01)
            {
                GameObject root = new GameObject("HD_Ruin_StoneDebris_01");
                Mesh debrisMesh = CreateRockClusterMesh("Mesh_HD_StoneDebris", 5, 1.2f);
                SaveMeshAsset(debrisMesh, "Mesh_HD_StoneDebris");
                AttachMesh(root, debrisMesh, masonryMat, "Debris_Mesh");
                SavePrefab(root, $"{RUIN_DIR}/HD_Ruin_StoneDebris_01.prefab");
            }
        }
        #endregion

        #region Mesh Synthesizer Helpers
        private static Mesh CreateCurvedTrunkMesh(string name, float height, float baseRadius, float topRadius, int radialSegments, int heightSegments, float curveAmount)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int y = 0; y <= heightSegments; y++)
            {
                float v = (float)y / heightSegments;
                float currentY = v * height;
                float currentRadius = Mathf.Lerp(baseRadius, topRadius, v);

                // Natural organic curve displacement
                float curveX = Mathf.Sin(v * Mathf.PI * 0.8f) * curveAmount;
                float curveZ = Mathf.Cos(v * Mathf.PI * 0.5f) * (curveAmount * 0.4f);

                // Flared base roots for natural jungle trunk
                if (v < 0.25f)
                {
                    float flare = (1.0f - v / 0.25f) * (baseRadius * 0.6f);
                    currentRadius += flare;
                }

                for (int x = 0; x <= radialSegments; x++)
                {
                    float u = (float)x / radialSegments;
                    float angle = u * Mathf.PI * 2f;

                    // Organic trunk noise
                    float noise = Mathf.PerlinNoise(Mathf.Cos(angle) * 2f, v * 4f) * (currentRadius * 0.15f);
                    float r = currentRadius + noise;

                    float vx = Mathf.Cos(angle) * r + curveX;
                    float vz = Mathf.Sin(angle) * r + curveZ;

                    verts.Add(new Vector3(vx, currentY, vz));
                    uvs.Add(new Vector2(u * 2f, v * (height * 0.5f)));
                }
            }

            for (int y = 0; y < heightSegments; y++)
            {
                for (int x = 0; x < radialSegments; x++)
                {
                    int curr = y * (radialSegments + 1) + x;
                    int next = curr + radialSegments + 1;

                    tris.Add(curr);
                    tris.Add(next);
                    tris.Add(curr + 1);

                    tris.Add(curr + 1);
                    tris.Add(next);
                    tris.Add(next + 1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateFoliageDomeMesh(string name, float width, float height, int radialSegments, int latSegments)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int lat = 0; lat <= latSegments; lat++)
            {
                float v = (float)lat / latSegments;
                float phi = v * (Mathf.PI * 0.5f); // Hemisphere dome
                float y = Mathf.Sin(phi) * height;
                float r = Mathf.Cos(phi) * (width * 0.5f);

                for (int lon = 0; lon <= radialSegments; lon++)
                {
                    float u = (float)lon / radialSegments;
                    float theta = u * Mathf.PI * 2f;

                    // Organic clump displacement
                    float clumpNoise = Mathf.PerlinNoise(Mathf.Cos(theta) * 3f, Mathf.Sin(phi) * 3f) * 0.35f;
                    float finalR = r * (1.0f + clumpNoise);

                    float x = Mathf.Cos(theta) * finalR;
                    float z = Mathf.Sin(theta) * finalR;

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(u * 3f, v * 2f));
                }
            }

            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < radialSegments; lon++)
                {
                    int curr = lat * (radialSegments + 1) + lon;
                    int next = curr + radialSegments + 1;

                    tris.Add(curr);
                    tris.Add(curr + 1);
                    tris.Add(next);

                    tris.Add(curr + 1);
                    tris.Add(next + 1);
                    tris.Add(next);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateCurvedFrondMesh(string name, float length, float maxWidth, int lengthSegments, int widthSegments)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int z = 0; z <= lengthSegments; z++)
            {
                float v = (float)z / lengthSegments;
                float currentZ = v * length;

                // Natural arching drape curve
                float currentY = -Mathf.Pow(v, 2.2f) * (length * 0.45f);

                // Leaf profile shape: narrow at stem, wide in middle, pointed at tip
                float w = Mathf.Sin(v * Mathf.PI) * maxWidth;

                for (int x = 0; x <= widthSegments; x++)
                {
                    float u = (float)x / widthSegments;
                    float currentX = (u - 0.5f) * w;

                    // Leaf center crease
                    float creaseY = -Mathf.Abs(u - 0.5f) * (w * 0.25f);

                    verts.Add(new Vector3(currentX, currentY + creaseY, currentZ));
                    uvs.Add(new Vector2(u, v));
                }
            }

            for (int z = 0; z < lengthSegments; z++)
            {
                for (int x = 0; x < widthSegments; x++)
                {
                    int curr = z * (widthSegments + 1) + x;
                    int next = curr + widthSegments + 1;

                    // Double-sided triangles for foliage
                    tris.Add(curr);
                    tris.Add(next);
                    tris.Add(curr + 1);

                    tris.Add(curr + 1);
                    tris.Add(next);
                    tris.Add(next + 1);

                    tris.Add(curr);
                    tris.Add(curr + 1);
                    tris.Add(next);

                    tris.Add(curr + 1);
                    tris.Add(next + 1);
                    tris.Add(next);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateBroadLeafBladeMesh(string name, float length, float width, int segments)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int i = 0; i <= segments; i++)
            {
                float v = (float)i / segments;
                float z = v * length;
                float y = -Mathf.Pow(v, 1.8f) * (length * 0.35f);
                float w = Mathf.Sin(v * Mathf.PI * 0.9f) * width;

                // Center vein
                verts.Add(new Vector3(0, y, z));
                uvs.Add(new Vector2(0.5f, v));

                // Left edge
                verts.Add(new Vector3(-w * 0.5f, y - w * 0.15f, z));
                uvs.Add(new Vector2(0f, v));

                // Right edge
                verts.Add(new Vector3(w * 0.5f, y - w * 0.15f, z));
                uvs.Add(new Vector2(1f, v));
            }

            for (int i = 0; i < segments; i++)
            {
                int c0 = i * 3;
                int cL = c0 + 1;
                int cR = c0 + 2;

                int n0 = (i + 1) * 3;
                int nL = n0 + 1;
                int nR = n0 + 2;

                // Left side
                tris.Add(c0);
                tris.Add(cL);
                tris.Add(n0);

                tris.Add(cL);
                tris.Add(nL);
                tris.Add(n0);

                // Right side
                tris.Add(c0);
                tris.Add(n0);
                tris.Add(cR);

                tris.Add(cR);
                tris.Add(n0);
                tris.Add(nR);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateHollowLogMesh(string name, float length, float radius, float innerRadius, int radialSegments, int lengthSegments)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int z = 0; z <= lengthSegments; z++)
            {
                float v = (float)z / lengthSegments;
                float curZ = (v - 0.5f) * length;

                for (int x = 0; x <= radialSegments; x++)
                {
                    float u = (float)x / radialSegments;
                    float angle = u * Mathf.PI * 2f;

                    float noise = Mathf.PerlinNoise(Mathf.Cos(angle) * 3f, v * 5f) * 0.08f;
                    float r = radius + noise;

                    float vx = Mathf.Cos(angle) * r;
                    float vy = Mathf.Sin(angle) * r + radius; // rests on ground

                    verts.Add(new Vector3(vx, vy, curZ));
                    uvs.Add(new Vector2(u * 2f, v * 3f));
                }
            }

            for (int z = 0; z < lengthSegments; z++)
            {
                for (int x = 0; x < radialSegments; x++)
                {
                    int curr = z * (radialSegments + 1) + x;
                    int next = curr + radialSegments + 1;

                    tris.Add(curr);
                    tris.Add(next);
                    tris.Add(curr + 1);

                    tris.Add(curr + 1);
                    tris.Add(next);
                    tris.Add(next + 1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateSculptedRockMesh(string name, Vector3 bounds, int latSegments, int lonSegments, float roughness)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int lat = 0; lat <= latSegments; lat++)
            {
                float v = (float)lat / latSegments;
                float phi = (v - 0.5f) * Mathf.PI; // -PI/2 to PI/2

                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    float u = (float)lon / lonSegments;
                    float theta = u * Mathf.PI * 2f;

                    Vector3 unitDir = new Vector3(
                        Mathf.Cos(phi) * Mathf.Cos(theta),
                        Mathf.Sin(phi),
                        Mathf.Cos(phi) * Mathf.Sin(theta)
                    );

                    // Multi-frequency rock fractal displacement
                    float n1 = Mathf.PerlinNoise(unitDir.x * 2.5f + 1.2f, unitDir.y * 2.5f + 3.4f) * roughness;
                    float n2 = Mathf.PerlinNoise(unitDir.x * 6.0f, unitDir.z * 6.0f) * (roughness * 0.4f);
                    float disp = 1.0f + (n1 + n2 - roughness * 0.7f);

                    Vector3 pos = Vector3.Scale(unitDir * disp, bounds * 0.5f);
                    // Rest base near zero
                    pos.y += bounds.y * 0.4f;

                    verts.Add(pos);
                    uvs.Add(new Vector2(u * 2f, v * 2f));
                }
            }

            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int curr = lat * (lonSegments + 1) + lon;
                    int next = curr + lonSegments + 1;

                    tris.Add(curr);
                    tris.Add(next);
                    tris.Add(curr + 1);

                    tris.Add(curr + 1);
                    tris.Add(next);
                    tris.Add(next + 1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateRockClusterMesh(string name, int rockCount, float spread)
        {
            CombineInstance[] combine = new CombineInstance[rockCount];
            for (int i = 0; i < rockCount; i++)
            {
                float size = 0.35f + (i % 3) * 0.2f;
                Mesh subRock = CreateSculptedRockMesh($"SubRock_{i}", Vector3.one * size, 6, 6, 0.35f);

                float angle = (float)i / rockCount * Mathf.PI * 2f;
                float dist = (float)i / rockCount * (spread * 0.45f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);

                combine[i].mesh = subRock;
                combine[i].transform = Matrix4x4.TRS(pos, Quaternion.Euler(i * 35f, i * 50f, 0), Vector3.one);
            }

            Mesh clusterMesh = new Mesh { name = name };
            clusterMesh.CombineMeshes(combine, true, true);
            clusterMesh.RecalculateNormals();
            clusterMesh.RecalculateTangents();
            clusterMesh.RecalculateBounds();
            return clusterMesh;
        }

        private static Mesh CreateCliffFaceMesh(string name, float width, float height, float depth, int segX, int segY)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int y = 0; y <= segY; y++)
            {
                float v = (float)y / segY;
                float curY = v * height;

                for (int x = 0; x <= segX; x++)
                {
                    float u = (float)x / segX;
                    float curX = (u - 0.5f) * width;

                    // Stratified vertical fault displacement
                    float strata = Mathf.Sin(v * 16f) * 0.25f;
                    float cracks = Mathf.PerlinNoise(u * 5f, v * 5f) * (depth * 0.45f);
                    float curZ = strata + cracks;

                    verts.Add(new Vector3(curX, curY, curZ));
                    uvs.Add(new Vector2(u * 3f, v * 4f));
                }
            }

            for (int y = 0; y < segY; y++)
            {
                for (int x = 0; x < segX; x++)
                {
                    int curr = y * (segX + 1) + x;
                    int next = curr + segX + 1;

                    tris.Add(curr);
                    tris.Add(next);
                    tris.Add(curr + 1);

                    tris.Add(curr + 1);
                    tris.Add(next);
                    tris.Add(next + 1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateGroundCoverPatchMesh(string name, float radius, int leafCount)
        {
            CombineInstance[] combine = new CombineInstance[leafCount];
            for (int i = 0; i < leafCount; i++)
            {
                Mesh leaf = CreateBroadLeafBladeMesh($"GroundLeaf_{i}", 0.6f, 0.35f, 4);
                float angle = (float)i / leafCount * 360f;
                float dist = UnityEngine.Random.Range(0.2f, radius * 0.5f);
                Vector3 pos = Quaternion.Euler(0, angle, 0) * new Vector3(dist, 0.05f, 0);

                combine[i].mesh = leaf;
                combine[i].transform = Matrix4x4.TRS(pos, Quaternion.Euler(-15f, angle, 0), Vector3.one * UnityEngine.Random.Range(0.7f, 1.2f));
            }

            Mesh patch = new Mesh { name = name };
            patch.CombineMeshes(combine, true, true);
            patch.RecalculateNormals();
            patch.RecalculateTangents();
            patch.RecalculateBounds();
            return patch;
        }

        private static Mesh CreateHangingVineMesh(string name, float length, float radius, int segments)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int radialSegs = 6;
            for (int i = 0; i <= segments; i++)
            {
                float v = (float)i / segments;
                float curY = -v * length;

                // Twisted winding spiral
                float spiralX = Mathf.Sin(v * Mathf.PI * 4f) * (radius * 1.5f);
                float spiralZ = Mathf.Cos(v * Mathf.PI * 4f) * (radius * 1.5f);

                for (int j = 0; j <= radialSegs; j++)
                {
                    float u = (float)j / radialSegs;
                    float angle = u * Mathf.PI * 2f;

                    float vx = Mathf.Cos(angle) * radius + spiralX;
                    float vz = Mathf.Sin(angle) * radius + spiralZ;

                    verts.Add(new Vector3(vx, curY, vz));
                    uvs.Add(new Vector2(u, v * 6f));
                }
            }

            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < radialSegs; j++)
                {
                    int curr = i * (radialSegs + 1) + j;
                    int next = curr + radialSegs + 1;

                    tris.Add(curr);
                    tris.Add(next);
                    tris.Add(curr + 1);

                    tris.Add(curr + 1);
                    tris.Add(next);
                    tris.Add(next + 1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateFlowerPetalsMesh(string name, float radius)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int petals = 5;
            verts.Add(new Vector3(0, 0.05f, 0)); // Center
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i < petals; i++)
            {
                float a1 = (float)i / petals * Mathf.PI * 2f;
                float a2 = ((float)i + 0.5f) / petals * Mathf.PI * 2f;
                float a3 = ((float)i + 1.0f) / petals * Mathf.PI * 2f;

                Vector3 tip = new Vector3(Mathf.Cos(a2) * radius, -0.05f, Mathf.Sin(a2) * radius);
                Vector3 edgeL = new Vector3(Mathf.Cos(a1) * (radius * 0.4f), 0, Mathf.Sin(a1) * (radius * 0.4f));
                Vector3 edgeR = new Vector3(Mathf.Cos(a3) * (radius * 0.4f), 0, Mathf.Sin(a3) * (radius * 0.4f));

                int idx = verts.Count;
                verts.Add(edgeL);
                verts.Add(tip);
                verts.Add(edgeR);

                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0.5f, 1f));
                uvs.Add(new Vector2(1f, 0f));

                tris.Add(0);
                tris.Add(idx);
                tris.Add(idx + 1);

                tris.Add(0);
                tris.Add(idx + 1);
                tris.Add(idx + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateFlutedPillarMesh(string name, float height, float radius, int flutes)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int radialSegs = flutes * 4;
            int heightSegs = 10;

            for (int y = 0; y <= heightSegs; y++)
            {
                float v = (float)y / heightSegs;
                float curY = v * height;

                // Base and Capital plinths
                float r = radius;
                if (v < 0.12f || v > 0.88f)
                {
                    r = radius * 1.35f;
                }

                for (int x = 0; x <= radialSegs; x++)
                {
                    float u = (float)x / radialSegs;
                    float angle = u * Mathf.PI * 2f;

                    // Classical architectural fluting indentations
                    float flute = (v >= 0.12f && v <= 0.88f) ? Mathf.Sin(u * Mathf.PI * 2f * flutes) * 0.04f : 0f;
                    float curRadius = r - flute;

                    float vx = Mathf.Cos(angle) * curRadius;
                    float vz = Mathf.Sin(angle) * curRadius;

                    verts.Add(new Vector3(vx, curY, vz));
                    uvs.Add(new Vector2(u * 2f, v * 4f));
                }
            }

            for (int y = 0; y < heightSegs; y++)
            {
                for (int x = 0; x < radialSegs; x++)
                {
                    int curr = y * (radialSegs + 1) + x;
                    int next = curr + radialSegs + 1;

                    tris.Add(curr);
                    tris.Add(next);
                    tris.Add(curr + 1);

                    tris.Add(curr + 1);
                    tris.Add(next);
                    tris.Add(next + 1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateCarvedLintelMesh(string name, float length, float height, float depth)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int segX = 12;
            int segY = 4;

            for (int y = 0; y <= segY; y++)
            {
                float v = (float)y / segY;
                float curY = (v - 0.5f) * height;

                for (int x = 0; x <= segX; x++)
                {
                    float u = (float)x / segX;
                    float curX = (u - 0.5f) * length;

                    // Front carved face with stepped moulding
                    float curZ = depth * 0.5f - Mathf.Abs(Mathf.Sin(u * 10f * Mathf.PI)) * 0.05f;

                    verts.Add(new Vector3(curX, curY, curZ));
                    uvs.Add(new Vector2(u * 3f, v));
                }
            }

            for (int y = 0; y < segY; y++)
            {
                for (int x = 0; x < segX; x++)
                {
                    int curr = y * (segX + 1) + x;
                    int next = curr + segX + 1;

                    tris.Add(curr);
                    tris.Add(next);
                    tris.Add(curr + 1);

                    tris.Add(curr + 1);
                    tris.Add(next);
                    tris.Add(next + 1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateBrokenMasonryWallMesh(string name, float length, float height, float thickness)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int segX = 10;
            int segY = 8;

            for (int y = 0; y <= segY; y++)
            {
                float v = (float)y / segY;
                float curY = v * height;

                for (int x = 0; x <= segX; x++)
                {
                    float u = (float)x / segX;
                    float curX = (u - 0.5f) * length;

                    // Broken step decay on one end
                    float decay = (u > 0.6f) ? (u - 0.6f) * (height * 0.8f) : 0f;
                    float adjustedY = Mathf.Max(0, curY - decay);

                    float blockDisplacement = Mathf.Sin(u * 16f) * 0.04f;
                    float curZ = thickness * 0.5f + blockDisplacement;

                    verts.Add(new Vector3(curX, adjustedY, curZ));
                    uvs.Add(new Vector2(u * 2f, v * 2f));
                }
            }

            for (int y = 0; y < segY; y++)
            {
                for (int x = 0; x < segX; x++)
                {
                    int curr = y * (segX + 1) + x;
                    int next = curr + segX + 1;

                    tris.Add(curr);
                    tris.Add(next);
                    tris.Add(curr + 1);

                    tris.Add(curr + 1);
                    tris.Add(next);
                    tris.Add(next + 1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateRunePedestalMesh(string name, float height, float radius)
        {
            Mesh mesh = new Mesh { name = name };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int sides = 8; // Octagonal ceremonial pedestal
            int layers = 5;

            for (int y = 0; y <= layers; y++)
            {
                float v = (float)y / layers;
                float curY = v * height;

                // Stepped tier altar radius
                float r = radius;
                if (v < 0.25f) r = radius * 1.3f;
                else if (v > 0.8f) r = radius * 1.15f;

                for (int s = 0; s <= sides; s++)
                {
                    float u = (float)s / sides;
                    float angle = u * Mathf.PI * 2f;

                    float vx = Mathf.Cos(angle) * r;
                    float vz = Mathf.Sin(angle) * r;

                    verts.Add(new Vector3(vx, curY, vz));
                    uvs.Add(new Vector2(u * 2f, v));
                }
            }

            for (int y = 0; y < layers; y++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int curr = y * (sides + 1) + s;
                    int next = curr + sides + 1;

                    tris.Add(curr);
                    tris.Add(next);
                    tris.Add(curr + 1);

                    tris.Add(curr + 1);
                    tris.Add(next);
                    tris.Add(next + 1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
        #endregion

        #region Prefab & Asset Utilities
        private static GameObject AttachMesh(GameObject parent, Mesh mesh, Material mat, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
            return go;
        }

        private static void SaveMeshAsset(Mesh mesh, string name)
        {
            string path = $"{MESH_DIR}/{name}.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                EditorUtility.CopySerialized(mesh, existing);
            }
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
        }
        #endregion
    }
}
