using System.IO;
using UnityEditor;
using UnityEngine;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Generates stylized, crisp 2D UI sprites (Hearts, Energy Bolts, Coins, Bananas, Gems, Buttons, Joysticks)
    /// and saves PNG textures under Assets/Art/UI/.
    /// Configures TextureImporter settings as Sprite (2D and UI).
    /// </summary>
    public static class ProceduralTextureFactory
    {
        private const string UI_DIR = "Assets/Art/UI";

        public static void GenerateAllUISprites()
        {
            EnsureDirectoryExists(UI_DIR);

            CreateHeartSprite();
            CreateEnergyBoltSprite();
            CreateCoinSprite();
            CreateBananaSprite();
            CreateGemSprite();
            CreateActionBtnSprite("UI_Btn_Jump", new Color(0.2f, 0.7f, 1.0f));
            CreateActionBtnSprite("UI_Btn_Attack", new Color(1.0f, 0.4f, 0.2f));
            CreateActionBtnSprite("UI_Btn_Smash", new Color(0.9f, 0.2f, 0.3f));
            CreateActionBtnSprite("UI_Btn_Blast", new Color(0.3f, 0.9f, 0.7f));
            CreateJoypadBaseSprite();
            CreateJoypadKnobSprite();
            CreatePanelFrameSprite();
            CreateStarRatingSprite();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProceduralTextureFactory] All UI PNG Sprites generated and configured in Assets/Art/UI/!");
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static void CreateHeartSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color heartColor = new Color(1.0f, 0.22f, 0.35f, 1.0f);
            Color highlight = new Color(1.0f, 0.6f, 0.7f, 1.0f);
            Color border = new Color(0.6f, 0.05f, 0.15f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - size * 0.5f) / (size * 0.45f);
                    float ny = (y - size * 0.45f) / (size * 0.45f);

                    // Heart equation: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
                    float f = Mathf.Pow(nx * nx + ny * ny - 1f, 3) - nx * nx * Mathf.Pow(ny, 3);

                    if (f <= 0.05f)
                    {
                        if (f > -0.05f) tex.SetPixel(x, y, border);
                        else if (nx < -0.2f && ny > 0.2f) tex.SetPixel(x, y, highlight);
                        else tex.SetPixel(x, y, heartColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, $"{UI_DIR}/UI_Heart_Health.png");
        }

        private static void CreateEnergyBoltSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color boltColor = new Color(0.15f, 0.85f, 1.0f, 1.0f);
            Color border = new Color(0.05f, 0.4f, 0.7f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size;
                    float ny = (float)y / size;

                    // Lightning bolt zigzag polygon
                    bool inUpper = (nx >= 0.45f - ny * 0.3f && nx <= 0.75f - ny * 0.2f && ny >= 0.45f);
                    bool inLower = (nx >= 0.25f - (ny - 0.5f) * 0.3f && nx <= 0.55f - (ny - 0.5f) * 0.2f && ny < 0.55f);

                    if (inUpper || inLower)
                    {
                        tex.SetPixel(x, y, boltColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, $"{UI_DIR}/UI_Energy_Bolt.png");
        }

        private static void CreateCoinSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color gold = new Color(1.0f, 0.84f, 0.0f, 1.0f);
            Color goldDark = new Color(0.85f, 0.65f, 0.0f, 1.0f);
            Color shine = new Color(1.0f, 0.96f, 0.6f, 1.0f);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.44f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        if (dist > radius - 6f) tex.SetPixel(x, y, goldDark);
                        else if (dist < radius * 0.65f && Mathf.Abs(x - center.x) < 4f) tex.SetPixel(x, y, goldDark);
                        else if (x < center.x - 10f && y > center.y + 10f) tex.SetPixel(x, y, shine);
                        else tex.SetPixel(x, y, gold);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, $"{UI_DIR}/UI_Coin_Gold.png");
        }

        private static void CreateBananaSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color yellow = new Color(1.0f, 0.9f, 0.1f, 1.0f);
            Color brown = new Color(0.45f, 0.25f, 0.05f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - size * 0.5f) / (size * 0.4f);
                    float ny = (y - size * 0.5f) / (size * 0.4f);

                    // Crescent curve
                    float curve = ny - (nx * nx * 0.4f - 0.2f);
                    if (Mathf.Abs(curve) < 0.22f && nx > -0.8f && nx < 0.8f)
                    {
                        if (nx > 0.7f || nx < -0.7f) tex.SetPixel(x, y, brown);
                        else tex.SetPixel(x, y, yellow);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, $"{UI_DIR}/UI_Banana_Food.png");
        }

        private static void CreateGemSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color cyan = new Color(0.1f, 0.95f, 0.85f, 1.0f);
            Color darkCyan = new Color(0.05f, 0.6f, 0.55f, 1.0f);
            Color lightCyan = new Color(0.7f, 1.0f, 0.95f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs((x - size * 0.5f) / (size * 0.4f));
                    float ny = (y - size * 0.5f) / (size * 0.4f);

                    // Diamond shape: upper trap + lower triangle
                    bool inUpper = (ny >= 0 && ny <= 0.6f && nx <= 0.8f - ny * 0.5f);
                    bool inLower = (ny < 0 && ny >= -0.8f && nx <= 0.8f + ny);

                    if (inUpper || inLower)
                    {
                        if (x < size * 0.45f && ny > 0.1f) tex.SetPixel(x, y, lightCyan);
                        else if (y < size * 0.35f) tex.SetPixel(x, y, darkCyan);
                        else tex.SetPixel(x, y, cyan);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, $"{UI_DIR}/UI_Gem_Diamond.png");
        }

        private static void CreateActionBtnSprite(string name, Color baseColor)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color darkBorder = baseColor * 0.6f;
            darkBorder.a = 1.0f;
            Color innerHighlight = Color.Lerp(baseColor, Color.white, 0.3f);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        if (dist > radius - 6f) tex.SetPixel(x, y, darkBorder);
                        else if (dist < radius * 0.75f && y > center.y) tex.SetPixel(x, y, innerHighlight);
                        else tex.SetPixel(x, y, baseColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, $"{UI_DIR}/{name}.png");
        }

        private static void CreateJoypadBaseSprite()
        {
            int size = 192;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color baseColor = new Color(0.1f, 0.15f, 0.2f, 0.55f);
            Color rim = new Color(0.3f, 0.45f, 0.6f, 0.8f);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        if (dist > radius - 5f) tex.SetPixel(x, y, rim);
                        else tex.SetPixel(x, y, baseColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, $"{UI_DIR}/UI_Joypad_Base.png");
        }

        private static void CreateJoypadKnobSprite()
        {
            int size = 96;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color knobColor = new Color(0.25f, 0.7f, 0.95f, 0.85f);
            Color centerDot = new Color(0.9f, 0.95f, 1.0f, 0.95f);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.44f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        if (dist < 12f) tex.SetPixel(x, y, centerDot);
                        else tex.SetPixel(x, y, knobColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, $"{UI_DIR}/UI_Joypad_Knob.png");
        }

        private static void CreatePanelFrameSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color bg = new Color(0.12f, 0.16f, 0.22f, 0.92f);
            Color border = new Color(0.85f, 0.7f, 0.3f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < 6 || x >= size - 6 || y < 6 || y >= size - 6)
                    {
                        tex.SetPixel(x, y, border);
                    }
                    else
                    {
                        tex.SetPixel(x, y, bg);
                    }
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, $"{UI_DIR}/UI_Panel_Frame.png");
        }

        private static void CreateStarRatingSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color gold = new Color(1.0f, 0.85f, 0.1f, 1.0f);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x - center.x, y - center.y);
                    float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    float dist = p.magnitude;
                    // 5-point star radius modulation
                    float starRadius = (size * 0.38f) * (0.6f + 0.4f * Mathf.Cos(angle * 5f * Mathf.Deg2Rad));

                    if (dist <= starRadius)
                    {
                        tex.SetPixel(x, y, gold);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, $"{UI_DIR}/UI_Star_Rating.png");
        }

        private static void SaveTextureAndSetSprite(Texture2D tex, string filePath)
        {
            byte[] pngData = tex.EncodeToPNG();
            File.WriteAllBytes(filePath, pngData);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }
    }
}
