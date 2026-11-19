using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Yowol
{
    /// <summary>
    /// Pit / out-of-bounds. Default: returns the player to safe ground with a small HP penalty.
    /// With <see cref="instantKill"/> (Stage3 pits) falling in is death.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class KillZone : MonoBehaviour
    {
        public int damage = 15;
        [Tooltip("Falling in kills the player (game over) instead of respawning with damage")]
        public bool instantKill;

        void OnTriggerEnter2D(Collider2D other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc != null && !pc.IsDead)
            {
                if (instantKill)
                {
                    pc.Body.linearVelocity = Vector2.zero;
                    pc.Body.simulated = false;      // stop the fall inside the pit
                    CameraRig.Shake(0.4f, 0.3f);
                    pc.Health.Kill();
                }
                else ScreenFader.Instance.Blink(() => pc.Respawn(damage));
                return;
            }
            var e = other.GetComponentInParent<EnemyBase>();
            if (e != null) e.GetComponent<Health>()?.Kill();
        }
    }
}
