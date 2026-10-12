using System.Collections.Generic;
using UnityEngine;

namespace PotatoHeroes
{
    // ===== Капли опыта: летят к игроку, подбираются автоматически =====
    [RequireComponent(typeof(SpriteRenderer))]
    public class XPDrop : MonoBehaviour
    {
        public int value;
        Vector2 _vel; float _t;
        static readonly List<XPDrop> Active = new();

        public static void Spawn(Vector3 pos, int xpValue)
        {
            if (AssetBank.I == null) return;
            // 1 большая или несколько мелких кристаллов
            int count = Mathf.Clamp(Mathf.CeilToInt(xpValue / 2f), 1, 4);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("xp");
                go.transform.position = pos + new Vector3(Random.Range(-.3f,.3f), Random.Range(-.2f,.3f));
                var sr = go.AddComponent<SpriteRenderer>();
                int v = Mathf.Max(1, xpValue / count);
                Sprite spr = v >= 8 ? AssetBank.I.xpRed : (v >= 4 ? AssetBank.I.xpYellow : AssetBank.I.xpBlue);
                sr.sprite = spr; sr.sortingOrder = 3;
                var d = go.AddComponent<XPDrop>();
                d.value = v;
                d._vel = new Vector2(Random.Range(-2f,2f), Random.Range(1f,3f));
                Active.Add(d);
            }
        }

        void Update()
        {
            _t += Time.deltaTime;
            var p = (Vector2)transform.position;
            if (Player.I && Player.I.Alive)
            {
                var to = Player.I.Position - p;
                float dist = to.magnitude;
                if (dist < 4.5f) // магнит
                {
                    float pull = Mathf.Lerp(9f, 2f, dist / 4.5f);
                    _vel = Vector2.Lerp(_vel, to.normalized * pull, Time.deltaTime * 8f);
                }
                if (dist < .45f) { GameManager.I.AddXP(value); Destroy(gameObject); return; }
            }
            _vel *= (1f - Time.deltaTime * 2f);
            transform.position = p + _vel * Time.deltaTime;
            if (_t > 30f) Destroy(gameObject);
        }
    }

    // ===== Сундуки с оружием (появляются в конце волны как бонус) =====
    public class ChestPickup : MonoBehaviour
    {
        public WeaponKind kind; public int tier;
        public static ChestPickup Spawn(Vector3 pos, WeaponKind k, int tier)
        {
            if (AssetBank.I == null) return null;
            var go = new GameObject("chest");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetBank.I.chest; sr.sortingOrder = 4;
            var c = go.AddComponent<ChestPickup>();
            c.kind = k; c.tier = tier;
            return c;
        }

        void Update()
        {
            if (!Player.I || !Player.I.Alive) return;
            if (((Vector2)transform.position - Player.I.Position).sqrMagnitude < .5f)
            {
                GameManager.I.GrantWeapon(kind, tier);
                DamageText.Spawn(transform.position + Vector3.up, 0, "Получено оружие!");
                Destroy(gameObject);
            }
        }
    }

    // ===== Всплывающий текст урона =====
    public class DamageText : MonoBehaviour
    {
        public static void Spawn(Vector3 pos, int dmg, string overrideText = null)
        {
            if (HUD.I == null) return;
            HUD.I.ShowFloatingText(pos, overrideText ?? dmg.ToString(), overrideText != null);
        }
    }

    // ===== Искорка попадания (4-кадровый vfx) =====
    public class VfxSpark : MonoBehaviour
    {
        public static void Spawn(Vector3 pos)
        {
            if (AssetBank.I == null) return;
            var frames = AssetBank.I.animFrames.TryGetValue("vfx_spark", out var f) ? f : null;
            if (frames == null)
            {
                // вырезаем кадры из sheets["vfx_spark"] лениво
                if (AssetBank.I.sheets.TryGetValue("vfx_spark", out var tex))
                {
                    frames = new Sprite[4];
                    for (int i = 0; i < 4; i++)
                        frames[i] = Sprite.Create(tex, new Rect(i*12,0,12,12), new Vector2(.5f,.5f), GameCfg.PPU);
                    AssetBank.I.animFrames["vfx_spark"] = frames;
                }
            }
            if (frames == null) return;
            var go = new GameObject("spark");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            var anim = go.AddComponent<SpriteAnimator>();
            anim.Play(frames, 24f); anim.loop = false;
            Destroy(go, .18f);
        }
    }

    // ===== Эффект взмаша ближнего боя =====
    public class MeleeFx : MonoBehaviour
    {
        public static void Spawn(Vector3 pos, Vector2 dir, float reach)
        {
            if (AssetBank.I == null || AssetBank.I.swing == null) return;
            var go = new GameObject("swing");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetBank.I.swing; sr.sortingOrder = 9;
            float scale = reach / 1.2f;
            go.transform.localScale = new Vector3(scale, scale, 1);
            go.transform.rotation = Quaternion.LookRotation(Vector3.forward, new Vector3(dir.y, -dir.x));
            Destroy(go, .12f);
        }
    }
}
