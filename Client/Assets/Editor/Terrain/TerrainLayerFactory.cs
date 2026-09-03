using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    /// <summary>
    /// Order corresponds to splat channel index:
    /// 0 = Desert, 1 = Grass, 2 = Mud, 3 = Asphalt.
    /// </summary>
    public static class TerrainLayerFactory
    {
        public const string LayerFolder = "Assets/BundleRes/Terrain/Layer";
        const int TexSize = 512;

        public static TerrainLayer[] EnsureAll(bool forceRegen)
        {
            Directory.CreateDirectory(LayerFolder);

            var desertTex = EnsureTexture("Terrain_Desert_D.png", BakeDesert, forceRegen);
            var grassTex = EnsureTexture("Terrain_Grass_D.png", BakeGrass, forceRegen);
            var mudTex = EnsureTexture("Terrain_Mud_D.png", BakeMud, forceRegen);
            var asphaltTex = EnsureTexture("Terrain_Asphalt_D.png", BakeAsphalt, forceRegen);

            var desertLayer = EnsureLayer("Terrain_Desert.terrainlayer", desertTex, new Vector2(15f, 15f), 0.02f, 0f, forceRegen);
            var grassLayer = EnsureLayer("Terrain_Grass.terrainlayer", grassTex, new Vector2(12f, 12f), 0.04f, 0f, forceRegen);
            var mudLayer = EnsureLayer("Terrain_Mud.terrainlayer", mudTex, new Vector2(6f, 6f), 0.22f, 0f, forceRegen);
            var asphaltLayer = EnsureLayer("Terrain_Asphalt.terrainlayer", asphaltTex, new Vector2(6f, 6f), 0.35f, 0f, forceRegen);

            return new[] { desertLayer, grassLayer, mudLayer, asphaltLayer };
        }

        static Texture2D EnsureTexture(string fileName, System.Action<Color[]> baker, bool forceRegen)
        {
            string path = $"{LayerFolder}/{fileName}";
            if (!forceRegen && File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (existing != null) return existing;
            }

            var pixels = new Color[TexSize * TexSize];
            baker(pixels);
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, true, false);
            tex.SetPixels(pixels);
            tex.Apply(true, false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Default;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.filterMode = FilterMode.Trilinear;
                imp.anisoLevel = 8;
                imp.sRGBTexture = true;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static TerrainLayer EnsureLayer(string fileName, Texture2D tex, Vector2 tile, float smoothness, float metallic, bool forceRegen)
        {
            string path = $"{LayerFolder}/{fileName}";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture = tex;
            layer.tileSize = tile;
            layer.tileOffset = Vector2.zero;
            layer.metallic = metallic;
            layer.smoothness = smoothness;
            layer.specular = Color.gray * 0.15f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        // ---- Procedural bakers ----

        static void BakeDesert(Color[] px)
        {
            var rng = new System.Random(9001);
            Vector2 o = new Vector2((float)rng.NextDouble() * 1000, (float)rng.NextDouble() * 1000);
            Color warm = new Color(0.86f, 0.72f, 0.44f);
            Color dark = new Color(0.62f, 0.48f, 0.24f);
            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                {
                    float n = Mathf.PerlinNoise((x + o.x) * 0.025f, (y + o.y) * 0.025f);
                    float g = Mathf.PerlinNoise((x + o.x) * 0.18f, (y + o.y) * 0.18f);
                    float t = Mathf.Clamp01(n * 0.6f + g * 0.4f);
                    var c = Color.Lerp(dark, warm, t);
                    c.r += ((float)rng.NextDouble() - 0.5f) * 0.03f;
                    c.g += ((float)rng.NextDouble() - 0.5f) * 0.02f;
                    c.a = 1f;
                    px[y * TexSize + x] = c;
                }
        }

        static void BakeGrass(Color[] px)
        {
            var rng = new System.Random(9002);
            Vector2 o = new Vector2((float)rng.NextDouble() * 1000, (float)rng.NextDouble() * 1000);
            Color darkG = new Color(0.16f, 0.30f, 0.10f);
            Color midG = new Color(0.30f, 0.48f, 0.16f);
            Color lightG = new Color(0.42f, 0.58f, 0.20f);
            Color brown = new Color(0.42f, 0.34f, 0.18f);
            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                {
                    float n1 = Mathf.PerlinNoise((x + o.x) * 0.03f, (y + o.y) * 0.03f);
                    float n2 = Mathf.PerlinNoise((x + o.x) * 0.11f, (y + o.y) * 0.11f);
                    float g = Mathf.Clamp01(n1 * 0.6f + n2 * 0.4f);
                    var c = Color.Lerp(darkG, midG, g);
                    c = Color.Lerp(c, lightG, Mathf.Pow(g, 3f));
                    float patch = Mathf.PerlinNoise((x + o.x + 200) * 0.008f, (y + o.y + 200) * 0.008f);
                    if (patch > 0.72f) c = Color.Lerp(c, brown, (patch - 0.72f) * 3f);
                    c.a = 1f;
                    px[y * TexSize + x] = c;
                }
        }

        static void BakeMud(Color[] px)
        {
            var rng = new System.Random(9003);
            Vector2 o = new Vector2((float)rng.NextDouble() * 1000, (float)rng.NextDouble() * 1000);
            Color deep = new Color(0.16f, 0.11f, 0.07f);
            Color mid = new Color(0.28f, 0.20f, 0.13f);
            Color wet = new Color(0.09f, 0.07f, 0.06f);
            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                {
                    float n = Mathf.PerlinNoise((x + o.x) * 0.04f, (y + o.y) * 0.04f);
                    float ripple = Mathf.PerlinNoise((x + o.x) * 0.22f, (y + o.y) * 0.22f);
                    var c = Color.Lerp(deep, mid, n);
                    if (ripple > 0.62f) c = Color.Lerp(c, wet, (ripple - 0.62f) * 2.6f);
                    c.r += ((float)rng.NextDouble() - 0.5f) * 0.02f;
                    c.a = 1f;
                    px[y * TexSize + x] = c;
                }
        }

        static void BakeAsphalt(Color[] px)
        {
            var rng = new System.Random(9004);
            // Voronoi pebble points
            const int cells = 32;
            var pts = new Vector2[cells * cells];
            for (int cy = 0; cy < cells; cy++)
                for (int cx = 0; cx < cells; cx++)
                    pts[cy * cells + cx] = new Vector2(
                        (cx + (float)rng.NextDouble()) / cells * TexSize,
                        (cy + (float)rng.NextDouble()) / cells * TexSize);

            Color dark = new Color(0.10f, 0.10f, 0.11f);
            Color light = new Color(0.28f, 0.28f, 0.30f);
            for (int y = 0; y < TexSize; y++)
                for (int x = 0; x < TexSize; x++)
                {
                    // find nearest point in 3x3 neighborhood cells
                    int gx = x * cells / TexSize;
                    int gy = y * cells / TexSize;
                    float best = float.PositiveInfinity;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = ((gx + dx) % cells + cells) % cells;
                            int ny = ((gy + dy) % cells + cells) % cells;
                            var p = pts[ny * cells + nx];
                            float d = (p.x - x) * (p.x - x) + (p.y - y) * (p.y - y);
                            if (d < best) best = d;
                        }
                    float t = Mathf.Clamp01(Mathf.Sqrt(best) / (TexSize / (float)cells));
                    var c = Color.Lerp(light, dark, t);
                    // fine noise
                    float grain = Mathf.PerlinNoise(x * 0.35f, y * 0.35f);
                    c *= 0.85f + grain * 0.3f;
                    c.a = 1f;
                    px[y * TexSize + x] = c;
                }
        }
    }
}
