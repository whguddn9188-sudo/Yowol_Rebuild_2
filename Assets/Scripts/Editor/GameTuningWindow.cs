#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Yowol.EditorTools
{
    /// <summary>
    /// Editor for Assets/Resources/Tuning/GameTuning.json.
    /// Menu: Yowol > Game Tuning (JSON)
    ///  - HUD / skill icon size and position, skill icons per weapon (drag a sprite in)
    ///  - skill effect scale (120%) and hit-box scale
    ///  - normal-attack wind-up per weapon (great sword 0.35 s)
    ///  - ranged lantern enemy stats, its sprite-sheet animations, and where it spawns
    /// Spawn points of the open stage scene are drawn in the Scene view and can be dragged.
    /// </summary>
    public class GameTuningWindow : EditorWindow
    {
        class Holder : ScriptableObject { public GameTuningData data; }

        const string IconFolder = "Assets/Resources/UI/SkillIcons";
        const string WeaponIconFolder = "Assets/Resources/UI/WeaponIcons";
        static readonly string[] Tabs = { "HUD & Skills", "Weapons", "Lantern", "Animations", "Spawns" };

        Holder holder;
        SerializedObject so;
        int tab;
        Vector2 scroll;
        bool dirty;

        [MenuItem("Yowol/Game Tuning (JSON)", priority = 0)]
        public static void Open() => GetWindow<GameTuningWindow>("Game Tuning");

        void OnEnable()
        {
            LoadFromDisk();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            if (holder != null) DestroyImmediate(holder);
        }

        // ---------------------------------------------------------------- load / save

        void LoadFromDisk()
        {
            GameTuningData d = null;
            if (File.Exists(GameTuning.AssetPath))
            {
                try { d = JsonUtility.FromJson<GameTuningData>(File.ReadAllText(GameTuning.AssetPath)); }
                catch (System.Exception e) { Debug.LogError("[GameTuning] invalid JSON: " + e.Message); }
            }
            SetData(d ?? GameTuning.CreateDefault());
            dirty = false;
        }

        void SetData(GameTuningData d)
        {
            if (holder == null) holder = CreateInstance<Holder>();
            holder.hideFlags = HideFlags.DontSave;
            holder.data = d;
            so = new SerializedObject(holder);
        }

        void Save()
        {
            so.ApplyModifiedProperties();
            Directory.CreateDirectory(Path.GetDirectoryName(GameTuning.AssetPath));
            File.WriteAllText(GameTuning.AssetPath, GameTuning.ToJson(holder.data));
            AssetDatabase.ImportAsset(GameTuning.AssetPath);
            GameTuning.Reload();
            dirty = false;
            ShowNotification(new GUIContent("Saved GameTuning.json"));
        }

        void ApplyToRunningGame()
        {
            so.ApplyModifiedProperties();
            // Deep copy so further edits don't leak in until applied again.
            GameTuning.Set(JsonUtility.FromJson<GameTuningData>(GameTuning.ToJson(holder.data)));
            ShowNotification(new GUIContent("Applied (not saved)"));
        }

        // ---------------------------------------------------------------- GUI

        void OnGUI()
        {
            if (so == null || holder == null) LoadFromDisk();
            so.Update();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(dirty ? "Save *" : "Save", EditorStyles.toolbarButton, GUILayout.Width(70))) Save();
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60))) LoadFromDisk();
                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                    if (GUILayout.Button("Apply to running game", EditorStyles.toolbarButton)) ApplyToRunningGame();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open JSON", EditorStyles.toolbarButton))
                    EditorUtility.RevealInFinder(GameTuning.AssetPath);
                if (GUILayout.Button("Defaults", EditorStyles.toolbarButton) &&
                    EditorUtility.DisplayDialog("Game Tuning", "Reset every value to the built-in defaults? (not saved until you press Save)", "Reset", "Cancel"))
                {
                    SetData(GameTuning.CreateDefault());
                    dirty = true;
                }
            }

            tab = GUILayout.Toolbar(tab, Tabs);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUI.BeginChangeCheck();
            var root = so.FindProperty("data");
            switch (tab)
            {
                case 0: DrawHudTab(root); break;
                case 1: DrawWeaponsTab(root); break;
                case 2: DrawChildren(root.FindPropertyRelative("lantern")); break;
                case 3:
                    EditorGUILayout.HelpBox("Sprite-sheet clips built at runtime. 'sheet' is a path inside a Resources folder (no extension). " +
                                            "Library 'Lantern' = enemy body, 'FX' = effects/projectiles usable with Fx.Play(name).", MessageType.Info);
                    EditorGUILayout.PropertyField(root.FindPropertyRelative("animations"), true);
                    break;
                case 4: DrawSpawnsTab(root); break;
            }
            if (EditorGUI.EndChangeCheck()) dirty = true;
            so.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();
        }

        static void DrawChildren(SerializedProperty p)
        {
            var it = p.Copy();
            var end = it.GetEndProperty();
            if (!it.NextVisible(true)) return;
            while (!SerializedProperty.EqualContents(it, end))
            {
                EditorGUILayout.PropertyField(it, true);
                if (!it.NextVisible(false)) break;
            }
        }

        void DrawHudTab(SerializedProperty root)
        {
            EditorGUILayout.LabelField("E / Q skill slots", EditorStyles.boldLabel);
            DrawChildren(root.FindPropertyRelative("hud"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Skill effects (player E / Q)", EditorStyles.boldLabel);
            DrawChildren(root.FindPropertyRelative("skillFx"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Weapon slot icon / Skill icons (E, Q)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Drag any sprite / texture in. Files outside a Resources folder are copied to Assets/Resources/UI/.", MessageType.None);
            foreach (var w in holder.data.weapons)
            {
                if (w == null) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(w.id, GUILayout.Width(90));
                    w.icon = IconField("Weapon", w.icon, w.id, WeaponIconFolder);
                    w.skill1Icon = IconField("E", w.skill1Icon, w.id + "_E", IconFolder);
                    w.skill2Icon = IconField("Q", w.skill2Icon, w.id + "_Q", IconFolder);
                }
            }
        }

        string IconField(string label, string path, string defaultName, string folder)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(90)))
            {
                GUILayout.Label(label, EditorStyles.miniBoldLabel);
                var current = string.IsNullOrEmpty(path) ? null : Resources.Load<Sprite>(path);
                var picked = (Sprite)EditorGUILayout.ObjectField(current, typeof(Sprite), false, GUILayout.Width(72), GUILayout.Height(72));
                if (picked != current)
                {
                    dirty = true;
                    return picked == null ? "" : ToResourcesPath(picked, defaultName, folder);
                }
            }
            return path;
        }

        /// <summary>Returns the Resources-relative path of a sprite, copying it into the icon folder if needed.</summary>
        static string ToResourcesPath(Sprite sprite, string defaultName, string folder)
        {
            string assetPath = AssetDatabase.GetAssetPath(sprite);
            int i = assetPath.IndexOf("/Resources/", System.StringComparison.Ordinal);
            if (i < 0)
            {
                Directory.CreateDirectory(folder);
                string target = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{defaultName}{Path.GetExtension(assetPath)}");
                AssetDatabase.CopyAsset(assetPath, target);
                var imp = AssetImporter.GetAtPath(target) as TextureImporter;
                if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.mipmapEnabled = false;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                }
                assetPath = target;
                i = assetPath.IndexOf("/Resources/", System.StringComparison.Ordinal);
                Debug.Log($"[GameTuning] Copied icon to {target}");
            }
            string rel = assetPath.Substring(i + "/Resources/".Length);
            return Path.ChangeExtension(rel, null).Replace('\\', '/');
        }

        void DrawWeaponsTab(SerializedProperty root)
        {
            EditorGUILayout.HelpBox("attackWindup = seconds the character holds the wind-up pose before every normal swing (기수 동작). " +
                                    "GreatSword default 0.35. Weapon damage / cooldown / skill numbers stay in Assets/Data/Weapons/*.asset.", MessageType.Info);
            EditorGUILayout.PropertyField(root.FindPropertyRelative("weapons"), true);
        }

        void DrawSpawnsTab(SerializedProperty root)
        {
            string scene = EditorSceneManager.GetActiveScene().name;
            EditorGUILayout.HelpBox($"Enemies spawned at stage start (by StageController). Points for the open scene ('{scene}') " +
                                    "are drawn in the Scene view and can be dragged. Remember to Save.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button($"+ Lantern at Scene view center ({scene})"))
                {
                    so.ApplyModifiedProperties();
                    var sv = SceneView.lastActiveSceneView;
                    Vector3 p = sv != null ? sv.pivot : Vector3.zero;
                    holder.data.spawns.Add(new SpawnDef { scene = scene, enemy = "Lantern", x = Mathf.Round(p.x * 10f) / 10f, y = Mathf.Round(p.y * 10f) / 10f });
                    so.Update();
                    dirty = true;
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.PropertyField(root.FindPropertyRelative("spawns"), true);
        }

        // ---------------------------------------------------------------- scene view handles

        void OnSceneGUI(SceneView view)
        {
            if (holder == null || holder.data == null || holder.data.spawns == null) return;
            string scene = EditorSceneManager.GetActiveScene().name;
            var t = holder.data.lantern;
            for (int i = 0; i < holder.data.spawns.Count; i++)
            {
                var s = holder.data.spawns[i];
                if (s == null || s.scene != scene) continue;
                var pos = new Vector3(s.x, s.y, 0f);
                bool lantern = s.enemy == "Lantern";
                Handles.color = lantern ? new Color(0.35f, 1f, 0.9f, 0.9f) : new Color(1f, 0.5f, 0.5f, 0.9f);
                float h = lantern && t != null ? t.bodySize.y + t.hoverHeight : 2f;
                Handles.DrawWireCube(pos + Vector3.up * h * 0.5f, new Vector3(1f, h, 0f));
                if (lantern && t != null)
                {
                    Handles.color = new Color(0.35f, 1f, 0.9f, 0.25f);
                    Handles.DrawWireDisc(pos + Vector3.up * 1.2f, Vector3.forward, t.attackRange);
                }
                Handles.Label(pos + Vector3.up * (h + 0.4f), $"{s.enemy} #{i}");
                EditorGUI.BeginChangeCheck();
                var np = Handles.FreeMoveHandle(pos + Vector3.up * 0.2f, 0.25f, Vector3.one * 0.1f, Handles.DotHandleCap) - Vector3.up * 0.2f;
                if (EditorGUI.EndChangeCheck())
                {
                    s.x = Mathf.Round(np.x * 10f) / 10f;
                    s.y = Mathf.Round(np.y * 100f) / 100f;
                    so.Update();
                    dirty = true;
                    Repaint();
                }
            }
        }
    }
}
#endif
