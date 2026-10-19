using System;
using System.IO;
using System.Linq;
using AnomalySystem;
using AnomalySystem.EditorTools;
using CameraSystem;
using PauseSystem;
using PlayerSystem;
using RoundSystem;
using SoundSystem;
using UISystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace LevelSystem.EditorTools
{
    /// <summary>
    /// Office menu: generates the textures, materials and palette, the office prefab, the anomaly prefabs and the
    /// Office scene (player, round logic, post-processing, VHS camera). Assets go to Assets/_Presentation/Office,
    /// the anomalies to Assets/_Presentation/Anomalies, the scene to Assets/_Presentation/Scenes/Office.unity.
    /// Re-running updates the materials and the office; existing anomaly prefabs are kept (they may have been
    /// edited), and the scene is only created once unless "Recreate Scene" is used.
    /// </summary>
    public static class OfficeMenu
    {
        const string Root = "Assets/_Presentation/Office";
        public const string AnomalyFolder = "Assets/_Presentation/Anomalies";
        public const string PalettePath = Root + "/OfficePalette.asset";
        public const string RoomPrefabPath = Root + "/Prefabs/OfficeRoom.prefab";
        const string MeshesPath = Root + "/Meshes/OfficeRoom_Meshes.asset";
        const string VolumePath = Root + "/OfficeVolume.asset";
        const string LibraryPath = AnomalyFolder + "/AnomalyLibrary.asset";
        public const string ScenePath = "Assets/_Presentation/Scenes/Office.unity";
        public const string MenuScenePath = "Assets/_Presentation/Scenes/Menu.unity";

        [MenuItem("Office/Build All")]
        public static void BuildAll()
        {
            var palette = BuildPalette();
            var prefab = BuildRoomPrefab(palette);
            var library = BuildAnomalyPrefabs(palette);
            PropAnomalies.Build(palette);
            BuildScene(palette, prefab, library, false);
            BuildMenuScene(false);
        }

        [MenuItem("Office/Recreate Menu Scene")]
        public static void RecreateMenuScene() => BuildMenuScene(true);

        /// <summary>Menu scene (language choice, Play) and both scenes in Build Settings, the menu first.</summary>
        public static void BuildMenuScene(bool overwrite)
        {
            if (overwrite || !File.Exists(MenuScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                new GameObject("Menu").AddComponent<MenuSystem.MainMenu>();
                EditorSceneManager.SaveScene(scene, MenuScenePath);
            }

            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == MenuScenePath || s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            scenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Office/Rebuild Room Prefab")]
        public static void RebuildRoom() => BuildRoomPrefab(BuildPalette());

        [MenuItem("Office/Recreate Scene")]
        public static void RecreateScene()
        {
            var palette = BuildPalette();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath) ?? BuildRoomPrefab(palette);
            BuildScene(palette, prefab, BuildAnomalyPrefabs(palette), true);
        }

        // ------------------------------------------------------------------ palette

        public static OfficePalette BuildPalette()
        {
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Textures");
            var palette = AssetDatabase.LoadAssetAtPath<OfficePalette>(PalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<OfficePalette>();
                AssetDatabase.CreateAsset(palette, PalettePath);
            }

            palette.carpet = Mat("Carpet", "#FFFFFF", 0f, texture: CarpetTexture(), tile: 0.5f);
            palette.corridorFloor = Mat("Corridor_Floor", "#FFFFFF", 0.3f, texture: VinylTexture(), tile: 0.5f);
            palette.wall = Mat("Wall", "#DCD9CF", 0.08f);
            palette.baseboard = Mat("Baseboard", "#5E6166", 0.3f);
            palette.ceiling = Mat("Ceiling", "#FFFFFF", 0.05f, texture: CeilingTexture(), tile: 0.5f);
            palette.lightPanel = Mat("Light_Panel", "#FFFFFF", 0.2f, emission: new Color(1f, 0.99f, 0.96f) * 2.2f);
            palette.lightPanelOff = Mat("Light_Panel_Off", "#BFC0BC", 0.3f);
            palette.doorLeaf = Mat("Door_Leaf", "#FFFFFF", 0.35f, texture: DoorWoodTexture(), tile: 1f);
            palette.doorFrame = Mat("Door_Frame", "#8F9297", 0.45f, 0.3f);
            palette.glass = Mat("Glass", "#CFE3EA", 0.95f, alpha: 0.15f);
            palette.windowLight = Mat("Window_Light", "#F4F6F8", 0f, emission: new Color(1f, 1f, 1f) * 1.7f);
            palette.windowNight = Screen("Window_Night", NightViewTexture(), 0.9f);
            palette.blinds = Mat("Blinds", "#D9D8D2", 0.35f);

            palette.deskTop = Mat("Desk_Top", "#FFFFFF", 0.3f, texture: WoodTexture(), tile: 1f);
            palette.deskFrame = Mat("Desk_Frame", "#A3A6AA", 0.45f, 0.4f);
            palette.partition = Mat("Partition", "#FFFFFF", 0.05f, texture: FabricTexture(), tile: 0.25f);
            palette.chairFabric = Mat("Chair_Fabric", "#2E3441", 0.1f);
            palette.metal = Mat("Metal", "#A2A6AB", 0.5f, 0.7f);
            palette.darkPlastic = Mat("Dark_Plastic", "#26282C", 0.45f);
            palette.whitePlastic = Mat("White_Plastic", "#E7E7E2", 0.35f);
            palette.lightPlastic = Mat("Light_Plastic", "#C9CAC4", 0.3f);
            palette.wood = Mat("Wood_Dark", "#6E4F35", 0.25f);
            palette.cardboard = Mat("Cardboard", "#A8835A", 0.1f);
            palette.coat = Mat("Coat", "#2C3A55", 0.1f);

            palette.screenDesktop = Screen("Screen_Desktop", DesktopTexture(), 1.1f);
            palette.screenEye = Screen("Screen_Eye", EyeTexture(), 1.3f);
            palette.screenOff = Mat("Screen_Off", "#0B0C0E", 0.85f);
            palette.copierScreen = Mat("Copier_Screen", "#5FD3B8", 0.6f, emission: new Color(0.37f, 0.83f, 0.72f) * 0.9f);
            palette.signPanel = Mat("Sign_Panel", "#1C1E22", 0.5f);
            palette.exitSign = Mat("Exit_Sign", "#19A447", 0.5f, emission: new Color(0.1f, 0.85f, 0.3f) * 1.5f);
            palette.text = TextMaterial();

            palette.leaves = Mat("Leaves", "#3E6E38", 0.2f);
            palette.pot = Mat("Pot", "#3A3B3F", 0.35f);
            palette.soil = Mat("Soil", "#3D2C20", 0.05f);
            palette.paper = Mat("Paper", "#F2F1EC", 0.1f);
            palette.stickyNote = Mat("Sticky_Note", "#F3DF6B", 0.1f);
            palette.whiteboard = Mat("Whiteboard", "#F6F7F7", 0.85f);
            palette.cork = Mat("Cork", "#FFFFFF", 0.05f, texture: CorkTexture(), tile: 0.3f);
            palette.clockFace = Mat("Clock_Face", "#FFFFFF", 0.3f, texture: ClockTexture(), tile: 1f);
            palette.red = Mat("Red", "#B4261F", 0.35f);
            palette.yellow = Mat("Yellow", "#E8C24A", 0.35f);
            palette.blue = Mat("Blue", "#2D5BA6", 0.35f);
            palette.water = Mat("Water", "#8FC4E6", 0.9f, alpha: 0.45f);
            palette.figure = Mat("Figure", "#040404", 0f);
            palette.posters = new[]
            {
                Mat("Poster_Mountains", "#FFFFFF", 0.2f, texture: MountainsPoster(), tile: 1f),
                Mat("Poster_Circles", "#FFFFFF", 0.2f, texture: CirclesPoster(), tile: 1f),
                Mat("Poster_Chart", "#FFFFFF", 0.2f, texture: ChartPoster(), tile: 1f),
            };
            palette.binders = new[]
            {
                Mat("Binder_Blue", "#2B4C8C", 0.35f), Mat("Binder_Red", "#9E2A2A", 0.35f), Mat("Binder_Green", "#2E7D4F", 0.35f),
                Mat("Binder_Black", "#222326", 0.35f), Mat("Binder_Gray", "#6B6E73", 0.35f),
            };

            palette.beige = Mat("Old_Beige", "#D5CAB1", 0.3f);
            palette.bakelite = Mat("Old_Bakelite", "#1C1917", 0.7f);
            palette.brass = Mat("Old_Brass", "#B58E3E", 0.6f, 0.85f);
            palette.chrome = Mat("Old_Chrome", "#C9CDD2", 0.8f, 0.9f);
            palette.tvScreen = Mat("Old_TV_Screen", "#2E3935", 0.92f, emission: new Color(0.05f, 0.07f, 0.06f));
            palette.terminalScreen = Screen("Old_Terminal_Screen", TerminalTexture(), 1.2f);
            palette.grille = Mat("Old_Grille", "#3A332C", 0.05f);
            palette.vintageGreen = Mat("Old_Green", "#557F74", 0.45f);
            palette.tvStatic = Screen("Old_TV_Static", TvStaticTexture(), 1.1f);
            palette.dialGlow = Mat("Old_Dial_Glow", "#F2B45A", 0.5f, emission: new Color(1f, 0.62f, 0.22f) * 1.4f);
            palette.recLamp = Mat("Old_Rec_Lamp", "#FF2A2A", 0.5f, emission: new Color(1f, 0.08f, 0.06f) * 2.5f);
            palette.vhs = LoadOrCreate("VHS", Shader.Find("Office/VHS"));
            // Light touch: the tape look must not hide what is on the desks, and the faults must not twitch.
            palette.vhs.SetFloat("_Noise", 0.02f);
            palette.vhs.SetFloat("_Bleed", 1.2f);
            palette.vhs.SetFloat("_Jitter", 0.0003f);
            palette.vhs.SetFloat("_Tracking", 0.2f);
            palette.vhs.SetFloat("_HeadSwitch", 0.35f);
            palette.vhs.SetFloat("_Dropouts", 0.06f);
            palette.vhs.SetFloat("_Scanlines", 0.08f);
            palette.vhs.SetFloat("_Saturation", 0.85f);
            palette.vhs.SetColor("_Tint", new Color(0.96f, 1f, 0.94f));
            palette.vhs.SetFloat("_Vignette", 0.7f);
            EditorUtility.SetDirty(palette.vhs);
            palette.doorOpen = AssetDatabase.LoadAssetAtPath<AudioClip>(DoorSounds + "/DoorOpen.wav");
            palette.doorClose = AssetDatabase.LoadAssetAtPath<AudioClip>(DoorSounds + "/DoorClosed.wav");

            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();
            return palette;
        }

        static Material Mat(string name, string hex, float smoothness, float metallic = 0f, float alpha = 1f, Texture2D texture = null,
            float tile = 1f, Color? emission = null)
        {
            var m = LoadOrCreate(name, Shader.Find("Universal Render Pipeline/Lit"));
            var color = Hex(hex);
            color.a = alpha;
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            m.SetTexture("_BaseMap", texture);
            m.SetTextureScale("_BaseMap", Vector2.one / tile);
            m.mainTexture = texture;
            m.mainTextureScale = Vector2.one / tile;
            if (alpha < 1f) MakeTransparent(m);
            if (emission.HasValue) Emissive(m, emission.Value);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// URP keeps _EMISSION only while the material's GI flags say it is emissive (its material validation
        /// switches the keyword off otherwise), so the flag matters, not just the keyword.
        /// </summary>
        static void Emissive(Material m, Color color)
        {
            m.SetColor("_EmissionColor", color);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.EnableKeyword("_EMISSION");
        }

        /// <summary>Self-lit picture: dark base so the room light barely touches it, the image in the emission.</summary>
        static Material Screen(string name, Texture2D texture, float brightness)
        {
            var m = Mat(name, "#1A1A1A", 0.8f, texture: texture);
            m.SetTexture("_EmissionMap", texture);
            Emissive(m, Color.white * brightness);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material TextMaterial()
        {
            var m = LoadOrCreate("Text", Shader.Find("Office/Text"));
            m.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material LoadOrCreate(string name, Shader shader)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader)
            {
                m.shader = shader;
            }
            return m;
        }

        static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", 1f);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        // ------------------------------------------------------------------ textures (drawn in code, saved as PNG)

        static Texture2D CarpetTexture()
        {
            var random = new System.Random(1);
            var c = Hex("#4E5865");
            return Texture("Carpet", 64, 64, (x, y) =>
            {
                float k = (float)random.NextDouble() * 0.12f - 0.06f + ((x + y) % 2 == 0 ? 0.03f : -0.02f);
                if (x == 0 || y == 0) k -= 0.14f;
                return Shade(c, k);
            });
        }

        static Texture2D VinylTexture()
        {
            var random = new System.Random(2);
            Color c = Hex("#BEBAB0"), dark = Hex("#8E8A81"), light = Hex("#D9D6CE"), seam = Hex("#A29E94");
            return Texture("Vinyl", 64, 64, (x, y) =>
            {
                if (x == 0 || y == 0) return seam;
                double r = random.NextDouble();
                return r < 0.04 ? dark : r < 0.08 ? light : Shade(c, (float)random.NextDouble() * 0.04f - 0.02f);
            });
        }

        static Texture2D CeilingTexture()
        {
            var random = new System.Random(3);
            Color c = Hex("#EEEEE9"), grid = Hex("#C6C6BF"), hole = Hex("#D4D4CD");
            return Texture("Ceiling_Tile", 64, 64, (x, y) =>
                x < 2 || y < 2 ? grid : random.NextDouble() < 0.05 ? hole : c);
        }

        static Texture2D WoodTexture() => Grain("Wood_Light", Hex("#D3BB95"), Hex("#B79A6F"), true, 4);
        static Texture2D DoorWoodTexture() => Grain("Wood_Door", Hex("#8C6A49"), Hex("#6C4F35"), false, 5);

        static Texture2D Grain(string file, Color light, Color dark, bool alongU, int seed)
        {
            var random = new System.Random(seed);
            return Texture(file, 128, 128, (x, y) =>
            {
                float a = alongU ? y : x, b = alongU ? x : y;
                float t = 0.5f + 0.5f * Mathf.Sin(a * 0.38f + Mathf.Sin(b * 0.024f) * 3f + Mathf.Sin(b * 0.11f + a * 0.05f) * 0.6f);
                return Color.Lerp(light, dark, t * 0.55f + (float)random.NextDouble() * 0.08f);
            });
        }

        static Texture2D FabricTexture()
        {
            var random = new System.Random(5);
            var c = Hex("#6A7482");
            return Texture("Fabric", 64, 64, (x, y) =>
                Shade(c, ((x / 2 + y / 2) % 2 == 0 ? 0.03f : -0.03f) + (float)random.NextDouble() * 0.08f - 0.04f));
        }

        static Texture2D CorkTexture()
        {
            var random = new System.Random(6);
            var c = Hex("#A67C51");
            return Texture("Cork", 64, 64, (x, y) =>
            {
                double r = random.NextDouble();
                return r < 0.12 ? Shade(c, -0.25f) : r < 0.2 ? Shade(c, 0.15f) : Shade(c, (float)random.NextDouble() * 0.1f - 0.05f);
            });
        }

        static Texture2D DesktopTexture()
        {
            Color top = Hex("#2E6FB4"), bottom = Hex("#163A6C"), bar = Hex("#1A1D24"), icon = Hex("#E8C24A"), window = Hex("#F2F3F5"),
                title = Hex("#C9CED6"), line = Hex("#AEB4BD");
            return Texture("Screen_Desktop", 128, 80, (x, y) =>
            {
                if (y < 7) return x >= 2 && x < 9 && y >= 1 && y < 6 ? Hex("#4A90D9") : bar;
                if (x >= 5 && x < 13 && (y >= 62 && y < 70 || y >= 48 && y < 56 || y >= 34 && y < 42)) return icon;
                if (x >= 30 && x < 112 && y >= 16 && y < 68)
                {
                    if (y >= 61) return title;
                    if (x >= 36 && x < 100 && y >= 22 && y < 56 && y % 5 == 0) return line;
                    return window;
                }
                return Color.Lerp(bottom, top, y / 80f);
            }, FilterMode.Bilinear, false);
        }

        static Texture2D EyeTexture()
        {
            var random = new System.Random(7);
            Color background = Hex("#130606"), sclera = Hex("#E7DCD3"), vein = Hex("#B0302A"), iris = Hex("#4A2E1A"), irisLight = Hex("#7A5432");
            return Texture("Screen_Eye", 128, 80, (x, y) =>
            {
                float ex = (x - 64f) / 54f, ey = (y - 40f) / 27f;
                if (Mathf.Abs(ex) >= 1f || Mathf.Abs(ey) >= 1f - ex * ex) return Shade(background, (float)random.NextDouble() * 0.3f);
                float r = Mathf.Sqrt((x - 64f) * (x - 64f) + (y - 40f) * (y - 40f));
                if (r < 6.5f) return Color.black;
                if (r < 17f) return Color.Lerp(iris, irisLight, (float)random.NextDouble() * 0.6f + (r - 6.5f) / 30f);
                float angle = Mathf.Atan2(y - 40f, x - 64f);
                return Mathf.Abs(Mathf.Sin(angle * 9f + r * 0.15f)) < 0.06f && r > 22f ? vein : sclera;
            }, FilterMode.Bilinear, false);
        }

        /// <summary>Night city far away: dark sky, a few stars, black towers with some lit windows.</summary>
        static Texture2D NightViewTexture()
        {
            var random = new System.Random(10);
            var heights = new int[17];
            for (int i = 0; i < heights.Length; i++) heights[i] = random.Next(18, 72);
            var lit = new bool[32 * 26];
            var warm = new bool[lit.Length];
            for (int i = 0; i < lit.Length; i++)
            {
                lit[i] = random.NextDouble() < 0.22;
                warm[i] = random.NextDouble() < 0.75;
            }
            var stars = new bool[128 * 128];
            for (int i = 0; i < stars.Length; i++) stars[i] = random.NextDouble() < 0.004;
            Color skyHigh = Hex("#070B16"), skyLow = Hex("#16203A"), tower = Hex("#030408"), window = Hex("#E8C27A"), coolWindow = Hex("#B9D2FF");
            return Texture("Night_View", 128, 128, (x, y) =>
            {
                int h = heights[x / 8];
                if (y < h)
                {
                    int cell = x / 4 + (y / 5) * 32;
                    bool pane = x % 4 >= 1 && x % 4 <= 2 && y % 5 >= 2 && y % 5 <= 3 && y < h - 3;
                    return pane && lit[cell] ? (warm[cell] ? window : coolWindow) : tower;
                }
                if (stars[y * 128 + x] && y > 80) return new Color(0.75f, 0.8f, 0.95f);
                return Color.Lerp(skyLow, skyHigh, (y - h) / (128f - h));
            }, FilterMode.Bilinear, false);
        }

        /// <summary>Snow on an old TV with no signal.</summary>
        static Texture2D TvStaticTexture()
        {
            var random = new System.Random(12);
            var rows = new float[96];
            for (int i = 0; i < rows.Length; i++) rows[i] = (float)random.NextDouble() * 0.25f - 0.12f;
            return Texture("TV_Static", 128, 96, (x, y) =>
            {
                float v = Mathf.Clamp01((float)random.NextDouble() * 0.85f + 0.1f + rows[y]);
                return new Color(v, v, v * 1.04f);
            }, FilterMode.Point, false);
        }

        /// <summary>Green-on-black text-mode screen for the old CRT monitor.</summary>
        static Texture2D TerminalTexture()
        {
            var random = new System.Random(9);
            Color background = Hex("#06120A"), glyph = Hex("#4CFF7A"), dim = Hex("#1F6B35");
            // Every text row gets a random line length; the glyphs are 3x5 blocks with gaps.
            var lengths = new int[12];
            for (int i = 0; i < lengths.Length; i++) lengths[i] = random.Next(4, 28);
            var cells = new bool[128 * 96];
            for (int i = 0; i < cells.Length; i++) cells[i] = random.NextDouble() < 0.62;
            return Texture("Screen_Terminal", 128, 96, (x, y) =>
            {
                int row = (95 - y) / 8, inRow = (95 - y) % 8, col = (x - 4) / 4, inCol = (x - 4) % 4;
                if (x < 4 || row >= lengths.Length || col >= lengths[row] || inRow < 2 || inCol == 3) return background;
                if (row == lengths.Length - 1 && col == 0) return glyph; // cursor
                return cells[y * 128 + x] ? (row == 0 ? glyph : Color.Lerp(dim, glyph, 0.7f)) : background;
            }, FilterMode.Bilinear, false);
        }

        static Texture2D ClockTexture()
        {
            Color face = Hex("#F7F7F3"), tick = Hex("#222326");
            return Texture("Clock_Face", 128, 128, (x, y) =>
            {
                float dx = x - 63.5f, dy = y - 63.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 61f) return tick;
                float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg; // 0 at 12 o'clock
                float toHour = Mathf.Abs(Mathf.DeltaAngle(angle, Mathf.Round(angle / 30f) * 30f)) * Mathf.Deg2Rad * r;
                float toMinute = Mathf.Abs(Mathf.DeltaAngle(angle, Mathf.Round(angle / 6f) * 6f)) * Mathf.Deg2Rad * r;
                bool quarter = Mathf.Abs(Mathf.DeltaAngle(angle, Mathf.Round(angle / 90f) * 90f)) < 5f;
                if (r > 44f && r < 56f && toHour < (quarter ? 2.6f : 1.6f)) return tick;
                if (r > 52f && r < 56f && toMinute < 0.6f) return tick;
                return face;
            }, FilterMode.Bilinear, false);
        }

        static Texture2D MountainsPoster()
        {
            Color skyTop = Hex("#F6C383"), skyLow = Hex("#E46B58"), sun = Hex("#FFE7AE"), far = Hex("#7B4B69"), near = Hex("#3E2A47"), white = Hex("#F4F2EC");
            return Texture("Poster_Mountains", 96, 128, (x, y) =>
            {
                if (x < 5 || x >= 91 || y >= 123) return white;
                if (y < 20) return y >= 8 && y < 12 && x >= 20 && x < 76 ? Hex("#55505A") : white;
                if (y < 30 + 10 * Mathf.Abs(Mathf.Sin(x * 0.06f + 1f)) + 6 * Mathf.Sin(x * 0.17f)) return near;
                if (y < 48 + 14 * Mathf.Sin(x * 0.07f) + 5 * Mathf.Sin(x * 0.23f)) return far;
                if ((x - 50) * (x - 50) + (y - 78) * (y - 78) < 15 * 15) return sun;
                return Color.Lerp(skyLow, skyTop, (y - 20f) / 103f);
            }, FilterMode.Bilinear, false);
        }

        static Texture2D CirclesPoster()
        {
            Color background = Hex("#2F4B7C"), white = Hex("#F4F2EC"), yellow = Hex("#F2C14E"), red = Hex("#E15554");
            return Texture("Poster_Circles", 96, 128, (x, y) =>
            {
                if (x < 5 || x >= 91 || y >= 123) return white;
                if (y < 22) return y >= 9 && y < 13 && x >= 18 && x < 78 ? Hex("#2F4B7C") : white;
                bool In(int cx, int cy, int r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) < r * r;
                if (In(48, 60, 17)) return red;
                if (In(60, 80, 19)) return yellow;
                if (In(36, 84, 21)) return white;
                return background;
            }, FilterMode.Bilinear, false);
        }

        static Texture2D ChartPoster()
        {
            Color paper = Hex("#F7F7F4"), axis = Hex("#333438"), blue = Hex("#3D7DD8"), red = Hex("#E15554"), green = Hex("#2E9E6A"), orange = Hex("#F08A24");
            int[] heights = { 28, 44, 36, 60 };
            Color[] colors = { blue, red, green, blue };
            return Texture("Poster_Chart", 96, 128, (x, y) =>
            {
                if (y >= 106 && y < 112 && x >= 14 && x < 70) return axis;
                if ((x >= 14 && x < 16 && y >= 24 && y < 100) || (y >= 24 && y < 26 && x >= 14 && x < 86)) return axis;
                float lineY = 36f + (x - 18f) * 0.85f;
                if (x >= 18 && x < 86 && Mathf.Abs(y - lineY) < 1.6f) return orange;
                for (int i = 0; i < heights.Length; i++)
                {
                    int x0 = 22 + i * 16;
                    if (x >= x0 && x < x0 + 10 && y >= 26 && y < 26 + heights[i]) return colors[i];
                }
                return paper;
            }, FilterMode.Bilinear, false);
        }

        /// <summary>Draws a texture pixel by pixel (y = 0 is the bottom row) into Textures/{file}.png and imports it.</summary>
        static Texture2D Texture(string file, int width, int height, Func<int, int, Color> pixel, FilterMode filter = FilterMode.Bilinear, bool repeat = true)
        {
            string path = $"{Root}/Textures/{file}.png";
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, pixel(x, y));
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                var wrap = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                if (importer.wrapMode != wrap || importer.filterMode != filter || importer.anisoLevel != 4)
                {
                    importer.wrapMode = wrap;
                    importer.filterMode = filter;
                    importer.anisoLevel = 4;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Color Shade(Color c, float k) => new Color(Mathf.Clamp01(c.r * (1f + k)), Mathf.Clamp01(c.g * (1f + k)), Mathf.Clamp01(c.b * (1f + k)), 1f);

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // ------------------------------------------------------------------ room prefab

        public static GameObject BuildRoomPrefab(OfficePalette palette)
        {
            EnsureFolder(Root + "/Prefabs");
            EnsureFolder(Root + "/Meshes");
            Prims.ClearCache();
            var room = OfficeRoomBuilder.Build(palette);
            try
            {
                SaveMeshes(room, MeshesPath);
                return PrefabUtility.SaveAsPrefabAsset(room, RoomPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(room);
                Prims.ClearCache();
            }
        }

        /// <summary>The generated meshes must live in an asset for a prefab to keep them.</summary>
        static void SaveMeshes(GameObject root, string path)
        {
            var meshes = root.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh)
                .Where(m => m != null && !AssetDatabase.Contains(m)).Distinct().ToList();
            var container = new Mesh { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(container, path);
            foreach (var mesh in meshes) AssetDatabase.AddObjectToAsset(mesh, container);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ anomaly prefabs

        /// <summary>
        /// Makes a prefab for every old-tech anomaly that does not have one yet (existing ones are left alone) and
        /// returns the library that lists the anomalies folder. Each prefab's pose is its place in the office.
        /// </summary>
        public static AnomalyLibrary BuildAnomalyPrefabs(OfficePalette palette, bool overwrite = false)
        {
            EnsureFolder(AnomalyFolder);
            EnsureFolder(Root + "/Meshes");
            var office = OfficeRoomBuilder.Build(palette); // the office stands at the origin while the models are placed
            try
            {
                foreach (var spec in OfficeRoomBuilder.AnomalySpecs)
                {
                    string path = $"{AnomalyFolder}/{spec.File}.prefab";
                    if (File.Exists(path) && !overwrite) continue;
                    Prims.ClearCache(); // fresh meshes, so this prefab depends on nothing but its own mesh asset
                    var model = spec.Build(office.transform, palette);
                    model.SetParent(null, true);
                    model.name = spec.File;
                    var anomaly = model.gameObject.AddComponent<AnomalyObject>();
                    anomaly.anomalyName = spec.Title;
                    anomaly.replaces = spec.Replaces;
                    AddHighlight(model);
                    SaveMeshes(model.gameObject, $"{Root}/Meshes/Anomaly_{spec.File}_Meshes.asset");
                    PrefabUtility.SaveAsPrefabAsset(model.gameObject, path);
                    Object.DestroyImmediate(model.gameObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(office);
                Prims.ClearCache();
            }

            var library = AssetDatabase.LoadAssetAtPath<AnomalyLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<AnomalyLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            AnomalyLibrarySync.Sync(library);
            return library;
        }

        internal static readonly Color HighlightColor = new Color(1f, 0.13f, 0.08f);
        internal const float HighlightIntensity = 4.5f;

        /// <summary>
        /// A red spot shining down on the object from just under the ceiling, so it stands out in the dark office
        /// (a spot, not a point light: no glowing patch on the ceiling to give the place away).
        /// </summary>
        internal static void AddHighlight(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var go = new GameObject("Highlight");
            go.transform.SetParent(model, false);
            float top = Mathf.Min(OfficeRoomBuilder.Height - 0.1f, bounds.max.y + 1.1f);
            go.transform.position = new Vector3(bounds.center.x, top, bounds.center.z);
            go.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            float height = top - bounds.min.y;
            light.spotAngle = Mathf.Clamp(2f * Mathf.Atan2(Mathf.Max(bounds.extents.x, bounds.extents.z) + 0.35f, height) * Mathf.Rad2Deg, 30f, 120f);
            light.innerSpotAngle = light.spotAngle * 0.5f;
            light.range = height + 1f;
            light.intensity = HighlightIntensity;
            light.color = HighlightColor;
            light.shadows = LightShadows.None;
        }

        // ------------------------------------------------------------------ scene

        public static void BuildScene(OfficePalette palette, GameObject roomPrefab, AnomalyLibrary library, bool overwrite)
        {
            if (!overwrite && File.Exists(ScenePath)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Night: almost no ambient light, the lamps and screens do the work.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.04f, 0.05f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = 0.15f;
            RenderSettings.fog = false;

            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = new Vector3(0f, 0.05f, 0.6f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.75f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.875f, 0f);
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.04f;
            var steps = player.AddComponent<AudioSource>();
            steps.playOnAwake = false;
            steps.spatialBlend = 0f;

            var head = new GameObject("Head") { tag = "MainCamera" };
            head.transform.SetParent(player.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = head.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 120f;
            cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            var cameraData = cam.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            head.AddComponent<AudioListener>();
            head.AddComponent<VhsEffect>().material = palette.vhs;
            var flashlight = head.AddComponent<Flashlight>();
            flashlight.clickSource = steps;
            ConfigureFlashlight(flashlight);

            var playerController = player.AddComponent<PlayerController>();
            playerController.head = head.transform;
            playerController.footsteps = steps;
            ConfigureFootsteps(playerController, palette);
            AddPlayerBody(playerController, flashlight);

            var game = new GameObject("Game");
            var round = game.AddComponent<RoundManager>();
            round.player = player.transform;
            round.palette = palette;
            round.roomPrefab = roomPrefab;
            round.anomalies = library;
            game.AddComponent<RoundHud>().round = round;
            game.AddComponent<GamePause>();
            game.AddComponent<PauseMenu>();
            AddAmbience(round);
            ScareSystem.EditorTools.ScareSetup.AddChase(game, round, playerController, cam);

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = BuildVolumeProfile();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        // ------------------------------------------------------------------ player body

        const string PlayerFolder = "Assets/_Presentation/Player";

        /// <summary>
        /// Brings an existing Office scene up to date: first-person body, torch sounds, Babaika chase, pause with its
        /// settings screen, ambience.
        /// </summary>
        [MenuItem("Office/Upgrade Open Scene")]
        public static void UpgradeOpenScene()
        {
            var controller = Object.FindFirstObjectByType<PlayerController>();
            var round = Object.FindFirstObjectByType<RoundManager>();
            if (controller == null || round == null)
            {
                Debug.LogWarning("Open the Office scene first (it needs a PlayerController and a RoundManager).");
                return;
            }
            var flashlight = controller.GetComponentInChildren<Flashlight>();
            AddPlayerBody(controller, flashlight);
            ConfigureFlashlight(flashlight);
            if (round.GetComponent<GamePause>() == null) round.gameObject.AddComponent<GamePause>();
            if (round.GetComponent<PauseMenu>() == null) round.gameObject.AddComponent<PauseMenu>();
            AddAmbience(round);
            var view = controller.head != null ? controller.head.GetComponent<Camera>() : Camera.main;
            ScareSystem.EditorTools.ScareSetup.AddChase(round.gameObject, round, controller, view);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }

        const string AmbienceFolder = "Assets/_Presentation/Sounds/Ambience";

        /// <summary>The level-driven ambience on the round's object, its stages being the loops in the Ambience folder by name.</summary>
        public static Ambience AddAmbience(RoundManager round)
        {
            var ambience = round.GetComponent<Ambience>();
            if (ambience == null) ambience = round.gameObject.AddComponent<Ambience>();
            ambience.round = round;
            ambience.stages = AssetDatabase.FindAssets("t:AudioClip", new[] { AmbienceFolder })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => AssetDatabase.LoadAssetAtPath<AudioClip>(path))
                .ToArray();
            EditorUtility.SetDirty(ambience);
            return ambience;
        }

        /// <summary>
        /// The first-person body under the player (a little behind the camera) and the torch hand under the camera,
        /// from the Blockbench models in the Player folder. Replaces earlier ones.
        /// </summary>
        public static void AddPlayerBody(PlayerController controller, Flashlight flashlight)
        {
            var bodyPrefab = ModelPrefab(PlayerFolder, "PlayerBody", 0.12f, root => root.AddComponent<PlayerBody>());
            var handPrefab = ModelPrefab(PlayerFolder, "FlashlightHand", 0.35f, null);
            if (bodyPrefab == null || handPrefab == null) return;

            foreach (var old in controller.GetComponentsInChildren<PlayerBody>(true)) Object.DestroyImmediate(old.gameObject);
            var body = (GameObject)PrefabUtility.InstantiatePrefab(bodyPrefab, controller.transform);
            body.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            body.transform.localRotation = Quaternion.identity;
            var playerBody = body.GetComponent<PlayerBody>();
            playerBody.controller = controller;
            playerBody.flashlight = flashlight;

            if (flashlight == null) return;
            if (flashlight.viewmodel != null) Object.DestroyImmediate(flashlight.viewmodel.gameObject);
            var hand = (GameObject)PrefabUtility.InstantiatePrefab(handPrefab, flashlight.transform);
            hand.transform.localPosition = flashlight.loweredPosition;
            hand.transform.localRotation = Quaternion.Euler(flashlight.loweredRotation);
            flashlight.viewmodel = hand.transform;
            flashlight.walker = controller;
            EditorUtility.SetDirty(flashlight);
        }

        /// <summary>
        /// Prefab for a Blockbench model: an empty root (with optional components) over the FBX, whose palette material
        /// is remapped to a URP Lit material next to it.
        /// </summary>
        static GameObject ModelPrefab(string folder, string name, float smoothness, Action<GameObject> setup)
        {
            string fbxPath = $"{folder}/{name}.fbx", prefabPath = $"{folder}/{name}.prefab", matPath = $"{folder}/{name}.mat";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (model == null)
            {
                Debug.LogWarning($"{fbxPath} is missing (export it from Blockbench first).");
                return null;
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{name}.png"));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name + ".png"), mat);
            importer.SaveAndReimport();

            var root = new GameObject(name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            instance.name = "Model";
            setup?.Invoke(root);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        const string FootstepsFolder = "Assets/_Presentation/Sounds/Footsteps";
        const string DoorSounds = "Assets/_Presentation/Sounds/Doors";
        const string FlashlightSounds = "Assets/_Presentation/Sounds/Flashlight";

        /// <summary>The torch's switch sounds from the Flashlight sounds folder.</summary>
        public static void ConfigureFlashlight(Flashlight flashlight)
        {
            if (flashlight == null) return;
            flashlight.onSound = AssetDatabase.LoadAssetAtPath<AudioClip>(FlashlightSounds + "/FonarikON.wav");
            flashlight.offSound = AssetDatabase.LoadAssetAtPath<AudioClip>(FlashlightSounds + "/FonarikOFF.wav");
            EditorUtility.SetDirty(flashlight);
        }

        /// <summary>Carpet steps in the offices, hard-floor steps elsewhere (samples from the Footsteps folder).</summary>
        public static void ConfigureFootsteps(PlayerController player, OfficePalette palette)
        {
            AudioClip[] Clips(string kind) => AssetDatabase.FindAssets("t:AudioClip " + kind, new[] { FootstepsFolder })
                .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null)
                .OrderBy(c => c.name)
                .ToArray();

            player.softSteps = Clips("Kover");
            player.hardSteps = Clips("Plitka");
            player.softFloors = new[] { palette.carpet };
            EditorUtility.SetDirty(player);
        }

        static VolumeProfile BuildVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumePath);
            }

            T Get<T>() where T : VolumeComponent
            {
                if (profile.TryGet(out T component)) return component;
                component = profile.Add<T>();
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
                return component;
            }

            var bloom = Get<Bloom>();
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.65f);
            Get<Tonemapping>().mode.Override(TonemappingMode.Neutral);
            var vignette = Get<Vignette>();
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.5f);
            Get<FilmGrain>().active = false; // the VHS camera has its own grain
            var color = Get<ColorAdjustments>();
            color.saturation.Override(-20f);
            color.contrast.Override(12f);
            color.postExposure.Override(0f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
