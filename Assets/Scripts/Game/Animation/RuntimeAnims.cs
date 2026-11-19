using System.Collections.Generic;
using UnityEngine;

namespace Yowol
{
    /// <summary>
    /// Builds SpriteAnimLibrary clips at runtime from the sprite sheets listed in GameTuning.json
    /// ("animations"). No import step is needed: drop a sheet into a Resources folder, describe its grid
    /// in the JSON and the clip exists. Used by the lantern enemy and its projectile / muzzle FX.
    /// </summary>
    public static class RuntimeAnims
    {
        static readonly Dictionary<string, SpriteAnimLibrary> libraries = new Dictionary<string, SpriteAnimLibrary>();
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        /// <summary>
        /// The project disables domain reload on entering Play mode (Enter Play Mode Options), so statics survive
        /// between plays while the sprites created at runtime are destroyed when Play stops. Clear the caches at the
        /// start of every play, otherwise the second play shows invisible lanterns / projectiles.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            libraries.Clear();
            sprites.Clear();
        }

        /// <summary>Drop cached libraries so the next request rebuilds them from the current JSON.</summary>
        public static void Invalidate()
        {
            // Old libraries stay alive for objects already using them; new spawns get the rebuilt ones.
            libraries.Clear();
            sprites.Clear();
        }

        public static SpriteAnimLibrary Library(string libraryName)
        {
            if (libraries.TryGetValue(libraryName, out var lib) && IsAlive(lib)) return lib;
            lib = ScriptableObject.CreateInstance<SpriteAnimLibrary>();
            lib.name = "Runtime_" + libraryName;
            lib.hideFlags = HideFlags.DontSave;
            foreach (var def in GameTuning.Data.animations)
            {
                if (def == null || def.library != libraryName) continue;
                var clip = Build(def);
                if (clip != null) lib.clips.Add(clip);
            }
            libraries[libraryName] = lib;
            return lib;
        }

        /// <summary>False if the library or any of its frames was destroyed (e.g. by leaving Play mode).</summary>
        static bool IsAlive(SpriteAnimLibrary lib)
        {
            if (lib == null) return false;
            foreach (var c in lib.clips)
                if (c?.frames != null)
                    foreach (var f in c.frames)
                        if (f == null) return false;
            return true;
        }

        static SpriteAnimLibrary.Clip Build(AnimDef def)
        {
            if (string.IsNullOrEmpty(def.sheet) || def.count <= 0 || def.cols <= 0 || def.rows <= 0) return null;
            var tex = Resources.Load<Texture2D>(def.sheet);
            if (tex == null)
            {
                Debug.LogWarning($"[RuntimeAnims] Sheet '{def.sheet}' not found in Resources (clip {def.name})");
                return null;
            }
            float cw = tex.width / (float)def.cols, ch = tex.height / (float)def.rows;
            var frames = new Sprite[def.count];
            var durs = new float[def.count];
            for (int i = 0; i < def.count; i++)
            {
                int cell = def.start + i;
                int cx = cell % def.cols, cy = cell / def.cols;
                string key = $"{def.sheet}|{cell}|{def.ppu}|{def.pivot.x}|{def.pivot.y}|{def.cols}x{def.rows}";
                if (!sprites.TryGetValue(key, out var s) || s == null)
                {
                    var rect = new Rect(Mathf.Floor(cx * cw), Mathf.Floor(tex.height - (cy + 1) * ch), Mathf.Floor(cw), Mathf.Floor(ch));
                    s = Sprite.Create(tex, rect, def.pivot, Mathf.Max(1f, def.ppu), 0, SpriteMeshType.FullRect);
                    s.name = $"{def.name}_{i:00}";
                    sprites[key] = s;
                }
                frames[i] = s;
                durs[i] = def.durations != null && def.durations.Length > 0
                    ? def.durations[Mathf.Min(i, def.durations.Length - 1)]
                    : 0.1f;
            }
            return new SpriteAnimLibrary.Clip { name = def.name, loop = def.loop, frames = frames, durations = durs };
        }
    }
}
