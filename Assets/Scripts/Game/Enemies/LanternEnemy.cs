using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Yowol
{
    /// <summary>
    /// Ghost lantern (원거리 몬스터): floats near the ground, keeps its distance from the player,
    /// telegraphs with a wind-up, then throws a teal flame projectile.
    /// Built entirely from GameTuning.json (stats in "lantern", animations in "animations"),
    /// so it needs no prefab; StageSpawns places it from the JSON "spawns" list.
    /// </summary>
    public class LanternEnemy : EnemyBase
    {
        enum State { Idle, Reposition, Windup, Attack, Recover }

        public const string ClipIdle = "Lantern_Idle";
        public const string ClipWindup = "Lantern_Windup";
        public const string ClipAttack = "Lantern_Attack";
        public const string ClipDeath = "Lantern_Death";
        public const string FxProjectile = "Lantern_Projectile";
        public const string FxFlame = "Lantern_FlameSweep";

        static PhysicsMaterial2D noFriction;
        /// <summary>The guide explains the lantern once per play session.</summary>
        static bool introShown;

        /// <summary>Statics survive between plays (domain reload is off for Play mode), so reset them here.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            introShown = false;
            noFriction = null;
        }

        State state = State.Idle;
        float stateTime;
        float nextAttackAt;
        bool fired;
        float homeX;
        int patrolDir = 1;
        float bobPhase;
        Light2D glow;

        static LanternTuning T => GameTuning.Data.lantern;

        protected override float ArtFacesRight => -1f;   // the lantern art leans/attacks to the left

        // ---------------------------------------------------------------- construction

        public static LanternEnemy Spawn(Vector3 groundPosition, Transform parent = null)
        {
            var t = T;
            var assets = GameAssets.I;
            var lib = RuntimeAnims.Library("Lantern");
            if (lib.Get(ClipIdle) == null)
            {
                Debug.LogError("[LanternEnemy] Lantern animations missing - check 'animations' in GameTuning.json");
                return null;
            }

            var go = new GameObject("Lantern") { layer = Layers.Enemy };
            go.SetActive(false);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = groundPosition;

            var visual = new GameObject("Visual") { layer = Layers.Enemy };
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, t.hoverHeight, 0f);
            visual.transform.localScale = Vector3.one * Mathf.Max(0.05f, t.visualScale);
            var sr = visual.AddComponent<SpriteRenderer>();
            if (assets != null) sr.sharedMaterial = t.unlit ? assets.unlitSprite : assets.litSprite;
            sr.sortingOrder = 19;
            var anim = visual.AddComponent<SpriteAnimator>();
            anim.library = lib;
            anim.defaultClip = ClipIdle;
            sr.sprite = lib.Get(ClipIdle).frames[0];

            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(visual.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = t.glowColor;
            light.intensity = t.glowIntensity;
            light.pointLightOuterRadius = t.glowRadius;
            light.pointLightInnerRadius = t.glowRadius * 0.15f;
            light.falloffIntensity = 0.7f;

            if (noFriction == null) noFriction = new PhysicsMaterial2D("LanternNoFriction") { friction = 0f, bounciness = 0f };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sharedMaterial = noFriction;
            var cap = go.AddComponent<CapsuleCollider2D>();
            cap.size = t.bodySize;
            cap.offset = new Vector2(0f, t.bodySize.y * 0.5f);
            cap.sharedMaterial = noFriction;

            var hp = go.AddComponent<Health>();
            hp.damageNumberOffset = new Vector2(0f, t.hoverHeight + 2.3f);
            go.AddComponent<HitFlash>();

            var le = go.AddComponent<LanternEnemy>();
            le.visual = visual.transform;
            le.anim = anim;
            le.maxHp = t.maxHp;
            le.contactDamage = t.contactDamage;
            le.knockbackResist = t.knockbackResist;
            le.glow = light;

            go.SetActive(true);
            return le;
        }

        protected override void Awake()
        {
            base.Awake();
            homeX = transform.position.x;
            bobPhase = Random.value * Mathf.PI * 2f;
            patrolDir = Random.value > 0.5f ? 1 : -1;
            nextAttackAt = Time.time + T.firstAttackDelay + Random.Range(0f, 0.6f);
            if (glow == null) glow = GetComponentInChildren<Light2D>();
            if (anim != null) anim.FrameChanged += OnFrame;
            Face(patrolDir > 0);
        }

        // ---------------------------------------------------------------- state machine

        void Enter(State s)
        {
            state = s;
            stateTime = 0f;
            switch (s)
            {
                case State.Windup:
                    anim.Play(ClipWindup, true);
                    AudioHub.Sfx(GameAssets.I.sfxBossCast, 0.3f, 0.15f);
                    break;
                case State.Attack:
                    fired = false;
                    anim.Play(ClipAttack, true);
                    break;
                case State.Recover:
                    anim.Play(ClipIdle);
                    nextAttackAt = Time.time + T.attackCooldown * Random.Range(0.85f, 1.15f);
                    break;
                default:
                    anim.Play(ClipIdle);
                    break;
            }
        }

        void Update()
        {
            if (dead) return;
            var t = T;
            stateTime += Time.deltaTime;

            // Floating body
            var lp = visual.localPosition;
            lp.y = t.hoverHeight + Mathf.Sin(Time.time * t.bobSpeed + bobPhase) * t.bobAmplitude;
            visual.localPosition = lp;

            // Wind-up telegraph: pulsing tint + brighter glow
            if (state == State.Windup)
            {
                float pulse = Mathf.PingPong(stateTime * 7f, 1f);
                flash?.SetBaseColor(Color.Lerp(Color.white, t.windupTint, pulse));
                if (glow != null) glow.intensity = t.glowIntensity * (1f + pulse * 1.6f);
            }
            else
            {
                flash?.SetBaseColor(Color.white);
                if (glow != null) glow.intensity = t.glowIntensity;
            }

            if (Stunned || Airborne) return;

            var player = Player;
            float dx = player != null ? player.position.x - transform.position.x : 0f;
            float dy = player != null ? Mathf.Abs(player.position.y - transform.position.y) : 99f;
            bool sees = PlayerAlive && Mathf.Abs(dx) < t.detectRange && dy < t.verticalSight;

            switch (state)
            {
                case State.Idle:
                    if (sees) { TryShowIntro(); Enter(State.Reposition); break; }
                    if (Mathf.Abs(transform.position.x - homeX) > t.patrolRange && Mathf.Sign(transform.position.x - homeX) == patrolDir)
                        patrolDir = -patrolDir;
                    if (!GroundAhead(patrolDir) || WallAhead(patrolDir)) patrolDir = -patrolDir;
                    Face(patrolDir > 0);
                    break;

                case State.Reposition:
                    if (!sees)
                    {
                        if (stateTime > 1.5f) { homeX = transform.position.x; Enter(State.Idle); }
                        break;
                    }
                    FaceToward(player.position);
                    if (Time.time >= nextAttackAt && Mathf.Abs(dx) <= t.attackRange) Enter(State.Windup);
                    break;

                case State.Windup:
                    if (player != null && stateTime < anim.LengthOf(ClipWindup) * 0.7f) FaceToward(player.position);
                    if (anim.IsFinished || anim.Current != ClipWindup) Enter(State.Attack);
                    break;

                case State.Attack:
                    if (anim.IsFinished || anim.Current != ClipAttack)
                    {
                        if (!fired) Fire();
                        Enter(State.Recover);
                    }
                    break;

                case State.Recover:
                    if (stateTime >= t.recoverTime) Enter(sees ? State.Reposition : State.Idle);
                    break;
            }
        }

        void FixedUpdate()
        {
            if (dead) return;
            var t = T;
            if (Stunned || Airborne)
            {
                if (!Airborne) rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, 0, 30f * Time.fixedDeltaTime), rb.linearVelocity.y);
                return;
            }

            float vx = 0f;
            if (state == State.Idle)
            {
                vx = patrolDir * t.patrolSpeed;
            }
            else if (state == State.Reposition && Player != null)
            {
                float dx = Player.position.x - transform.position.x;
                float adx = Mathf.Abs(dx);
                int toward = dx >= 0f ? 1 : -1;
                if (adx < t.minRange) vx = -toward * t.retreatSpeed;
                else if (adx > t.preferredRange) vx = toward * t.moveSpeed;
            }
            if (vx != 0f)
            {
                int dir = vx > 0f ? 1 : -1;
                if (!GroundAhead(dir) || WallAhead(dir)) vx = 0f;
            }
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
            if (contactDamage > 0) ContactDamage(new Vector2(t.bodySize.x * 0.8f, t.bodySize.y * 0.8f), new Vector2(0f, t.bodySize.y * 0.5f));
        }

        void TryShowIntro()
        {
            if (introShown) return;
            var lines = T.introLines;
            var ui = GameUI.Instance;
            if (lines == null || lines.Length == 0 || ui == null || ui.DialogueOpen || ui.Paused) return;
            introShown = true;
            nextAttackAt = Mathf.Max(nextAttackAt, Time.time + 0.8f);   // don't shoot the moment the dialogue closes
            string speaker = StageController.Current != null ? StageController.Current.guideName : "";
            ui.StartCoroutine(ui.Dialogue(speaker, lines));
        }

        void OnFrame(string clip, int frame)
        {
            if (dead || state != State.Attack || fired) return;
            if (clip == ClipAttack && frame >= T.fireFrame) Fire();
        }

        void Fire()
        {
            fired = true;
            var t = T;
            float f = FacingRight ? 1f : -1f;
            Vector2 muzzle = (Vector2)visual.position + new Vector2(f * t.muzzleOffset.x, t.muzzleOffset.y);

            // Muzzle flame (art faces left -> flip when facing right)
            Fx.Play(FxFlame, muzzle + new Vector2(f * t.flameOffset.x, t.flameOffset.y), FacingRight, t.flameScale, 0f, null, Fx.DefaultOrder + 1);
            Fx.Flash(muzzle, t.glowColor, 3f, 1.4f, 0.25f);

            Vector2 dir = new Vector2(f, 0f);
            var player = Player;
            if (t.aimAtPlayer && player != null)
            {
                Vector2 to = (Vector2)player.position + Vector2.up * 1.1f - muzzle;
                if (Mathf.Sign(to.x) == f && to.sqrMagnitude > 0.01f)
                {
                    float ang = Mathf.Clamp(Vector2.SignedAngle(dir, to), -t.maxAimAngle, t.maxAimAngle);
                    dir = Quaternion.Euler(0f, 0f, ang) * dir;
                }
            }

            var p = Projectile.Spawn(FxProjectile, muzzle, dir * t.projectileSpeed, t.projectileDamage, Layers.PlayerMask, t.projectileScale);
            if (p != null)
            {
                p.lifetime = t.projectileLifetime;
                p.hitSize = t.projectileHitSize;
                p.knockback = t.projectileKnockback;
                p.stopOnGround = true;
                p.hitFx = "SwordSpark";
                p.hitFxColor = t.glowColor;
                p.hitFxScale = 0.45f;
                p.owner = gameObject;
            }
            AudioHub.Sfx(GameAssets.I.sfxBossBeam, 0.45f, 0.12f);
        }

        // ---------------------------------------------------------------- damage & death

        protected override void OnDamaged(DamageInfo info)
        {
            base.OnDamaged(info);
            if (dead) return;
            if (state == State.Idle) Enter(State.Reposition);
            else if (state == State.Windup && info.heavy && T.heavyHitInterruptsWindup) Enter(State.Recover);
        }

        protected override System.Collections.IEnumerator DeathRoutine()
        {
            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            if (flash != null) { flash.SetBaseColor(Color.white); flash.enabled = false; }
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) sr.color = Color.white;
            anim.speed = 1f;
            anim.Play(ClipDeath, true);
            Fx.Flash(visual.position + Vector3.up * 1.1f, T.glowColor, 4f, 2f, 0.4f);
            float len = Mathf.Max(0.2f, anim.LengthOf(ClipDeath));
            float start = glow != null ? glow.intensity : 0f;
            for (float t = 0f; t < len; t += Time.deltaTime)
            {
                if (glow != null) glow.intensity = Mathf.Lerp(start * 2f, 0f, t / (len * 0.6f));
                yield return null;
            }
            Destroy(gameObject);
        }

        // ---------------------------------------------------------------- probes

        bool GroundAhead(int dir)
        {
            Vector2 from = (Vector2)transform.position + new Vector2(dir * (T.bodySize.x * 0.5f + 0.3f), 0.3f);
            return Physics2D.Raycast(from, Vector2.down, 1.2f, Layers.GroundMask);
        }

        bool WallAhead(int dir)
        {
            Vector2 from = (Vector2)transform.position + new Vector2(0f, 0.8f);
            return Physics2D.Raycast(from, new Vector2(dir, 0), T.bodySize.x * 0.5f + 0.4f, Layers.GroundMask);
        }
    }
}
