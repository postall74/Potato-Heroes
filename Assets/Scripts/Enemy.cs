using System.Collections.Generic;
using UnityEngine;

namespace PotatoHeroes
{
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new List<Enemy>();

        public EnemyDef def = new EnemyDef();   // всегда валидный экземпляр: исключает NRE,
                                                // если кто-то обратится к def до Setup()
        public float hp;
        // ПУБЛИЧНОЕ поле, а не property: при AddComponent<T> свойство вызывается ДО Awake,
        // и обращение к def до Setup() давало NullReferenceException. Значение ставится в Setup().
        public float Radius = .5f;
        public bool Alive => hp > 0f && !Destroying;
        public bool Destroying { get; private set; }

        Rigidbody2D _rb;
        SpriteRenderer _sr;
        SpriteAnimator _anim;
        float _touchCd, _flashT, _wobblePhase;
        Vector3 _baseScale;

        static int ENEMY_LAYER, PLAYER_LAYER, BULLET_LAYER;

        void Awake()
        {
            ENEMY_LAYER = LayerMask.NameToLayer("Enemy") >= 0 ? LayerMask.NameToLayer("Enemy") : 7;
            PLAYER_LAYER = LayerMask.NameToLayer("Player") >= 0 ? LayerMask.NameToLayer("Player") : 8;
            BULLET_LAYER = LayerMask.NameToLayer("Bullet") >= 0 ? LayerMask.NameToLayer("Bullet") : 9;

            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();   // спавнер создаёт его сразу после Enemy
            _anim = GetComponent<SpriteAnimator>();
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.gravityScale = 0f;
                _rb.freezeRotation = true;
            }
            gameObject.layer = ENEMY_LAYER;

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = Radius;   // дефолт .5f; точное значение переставим в Setup()
            col.isTrigger = false;
            _col = col;

            // в список All добавляемся только после полной настройки (см. Setup) —
            // иначе Update() других врагов увидит неинициализированный экземпляр.
        }

        CircleCollider2D _col;

        void OnDestroy() { All.Remove(this); }

        // Полный цикл настройки вызывается спавнером СРАЗУ после AddComponent<Enemy>() —
        // Awake к этому моменту уже отработал, все компоненты на месте.
        public void Configure(EnemyDef d, Sprite[] frames, float fps)
        {
            Setup(d, frames, fps);
            // размер задаётся PPU спрайтов из AssetBank
        }

        public void Setup(EnemyDef d, Sprite[] frames, float fps)
        {
            def = d; hp = d.hp;
            if (!All.Contains(this)) All.Add(this);   // регистрируемся только после инициализации
            Radius = d.spriteW * .42f;   // d.spriteW — логический размер в юнитах (см. EnemyDef.Make)
            if (_col != null) _col.radius = Radius;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_anim == null) _anim = GetComponent<SpriteAnimator>();
            if (_sr != null && frames != null && frames.Length > 0) _sr.sprite = frames[0];
            if (_anim != null) _anim.Play(frames, fps);
            _baseScale = transform.localScale;
            _wobblePhase = Random.value * 6.28f;
        }

        void TryTouchAttack()
        {
            if (_touchCd > 0f || !Alive || !Player.I || !Player.I.Alive) return;
            _touchCd = .55f;
            Player.I.TakeDamage(def.damage);
        }

        void Update()
        {
            if (!Alive || !Player.I || !Player.I.Alive) { return; }
            _touchCd -= Time.deltaTime;
            _flashT = Mathf.Max(0, _flashT - Time.deltaTime);
            if (_flashT <= 0) _sr.color = Color.white;

            var target = Position;
            var pos = _rb.position;
            var dir = (target - pos);
            float dist = dir.magnitude;
            if (dist > .001f) dir /= dist;

            // Небольшое "дышащее" покачивание скорости — живость mob'ов
            float spd = def.speed * (1f + .12f * Mathf.Sin(Time.time * 3f + _wobblePhase));

            // Расталкивание от других врагов (скольжение вдоль касательной)
            foreach (var e in All.ToArray())
            {
                if (e == this || !e.Alive) continue;
                var v = pos - e._rb.position;
                float d = v.magnitude;
                float minD = Radius + e.Radius;
                if (d < minD && d > .0001f)
                    pos += v / d * (minD - d) * .5f;
            }

            if (dist > .9f)
                _rb.MovePosition(pos + dir * spd * Time.deltaTime);
            else
                TryTouchAttack();

            // Дистанционная проверка касания (страховка без ContactCaster)
            if (dist < Radius + .5f) TryTouchAttack();

            _sr.flipX = dir.x < 0f;
        }

        public void TakeDamage(float dmg, bool fromBullet)
        {
            if (!Alive) return;
            hp -= dmg;
            _flashT = .1f;
            _sr.color = Color.white * 1.6f;
            DamageText.Spawn(transform.position + Vector3.up * .6f, Mathf.RoundToInt(dmg));
            VfxSpark.Spawn(transform.position);
            if (hp <= 0f) Die();
        }

        void Die()
        {
            if (Destroying) return;
            Destroying = true;
            XPDrop.Spawn(transform.position, Mathf.RoundToInt(def.xpValue));
            if (def.type == EnemyType.BossKingGelatin) GameManager.I?.OnBossKilled();
            if (Player.I && Player.I.Alive && Player.I.lifeSteal > 0) Player.I.Heal(Player.I.lifeSteal);
            GameManager.I?.CountKill();
            Destroy(gameObject, .02f);
        }

        static Vector2 Position => Player.I ? Player.I.Position : Vector2.zero;
    }
}
