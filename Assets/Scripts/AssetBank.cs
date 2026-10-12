using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PotatoHeroes
{
    // Единое хранилище текстур/спрайтов. Грузит PNG из Assets/Sprites напрямую с диска,
    // поэтому не требует ручной настройки импорта в инспекторе.
    public class AssetBank : MonoBehaviour
    {
        public static AssetBank I { get; private set; }

        public Dictionary<string, Texture2D> sheets = new Dictionary<string, Texture2D>();
        public Dictionary<string, Sprite[]> animFrames = new Dictionary<string, Sprite[]>(); // name -> кадры
        public Sprite bulletSpr, xpBlue, xpYellow, xpRed, chest, swing, floorTile, wallTile;
        public Sprite[] weaponIcons;
        public Font font;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            LoadAll();
        }

        string SpritesDir()
        {
            // Ищем папку Assets/Sprites относительно корня проекта
            string[] candidates =
            {
                Path.Combine(Application.dataPath, "Sprites"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Sprites"),
            };
            foreach (var c in candidates)
                if (Directory.Exists(c)) return c;
            return candidates[0];
        }

        Texture2D LoadTex(string file)
        {
            var path = Path.Combine(SpritesDir(), file + ".png");
            if (!File.Exists(path)) { Debug.LogError($"[AssetBank] Нет файла {path} — запустите generate_sprites.py"); return MakePlaceholder(); }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(path));
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = file;
            return tex;
        }

        Texture2D MakePlaceholder()
        {
            var t = new Texture2D(8, 8);
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
                t.SetPixel(x, y, ((x + y) % 2 == 0) ? Color.magenta : Color.black);
            t.Apply(); t.name = "placeholder";
            return t;
        }

        void LoadAll()
        {
            string[] chars = { "char_captain_tuber", "char_engineer_peel", "char_medic_sprout", "char_mage_truffle" };
            string[] mobs = { "mob_slime_green", "mob_slime_red", "mob_slime_blue", "mob_bat", "mob_ghost", "mob_boss_king_gelatin" };

            foreach (var n in chars) sheets[n] = LoadTex(n);
            foreach (var n in mobs) sheets[n] = LoadTex(n);

            sheets["vfx_spark"] = LoadTex("vfx_spark");
            var bulletsTex = LoadTex("bullets");
            var gemsTex = LoadTex("xp_gem");
            chest = MkSprite(LoadTex("chest"), 16, 16, 0);
            swing = MkSprite(LoadTex("swing"), 32, 32, 0);
            floorTile = MkSprite(LoadTex("floor_tile"), 32, 32, 0);
            wallTile = MkSprite(LoadTex("wall_tile"), 32, 32, 0);

            bulletSpr = Cut(bulletsTex, 0, 8, 8);
            xpBlue = Cut(gemsTex, 0, 8, 8);
            xpYellow = Cut(gemsTex, 1, 8, 8);
            xpRed = Cut(gemsTex, 2, 8, 8);

            var iconsTex = LoadTex("weapons_icons");
            weaponIcons = new Sprite[6];
            for (int i = 0; i < 6; i++) weaponIcons[i] = Cut(iconsTex, i, 16, 16);

            // Кадры анимаций: персонажи 32x32 x4, слизни/летучая мышь 24x24 x4, призрак 24x28, босс 40x40
            foreach (var n in chars) animFrames[n] = Frames(sheets[n], 4, 32, 32);
            foreach (var n in mobs)
            {
                int fw = n.Contains("boss") ? 40 : 24;
                int fh = n.Contains("ghost") ? 28 : (n.Contains("boss") ? 40 : 24);
                animFrames[n] = Frames(sheets[n], 4, fw, fh);
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = LegacyFont.Get();
        }

        Sprite Cut(Texture2D tex, int frame, int fw, int fh) =>
            Sprite.Create(tex, new Rect(frame * fw, 0, fw, fh), new Vector2(.5f, .5f), GameCfg.PPU);

        Sprite MkSprite(Texture2D tex, int w, int h, int frame) =>
            Sprite.Create(tex, new Rect(frame * w, 0, w, h), new Vector2(.5f, .5f), GameCfg.PPU);

        Sprite[] Frames(Texture2D tex, int count, int fw, int fh)
        {
            var arr = new Sprite[count];
            for (int i = 0; i < count; i++) arr[i] = Cut(tex, i, fw, fh);
            return arr;
        }
    }

    // Fallback-получение встроенного шрифта
    public static class LegacyFont
    {
        public static Font Get()
        {
            var go = new GameObject("__tmpText");
            var txt = go.AddComponent<UnityEngine.UI.Text>();
            var f = txt.font;
            Object.Destroy(go);
            return f;
        }
    }
}
