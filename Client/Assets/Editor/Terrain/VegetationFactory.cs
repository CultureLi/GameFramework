using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameFramework.EditorTools.TerrainGen
{
    public class VegetationSet
    {
        public List<GameObject> trees = new List<GameObject>();
        public List<GameObject> grasses = new List<GameObject>();
        public List<GameObject> flowers = new List<GameObject>();
        public List<GameObject> buildings = new List<GameObject>();
    }

    public static class VegetationFactory
    {
        public const string VegFolder = "Assets/BundleRes/Terrain/Vegetation";
        public const string DecoFolder = "Assets/BundleRes/Terrain/Decoration";
        public const string MatFolder = "Assets/BundleRes/Material/Scene";

        public static VegetationSet EnsureAll(bool forceRegen)
        {
            Directory.CreateDirectory(VegFolder);
            Directory.CreateDirectory(DecoFolder);
            Directory.CreateDirectory(MatFolder);

            var foliageShader = Shader.Find(ShaderCodegen.FoliageShaderName);
            var buildingShader = Shader.Find(ShaderCodegen.BuildingShaderName);
            if (foliageShader == null || buildingShader == null)
            {
                Debug.LogError("[TerrainGen] Shaders not compiled yet. Wait for Unity import and try again.");
                return null;
            }

            var set = new VegetationSet();

            // Textures used by materials
            var leafTex = EnsureTexture($"{VegFolder}/tex_tree_leaf.png", 256, 256, BakeTreeLeaf, forceRegen);
            var grass1Tex = EnsureTexture($"{VegFolder}/tex_grass_a.png", 256, 128, px => BakeGrassTuft(px, 256, 128, 9701, new Color(0.32f, 0.50f, 0.20f)), forceRegen);
            var grass2Tex = EnsureTexture($"{VegFolder}/tex_grass_b.png", 256, 128, px => BakeGrassTuft(px, 256, 128, 9702, new Color(0.40f, 0.58f, 0.22f)), forceRegen);
            var flowerRedTex = EnsureTexture($"{VegFolder}/tex_flower_red.png", 128, 128, px => BakeFlower(px, 128, 128, 9703, new Color(0.85f, 0.20f, 0.20f)), forceRegen);
            var flowerYellowTex = EnsureTexture($"{VegFolder}/tex_flower_yellow.png", 128, 128, px => BakeFlower(px, 128, 128, 9704, new Color(0.95f, 0.85f, 0.20f)), forceRegen);
            var flowerPurpleTex = EnsureTexture($"{VegFolder}/tex_flower_purple.png", 128, 128, px => BakeFlower(px, 128, 128, 9705, new Color(0.70f, 0.35f, 0.85f)), forceRegen);
            var trunkTex = EnsureTexture($"{DecoFolder}/tex_bark.png", 128, 128, px => BakeBark(px, 128, 128), forceRegen);
            var wallTex = EnsureTexture($"{DecoFolder}/tex_wall.png", 128, 128, px => BakeWall(px, 128, 128, 8801, new Color(0.85f, 0.78f, 0.62f)), forceRegen);
            var roofTex = EnsureTexture($"{DecoFolder}/tex_roof.png", 128, 128, px => BakeWall(px, 128, 128, 8802, new Color(0.55f, 0.22f, 0.18f)), forceRegen);

            // Materials
            var leafMat = EnsureMaterial($"{MatFolder}/Foliage_TreeLeaf.mat", foliageShader, leafTex, Color.white, forceRegen, 0.35f, 1.2f, 0.9f);
            var trunkMat = EnsureBuildingMaterial($"{MatFolder}/Building_Trunk.mat", buildingShader, trunkTex, new Color(0.42f, 0.28f, 0.18f), 0.15f, forceRegen);
            var grass1Mat = EnsureMaterial($"{MatFolder}/Foliage_GrassA.mat", foliageShader, grass1Tex, Color.white, forceRegen, 0.28f, 2.0f, 1.8f);
            var grass2Mat = EnsureMaterial($"{MatFolder}/Foliage_GrassB.mat", foliageShader, grass2Tex, Color.white, forceRegen, 0.28f, 2.0f, 1.8f);
            var flowerRedMat = EnsureMaterial($"{MatFolder}/Foliage_FlowerRed.mat", foliageShader, flowerRedTex, Color.white, forceRegen, 0.30f, 1.5f, 1.4f);
            var flowerYellowMat = EnsureMaterial($"{MatFolder}/Foliage_FlowerYellow.mat", foliageShader, flowerYellowTex, Color.white, forceRegen, 0.30f, 1.5f, 1.4f);
            var flowerPurpleMat = EnsureMaterial($"{MatFolder}/Foliage_FlowerPurple.mat", foliageShader, flowerPurpleTex, Color.white, forceRegen, 0.30f, 1.5f, 1.4f);

            Color[] wallTints = { new Color(1f, 0.95f, 0.82f), new Color(0.90f, 0.86f, 0.70f), new Color(0.80f, 0.72f, 0.58f), new Color(0.62f, 0.58f, 0.52f) };
            Color[] roofTints = { new Color(0.85f, 0.28f, 0.22f), new Color(0.55f, 0.35f, 0.30f), new Color(0.35f, 0.42f, 0.55f), new Color(0.45f, 0.45f, 0.45f) };

            // Meshes + prefabs
            for (int i = 0; i < 3; i++)
            {
                var mesh = BuildTreeMesh(i);
                var meshPath = $"{VegFolder}/mesh_tree_{i}.asset";
                SaveMesh(mesh, meshPath, forceRegen);
                var prefab = BuildPrefab($"{VegFolder}/pf_tree_{i}.prefab", meshPath, new[] { trunkMat, leafMat }, forceRegen);
                set.trees.Add(prefab);
            }

            for (int i = 0; i < 2; i++)
            {
                var mesh = BuildCrossedQuadMesh(0.35f + i * 0.15f, 0.20f + i * 0.05f, 3);
                var meshPath = $"{VegFolder}/mesh_grass_{i}.asset";
                SaveMesh(mesh, meshPath, forceRegen);
                var mat = i == 0 ? grass1Mat : grass2Mat;
                var prefab = BuildPrefab($"{VegFolder}/pf_grass_{i}.prefab", meshPath, new[] { mat }, forceRegen);
                set.grasses.Add(prefab);
            }

            var flowerMats = new[] { flowerRedMat, flowerYellowMat, flowerPurpleMat };
            var flowerNames = new[] { "red", "yellow", "purple" };
            for (int i = 0; i < 3; i++)
            {
                var mesh = BuildCrossedQuadMesh(0.22f, 0.22f, 2);
                var meshPath = $"{VegFolder}/mesh_flower_{flowerNames[i]}.asset";
                SaveMesh(mesh, meshPath, forceRegen);
                var prefab = BuildPrefab($"{VegFolder}/pf_flower_{flowerNames[i]}.prefab", meshPath, new[] { flowerMats[i] }, forceRegen);
                set.flowers.Add(prefab);
            }

            for (int i = 0; i < 4; i++)
            {
                var mesh = BuildBuildingMesh(i);
                var meshPath = $"{DecoFolder}/mesh_building_{i}.asset";
                SaveMesh(mesh, meshPath, forceRegen);
                var wallMat = EnsureBuildingMaterial($"{MatFolder}/Building_Wall_{i}.mat", buildingShader, wallTex, wallTints[i], 0.10f, forceRegen);
                var roofMat = EnsureBuildingMaterial($"{MatFolder}/Building_Roof_{i}.mat", buildingShader, roofTex, roofTints[i], 0.20f, forceRegen);
                var prefab = BuildPrefab($"{DecoFolder}/pf_building_{i}.prefab", meshPath, new[] { wallMat, roofMat }, forceRegen);
                set.buildings.Add(prefab);
            }

            AssetDatabase.SaveAssets();
            return set;
        }

        // ---------------------------------------------------------------------- Textures

        static Texture2D EnsureTexture(string path, int w, int h, System.Action<Color[]> baker, bool forceRegen)
        {
            if (!forceRegen && File.Exists(path))
            {
                var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (existing != null) return existing;
            }
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0f, 0f, 0f, 0f);
            baker(px);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true, false);
            tex.SetPixels(px);
            tex.Apply(true, false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Default;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.filterMode = FilterMode.Bilinear;
                imp.alphaSource = TextureImporterAlphaSource.FromInput;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = true;
                imp.sRGBTexture = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void BakeTreeLeaf(Color[] px)
        {
            int w = 256, h = 256;
            var rng = new System.Random(7101);
            Vector2 o = new Vector2((float)rng.NextDouble() * 500, (float)rng.NextDouble() * 500);
            Color darkG = new Color(0.10f, 0.28f, 0.12f);
            Color midG = new Color(0.20f, 0.44f, 0.18f);
            Color lightG = new Color(0.36f, 0.60f, 0.24f);
            Vector2 c = new Vector2(w * 0.5f, h * 0.5f);
            float rMax = Mathf.Min(w, h) * 0.48f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - c.x) / (w * 0.5f);
                    float dy = (y - c.y) / (h * 0.5f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = Mathf.PerlinNoise((x + o.x) * 0.06f, (y + o.y) * 0.06f);
                    float mask = Mathf.SmoothStep(1f, 0.65f, r) * (edge * 0.5f + 0.5f);
                    // Break edges further
                    float rip = Mathf.PerlinNoise((x + o.x) * 0.25f, (y + o.y) * 0.25f);
                    if (r > 0.75f && rip < 0.55f) mask *= 0.15f;
                    mask = Mathf.Clamp01(mask - 0.25f) * 1.5f;

                    float n = Mathf.PerlinNoise((x + o.x) * 0.08f, (y + o.y) * 0.08f);
                    Color col = Color.Lerp(darkG, midG, n);
                    col = Color.Lerp(col, lightG, Mathf.Pow(n, 3f));
                    col.a = Mathf.Clamp01(mask);
                    px[y * w + x] = col;
                    _ = rMax;
                }
        }

        static void BakeGrassTuft(Color[] px, int w, int h, int seed, Color tint)
        {
            var rng = new System.Random(seed);
            int blades = 28;
            for (int b = 0; b < blades; b++)
            {
                float bx = (float)rng.NextDouble() * w;
                float bh = h * (0.55f + (float)rng.NextDouble() * 0.42f);
                float tilt = ((float)rng.NextDouble() - 0.5f) * 12f;
                float width = 1.4f + (float)rng.NextDouble() * 1.6f;
                for (int y = 0; y < bh; y++)
                {
                    float t = y / bh;
                    float taper = 1f - Mathf.Pow(t, 1.4f);
                    float x = bx + tilt * t;
                    int xw = Mathf.CeilToInt(width * taper);
                    Color shade = tint * (0.75f + (1f - t) * 0.35f);
                    shade.a = 1f;
                    for (int dx = -xw; dx <= xw; dx++)
                    {
                        int xi = Mathf.Clamp((int)x + dx, 0, w - 1);
                        int yi = Mathf.Clamp(y, 0, h - 1);
                        px[yi * w + xi] = shade;
                    }
                }
            }
            // fringe noise to break bottom edge
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = px[y * w + x];
                    if (c.a > 0f && Mathf.PerlinNoise(x * 0.4f, y * 0.4f) < 0.4f) px[y * w + x] = new Color(0, 0, 0, 0);
                }
        }

        static void BakeFlower(Color[] px, int w, int h, int seed, Color petal)
        {
            var rng = new System.Random(seed);
            Vector2 c = new Vector2(w * 0.5f, h * 0.55f);
            float rOuter = Mathf.Min(w, h) * 0.42f;
            float rInner = rOuter * 0.32f;
            Color center = new Color(1f, 0.85f, 0.20f, 1f);
            int petals = 5;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = x - c.x, dy = y - c.y;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float petalMask = 0.5f + 0.5f * Mathf.Cos(ang * petals);
                    float pR = rInner + (rOuter - rInner) * petalMask;
                    if (r < pR)
                    {
                        Color col = petal;
                        if (r < rInner * 0.95f) col = Color.Lerp(petal, center, Mathf.SmoothStep(0f, 1f, 1f - r / rInner));
                        col *= 0.7f + 0.3f * (1f - r / pR);
                        col.a = 1f;
                        px[y * w + x] = col;
                    }
                }
            // Add stem
            int stemX = w / 2;
            for (int y = 0; y < h * 0.5f; y++)
            {
                int xi = Mathf.Clamp(stemX + (int)((rng.NextDouble() - 0.5) * 2), 0, w - 1);
                if (px[y * w + xi].a < 0.05f) px[y * w + xi] = new Color(0.16f, 0.35f, 0.12f, 1f);
            }
        }

        static void BakeBark(Color[] px, int w, int h)
        {
            var rng = new System.Random(6001);
            Vector2 o = new Vector2((float)rng.NextDouble() * 500, (float)rng.NextDouble() * 500);
            Color dark = new Color(0.20f, 0.14f, 0.09f);
            Color light = new Color(0.45f, 0.30f, 0.18f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float stripe = Mathf.PerlinNoise((x + o.x) * 0.08f, (y + o.y) * 0.35f);
                    float grain = Mathf.PerlinNoise((x + o.x) * 0.6f, (y + o.y) * 0.6f);
                    var c = Color.Lerp(dark, light, stripe * 0.8f + grain * 0.2f);
                    c.a = 1f;
                    px[y * w + x] = c;
                }
        }

        static void BakeWall(Color[] px, int w, int h, int seed, Color baseCol)
        {
            var rng = new System.Random(seed);
            Vector2 o = new Vector2((float)rng.NextDouble() * 500, (float)rng.NextDouble() * 500);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float g = Mathf.PerlinNoise((x + o.x) * 0.15f, (y + o.y) * 0.15f);
                    var c = baseCol * (0.75f + g * 0.35f);
                    c.a = 1f;
                    px[y * w + x] = c;
                }
        }

        // ---------------------------------------------------------------------- Materials

        static Material EnsureMaterial(string path, Shader shader, Texture2D tex, Color color, bool forceRegen,
            float cutoff, float windStrength, float windSpeed)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (forceRegen)
            {
                mat.shader = shader;
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", cutoff);
            if (mat.HasProperty("_WindStrength")) mat.SetFloat("_WindStrength", windStrength);
            if (mat.HasProperty("_WindSpeed")) mat.SetFloat("_WindSpeed", windSpeed);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material EnsureBuildingMaterial(string path, Shader shader, Texture2D tex, Color color, float smoothness, bool forceRegen)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (forceRegen)
            {
                mat.shader = shader;
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------------------------------------------------------------------- Meshes

        static void SaveMesh(Mesh mesh, string path, bool forceRegen)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null && !forceRegen)
            {
                Object.DestroyImmediate(mesh);
                return;
            }
            if (existing != null)
            {
                // Overwrite in place
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                return;
            }
            AssetDatabase.CreateAsset(mesh, path);
        }

        static Mesh BuildTreeMesh(int variant)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var trisTrunk = new List<int>();
            var trisLeaves = new List<int>();

            float trunkHeight = 2.0f + variant * 0.8f;
            float trunkRadius = 0.10f + variant * 0.02f;
            float canopyR = 1.1f + variant * 0.35f;
            float canopyH = 1.3f + variant * 0.35f;
            float canopyYBase = trunkHeight - 0.3f;

            // Trunk: 8-sided cylinder, 3 rings
            const int trunkSides = 8;
            const int trunkRings = 3;
            int trunkStart = verts.Count;
            for (int r = 0; r <= trunkRings; r++)
            {
                float t = r / (float)trunkRings;
                float y = t * trunkHeight;
                float rad = Mathf.Lerp(trunkRadius, trunkRadius * 0.7f, t);
                for (int s = 0; s < trunkSides; s++)
                {
                    float a = s / (float)trunkSides * Mathf.PI * 2f;
                    Vector3 p = new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad);
                    verts.Add(p);
                    normals.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
                    uvs.Add(new Vector2(s / (float)trunkSides, t));
                }
            }
            for (int r = 0; r < trunkRings; r++)
            {
                for (int s = 0; s < trunkSides; s++)
                {
                    int a0 = trunkStart + r * trunkSides + s;
                    int a1 = trunkStart + r * trunkSides + (s + 1) % trunkSides;
                    int b0 = trunkStart + (r + 1) * trunkSides + s;
                    int b1 = trunkStart + (r + 1) * trunkSides + (s + 1) % trunkSides;
                    trisTrunk.Add(a0); trisTrunk.Add(b0); trisTrunk.Add(a1);
                    trisTrunk.Add(a1); trisTrunk.Add(b0); trisTrunk.Add(b1);
                }
            }

            // Canopy: 6 crossed quads at 30-degree spacings, angled upwards
            int planes = 6;
            for (int i = 0; i < planes; i++)
            {
                float ang = i * Mathf.PI / planes;
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                float tilt = (i % 2 == 0) ? 0.35f : -0.35f;
                Vector3 up = new Vector3(dir.x * tilt, 1f, dir.z * tilt).normalized;
                Vector3 side = Vector3.Cross(up, dir).normalized;
                Vector3 center = new Vector3(0, canopyYBase + canopyH * 0.5f, 0);

                int b = verts.Count;
                Vector3 n = Vector3.Cross(side, up);
                verts.Add(center - side * canopyR - up * canopyH * 0.5f); uvs.Add(new Vector2(0, 0)); normals.Add(n);
                verts.Add(center + side * canopyR - up * canopyH * 0.5f); uvs.Add(new Vector2(1, 0)); normals.Add(n);
                verts.Add(center + side * canopyR + up * canopyH * 0.5f); uvs.Add(new Vector2(1, 1)); normals.Add(n);
                verts.Add(center - side * canopyR + up * canopyH * 0.5f); uvs.Add(new Vector2(0, 1)); normals.Add(n);
                trisLeaves.Add(b); trisLeaves.Add(b + 2); trisLeaves.Add(b + 1);
                trisLeaves.Add(b); trisLeaves.Add(b + 3); trisLeaves.Add(b + 2);
            }

            var mesh = new Mesh();
            mesh.name = $"tree_{variant}";
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trisTrunk, 0);
            mesh.SetTriangles(trisLeaves, 1);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static Mesh BuildCrossedQuadMesh(float halfWidth, float height, int planes)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < planes; i++)
            {
                float ang = i * Mathf.PI / planes;
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 side = new Vector3(dir.z, 0f, -dir.x);
                Vector3 up = Vector3.up;
                Vector3 n = Vector3.Cross(side, up);
                int b = verts.Count;
                verts.Add(-side * halfWidth); uvs.Add(new Vector2(0, 0)); normals.Add(n);
                verts.Add(side * halfWidth); uvs.Add(new Vector2(1, 0)); normals.Add(n);
                verts.Add(side * halfWidth + up * height); uvs.Add(new Vector2(1, 1)); normals.Add(n);
                verts.Add(-side * halfWidth + up * height); uvs.Add(new Vector2(0, 1)); normals.Add(n);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
            }
            var mesh = new Mesh();
            mesh.name = "crossed_quad";
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static Mesh BuildBuildingMesh(int variant)
        {
            // Sizes
            Vector3 size = variant switch
            {
                0 => new Vector3(4f, 3.2f, 4f),
                1 => new Vector3(5f, 4.0f, 3f),
                2 => new Vector3(3f, 5.5f, 3f),
                _ => new Vector3(6f, 3.5f, 4.5f),
            };
            float roofHeight = variant == 2 ? 2.2f : 1.4f;
            bool pointyRoof = variant != 3;

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var wallTris = new List<int>();
            var roofTris = new List<int>();

            // Box walls (submesh 0) - 4 side quads
            Vector3 hs = size * 0.5f;
            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, List<int> subTris)
            {
                int i = verts.Count;
                verts.Add(a); uvs.Add(new Vector2(0, 0)); normals.Add(n);
                verts.Add(b); uvs.Add(new Vector2(1, 0)); normals.Add(n);
                verts.Add(c); uvs.Add(new Vector2(1, 1)); normals.Add(n);
                verts.Add(d); uvs.Add(new Vector2(0, 1)); normals.Add(n);
                subTris.Add(i); subTris.Add(i + 2); subTris.Add(i + 1);
                subTris.Add(i); subTris.Add(i + 3); subTris.Add(i + 2);
            }

            float y0 = 0f, y1 = size.y;
            AddQuad(new Vector3(-hs.x, y0, hs.z), new Vector3(hs.x, y0, hs.z),
                    new Vector3(hs.x, y1, hs.z), new Vector3(-hs.x, y1, hs.z),
                    Vector3.forward, wallTris);
            AddQuad(new Vector3(hs.x, y0, -hs.z), new Vector3(-hs.x, y0, -hs.z),
                    new Vector3(-hs.x, y1, -hs.z), new Vector3(hs.x, y1, -hs.z),
                    Vector3.back, wallTris);
            AddQuad(new Vector3(hs.x, y0, hs.z), new Vector3(hs.x, y0, -hs.z),
                    new Vector3(hs.x, y1, -hs.z), new Vector3(hs.x, y1, hs.z),
                    Vector3.right, wallTris);
            AddQuad(new Vector3(-hs.x, y0, -hs.z), new Vector3(-hs.x, y0, hs.z),
                    new Vector3(-hs.x, y1, hs.z), new Vector3(-hs.x, y1, -hs.z),
                    Vector3.left, wallTris);

            // Roof (submesh 1)
            if (pointyRoof)
            {
                Vector3 apex = new Vector3(0, size.y + roofHeight, 0);
                int i = verts.Count;
                Vector3[] baseP = {
                    new Vector3(-hs.x, size.y, hs.z),
                    new Vector3(hs.x, size.y, hs.z),
                    new Vector3(hs.x, size.y, -hs.z),
                    new Vector3(-hs.x, size.y, -hs.z),
                };
                // 4 triangles
                for (int s = 0; s < 4; s++)
                {
                    Vector3 a = baseP[s];
                    Vector3 b = baseP[(s + 1) % 4];
                    Vector3 n = Vector3.Cross(b - a, apex - a).normalized;
                    int t = verts.Count;
                    verts.Add(a); uvs.Add(new Vector2(0, 0)); normals.Add(n);
                    verts.Add(b); uvs.Add(new Vector2(1, 0)); normals.Add(n);
                    verts.Add(apex); uvs.Add(new Vector2(0.5f, 1)); normals.Add(n);
                    roofTris.Add(t); roofTris.Add(t + 2); roofTris.Add(t + 1);
                }
                _ = i;
            }
            else
            {
                // Flat roof: single quad extended slightly
                float e = 0.2f;
                Vector3 n = Vector3.up;
                int t = verts.Count;
                verts.Add(new Vector3(-hs.x - e, size.y, -hs.z - e)); uvs.Add(new Vector2(0, 0)); normals.Add(n);
                verts.Add(new Vector3(hs.x + e, size.y, -hs.z - e)); uvs.Add(new Vector2(1, 0)); normals.Add(n);
                verts.Add(new Vector3(hs.x + e, size.y, hs.z + e)); uvs.Add(new Vector2(1, 1)); normals.Add(n);
                verts.Add(new Vector3(-hs.x - e, size.y, hs.z + e)); uvs.Add(new Vector2(0, 1)); normals.Add(n);
                roofTris.Add(t); roofTris.Add(t + 2); roofTris.Add(t + 1);
                roofTris.Add(t); roofTris.Add(t + 3); roofTris.Add(t + 2);
            }

            var mesh = new Mesh();
            mesh.name = $"building_{variant}";
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(wallTris, 0);
            mesh.SetTriangles(roofTris, 1);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        // ---------------------------------------------------------------------- Prefabs

        static GameObject BuildPrefab(string path, string meshPath, Material[] mats, bool forceRegen)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && !forceRegen) return existing;

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            var go = new GameObject(Path.GetFileNameWithoutExtension(path));
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;

            GameObject prefab;
            if (existing != null)
                prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            else
                prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
