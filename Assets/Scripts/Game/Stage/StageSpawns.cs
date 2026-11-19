using UnityEngine;

namespace Yowol
{
    /// <summary>
    /// Spawns the enemies listed under "spawns" in GameTuning.json for the current scene.
    /// Called by StageController at stage start, so the scene files themselves stay untouched.
    /// </summary>
    public static class StageSpawns
    {
        public static int SpawnFor(string sceneName)
        {
            var list = GameTuning.Data.spawns;
            if (list == null || list.Count == 0) return 0;
            Transform parent = null;
            int n = 0;
            foreach (var s in list)
            {
                if (s == null || s.scene != sceneName) continue;
                if (parent == null) parent = new GameObject("Enemies_FromJson").transform;
                var pos = GroundPoint(s);
                switch ((s.enemy ?? "").Trim().ToLowerInvariant())
                {
                    case "lantern":
                        if (LanternEnemy.Spawn(pos, parent) != null) n++;
                        break;
                    case "reaper":
                        var prefab = GameAssets.I != null ? GameAssets.I.reaperPrefab : null;
                        if (prefab != null) { Object.Instantiate(prefab, pos, Quaternion.identity, parent); n++; }
                        break;
                    default:
                        Debug.LogWarning($"[StageSpawns] Unknown enemy '{s.enemy}' in GameTuning.json");
                        break;
                }
            }
            return n;
        }

        public static Vector3 GroundPoint(SpawnDef s)
        {
            var pos = new Vector3(s.x, s.y, 0f);
            if (!s.snapToGround) return pos;
            var hit = Physics2D.Raycast(new Vector2(s.x, s.y + 1.5f), Vector2.down, 8f, Layers.GroundMask);
            if (hit.collider != null) pos.y = hit.point.y + 0.05f;
            return pos;
        }
    }
}
