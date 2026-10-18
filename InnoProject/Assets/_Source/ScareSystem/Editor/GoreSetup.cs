using System;
using System.IO;
using System.Linq;
using PlayerSystem;
using RoundSystem;
using SoundSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

namespace ScareSystem.EditorTools
{
    /// <summary>
    /// Assets of the Babaika's aftermath in Assets/_Presentation/Gore: splash, smear and gut textures drawn in code
    /// (always the same, so re-running changes nothing), their URP Lit materials, and the <see cref="GoreOffice"/>
    /// on the Game object with the blood steps from Sounds/Footsteps (Blood*).
    /// </summary>
    public static class GoreSetup
    {
        public const string Folder = "Assets/_Presentation/Gore";
        const string FootstepsFolder = "Assets/_Presentation/Sounds/Footsteps";

        /// <summary>Adds (or refreshes) the aftermath on the Game object.</summary>
        public static GoreOffice AddGore(GameObject game, RoundManager round, BabaikaChase chase, PlayerController player, Camera view)
        {
            var gore = game.GetComponent<GoreOffice>();
            if (gore == null) gore = game.AddComponent<GoreOffice>();
            gore.round = round;
            gore.chase = chase;
            gore.player = player;
            gore.view = view;
            gore.sounds = ScareSetup.EnsureSoundBank();
            gore.ambience = game.GetComponent<Ambience>();
            gore.kit = EnsureKit();
            EditorUtility.SetDirty(gore);
            return gore;
        }

        public static GoreBuilder.Kit EnsureKit()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Presentation", "Gore");
            var splash = SaveTexture("Gore_Splash", 256, 256, Splash(256, new Random(11)), TextureWrapMode.Clamp, TextureWrapMode.Clamp);
            var smear = SaveTexture("Gore_Smear", 512, 128, Smear(512, 128, new Random(12)), TextureWrapMode.Repeat, TextureWrapMode.Clamp);
            var skin = SaveTexture("Gore_Guts", 128, 128, Guts(128, new Random(13)), TextureWrapMode.Repeat, TextureWrapMode.Repeat);
            var (muscleColor, muscleHeight) = Muscle(256, new Random(14));
            var fibres = SaveTexture("Gore_Muscle", 256, 256, muscleColor, TextureWrapMode.Repeat, TextureWrapMode.Clamp);
            var relief = SaveNormalMap("Gore_Muscle_Normal", 256, muscleHeight, 5f);
            var kit = new GoreBuilder.Kit
            {
                guts = SaveMaterial("Gore_Guts", Color.white, 0.82f, skin),
                gutsDark = SaveMaterial("Gore_Guts_Dark", new Color(0.7f, 0.45f, 0.5f), 0.8f, skin),
                organ = SaveMaterial("Gore_Organ", new Color(0.35f, 0.07f, 0.065f), 0.6f),
                muscle = SaveMaterial("Gore_Muscle", Color.white, 0.68f, fibres, normal: relief),
                bone = SaveMaterial("Gore_Bone", new Color(0.92f, 0.88f, 0.8f), 0.55f),
                pool = SaveMaterial("Gore_Pool", new Color(0.2f, 0.01f, 0.015f), 0.93f),
                splatter = SaveMaterial("Gore_Splash", new Color(0.45f, 0.02f, 0.03f), 0.8f, splash, 0.5f),
                smear = SaveMaterial("Gore_Smear", new Color(0.34f, 0.015f, 0.02f), 0.72f, smear, 0.45f),
                steps = AssetDatabase.FindAssets("t:AudioClip Blood", new[] { FootstepsFolder })
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(path => AssetDatabase.LoadAssetAtPath<AudioClip>(path))
                    .ToArray(),
            };
            AssetDatabase.SaveAssets();
            return kit;
        }

        // ------------------------------------------------------------------ textures (white shapes; the materials tint them)

