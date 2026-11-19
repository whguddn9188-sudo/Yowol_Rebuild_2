using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Yowol
{
    /// <summary>
    /// Blends the global 2D light between "keys" placed along camera X
    /// (Stage3: warm furnace light in the factory → cold moonlight on the rooftops).
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class ZoneAmbience : MonoBehaviour
    {
        [System.Serializable]
        public struct Key
        {
            public float x;
            public Color color;
            public float intensity;
        }

        public Key[] keys =
        {
            new Key { x = 145f, color = new Color(1f, 0.93f, 0.85f), intensity = 0.9f },
            new Key { x = 165f, color = new Color(0.72f, 0.76f, 1f), intensity = 0.85f },
        };

        Light2D globalLight;
        Transform cam;

        void Awake() => globalLight = GetComponent<Light2D>();

        void LateUpdate()
        {
            if (keys == null || keys.Length == 0) return;
            if (cam == null) { var c = Camera.main; if (c == null) return; cam = c.transform; }
            float x = cam.position.x;
            if (x <= keys[0].x) { Set(keys[0].color, keys[0].intensity); return; }
            for (int i = 1; i < keys.Length; i++)
            {
                if (x > keys[i].x) continue;
                float t = Mathf.InverseLerp(keys[i - 1].x, keys[i].x, x);
                Set(Color.Lerp(keys[i - 1].color, keys[i].color, t), Mathf.Lerp(keys[i - 1].intensity, keys[i].intensity, t));
                return;
            }
            var last = keys[keys.Length - 1];
            Set(last.color, last.intensity);
        }

        void Set(Color c, float i) { globalLight.color = c; globalLight.intensity = i; }
    }
}
