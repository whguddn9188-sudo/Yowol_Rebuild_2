using System.Collections;
using UnityEngine;

namespace Yowol
{
    /// <summary>
    /// Brick ledge that starts shaking when the player stands on it, then falls away and comes back later.
    /// Put it on the Ground layer with a BoxCollider2D and a child (or own) SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CrumblePlatform : MonoBehaviour
    {
        [Tooltip("Seconds of shaking after the player steps on it")]
        public float shakeTime = 0.6f;
        [Tooltip("Seconds until it reappears")]
        public float respawnTime = 3.5f;
        public float shakeAmount = 0.06f;
        public Color warnColor = new Color(1f, 0.7f, 0.6f, 1f);

        BoxCollider2D box;
        SpriteRenderer[] renderers;
        Color[] baseColors;
        Vector3 home;
        bool busy;

        void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
            home = transform.position;
        }

        void FixedUpdate()
        {
            if (busy) return;
            var b = box.bounds;
            var hit = Physics2D.OverlapBox(new Vector2(b.center.x, b.max.y + 0.1f), new Vector2(b.size.x - 0.1f, 0.2f), 0f, Layers.PlayerMask);
            if (hit != null) StartCoroutine(Crumble());
        }

        IEnumerator Crumble()
        {
            busy = true;
            AudioHub.Sfx(GameAssets.I.sfxFootstep, 0.6f, 0.2f);
            for (float t = 0f; t < shakeTime; t += Time.deltaTime)
            {
                float k = t / shakeTime;
                transform.position = home + (Vector3)(Random.insideUnitCircle * shakeAmount * (0.5f + k));
                for (int i = 0; i < renderers.Length; i++) renderers[i].color = Color.Lerp(baseColors[i], warnColor, k);
                yield return null;
            }
            transform.position = home;
            box.enabled = false;
            Fx.Play("FireDust", home + Vector3.up * 0.2f, false, 0.5f, 0f, new Color(0.7f, 0.65f, 0.6f, 0.8f));
            CameraRig.Shake(0.12f, 0.15f);
            AudioHub.Sfx(GameAssets.I.sfxExplosion, 0.35f, 0.15f);
            // fall and fade
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                transform.position = home + Vector3.down * (t * t * 12f);
                for (int i = 0; i < renderers.Length; i++) { var c = baseColors[i]; c.a *= 1f - t / 0.6f; renderers[i].color = c; }
                yield return null;
            }
            foreach (var r in renderers) r.enabled = false;
            yield return new WaitForSeconds(respawnTime);
            transform.position = home;
            // wait until nobody stands in the way, then fade back in
            Vector2 size = Vector2.Scale(box.size, transform.lossyScale);
            Vector2 center = (Vector2)home + Vector2.Scale(box.offset, transform.lossyScale);
            while (Physics2D.OverlapBox(center, size, 0f, Layers.PlayerMask | Layers.EnemyMask) != null)
                yield return new WaitForSeconds(0.2f);
            foreach (var r in renderers) r.enabled = true;
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                for (int i = 0; i < renderers.Length; i++) { var c = baseColors[i]; c.a *= t / 0.3f; renderers[i].color = c; }
                yield return null;
            }
            for (int i = 0; i < renderers.Length; i++) renderers[i].color = baseColors[i];
            box.enabled = true;
            busy = false;
        }
    }
}