        /// <summary>A splash: a ragged blob, streaks flung out from it and drops around.</summary>
        static Color[] Splash(int size, Random rng)
        {
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            float phase1 = R(0f, 6.3f), phase2 = R(0f, 6.3f);
            var rays = Enumerable.Range(0, 13).Select(_ => (angle: R(-Mathf.PI, Mathf.PI), length: R(0.55f, 0.95f), width: R(0.03f, 0.07f))).ToArray();
            var drops = Enumerable.Range(0, 26).Select(_ =>
            {
                float a = R(0f, 6.3f), d = R(0.5f, 0.97f);
                return (center: new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d, radius: R(0.012f, 0.045f));
            }).ToArray();

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                    float d = p.magnitude, a = Mathf.Atan2(p.y, p.x);
                    bool inside = d < 0.36f + 0.06f * Mathf.Sin(a * 5f + phase1) + 0.04f * Mathf.Sin(a * 9f + phase2)
                                  + 0.06f * (Mathf.PerlinNoise(p.x * 4f + 10f, p.y * 4f + 10f) - 0.5f);
                    foreach (var ray in rays)
                        inside |= d < ray.length && Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, ray.angle * Mathf.Rad2Deg)) * Mathf.Deg2Rad * d < ray.width * (1f - d / ray.length);
                    foreach (var drop in drops)
                        inside |= (p - drop.center).sqrMagnitude < drop.radius * drop.radius;
                    float shade = Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(d * 1.4f)) * (0.9f + 0.1f * Mathf.PerlinNoise(p.x * 12f, p.y * 12f));
                    pixels[y * size + x] = inside ? new Color(shade, shade, shade, 1f) : new Color(1f, 1f, 1f, 0f);
                }
            }
            return pixels;
        }

        /// <summary>A drag mark: a band with a wavy edge and streaks along it, seamless along x.</summary>
        static Color[] Smear(int width, int height, Random rng)
        {
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            float f1 = R(0f, 6.3f), f2 = R(0f, 6.3f), f3 = R(0f, 6.3f), f4 = R(0f, 6.3f);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width * 2f * Mathf.PI, v = (y + 0.5f) / height; // u goes once round: seamless
                    float edge = 0.36f + 0.06f * Mathf.Sin(u * 3f + f1) + 0.03f * Mathf.Sin(u * 7f + f2) + 0.03f * Mathf.Sin(u * 13f + f3);
                    float streak = 0.6f * (0.5f + 0.5f * Mathf.Sin(v * 95f + 2f * Mathf.Sin(u * 2f + f4)))
                                   + 0.4f * Mathf.PerlinNoise(v * 20f, Mathf.Cos(u) * 1.5f + 3f);
                    bool inside = Mathf.Abs(v - 0.5f) < edge && streak > 0.3f;
                    float shade = Mathf.Lerp(0.65f, 1f, streak);
                    pixels[y * width + x] = inside ? new Color(shade, shade, shade, 1f) : new Color(1f, 1f, 1f, 0f);
                }
            }
            return pixels;
        }

        /// <summary>Wet pink-red with darker patches and thin veins, seamless both ways.</summary>
        static Color[] Guts(int size, Random rng)
        {
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            float a1 = R(0f, 6.3f), a2 = R(0f, 6.3f), a3 = R(0f, 6.3f), a4 = R(0f, 6.3f);
            var dark = new Color(0.55f, 0.16f, 0.17f);
            var light = new Color(0.86f, 0.5f, 0.5f);
            var vein = new Color(0.35f, 0.06f, 0.12f);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = 2f * Mathf.PI * x / size, v = 2f * Mathf.PI * y / size;
                    float n = 0.5f + 0.25f * Mathf.Sin(u * 2f + v + a1) + 0.15f * Mathf.Sin(u * 3f - v * 2f + a2) + 0.1f * Mathf.Sin(u * 5f + v * 4f + a3);
                    var color = Color.Lerp(dark, light, Mathf.Clamp01(n));
                    float line = Mathf.Abs(Mathf.Sin(u * 3f + 0.6f * Mathf.Sin(v * 2f + a4)));
                    if (line < 0.07f) color = Color.Lerp(color, vein, 0.7f * (1f - line / 0.07f));
                    pixels[y * size + x] = color;
                }
            }
            return pixels;
        }

        /// <summary>
        /// Bare muscle, colour and height: bundles of fibres running along v (seamless across u, around a limb) with
        /// dark grooves between them and fine striation, white fascia streaks, white tendon at both ends of v (the
        /// joints), dark clots of blood.
        /// </summary>
        static (Color[] color, float[] height) Muscle(int size, Random rng)
        {
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            float p1 = R(0f, 6.3f), p2 = R(0f, 6.3f), p3 = R(0f, 6.3f), p4 = R(0f, 6.3f);
            var deep = new Color(0.26f, 0.025f, 0.035f);
            var red = new Color(0.78f, 0.16f, 0.15f);
            var tendon = new Color(0.88f, 0.78f, 0.72f);
            var clot = new Color(0.18f, 0.012f, 0.018f);
            var pixels = new Color[size * size];
            var heights = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = 2f * Mathf.PI * x / size, v = (y + 0.5f) / size;
                    // Bundles with thin grooves between them that wander along the muscle, and fine striation on top.
                    float groove = Mathf.Abs(Mathf.Sin(u * 10f + 1.4f * Mathf.Sin(v * 7f + p1) + 0.6f * Mathf.Sin(u * 2f + p2)));
                    float bundle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.35f, groove));
                    float fine = 0.5f + 0.5f * Mathf.Sin(u * 46f + 2f * Mathf.Sin(v * 19f + p3));
                    float shade = Mathf.PerlinNoise(Mathf.Cos(u) * 1.5f + 3f, v * 3f + Mathf.Sin(u) * 1.5f);
                    var color = Color.Lerp(deep, red, bundle * (0.82f + 0.18f * fine) * Mathf.Lerp(0.75f, 1.1f, shade));
                    float fascia = Mathf.Sin(u * 3f + v * 2.5f + p4);
                    if (fascia > 0.975f) color = Color.Lerp(color, tendon, 0.55f * Mathf.InverseLerp(0.975f, 1f, fascia));
                    float ends = Mathf.Max(Mathf.InverseLerp(0.13f, 0.02f, v), Mathf.InverseLerp(0.87f, 0.98f, v));
                    color = Color.Lerp(color, tendon, ends * (0.75f + 0.25f * fine));
                    float blood = Mathf.PerlinNoise(Mathf.Cos(u) * 2f + 7f, v * 6f + Mathf.Sin(u) * 2f);
                    if (blood > 0.6f) color = Color.Lerp(color, clot, Mathf.InverseLerp(0.6f, 0.8f, blood) * 0.85f);
                    pixels[y * size + x] = color;
                    heights[y * size + x] = bundle * 0.85f + fine * 0.15f;
                }
            }
            return (pixels, heights);
        }

        /// <summary>A tangent-space normal map from a height field (seamless across x, clamped along y).</summary>
        static Texture2D SaveNormalMap(string name, int size, float[] height, float strength)
        {
            float H(int x, int y) => height[Mathf.Clamp(y, 0, size - 1) * size + ((x % size) + size) % size];
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var n = new Vector3((H(x - 1, y) - H(x + 1, y)) * strength, (H(x, y - 1) - H(x, y + 1)) * strength, 1f).normalized;
                    pixels[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }
            string path = $"{Folder}/{name}.png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.NormalMap;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Texture2D SaveTexture(string name, int width, int height, Color[] pixels, TextureWrapMode wrapU, TextureWrapMode wrapV)
        {
            string path = $"{Folder}/{name}.png";
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapModeU = wrapU;
            importer.wrapModeV = wrapV;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>URP Lit; with a cutoff the texture's alpha cuts the shape out (opaque pass, no sorting trouble).</summary>
        static Material SaveMaterial(string name, Color color, float smoothness, Texture2D map = null, float cutoff = 0f, Texture2D normal = null)
        {
            string path = $"{Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", map);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            if (normal != null) material.EnableKeyword("_NORMALMAP");
            else material.DisableKeyword("_NORMALMAP");
            bool cut = cutoff > 0f;
            material.SetFloat("_AlphaClip", cut ? 1f : 0f);
            material.SetFloat("_Cutoff", cut ? cutoff : 0.5f);
            if (cut) material.EnableKeyword("_ALPHATEST_ON");
            else material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", cut ? "TransparentCutout" : "Opaque");
            material.renderQueue = cut ? (int)RenderQueue.AlphaTest : -1;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
