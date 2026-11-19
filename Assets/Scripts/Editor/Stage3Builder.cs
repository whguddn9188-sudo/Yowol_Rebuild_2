#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using B = Yowol.EditorTools.YowolBuilder;

namespace Yowol.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/Stage3.unity — two zones in one stage (≈ 364 units long):
    ///   A. "무너진 공장 지대"  x -8 … 160   brick ledges, ramps, steel trusses, moving platform, lift, crumbling bridge
    ///   B. "달빛 옥상"        x 150 … 356  rooftops, iron stairs, truss bridges, exhaust-fan/water-tank steps,
    ///                                      moving / crumbling / lift trusses, deadly alleys between buildings
    /// Backgrounds are layered (sky, far skyline, near rooftops, foreground poles / pillars) with parallax, and
    /// cross-fade between the zones by camera X (BackdropLayer) while the ambient light turns from furnace to moonlight.
    /// Enemies come from GameTuning.json → "spawns" (scene "Stage3"). Order: Stage2 → Stage3 → BossStage.
    /// Menu: Yowol > Build Stage3   (rebuilding overwrites hand edits in Stage3.unity)
    /// </summary>
    public static class Stage3Builder
    {
        const string SceneName = "Stage3";
        const string ScenePath = "Assets/Scenes/Stage3.unity";
        const string Art = "Assets/Art/Sprites/Stage_3/";
        const string Roof = Art + "Rooftops/";
        const float Ppu = 100f;             // factory tiles
        const float RoofPpu = 80f;          // rooftop pack
        const float IronPpu = 180f;         // steel truss
        const float Bottom = -14f;          // solid ground extends down to here
        const float PitKillY = -7f;         // factory pits
        const float AlleyKillY = -3f;       // gaps between rooftops

        const float MinX = -8f, MaxX = 356f;
        const float ZoneSwitch = 155f;      // factory → rooftops (camera X)
        static readonly Vector2 FactoryAnchor = new Vector2(72f, 3f);
        static readonly Vector2 RoofAnchor = new Vector2(255f, 8f);

        static Transform level, deco;
        static Sprite floorS, t0, t1, t2, t3, t5, stair, bg, soft;
        static Sprite sky, bFlat, bRail, facade, stUp, stDown, pole, tank, antenna, fan, iron;
        static Material lit, unlit;
        static System.Random rng;

        [MenuItem("Yowol/Build Stage3", priority = 20)]
        public static void BuildMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Build Stage3", "Stage3.unity를 다시 만듭니다. 씬에서 직접 수정한 내용은 사라집니다. 계속할까요?", "Build", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Debug.Log(BuildScene(true));
        }

        public static string BuildScene(bool keepOpen)
        {
            ImportArt();
            rng = new System.Random(1337);
            var scene = B.NewScene(SceneName);
            var cam = B.MakeCamera(5f, new Color(0.03f, 0.03f, 0.045f));
            B.AddVolume();
            var global = B.AddGlobalLight(new Color(1f, 0.93f, 0.85f), 0.9f);
            var amb = global.gameObject.AddComponent<ZoneAmbience>();
            amb.keys = new[]
            {
                new ZoneAmbience.Key { x = ZoneSwitch - 10f, color = new Color(1f, 0.93f, 0.85f), intensity = 0.9f },
                new ZoneAmbience.Key { x = ZoneSwitch + 8f, color = new Color(0.74f, 0.77f, 1f), intensity = 0.88f },
            };
            lit = B.Mat("Sprite-Lit-Default.mat");
            unlit = B.Mat("Sprite-Unlit-Default.mat");
            level = new GameObject("Level").transform;
            deco = new GameObject("Deco").transform;

            BuildBackdrops();
            BuildFactory();
            BuildRooftops();

            B.Wall("Wall_Left", new Vector2(MinX - 0.5f, 6f), new Vector2(1f, 44f));
            B.Wall("Wall_Right", new Vector2(MaxX + 0.5f, 6f), new Vector2(1f, 44f));

            var sc = B.MakeStageController("STAGE 3", "무너진 공장 지대 · 달빛 옥상", new Vector3(-2f, 0.2f), new Rect(MinX, -9f, MaxX - MinX, 31f), 5f);
            sc.openingLines = new[]
            {
                "공장 지대를 지나 옥상으로 올라가야 해요. 발판이 위아래로 엇갈려 있네요.",
                "아래로 떨어지면 다시 올라올 수 없어요. 발밑을 조심하세요!",
            };
            sc.clearLines = new[] { "혼령들이 잠잠해졌어요. 이 옥상 끝에서 『혼돈』의 기운이 느껴져요..." };
            sc.exit = B.Exit(new Vector2(352f, 9.4f), new[] { "저 너머가 『혼돈』이 기다리는 옥상이에요.", "각오는 되셨나요, 창조자님?" });

            // Guide lines along the way
            B.Trigger(new Vector2(6f, 2f), new Vector2(1.5f, 4f), new[] { "오르막은 그냥 걸어 올라가면 돼요. 그 너머 구덩이는 떨어지면 끝이에요." });
            B.Trigger(new Vector2(60f, 2f), new Vector2(1.5f, 4f), new[] { "움직이는 발판이에요. 올라타면 건너편까지 데려다줘요." });
            B.Trigger(new Vector2(80f, 2f), new Vector2(1.5f, 4f), new[] { "승강 발판으로 위층에 올라가세요. 위에서 등불 혼령이 노리고 있어요!" });
            B.Trigger(new Vector2(97f, 8.5f), new Vector2(1.5f, 4f), new[] { "금 간 벽돌은 밟으면 무너져요. 멈추지 말고 건너세요!" });
            B.Trigger(new Vector2(153f, 2.5f), new Vector2(1.5f, 5f), new[] { "공장을 빠져나왔어요! 여기서부터는 달빛 옥상이에요.", "건물 사이 골목으로 떨어지면 끝이에요. 철골 다리를 이용하세요." });
            B.Trigger(new Vector2(188f, 4.5f), new Vector2(1.5f, 4f), new[] { "환풍기를 밟고 윗 옥상으로 올라가세요." });
            B.Trigger(new Vector2(210f, 7f), new Vector2(1.5f, 4f), new[] { "움직이는 철골을 타고 건너세요." });
            B.Trigger(new Vector2(259f, 9f), new Vector2(1.5f, 4f), new[] { "녹슨 철골은 밟으면 금방 떨어져요!" });
            B.Trigger(new Vector2(309f, 6f), new Vector2(1.5f, 4f), new[] { "승강 철골이 꼭대기 옥상까지 데려다줄 거예요." });

            B.GuideSpiritObject(new Vector3(-3f, 2.4f));
            cam.transform.position = new Vector3(-2f, 2.3f, -10f);

            B.Save(scene, SceneName);
            AddToBuildSettings();
            LinkStage2Exit();
            if (keepOpen) EditorSceneManager.OpenScene(ScenePath);
            return $"Scene Stage3 built: {ScenePath} (length {MaxX - MinX:0} units)";
        }

        // ================================================================== A. FACTORY  (x -8 … 160)

        static void BuildFactory()
        {
            // 1. Start yard + ramp up
            Solid("Ground_Start", -8f, 21.05f, 0f);
            Ramp("Ramp_Up_1", 16f, 0f, true);
            Bricks("Ledge_A", 21.05f, 31f, 2.55f);

            // 2. Pit: brick step, then a steel truss
            Pit(31f, 41.8f, PitKillY, true);
            Block("Step_1", t5, 33.0f, 3.3f);
            Truss("Truss_Pit1", 36.4f, 3.8f, 0.45f);
            Bricks("Ledge_B", 41.8f, 50f, 2.55f);
            Ramp("Ramp_Down_1", 50f, 0f, false);
            Solid("Ground_Mid", 50f, 62f, 0f);
            Truss("Truss_Overpass", 52.5f, 3.4f, 0.42f);   // optional perch over the reaper

            // 3. Wide pit crossed by a moving platform
            Pit(62f, 76f, PitKillY, true);
            Mover("Mover_Pit", t3, 62.3f, 0f, new Vector2(9.0f, 0f), 2.6f, 0.7f, 0f);

            // 4. Lift up to the high ledge
            Solid("Ground_Lift", 76f, 88f, 0f);
            Mover("Elevator", t5, 85.2f, 1.75f, new Vector2(0f, 4.8f), 2.4f, 1.0f, 0f);
            Bricks("HighLedge", 88f, 100f, 6.5f);

            // 5. Crumbling bridge over a deep pit
            Pit(100f, 112.4f, PitKillY, true);
            for (int i = 0; i < 4; i++) Crumble($"Crumble_{i}", 100.35f + i * 3.0f, 6.5f);

            // 6. Ramps down, then iron stairs up and out of the factory
            Bricks("Ledge_C", 112.4f, 118f, 6.0f);
            Ramp("Ramp_Down_2", 118f, 3.45f, false);
            Bricks("Ledge_D", 123.05f, 127f, 3.45f);
            Ramp("Ramp_Down_3", 127f, 0.9f, false);
            Solid("Ground_Exit", 118f, 160f, 0.9f, 127f);
            Stairs("Stairs_Out", 150f, 0.9f, true);            // → 159.6, 2.9 (first rooftop)

            foreach (var p in new[] { new Vector2(10, 5), new Vector2(46, 7), new Vector2(70, 4), new Vector2(94, 11), new Vector2(124, 9), new Vector2(141, 5) })
                B.AddPointLight(deco, new Vector3(p.x, p.y, 0f), new Color(1f, 0.62f, 0.32f), 9f, 0.55f).name = "Light_Furnace";
        }

        // ================================================================== B. ROOFTOPS  (x 159.6 … 356)

        static void BuildRooftops()
        {
            // R1 → truss bridge → R2
            Building("R1", 159.6f, 178f, 2.9f, true);
            Antenna(162.5f, 2.9f, false);
            Tank(172f, 2.9f, false);
            Pit(178f, 183.5f, AlleyKillY, false);
            Truss("Bridge_1", 177.4f, 2.92f, 0.6f);

            // R2: exhaust fan is the step up to R3
            Building("R2", 183.5f, 196f, 2.9f, false);
            Fan(191f, 2.9f);
            RoofPole(186.5f, 2.9f);

            // R3 (higher) → moving truss over a wide alley → R4
            Building("R3", 196f, 212f, 5.6f, false);
            Antenna(199.5f, 5.6f, true);
            Pit(212f, 224f, AlleyKillY, false);
            TrussMover("Mover_Roof", 212.3f, 5.6f, 0.42f, new Vector2(6.5f, 0f), 2.4f, 0.8f);

            // R4 + iron stairs up to R5
            Building("R4", 224f, 245.6f, 5.6f, false);
            Tank(225.2f, 5.6f, false);
            Stairs("Stairs_R4", 236f, 5.6f, true);               // → 245.6, 7.6

            // R5 (railing) → crumbling rusty trusses → R6
            Building("R5", 245.6f, 262f, 7.6f, true);
            RoofPole(249f, 7.6f);
            Pit(262f, 276f, AlleyKillY, false);
            for (int i = 0; i < 4; i++) TrussCrumble($"RustyTruss_{i}", 262.4f + i * 3.5f, 7.6f, 0.27f);

            // R6: exhaust fan step (optional perch over the lantern), then stairs down onto R7
            Building("R6", 276f, 290f, 6.4f, false);
            Tank(287.2f, 6.4f, false);
            Fan(279.5f, 6.4f);
            Stairs("Stairs_R6", 290f, 4.4f, false);              // 290, 6.4 → 299.6, 4.4
            Building("R7", 290f, 312f, 4.4f, false);
            Fan(307.2f, 4.4f);
            RoofPole(302f, 4.4f);

            // Lift truss up the side of the tallest building
            Pit(312f, 319f, AlleyKillY, false);
            TrussMover("Lift_Roof", 312.6f, 4.4f, 0.5f, new Vector2(0f, 5.0f), 2.6f, 1.1f);

            // R8: final roof (exit at 352)
            Building("R8a", 319f, 336f, 9.4f, false);
            Building("R8b", 336f, MaxX, 9.4f, true);
            Antenna(323f, 9.4f, false);
            Tank(332f, 9.4f, false);
            RoofPole(330f, 9.4f);

            // wires between the rooftop poles
            WireChain(level, new[] { new Vector2(186.5f, 2.9f), new Vector2(249f, 7.6f), new Vector2(302f, 4.4f), new Vector2(330f, 9.4f) }, 3, new Color(0.05f, 0.05f, 0.06f, 1f), 0.05f);

            // a few warm windows
            foreach (var p in new[] { new Vector2(166, 0.5f), new Vector2(204, 3.2f), new Vector2(231, 3.0f), new Vector2(253, 5.0f), new Vector2(283, 4.0f), new Vector2(328, 7.0f), new Vector2(347, 6.5f) })
                B.AddPointLight(deco, new Vector3(p.x, p.y, 0f), new Color(1f, 0.78f, 0.45f), 3.2f, 0.45f).name = "Light_Window";
        }

        // ================================================================== backdrops

        static void BuildBackdrops()
        {
            // Sky / skyline plate (always present, the factory art covers it in zone A)
            var skyGo = Spr("Sky_VioletDawn", sky, Vector2.zero, deco, -100, null, new Color(0.92f, 0.9f, 0.95f), unlit);
            skyGo.transform.localScale = Vector3.one * 2.6f;
            skyGo.transform.position = new Vector3(RoofAnchor.x, RoofAnchor.y - 2f, 0f);
            Layer(skyGo.transform, new Vector2(0.94f, 0.94f), RoofAnchor, false);

            // ---- Zone A: factory
            var fBg = Group("Factory_BG", new Vector2(0.9f, 0.9f), FactoryAnchor, float.NegativeInfinity, ZoneSwitch - 5f);
            var bgGo = Spr("BG_Factory", bg, new Vector2(FactoryAnchor.x, FactoryAnchor.y + 1f), fBg, -95, null, new Color(0.72f, 0.68f, 0.64f), unlit);
            bgGo.transform.localScale = Vector3.one * 6.5f;

            var fMid = Group("Factory_Structure", new Vector2(0.55f, 0.6f), FactoryAnchor, float.NegativeInfinity, ZoneSwitch - 5f);
            var pillarC = new Color(0.44f, 0.39f, 0.34f, 0.9f);     // mid-ground: lighter than the foreground, darker than the level
            for (float x = 30f; x <= 124f; x += 16f)
                Spr("Pillar", t1, new Vector2(x, -24f), fMid, -90, new Vector2(1.0f, 50f), pillarC, unlit);
            for (int i = 0; i < 8; i++)
            {
                float x = 26f + i * 13f, y = 9.5f + (i % 3) * 1.4f;
                var g = Spr("Girder", iron, new Vector2(x, y), fMid, -89, null, new Color(0.4f, 0.37f, 0.34f, 0.9f), unlit);
                g.transform.localScale = Vector3.one * 0.85f;
            }
            for (float x = 30f; x <= 120f; x += 11f)
                Line(fMid, new[] { new Vector3(x, 22f), new Vector3(x + 0.3f, 13f + (float)rng.NextDouble() * 3f) }, new Color(0.12f, 0.11f, 0.1f), 0.08f, -88);

            var fFront = Group("Factory_Foreground", new Vector2(-0.3f, 0f), FactoryAnchor, float.NegativeInfinity, ZoneSwitch - 5f);
            for (float x = -28f; x <= 198f; x += 22f)
            {
                // semi-transparent so a pillar passing in front never hides the player
                Spr("FG_Pillar", t1, new Vector2(x, -16f), fFront, 60, new Vector2(0.8f, 44f), new Color(0.045f, 0.04f, 0.035f, 0.55f), unlit);
                var beam = Spr("FG_Beam", iron, new Vector2(x - 4f, 12.5f), fFront, 61, null, new Color(0.05f, 0.045f, 0.04f, 0.7f), unlit);
                beam.transform.localScale = Vector3.one * 0.8f;
            }

            // ---- Zone B: rooftops
            var far = Group("Roof_FarCity", new Vector2(0.7f, 0.75f), RoofAnchor, ZoneSwitch + 5f, float.PositiveInfinity);
            Skyline(far, 210f, 300f, 0.55f, 3f, 9f, new Color(0.17f, 0.15f, 0.22f), -90, false);

            var near = Group("Roof_NearCity", new Vector2(0.4f, 0.5f), RoofAnchor, ZoneSwitch + 5f, float.PositiveInfinity);
            Skyline(near, 182f, 322f, 0.8f, 0f, 6f, new Color(0.27f, 0.25f, 0.32f), -80, true);

            var front = Group("Roof_Foreground", new Vector2(-0.25f, -0.1f), RoofAnchor, ZoneSwitch + 5f, float.PositiveInfinity);
            var tops = new System.Collections.Generic.List<Vector2>();
            for (float x = 118f; x <= 384f; x += 24f + (float)rng.NextDouble() * 6f)
            {
                var p = Spr("FG_Pole", pole, new Vector2(x, -8.5f), front, 62, null, new Color(0.035f, 0.035f, 0.045f, 0.85f), unlit);
                p.transform.localScale = Vector3.one * 1.15f;
                tops.Add(new Vector2(x + pole.bounds.size.x * 1.15f * 0.5f, -8.5f + pole.bounds.size.y * 1.15f * 0.93f));
            }
            for (int i = 0; i + 1 < tops.Count; i++)
                for (int k = 0; k < 2; k++)
                    Line(front, Sag(tops[i] + Vector2.down * (k * 0.35f), tops[i + 1] + Vector2.down * (k * 0.35f), 1.3f), new Color(0.03f, 0.03f, 0.04f), 0.06f, 61);
        }

        /// <summary>Row of silhouette buildings with tanks / antennas / poles on top.</summary>
        static void Skyline(Transform parent, float x0, float x1, float scale, float topMin, float topMax, Color tint, int order, bool props)
        {
            float x = x0;
            var poleTops = new System.Collections.Generic.List<Vector2>();
            while (x < x1)
            {
                bool railing = rng.NextDouble() < 0.35;
                var s = railing ? bRail : bFlat;
                float w = s.bounds.size.x * scale * (railing ? 1f : (0.6f + (float)rng.NextDouble() * 0.6f));
                float top = topMin + (float)rng.NextDouble() * (topMax - topMin);
                float h = s.bounds.size.y * scale;
                float y = top - h;
                var b = Spr("Bld", s, new Vector2(x, y), parent, order + 1, new Vector2(w / scale, s.bounds.size.y), tint, unlit);
                b.transform.localScale = Vector3.one * scale;
                var f = Spr("Facade", facade, new Vector2(x, -30f), parent, order, new Vector2(w / scale, (y + 30f) / scale + 0.05f), Dim(tint, 0.92f), unlit);
                f.transform.localScale = Vector3.one * scale;
                if (props && rng.NextDouble() < 0.7)
                {
                    float roll = (float)rng.NextDouble();
                    float px = x + w * (0.2f + (float)rng.NextDouble() * 0.5f);
                    float roofY = railing ? y + 2.55f * scale : top;
                    if (roll < 0.35f) PropSilhouette(parent, tank, px, roofY, scale * 0.9f, tint, order + 2);
                    else if (roll < 0.65f) PropSilhouette(parent, antenna, px, roofY, scale * 0.9f, tint, order + 2);
                    else
                    {
                        PropSilhouette(parent, pole, px, roofY, scale * 0.55f, Dim(tint, 0.9f), order + 2);
                        poleTops.Add(new Vector2(px + pole.bounds.size.x * scale * 0.55f * 0.5f, roofY + pole.bounds.size.y * scale * 0.55f * 0.93f));
                    }
                }
                else if (!props && rng.NextDouble() < 0.45)
                    PropSilhouette(parent, rng.NextDouble() < 0.5 ? tank : antenna, x + w * 0.4f, railing ? y + 2.55f * scale : top, scale * 0.8f, tint, order + 2);
                x += w + (rng.NextDouble() < 0.3 ? 0.4f + (float)rng.NextDouble() * 1.5f : 0f);
            }
            for (int i = 0; i + 1 < poleTops.Count; i++)
                if (poleTops[i + 1].x - poleTops[i].x < 30f)
                    Line(parent, Sag(poleTops[i], poleTops[i + 1], 0.8f), Dim(tint, 0.6f), 0.04f, order + 3);
        }

        static void PropSilhouette(Transform parent, Sprite s, float x, float baseY, float scale, Color tint, int order)
        {
            var go = Spr(s.name, s, new Vector2(x, baseY), parent, order, null, tint, unlit);
            go.transform.localScale = Vector3.one * scale;
        }

        static Transform Group(string name, Vector2 factor, Vector2 anchor, float showFrom, float showTo)
        {
            var go = new GameObject(name);
            go.transform.SetParent(deco, false);
            Layer(go.transform, factor, anchor, true, showFrom, showTo);
            return go.transform;
        }

        static void Layer(Transform t, Vector2 factor, Vector2 anchor, bool zone, float showFrom = float.NegativeInfinity, float showTo = float.PositiveInfinity)
        {
            var l = t.gameObject.AddComponent<BackdropLayer>();
            l.factor = factor; l.anchor = anchor;
            l.useZone = zone; l.showFrom = showFrom; l.showTo = showTo; l.fade = 12f;
        }

        // ================================================================== factory pieces

        /// <summary>Ground with the floor strip on top and a dark brick mass down to Bottom. Collider covers it all.</summary>
        static void Solid(string name, float x0, float x1, float top, float surfaceFrom = float.NaN)
        {
            var root = Piece(name, x0, x1, Bottom, top);
            float fx0 = float.IsNaN(surfaceFrom) ? x0 : surfaceFrom;
            Spr("Floor", floorS, new Vector2(fx0, top), root, 2, new Vector2(x1 - fx0, floorS.bounds.size.y));
            Fill(root, x0, x1, top - floorS.bounds.size.y + 0.02f);
        }

        static void Bricks(string name, float x0, float x1, float top)
        {
            var root = Piece(name, x0, x1, Bottom, top);
            float h = t1.bounds.size.y, wl = t0.bounds.size.x, wr = t2.bounds.size.x;
            float y = top - h;
            Spr("Left", t0, new Vector2(x0, y), root, 3);
            Spr("Mid", t1, new Vector2(x0 + wl, y), root, 3, new Vector2(Mathf.Max(0.1f, x1 - x0 - wl - wr), h));
            Spr("Right", t2, new Vector2(x1 - wr, y), root, 3);
            Spr("Lower", t1, new Vector2(x0, Bottom), root, 1, new Vector2(x1 - x0, y - Bottom), FillColor);
        }

        static GameObject Block(string name, Sprite s, float x, float top)
        {
            var go = Spr(name, s, new Vector2(x, top - s.bounds.size.y), level, 4);
            go.layer = Layers.Ground;
            go.AddComponent<BoxCollider2D>();
            return go;
        }

        static void Ramp(string name, float x0, float yBottom, bool rising)
        {
            float w = stair.bounds.size.x, h = stair.bounds.size.y;
            var go = Spr(name, stair, new Vector2(x0, yBottom), level, 3);
            go.GetComponent<SpriteRenderer>().flipX = rising;
            if (rising) go.transform.position = new Vector3(x0 + w, yBottom, 0f);
            go.layer = Layers.Ground;
            var poly = go.AddComponent<PolygonCollider2D>();
            poly.points = rising
                ? new[] { new Vector2(-w, 0f), new Vector2(0f, 0f), new Vector2(0f, h - 0.04f), new Vector2(-0.25f, h - 0.04f) }
                : new[] { new Vector2(0f, 0f), new Vector2(0f, h - 0.04f), new Vector2(0.25f, h - 0.04f), new Vector2(w, 0f) };
            Fill(go.transform, x0, x0 + w, yBottom + 0.02f, true);
        }

        static void Pit(float x0, float x1, float killY, bool embers)
        {
            var go = new GameObject("DeathPit");
            go.transform.SetParent(level, false);
            go.transform.position = new Vector3((x0 + x1) / 2f, killY, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(x1 - x0 + 2f, 2f);
            go.AddComponent<KillZone>().instantKill = true;
            if (embers)
            {
                var glow = Spr("Glow", soft, Vector2.zero, go.transform, -5, null, new Color(1f, 0.35f, 0.15f, 0.55f), unlit);
                glow.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                glow.transform.localScale = new Vector3((x1 - x0) * 0.9f, 6f, 1f) / Mathf.Max(0.01f, soft.bounds.size.x);
                B.AddPointLight(go.transform, new Vector3(0f, 2f, 0f), new Color(1f, 0.35f, 0.15f), Mathf.Max(4f, (x1 - x0) * 0.6f), 0.8f).name = "PitLight";
            }
            else
            {
                // a cold street lamp far down the alley
                B.AddPointLight(go.transform, new Vector3(0f, 1.5f, 0f), new Color(0.55f, 0.65f, 1f), Mathf.Max(3f, (x1 - x0) * 0.5f), 0.5f).name = "AlleyLight";
            }
        }

        static void Mover(string name, Sprite s, float x, float top, Vector2 travel, float duration, float pause, float phase)
        {
            var go = Block(name, s, x, top);
            go.GetComponent<SpriteRenderer>().color = new Color(0.9f, 0.85f, 0.78f);
            var m = go.AddComponent<MovingPlatform>();
            m.travel = travel; m.duration = duration; m.pause = pause; m.phase = phase;
            B.AddPointLight(go.transform, new Vector3(s.bounds.size.x / 2f, s.bounds.size.y + 0.3f, 0f), new Color(1f, 0.8f, 0.5f), 2.5f, 0.5f);
        }

        static void Crumble(string name, float x, float top)
        {
            var go = Block(name, t5, x, top);
            go.GetComponent<SpriteRenderer>().color = new Color(0.85f, 0.72f, 0.66f);
            go.AddComponent<CrumblePlatform>();
        }

        // ================================================================== rooftop pieces

        /// <summary>Building whose roof is walkable at <paramref name="top"/>; facade continues down to Bottom.</summary>
        static void Building(string name, float x0, float x1, float top, bool railing)
        {
            var root = Piece(name, x0, x1, Bottom, top);
            var s = railing ? bRail : bFlat;
            float h = s.bounds.size.y;
            float roofFromBottom = railing ? 204f / RoofPpu : h;          // railing art: the roof line sits under the rail
            float y = top - roofFromBottom;
            Spr("Roof", s, new Vector2(x0, y), root, 2, new Vector2(x1 - x0, h));
            Spr("Facade", facade, new Vector2(x0, Bottom), root, 1, new Vector2(x1 - x0, y - Bottom + 0.03f), new Color(0.9f, 0.9f, 0.92f));
        }

        /// <summary>Iron stairs (≈12° ramp). x0 = left end; yLow = roof height at the low end.</summary>
        static void Stairs(string name, float x0, float yLow, bool up)
        {
            var s = up ? stUp : stDown;
            float w = s.bounds.size.x;
            float rise = 160f / RoofPpu;                 // 2.0 units between the two ends
            float yLeft = up ? yLow : yLow + rise;       // walk height at the left end
            var go = Spr(name, s, new Vector2(x0, yLeft - (up ? 12f : 172f) / RoofPpu), level, 3);
            go.layer = Layers.Ground;
            var edge = go.AddComponent<EdgeCollider2D>();
            float a = (up ? 12f : 172f) / RoofPpu, b = (up ? 172f : 12f) / RoofPpu;
            edge.points = up
                ? new[] { new Vector2(0f, a), new Vector2(w, b), new Vector2(w + 0.3f, b) }
                : new[] { new Vector2(-0.3f, a), new Vector2(0f, a), new Vector2(w, b) };
        }

        static GameObject TrussObj(string name, float x, float deckTop, float scale, int order = 4)
        {
            var go = Spr(name, iron, new Vector2(x, deckTop + 0.03f), level, order);
            go.transform.localScale = Vector3.one * scale;
            go.layer = Layers.Ground;
            var box = go.AddComponent<BoxCollider2D>();
            float w = iron.bounds.size.x, thick = 0.38f / scale;
            box.size = new Vector2(w, thick);
            box.offset = new Vector2(w / 2f, -0.03f / scale - thick / 2f);
            box.usedByEffector = true;
            var eff = go.AddComponent<PlatformEffector2D>();     // jump up through it, land on top
            eff.useOneWay = true;
            eff.surfaceArc = 160f;
            return go;
        }

        static void Truss(string name, float x, float deckTop, float scale) => TrussObj(name, x, deckTop, scale);

        static void TrussMover(string name, float x, float deckTop, float scale, Vector2 travel, float duration, float pause)
        {
            var go = TrussObj(name, x, deckTop, scale);
            var m = go.AddComponent<MovingPlatform>();
            m.travel = travel; m.duration = duration; m.pause = pause;
            B.AddPointLight(go.transform, new Vector3(iron.bounds.size.x / 2f, 0.8f / scale, 0f), new Color(0.75f, 0.82f, 1f), 2.6f / scale, 0.55f);
        }

        static void TrussCrumble(string name, float x, float deckTop, float scale)
        {
            var go = TrussObj(name, x, deckTop, scale);
            go.GetComponent<SpriteRenderer>().color = new Color(0.9f, 0.68f, 0.55f);   // rusty = unstable
            go.AddComponent<CrumblePlatform>();
        }

        static void Tank(float x, float roofY, bool solid)
        {
            var go = Spr("Water_Tank", tank, new Vector2(x, roofY), level, solid ? 4 : 1);
            if (!solid) return;
            go.layer = Layers.Ground;
            var box = go.AddComponent<BoxCollider2D>();
            float w = tank.bounds.size.x, h = tank.bounds.size.y;
            box.size = new Vector2(w * 0.78f, h * 0.62f);
            box.offset = new Vector2(w * 0.43f, h * 0.32f + box.size.y / 2f);
        }

        static void Fan(float x, float roofY)
        {
            var go = Spr("Exhaust_Fan", fan, new Vector2(x, roofY), level, 4);
            go.layer = Layers.Ground;
            var box = go.AddComponent<BoxCollider2D>();
            float w = fan.bounds.size.x, h = fan.bounds.size.y;
            box.size = new Vector2(w * 0.62f, h * 0.95f);                    // the box part, not the duct
            box.offset = new Vector2(w * 0.33f, box.size.y / 2f);
        }

        static void Antenna(float x, float roofY, bool flip)
        {
            var go = Spr("TV_Antenna", antenna, new Vector2(x, roofY), level, 1);
            go.GetComponent<SpriteRenderer>().flipX = flip;
            go.transform.localScale = Vector3.one * 0.8f;
        }

        static void RoofPole(float x, float roofY)
        {
            var go = Spr("Utility_Pole", pole, new Vector2(x - pole.bounds.size.x * 0.35f, roofY), level, 1);
            go.transform.localScale = Vector3.one * 0.7f;
        }

        static void WireChain(Transform parent, Vector2[] poleBases, int count, Color c, float width)
        {
            float topY = pole.bounds.size.y * 0.7f * 0.93f;
            for (int i = 0; i + 1 < poleBases.Length; i++)
                for (int k = 0; k < count; k++)
                {
                    var a = poleBases[i] + new Vector2(0f, topY - k * 0.3f);
                    var b = poleBases[i + 1] + new Vector2(0f, topY - k * 0.3f);
                    Line(parent, Sag(a, b, 1.6f + k * 0.2f), c, width, 0);
                }
        }

        // ================================================================== helpers

        static Color Dim(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        static Vector3[] Sag(Vector2 a, Vector2 b, float sag, int n = 18)
        {
            var pts = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (n - 1f);
                var p = Vector2.Lerp(a, b, t);
                p.y -= sag * 4f * t * (1f - t);
                pts[i] = p;
            }
            return pts;
        }

        static void Line(Transform parent, Vector3[] pts, Color c, float width, int order)
        {
            var go = new GameObject("Wire");
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = pts.Length;
            lr.SetPositions(pts);
            lr.startWidth = lr.endWidth = width;
            lr.sharedMaterial = unlit;
            lr.startColor = lr.endColor = c;
            lr.sortingOrder = order;
            lr.numCapVertices = 2;
        }

        static Transform Piece(string name, float x0, float x1, float y0, float y1)
        {
            var go = new GameObject(name) { layer = Layers.Ground };
            go.transform.SetParent(level, false);
            go.transform.position = Vector3.zero;
            var box = go.AddComponent<BoxCollider2D>();
            box.offset = new Vector2((x0 + x1) / 2f, (y0 + y1) / 2f);
            box.size = new Vector2(x1 - x0, y1 - y0);
            return go.transform;
        }

        static readonly Color FillColor = new Color(0.36f, 0.34f, 0.35f);

        static void Fill(Transform parent, float x0, float x1, float top, bool behind = false)
        {
            Spr("Fill", t1, new Vector2(x0, Bottom), parent, behind ? 0 : 1, new Vector2(x1 - x0, top - Bottom), FillColor);
        }

        static GameObject Spr(string name, Sprite s, Vector2 pos, Transform parent, int order, Vector2? tiled = null, Color? color = null, Material mat = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sharedMaterial = mat != null ? mat : lit;
            sr.sortingOrder = order;
            if (color.HasValue) sr.color = color.Value;
            if (tiled.HasValue)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = tiled.Value;
            }
            return go;
        }

        static void ImportArt()
        {
            B.EnsureFolder("Assets/Art/Sprites/Stage_3");
            var tl = new Vector2(0f, 1f); var bl = Vector2.zero; var c = new Vector2(0.5f, 0.5f);
            floorS = B.ImportSingle(Art + "floor.png", Ppu, tl, true);
            t0 = B.ImportSingle(Art + "Tile_0.png", Ppu, bl, true);
            t1 = B.ImportSingle(Art + "Tile_1.png", Ppu, bl, true);
            t2 = B.ImportSingle(Art + "Tile_2.png", Ppu, bl, true);
            t3 = B.ImportSingle(Art + "Tile_3.png", Ppu, bl, false);
            B.ImportSingle(Art + "Tile_4.png", Ppu, bl, false);
            t5 = B.ImportSingle(Art + "Tile_5.png", Ppu, bl, false);
            stair = B.ImportSingle(Art + "Stair_0.png", Ppu, bl, false);
            bg = B.ImportSingle(Art + "Stage_3_BG.jpg", Ppu, c, false);
            iron = B.ImportSingle(Art + "Iron_Frame.png", IronPpu, tl, false);
            soft = B.SpriteAt("Assets/Art/Generated/soft_circle.png");

            sky = B.ImportSingle(Roof + "Violet_Dawn.png", Ppu, c, false);
            bFlat = B.ImportSingle(Roof + "Building_Flat.png", RoofPpu, bl, false);
            bRail = B.ImportSingle(Roof + "Building_Railing.png", RoofPpu, bl, false);
            facade = B.ImportSingle(Roof + "Facade_Extension.png", RoofPpu, bl, false);
            stUp = B.ImportSingle(Roof + "Iron_Stairs_Up.png", RoofPpu, bl, false);
            stDown = B.ImportSingle(Roof + "Iron_Stairs_Down.png", RoofPpu, bl, false);
            pole = B.ImportSingle(Roof + "Utility_Pole.png", RoofPpu, bl, false);
            tank = B.ImportSingle(Roof + "Water_Tank.png", RoofPpu, bl, false);
            antenna = B.ImportSingle(Roof + "TV_Antenna.png", RoofPpu, bl, false);
            fan = B.ImportSingle(Roof + "Exhaust_Fan.png", RoofPpu, bl, false);
        }

        static void AddToBuildSettings()
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == ScenePath)) return;
            int boss = list.FindIndex(s => s.path.EndsWith("/BossStage.unity"));
            var entry = new EditorBuildSettingsScene(ScenePath, true);
            if (boss >= 0) list.Insert(boss, entry); else list.Add(entry);
            EditorBuildSettings.scenes = list.ToArray();
        }

        /// <summary>Stage2's exit leads into the factory.</summary>
        static void LinkStage2Exit()
        {
            const string stage2 = "Assets/Scenes/Stage2.unity";
            if (!File.Exists(stage2)) return;
            // Opened on its own (not additively) so two stages' global lights never coexist.
            var s2 = EditorSceneManager.OpenScene(stage2, OpenSceneMode.Single);
            foreach (var root in s2.GetRootGameObjects())
                foreach (var exit in root.GetComponentsInChildren<StageExit>(true))
                {
                    Undo.RecordObject(exit, "Stage2 exit lines");
                    exit.enterLines = new[] { "이 건물 뒤편은 무너진 공장 지대로 이어져요.", "발밑을 조심하면서 가죠." };
                    EditorUtility.SetDirty(exit);
                }
            EditorSceneManager.SaveScene(s2);
        }
    }
}
#endif
