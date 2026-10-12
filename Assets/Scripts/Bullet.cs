using UnityEngine;

namespace PotatoHeroes
{
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public class Bullet : MonoBehaviour
    {
        Vector2 _dir; float _speed, _dmg, _life;
        static int ENEMY_LAYER;

        public static void Spawn(Vector3 pos, Vector2 dir, float dmg, float speed, int colorIdx)
        {
            if (AssetBank.I == null) return;
            var go = new GameObject("Bullet");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetBank.I.bulletSpr;
            sr.pixelsPerUnit = GameCfg.PPU;   // фиксируем физический размер снаряда
            sr.color = GameCfg.BulletColors[colorIdx % GameCfg.BulletColors.Length];
            sr.sortingOrder = 5;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var b = go.AddComponent<Bullet>();
            b._dir = dir.normalized; b._speed = speed; b._dmg = dmg; b._life = 1.6f;
            go.transform.rotation = Quaternion.LookRotation(Vector3.forward, new Vector3(-b._dir.y, b._dir.x));
            ENEMY_LAYER = LayerMask.NameToLayer("Enemy") >= 0 ? LayerMask.NameToLayer("Enemy") : 7;
            go.layer = LayerMask.NameToLayer("Bullet") >= 0 ? LayerMask.NameToLayer("Bullet") : 9;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = .18f; col.isTrigger = true;
        }

        void Update()
        {
            transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
            _life -= Time.deltaTime;
            if (_life <= 0) { Destroy(gameObject); return; }

            // Проверка попаданий через Physics2D (kinematic-триггеры с kinematic-enemy не генерируют события)
            var hits = Physics2D.OverlapCircleAll((Vector2)transform.position, .2f, 1 << ENEMY_LAYER);
            foreach (var h in hits)
            {
                var e = h.GetComponent<Enemy>();
                if (e && e.Alive)
                {
                    e.TakeDamage(_dmg, true);
                    VfxSpark.Spawn(transform.position);
                    Destroy(gameObject);
                    return;
                }
            }
            // Вылет за арену
            var p = transform.position;
            if (Mathf.Abs(p.x) > GameCfg.ArenaW/2+1 || Mathf.Abs(p.y) > GameCfg.ArenaH/2+1)
                Destroy(gameObject);
        }
    }
}
