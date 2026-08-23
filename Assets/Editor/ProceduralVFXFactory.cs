using System.IO;
using UnityEditor;
using UnityEngine;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Generates stylized, mobile-optimized Particle System prefabs for combat, hazards, puzzles, and environment.
    /// Saves prefabs under Assets/Art/VFX/.
    /// </summary>
    public static class ProceduralVFXFactory
    {
        private const string VFX_DIR = "Assets/Art/VFX";

        public static void GenerateAllVFXPrefabs()
        {
            EnsureDirectoryExists(VFX_DIR);

            CreateEnergyBlastMuzzle();
            CreateProjectileTrail();
            CreateImpactSparks();
            CreateGroundSmashShockwave();
            CreateFireHazardFlames();
            CreatePoisonSporeCloud();
            CreateRuneActivationGlow();
            CreateAncientDoorMagic();
            CreateCheckpointBeam();
            CreatePortalVortex();
            CreateWaterSplashMist();
            CreateEvolutionTransformation();
            CreateGuardianAura();
            CreateBossDeathBurst();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProceduralVFXFactory] All 14 Mobile Particle VFX Prefabs generated successfully in Assets/Art/VFX/!");
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static void CreateEnergyBlastMuzzle()
        {
            GameObject go = new GameObject("VFX_EnergyBlast_Muzzle");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.25f;
            main.startSpeed = 5.0f;
            main.startSize = 0.4f;
            main.startColor = new Color(0.2f, 0.85f, 1.0f, 0.9f);
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_EnergyBlast_Muzzle.prefab");
        }

        private static void CreateProjectileTrail()
        {
            GameObject go = new GameObject("VFX_Projectile_Trail");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.0f;
            main.loop = true;
            main.startLifetime = 0.35f;
            main.startSpeed = 0.2f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.1f, 0.9f, 1.0f, 0.7f);

            var emission = ps.emission;
            emission.rateOverTime = 25;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_Projectile_Trail.prefab");
        }

        private static void CreateImpactSparks()
        {
            GameObject go = new GameObject("VFX_Impact_Sparks");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.startLifetime = 0.3f;
            main.startSpeed = 8.0f;
            main.startSize = 0.2f;
            main.startColor = new Color(1.0f, 0.85f, 0.2f, 1.0f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.1f;

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_Impact_Sparks.prefab");
        }

        private static void CreateGroundSmashShockwave()
        {
            GameObject go = new GameObject("VFX_GroundSmash_Shockwave");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.8f;
            main.loop = false;
            main.startLifetime = 0.4f;
            main.startSpeed = 10.0f;
            main.startSize = 0.45f;
            main.startColor = new Color(1.0f, 0.6f, 0.1f, 0.9f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 35) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.8f;
            shape.rotation = new Vector3(90, 0, 0);

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_GroundSmash_Shockwave.prefab");
        }

        private static void CreateFireHazardFlames()
        {
            GameObject go = new GameObject("VFX_FireHazard_Flames");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2.0f;
            main.loop = true;
            main.startLifetime = 0.8f;
            main.startSpeed = 2.5f;
            main.startSize = 0.6f;
            main.startColor = new Color(1.0f, 0.4f, 0.05f, 0.85f);

            var emission = ps.emission;
            emission.rateOverTime = 20;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.4f;
            shape.rotation = new Vector3(-90, 0, 0);

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_FireHazard_Flames.prefab");
        }

        private static void CreatePoisonSporeCloud()
        {
            GameObject go = new GameObject("VFX_Poison_SporeCloud");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 3.0f;
            main.loop = true;
            main.startLifetime = 1.6f;
            main.startSpeed = 0.8f;
            main.startSize = 0.7f;
            main.startColor = new Color(0.4f, 0.95f, 0.15f, 0.5f);

            var emission = ps.emission;
            emission.rateOverTime = 16;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.2f;

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_Poison_SporeCloud.prefab");
        }

        private static void CreateRuneActivationGlow()
        {
            GameObject go = new GameObject("VFX_Rune_ActivationGlow");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.0f;
            main.loop = false;
            main.startLifetime = 0.8f;
            main.startSpeed = 3.0f;
            main.startSize = 0.35f;
            main.startColor = new Color(0.2f, 1.0f, 0.8f, 0.95f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.6f;
            shape.rotation = new Vector3(90, 0, 0);

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_Rune_ActivationGlow.prefab");
        }

        private static void CreateAncientDoorMagic()
        {
            GameObject go = new GameObject("VFX_AncientDoor_Magic");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2.0f;
            main.loop = false;
            main.startLifetime = 1.2f;
            main.startSpeed = 2.0f;
            main.startSize = 0.5f;
            main.startColor = new Color(0.9f, 0.7f, 0.1f, 0.8f);

            var emission = ps.emission;
            emission.rateOverTime = 30;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(3f, 4f, 0.5f);

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_AncientDoor_Magic.prefab");
        }

        private static void CreateCheckpointBeam()
        {
            GameObject go = new GameObject("VFX_Checkpoint_Beam");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2.0f;
            main.loop = true;
            main.startLifetime = 1.5f;
            main.startSpeed = 4.0f;
            main.startSize = 0.35f;
            main.startColor = new Color(0.1f, 0.9f, 0.3f, 0.75f);

            var emission = ps.emission;
            emission.rateOverTime = 18;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.4f;
            shape.rotation = new Vector3(-90, 0, 0);

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_Checkpoint_Beam.prefab");
        }

        private static void CreatePortalVortex()
        {
            GameObject go = new GameObject("VFX_Portal_Vortex");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2.0f;
            main.loop = true;
            main.startLifetime = 1.2f;
            main.startSpeed = -1.5f; // Sucks inward
            main.startSize = 0.4f;
            main.startColor = new Color(0.3f, 0.85f, 1.0f, 0.8f);

            var emission = ps.emission;
            emission.rateOverTime = 25;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.5f;

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_Portal_Vortex.prefab");
        }

        private static void CreateWaterSplashMist()
        {
            GameObject go = new GameObject("VFX_WaterSplash_Mist");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.5f;
            main.loop = true;
            main.startLifetime = 1.0f;
            main.startSpeed = 2.5f;
            main.startSize = 0.6f;
            main.startColor = new Color(0.8f, 0.95f, 1.0f, 0.4f);

            var emission = ps.emission;
            emission.rateOverTime = 20;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.6f;

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_WaterSplash_Mist.prefab");
        }

        private static void CreateEvolutionTransformation()
        {
            GameObject go = new GameObject("VFX_Evolution_Transformation");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.5f;
            main.loop = false;
            main.startLifetime = 1.0f;
            main.startSpeed = 6.0f;
            main.startSize = 0.5f;
            main.startColor = new Color(1.0f, 0.85f, 0.1f, 0.95f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f;

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_Evolution_Transformation.prefab");
        }

        private static void CreateGuardianAura()
        {
            GameObject go = new GameObject("VFX_Guardian_Aura");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2.0f;
            main.loop = true;
            main.startLifetime = 1.0f;
            main.startSpeed = 1.2f;
            main.startSize = 0.35f;
            main.startColor = new Color(0.95f, 0.8f, 0.2f, 0.6f);

            var emission = ps.emission;
            emission.rateOverTime = 15;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.6f;
            shape.rotation = new Vector3(-90, 0, 0);

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_Guardian_Aura.prefab");
        }

        private static void CreateBossDeathBurst()
        {
            GameObject go = new GameObject("VFX_BossDeath_Burst");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2.0f;
            main.loop = false;
            main.startLifetime = 1.5f;
            main.startSpeed = 12.0f;
            main.startSize = 0.7f;
            main.startColor = new Color(0.9f, 0.2f, 0.1f, 0.95f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 60) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.2f;

            SaveAndDestroyPrefab(go, $"{VFX_DIR}/VFX_BossDeath_Burst.prefab");
        }

        private static void SaveAndDestroyPrefab(GameObject go, string prefabPath)
        {
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }
    }
}
