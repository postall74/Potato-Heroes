using System.Collections.Generic;
using UnityEngine;

namespace PotatoHeroes
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Player : MonoBehaviour
    {
        public static Player I { get; private set; }

        // Базовые характеристики
        // (значения задаёт GameManager при спавне)
        public CharacterDef cdef;
        public float maxHp = 100f, hp = 100f;
        public float baseSpeed = 5f;
        public int bonusDamage = 0;
        public float atkSpeedMul = 1f;   // множитель скорости атаки (>1 быстрее)
        public float critChance = .05f;
        public int armorPct = 0;         // -% полученного урона
        public int lifeSteal = 0;        // ОЗ за убийство
        public int bonusProjectiles = 0;
        public float rangeMul = 1f;
        public float xpMul = 1f;

        public List<WeaponDef> weapons = new();
        float[] _cds = new float[8];
        SpriteAnimator _anim;
        Rigidbody2D _rb;
        SpriteRenderer _sr;
        Vector3[] _weaponGfx = new Vector3[8];
        int _hurtFlashT;

        public bool Alive => hp > 0f;

        void Awake()
        {
            I = this;
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();
            _anim = GetComponent<SpriteAnimator>();
            var col = gameObject.AddComponent<CapsuleCollider2D>();
            col.size = GameCfg.S(20, 24);
            col.offset = new Vector2(0, .1f);
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            Physics2D.bounceThreshold = 0f;
        }

        public void Setup(CharacterDef def, Sprite[] frames)
        {
            cdef = def;
            maxHp = Mathf.RoundToInt(100f * def.hpMul);
            hp = maxHp;
            baseSpeed = 5f * def.spdMul;
            armorPct = Mathf.RoundToInt((1f - def.armorMul) * 100f);
            bonusDamage = Mathf.RoundToInt(5 * (def.dmgMul - 1f));
            _anim.Play(frames, 9f);
        }

        void Update()
        {
            if (!Alive) return;
            // Движение WASD/стрелки + мышь-направление
            Vector2 dir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            Vector2 mouse = MouseAim.WorldPoint;
            _rb.MovePosition(_rb.position + dir * baseSpeed * Time.deltaTime);
            // Ограничение арены
            var p = _rb.position;
            p.x = Mathf.Clamp(p.x, -GameCfg.ArenaW/2+.8f, GameCfg.ArenaW/2-.8f);
            p.y = Mathf.Clamp(p.y, -GameCfg.ArenaH/2+.8f, GameCfg.ArenaH/2-.8f);
            _rb.MovePosition(p);

            // Анимация: ходим или стоим
            float fps = dir.sqrMagnitude > .01f ? 10f : 4f;
            if (_anim.fps != fps) _anim.fps = fps;

            // Поворот к курсору
            float dx = mouse.x - transform.position.x;
            _sr.flipX = dx < 0f;

            // Стрельба по наведению (автоатака как в Brotato — стреляем всегда на ближайшего врага,
            // но если зажат ЛКМ — на курсор)
            bool manual = Input.GetMouseButton(0);
            for (int w = 0; w < weapons.Count; w++)
            {
                _cds[w] -= Time.deltaTime;
                if (_cds[w] <= 0f)
                {
                    var wd = weapons[w];
                    float aimRange = wd.melee ? wd.range * rangeMul : 999f;
                    Vector2 aim = manual ? (mouse - (Vector2)transform.position).normalized
                                         : AimNearestEnemy(aimRange);
                    if (aim == Vector2.zero && !manual) continue; // нет врагов — не тратим КД? (в Brotato ждём)
                    Fire(wd, aim);
                    _cds[w] = Mathf.Max(.05f, wd.cooldown / atkSpeedMul);
                }
            }
        }

        Vector2 AimNearestEnemy(float maxDist = 999f)
        {
            Enemy best = null; float bd = maxDist * maxDist;
            foreach (var e in Enemy.All)
            {
                if (!e.Alive) continue;
                float d = (e.transform.position - transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best ? ((Vector2)best.transform.position - (Vector2)transform.position).normalized : Vector2.zero;
        }

        void Fire(WeaponDef w, Vector2 aim)
        {
            int n = w.projectiles + bonusProjectiles;
            float dmg = (w.damage + bonusDamage) * cdef.dmgMul;
            if (w.melee)
            {
                // Ближний бой: мгновенный конус перед игроком
                MeleeFx.Spawn(transform.position + (Vector3)aim * .6f, aim, w.range * rangeMul);
                float reach = w.range * rangeMul;
                foreach (var e in Enemy.All)
                {
                    if (!e.Alive) continue;
                    var to = (Vector2)e.transform.position - (Vector2)transform.position;
                    if (to.magnitude <= reach + e.Radius && Vector2.Dot(to.normalized, aim) > .35f)
                        e.TakeDamage(RollCrit(dmg), false);
                }
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    float t = n == 1 ? 0f : (i / (float)(n - 1) - .5f);
                    float ang = t * w.spreadDeg * Mathf.Deg2Rad * 2f + Random.Range(-w.spreadDeg, w.spreadDeg) * .3f * Mathf.Deg2Rad;
                    Vector2 d = Quaternion.Euler(0, 0, ang * Mathf.Rad2Deg) * aim;
                    Bullet.Spawn(transform.position + (Vector3)d * .5f, d, RollCrit(dmg), w.bulletSpeed, w.iconIndex % GameCfg.BulletColors.Length);
                }
            }
        }

        float RollCrit(float dmg) => Random.value < critChance ? dmg * 2f : dmg;

        public void TakeDamage(float amount)
        {
            if (!Alive) return;
            amount = Mathf.Max(1f, amount * (1f - armorPct / 100f));
            hp -= amount;
            _hurtFlashT = 6;
            StartCoroutine(Flash());
            HUD.I?.Refresh();
            if (hp <= 0f) { hp = 0f; Die(); }
        }

        System.Collections.IEnumerator Flash()
        {
            _sr.color = Color.red;
            yield return new WaitForSeconds(.12f);
            _sr.color = Color.white;
        }

        public void Heal(int v) { hp = Mathf.Min(maxHp, hp + v); HUD.I?.Refresh(); }

        void Die()
        {
            _sr.color = new Color(.4f,.4f,.4f,1f);
            GetComponent<CapsuleCollider2D>().enabled = false;
            GameManager.I.OnPlayerDeath();
        }

        public Vector2 Position => _rb.position;
    }

    // Автоприцел мышью: мировые координаты курсора
    public static class MouseAim
    {
        public static Vector2 WorldPoint
        {
            get
            {
                var cam = Camera.main;
                if (!cam) return Vector2.zero;
                var mp = Input.mousePosition;
                if (mp.x < 0 || mp.y < 0) mp = new Vector2(Screen.width / 2f, Screen.height / 2f);
                return cam.ScreenToWorldPoint(mp);
            }
        }
    }
}
