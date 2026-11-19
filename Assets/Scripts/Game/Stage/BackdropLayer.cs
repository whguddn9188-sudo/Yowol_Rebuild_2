using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Yowol
{
    /// <summary>
    /// A group of background / foreground art that moves with the camera at <see cref="factor"/>
    /// (0 = fixed to the world, 1 = glued to the camera, negative = foreground that passes faster),
    /// and optionally fades in/out by camera X so one stage can change scenery (Stage3: factory → rooftops).
    /// The children sit at their authored positions when the camera is at <see cref="anchor"/>.
    /// </summary>
    [DefaultExecutionOrder(1000)]   // after CameraRig moved the camera
    public class BackdropLayer : MonoBehaviour
    {
        [Tooltip("x/y follow amount. 0.9 = far sky, 0.4 = distant buildings, -0.3 = foreground")]
        public Vector2 factor = new Vector2(0.5f, 0.5f);
        [Tooltip("Camera position at which the layer sits exactly where it was placed in the editor")]
        public Vector2 anchor;

        [Header("Zone (camera X)")]
        public bool useZone;
        [Tooltip("Fully visible from this camera X ...")]
        public float showFrom = float.NegativeInfinity;
        [Tooltip("... up to this camera X")]
        public float showTo = float.PositiveInfinity;
        [Tooltip("Fade distance outside the zone")]
        public float fade = 10f;

        Transform cam;
        Vector3 home;
        readonly List<SpriteRenderer> sprites = new List<SpriteRenderer>();
        readonly List<Color> spriteColors = new List<Color>();
        readonly List<LineRenderer> lines = new List<LineRenderer>();
        readonly List<Color> lineColors = new List<Color>();
        readonly List<Light2D> lights = new List<Light2D>();
        readonly List<float> lightIntensity = new List<float>();
        float lastAlpha = -1f;

        void Awake()
        {
            home = transform.position;
            GetComponentsInChildren(true, sprites);
            foreach (var s in sprites) spriteColors.Add(s.color);
            GetComponentsInChildren(true, lines);
            foreach (var l in lines) lineColors.Add(l.startColor);
            GetComponentsInChildren(true, lights);
            foreach (var l in lights) lightIntensity.Add(l.intensity);
        }

        void LateUpdate()
        {
            if (cam == null)
            {
                var c = Camera.main;
                if (c == null) return;
                cam = c.transform;
            }
            Vector2 d = (Vector2)cam.position - anchor;
            transform.position = home + new Vector3(d.x * factor.x, d.y * factor.y, 0f);
            if (useZone) ApplyAlpha(ZoneAlpha(cam.position.x));
        }

        float ZoneAlpha(float x)
        {
            float f = Mathf.Max(0.01f, fade);
            float a = 1f;
            if (x < showFrom) a = Mathf.Clamp01(1f - (showFrom - x) / f);
            else if (x > showTo) a = Mathf.Clamp01(1f - (x - showTo) / f);
            return a * a * (3f - 2f * a);
        }

        void ApplyAlpha(float a)
        {
            if (Mathf.Abs(a - lastAlpha) < 0.002f) return;
            lastAlpha = a;
            bool visible = a > 0.001f;
            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] == null) continue;
                sprites[i].enabled = visible;
                var c = spriteColors[i]; c.a *= a; sprites[i].color = c;
            }
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] == null) continue;
                lines[i].enabled = visible;
                var c = lineColors[i]; c.a *= a; lines[i].startColor = lines[i].endColor = c;
            }
            for (int i = 0; i < lights.Count; i++)
                if (lights[i] != null) lights[i].intensity = lightIntensity[i] * a;
        }
    }
}
