using System.Collections.Generic;
using UnityEngine;

namespace Yowol
{
    /// <summary>
    /// Platform that travels back and forth between its start position and start + <see cref="travel"/>.
    /// Anything standing on it (player, enemies) is carried along.
    /// Put it on the Ground layer with a BoxCollider2D; the kinematic Rigidbody2D is added automatically.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [Tooltip("Offset of the far end from the start position (world units)")]
        public Vector2 travel = new Vector2(6f, 0f);
        [Tooltip("Seconds for one way")]
        public float duration = 3f;
        [Tooltip("Seconds to wait at each end")]
        public float pause = 0.6f;
        [Tooltip("Start phase 0..1 (offsets several platforms from each other)")]
        [Range(0f, 1f)] public float phase;
        [Tooltip("Ease in/out at the ends")]
        public bool smooth = true;

        /// <summary>Current velocity (units/s). PlayerController uses it so a rising lift still counts as "grounded".</summary>
        public Vector2 Velocity { get; private set; }

        Rigidbody2D body;
        BoxCollider2D box;
        Vector2 start;
        float time;
        readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();
        static readonly List<Collider2D> overlap = new List<Collider2D>(16);

        void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            body = GetComponent<Rigidbody2D>();
            if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.useFullKinematicContacts = true;
            start = transform.position;
            time = phase * Cycle;
        }

        float Cycle => 2f * (Mathf.Max(0.05f, duration) + Mathf.Max(0f, pause));

        Vector2 PositionAt(float t)
        {
            float d = Mathf.Max(0.05f, duration), p = Mathf.Max(0f, pause);
            t = Mathf.Repeat(t, Cycle);
            float k;
            if (t < p) k = 0f;
            else if (t < p + d) k = (t - p) / d;
            else if (t < 2f * p + d) k = 1f;
            else k = 1f - (t - 2f * p - d) / d;
            if (smooth) k = k * k * (3f - 2f * k);
            return start + travel * k;
        }

        void FixedUpdate()
        {
            time += Time.fixedDeltaTime;
            Vector2 next = PositionAt(time);
            Vector2 delta = next - body.position;
            Velocity = delta / Time.fixedDeltaTime;
            float platformVy = Mathf.Max(0f, Velocity.y);

            // Riders: dynamic bodies resting on the top surface.
            riders.Clear();
            var b = box.bounds;
            var filter = new ContactFilter2D();
            filter.SetLayerMask(Layers.PlayerMask | Layers.EnemyMask);
            filter.useTriggers = false;
            overlap.Clear();
            Physics2D.OverlapBox(new Vector2(b.center.x, b.max.y + 0.12f), new Vector2(b.size.x - 0.05f, 0.24f), 0f, filter, overlap);
            foreach (var c in overlap)
            {
                var r = c != null ? c.attachedRigidbody : null;
                // A rising platform pushes riders upward, so allow a bit more than its own vertical speed (a jump is far faster).
                if (r != null && r != body && r.bodyType == RigidbodyType2D.Dynamic && r.linearVelocity.y <= platformVy * 1.5f + 0.5f) riders.Add(r);
            }
            foreach (var r in riders) r.position += delta;

            body.MovePosition(next);
        }

        void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider2D>();
            Vector3 from = Application.isPlaying ? (Vector3)start : transform.position;
            Vector3 size = col != null ? (Vector3)Vector2.Scale(col.size, transform.lossyScale) : Vector3.one;
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.8f);
            Gizmos.DrawLine(from, from + (Vector3)travel);
            Gizmos.DrawWireCube(from + (Vector3)travel, size);
        }
    }
}
