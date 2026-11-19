using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Yowol
{
    // ====================================================================================================
    //  Game tuning data, loaded from  Assets/Resources/Tuning/GameTuning.json
    //  Edit with the menu  Yowol > Game Tuning (JSON)  or by hand in any text editor.
    //  In the Editor the file is read straight from disk, so a change is picked up on the next Play
    //  (or immediately with the window's "Apply to running game" button).
    // ====================================================================================================

    [Serializable]
    public class HudTuning
    {
        [Tooltip("Size of each E/Q skill slot (px at 1920x1080). Old value: 96")]
        public float skillSlotSize = 124f;
        [Tooltip("Size of the icon inside the slot (px)")]
        public float skillIconSize = 104f;
        [Tooltip("Distance between the E slot and the Q slot (center to center, px)")]
        public float skillSlotSpacing = 140f;
        [Tooltip("Position of the Q slot's bottom-right corner, measured from the screen's bottom-right (px)")]
        public Vector2 skillOffset = new Vector2(-40f, 48f);
        public float keyFontSize = 26f;
        public float timerFontSize = 34f;
        public float nameFontSize = 20f;
        public bool showSkillName = true;
        [Tooltip("Icon brightness while the skill is cooling down (0..1)")]
        public float cooldownIconBrightness = 0.45f;
    }

    [Serializable]
    public class SkillFxTuning
    {
        [Tooltip("Visual size multiplier for every E/Q skill effect of the player (2.2 = 220%)")]
        public float scale = 2.2f;
        [Tooltip("Hit-box size multiplier for E/Q skills (1 = original). Raise together with 'scale' if the bigger effect should also hit wider.")]
        public float hitboxScale = 1.0f;
    }

    [Serializable]
    public class WeaponTuning
    {
        [Tooltip("Sword, Bow or GreatSword")]
        public string id = "Sword";

        [Header("Normal attack wind-up (기수 동작)")]
        [Tooltip("Seconds the character holds the wind-up pose before every normal swing. 0 = none")]
        public float attackWindup;
        [Tooltip("Frame of the attack clip shown during the wind-up")]
        public int windupFrame;
        [Tooltip("Movement multiplier during the wind-up (0 = rooted)")]
        public float windupMoveMultiplier;
        [Tooltip("Only the first hit of a combo gets the wind-up")]
        public bool windupFirstComboOnly;
        [Tooltip("Pulse a tint on the character during the wind-up (telegraph)")]
        public bool windupTint = true;
        public Color windupColor = new Color(0.65f, 0.8f, 1f, 1f);

        [Header("Icons (path inside a Resources folder, no extension)")]
        [Tooltip("Weapon slot icon (1 / 2 / 3)")]
        public string icon;
        public string skill1Icon;
        public string skill2Icon;
    }

    [Serializable]
    public class LanternTuning
    {
        [Header("Body")]
        public int maxHp = 90;
        public int contactDamage = 0;
        [Range(0f, 1f)] public float knockbackResist = 0.35f;
        public Vector2 bodySize = new Vector2(1.0f, 2.2f);
        [Tooltip("The lantern floats this far above the ground")]
        public float hoverHeight = 0.3f;
        public float bobAmplitude = 0.08f;
        public float bobSpeed = 2.4f;
        public float visualScale = 1f;
        public bool unlit = false;
        public Color glowColor = new Color(0.35f, 1f, 0.9f, 1f);
        public float glowIntensity = 0.9f;
        public float glowRadius = 2.6f;

        [Header("AI")]
        public float detectRange = 11f;
        public float verticalSight = 3.5f;
        public float attackRange = 9f;
        [Tooltip("Moves closer when the player is farther than this")]
        public float preferredRange = 6.5f;
        [Tooltip("Backs away when the player is closer than this")]
        public float minRange = 3.5f;
        public float moveSpeed = 1.6f;
        public float retreatSpeed = 2.2f;
        public float patrolSpeed = 0.6f;
        public float patrolRange = 1.5f;
        public float attackCooldown = 2.6f;
        public float firstAttackDelay = 1.0f;
        public float recoverTime = 0.35f;
        public bool heavyHitInterruptsWindup = true;
        public Color windupTint = new Color(0.55f, 1f, 0.95f, 1f);
        [Tooltip("Guide lines shown once, the first time a lantern spots the player. Empty = none")]
        public string[] introLines =
        {
            "저건 등불 혼령이에요. 멀리서 청록 불꽃을 던져요!",
            "몸이 빛나며 기울면 곧 쏩니다. 점프로 피하거나, 대검 반격(E)으로 받아칠 수 있어요.",
        };

        [Header("Projectile")]
        [Tooltip("Frame of Lantern_Attack on which the flame is released")]
        public int fireFrame = 1;
        [Tooltip("Muzzle position relative to the floating body (x = forward)")]
        public Vector2 muzzleOffset = new Vector2(0.65f, 1.05f);
        public Vector2 flameOffset = new Vector2(0.35f, 0f);
        public float flameScale = 1f;
        public int projectileDamage = 10;
        public float projectileSpeed = 7.5f;
        public float projectileLifetime = 2.6f;
        public float projectileScale = 1f;
        public Vector2 projectileHitSize = new Vector2(0.55f, 0.45f);
        public float projectileKnockback = 3f;
        public bool aimAtPlayer = true;
        [Tooltip("Max aim angle up/down from horizontal (degrees)")]
        public float maxAimAngle = 20f;
    }

    [Serializable]
    public class AnimDef
    {
        [Tooltip("Clip name used by code, e.g. Lantern_Idle")]
        public string name;
        [Tooltip("'Lantern' for the enemy body, 'FX' for effects / projectiles")]
        public string library = "Lantern";
        [Tooltip("Sprite sheet path inside a Resources folder (no extension)")]
        public string sheet;
        public int cols = 4;
        public int rows = 2;
        [Tooltip("First cell (0 = top-left, reading left to right, top to bottom)")]
        public int start;
        public int count = 8;
        public bool loop;
        [Tooltip("Seconds per frame. One value = same for every frame")]
        public float[] durations = { 0.1f };
        [Tooltip("Pixels per unit (bigger = smaller on screen)")]
        public float ppu = 180f;
        [Tooltip("Pivot inside one cell (0..1). Characters: feet. Projectiles: the glowing head")]
        public Vector2 pivot = new Vector2(0.5f, 0.1f);
    }

    [Serializable]
    public class SpawnDef
    {
        [Tooltip("Scene name: Stage1, Stage2, BossStage")]
        public string scene = "Stage1";
        [Tooltip("Lantern or Reaper")]
        public string enemy = "Lantern";
        public float x;
        public float y;
        [Tooltip("Drop onto the ground found under (x, y)")]
        public bool snapToGround = true;
    }

    [Serializable]
    public class GameTuningData
    {
        public int version = 1;
        public HudTuning hud = new HudTuning();
        public SkillFxTuning skillFx = new SkillFxTuning();
        public List<WeaponTuning> weapons = new List<WeaponTuning>();
        public LanternTuning lantern = new LanternTuning();
        public List<AnimDef> animations = new List<AnimDef>();
        public List<SpawnDef> spawns = new List<SpawnDef>();
    }

    public static class GameTuning
    {
        public const string ResourcePath = "Tuning/GameTuning";
        public const string AssetPath = "Assets/Resources/Tuning/GameTuning.json";

        static GameTuningData data;
        static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static event Action Reloaded;

        /// <summary>Domain reload is disabled for Play mode in this project: re-read the JSON on every play.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            data = null;
            spriteCache.Clear();
            Reloaded = null;
        }

        public static GameTuningData Data
        {
            get
            {
                if (data == null) data = Load();
                return data;
            }
        }

        public static void Reload()
        {
            data = Load();
            spriteCache.Clear();
            RuntimeAnims.Invalidate();
            Reloaded?.Invoke();
        }

        /// <summary>Replace the data in memory (used by the editor window to preview without saving).</summary>
        public static void Set(GameTuningData d)
        {
            data = d;
            spriteCache.Clear();
            RuntimeAnims.Invalidate();
            Reloaded?.Invoke();
        }

        static GameTuningData Load()
        {
            string json = null;
#if UNITY_EDITOR
            // Read straight from disk so edits apply without a re-import.
            try { if (File.Exists(AssetPath)) json = File.ReadAllText(AssetPath); }
            catch (Exception e) { Debug.LogWarning("[GameTuning] " + e.Message); }
#endif
            if (json == null)
            {
                var ta = Resources.Load<TextAsset>(ResourcePath);
                if (ta != null) json = ta.text;
            }
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[GameTuning] GameTuning.json not found, using built-in defaults.");
                return CreateDefault();
            }
            try
            {
                var d = JsonUtility.FromJson<GameTuningData>(json);
                return Sanitize(d ?? CreateDefault());
            }
            catch (Exception e)
            {
                Debug.LogError("[GameTuning] GameTuning.json is not valid JSON, using defaults.\n" + e.Message);
                return CreateDefault();
            }
        }

        static GameTuningData Sanitize(GameTuningData d)
        {
            d.hud ??= new HudTuning();
            d.skillFx ??= new SkillFxTuning();
            d.weapons ??= new List<WeaponTuning>();
            d.lantern ??= new LanternTuning();
            d.animations ??= new List<AnimDef>();
            d.spawns ??= new List<SpawnDef>();
            if (d.skillFx.scale <= 0f) d.skillFx.scale = 1f;
            if (d.skillFx.hitboxScale <= 0f) d.skillFx.hitboxScale = 1f;
            return d;
        }

        public static string ToJson(GameTuningData d) => JsonUtility.ToJson(d, true);

        // ---------------------------------------------------------------- lookups

        public static WeaponTuning ForWeapon(WeaponId id)
        {
            string key = id.ToString();
            foreach (var w in Data.weapons)
                if (w != null && string.Equals(w.id, key, StringComparison.OrdinalIgnoreCase)) return w;
            return null;
        }

        /// <summary>Skill icon from the JSON (Resources path), falling back to the WeaponData asset.</summary>
        public static Sprite SkillIcon(WeaponData weapon, int skill)
        {
            if (weapon == null) return null;
            var t = ForWeapon(weapon.id);
            string path = t == null ? null : (skill == 0 ? t.skill1Icon : t.skill2Icon);
            var s = LoadSprite(path);
            if (s != null) return s;
            return skill == 0 ? weapon.skill1Icon : weapon.skill2Icon;
        }

        /// <summary>Weapon slot icon from the JSON, falling back to the WeaponData asset / GameAssets.</summary>
        public static Sprite WeaponIcon(WeaponData weapon, int slot)
        {
            if (weapon != null)
            {
                var t = ForWeapon(weapon.id);
                var s = LoadSprite(t?.icon);
                if (s != null) return s;
                if (weapon.icon != null) return weapon.icon;
            }
            var a = GameAssets.I;
            return a != null && a.weaponIcons != null && slot < a.weaponIcons.Length ? a.weaponIcons[slot] : null;
        }

        public static Sprite LoadSprite(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            if (spriteCache.TryGetValue(resourcePath, out var s) && s != null) return s;
            s = Resources.Load<Sprite>(resourcePath);
            if (s == null)
            {
                // A texture imported as "Default" still works: wrap it in a sprite.
                var tex = Resources.Load<Texture2D>(resourcePath);
                if (tex != null) s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            if (s == null) Debug.LogWarning($"[GameTuning] Sprite not found in Resources: '{resourcePath}'");
            spriteCache[resourcePath] = s;
            return s;
        }

        // ---------------------------------------------------------------- defaults (same as the shipped JSON)

        public static GameTuningData CreateDefault()
        {
            var d = new GameTuningData();
            d.weapons.Add(new WeaponTuning { id = "Sword", icon = "UI/WeaponIcons/Sword", skill1Icon = "UI/SkillIcons/Sword_E", skill2Icon = "UI/SkillIcons/Sword_Q" });
            d.weapons.Add(new WeaponTuning { id = "Bow", icon = "UI/WeaponIcons/Bow", skill1Icon = "UI/SkillIcons/Bow_E", skill2Icon = "UI/SkillIcons/Bow_Q" });
            d.weapons.Add(new WeaponTuning
            {
                id = "GreatSword", icon = "UI/WeaponIcons/GreatSword", attackWindup = 0.35f, windupFrame = 0, windupMoveMultiplier = 0f, windupTint = true,
                skill1Icon = "UI/SkillIcons/GreatSword_E", skill2Icon = "UI/SkillIcons/GreatSword_Q"
            });

            const string L = "Enemies/Lantern/";
            d.animations.Add(new AnimDef { name = "Lantern_Idle", library = "Lantern", sheet = L + "Lantern_Idle_Sheet", cols = 4, rows = 2, start = 0, count = 8, loop = true, durations = new[] { 0.15f }, ppu = 180f, pivot = new Vector2(0.525f, 0.1f) });
            d.animations.Add(new AnimDef { name = "Lantern_Windup", library = "Lantern", sheet = L + "Lantern_Attack_Sheet", cols = 4, rows = 3, start = 0, count = 4, loop = false, durations = new[] { 0.16f, 0.16f, 0.18f, 0.22f }, ppu = 148f, pivot = new Vector2(0.53f, 0.16f) });
            d.animations.Add(new AnimDef { name = "Lantern_Attack", library = "Lantern", sheet = L + "Lantern_Attack_Sheet", cols = 4, rows = 3, start = 4, count = 8, loop = false, durations = new[] { 0.07f, 0.06f, 0.09f, 0.09f, 0.1f, 0.12f, 0.14f, 0.28f }, ppu = 148f, pivot = new Vector2(0.53f, 0.16f) });
            d.animations.Add(new AnimDef { name = "Lantern_Death", library = "Lantern", sheet = L + "Lantern_Death_Sheet", cols = 4, rows = 2, start = 0, count = 8, loop = false, durations = new[] { 0.08f, 0.08f, 0.1f, 0.1f, 0.14f, 0.16f, 0.18f, 0.6f }, ppu = 180f, pivot = new Vector2(0.505f, 0.08f) });
            d.animations.Add(new AnimDef { name = "Lantern_Projectile", library = "FX", sheet = L + "Lantern_Projectile_Sheet", cols = 4, rows = 3, start = 0, count = 12, loop = true, durations = new[] { 0.1f }, ppu = 240f, pivot = new Vector2(0.75f, 0.47f) });
            d.animations.Add(new AnimDef { name = "Lantern_FlameSweep", library = "FX", sheet = L + "Lantern_FlameSweep_Sheet", cols = 4, rows = 2, start = 0, count = 8, loop = false, durations = new[] { 0.06f, 0.06f, 0.06f, 0.09f, 0.09f, 0.09f, 0.1f, 0.35f }, ppu = 230f, pivot = new Vector2(0.5f, 0.5f) });

            d.spawns.Add(new SpawnDef { scene = "Stage1", enemy = "Lantern", x = 4f, y = -10.43f });
            d.spawns.Add(new SpawnDef { scene = "Stage1", enemy = "Lantern", x = 31.5f, y = -10.43f });
            d.spawns.Add(new SpawnDef { scene = "Stage2", enemy = "Lantern", x = 81f, y = -10.02f });
            d.spawns.Add(new SpawnDef { scene = "Stage2", enemy = "Lantern", x = 102.5f, y = -10.02f });
            d.spawns.Add(new SpawnDef { scene = "Stage2", enemy = "Lantern", x = 123.5f, y = -10.02f });
            foreach (var (e, x, y) in new[] { ("Reaper", 26f, 2.55f), ("Lantern", 46.5f, 2.55f), ("Reaper", 57f, 0f), ("Reaper", 80.5f, 0f),
                                              ("Lantern", 95.5f, 6.5f), ("Reaper", 115f, 6f), ("Reaper", 138f, 0.9f), ("Lantern", 144f, 0.9f),
                                              // Stage3 rooftops
                                              ("Reaper", 170f, 2.9f), ("Lantern", 206f, 5.6f), ("Reaper", 229.5f, 5.6f), ("Reaper", 253.5f, 7.6f),
                                              ("Lantern", 286.5f, 6.4f), ("Reaper", 305.5f, 4.4f), ("Reaper", 327f, 9.4f), ("Lantern", 345.5f, 9.4f) })
                d.spawns.Add(new SpawnDef { scene = "Stage3", enemy = e, x = x, y = y });
            return d;
        }
    }
}
