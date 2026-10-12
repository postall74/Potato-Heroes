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
            if (!File.Exists(path)) { Debug.LogError($"[AssetBank] Нет файла {path}"); return MakePlaceholder(); }
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

        // ===== РАСКЛАДКА ШИТОВ (px). Проверена попиксельно по фактическим PNG: =====
        // Персонажи: 512x128 => 4 кадра по 128x128 (существенные пиксели в rows 44..127)
        // Слизи:     384x96  => 4 кадра по 96x96
        // Мышь:      384x96  => 4 кадра по 96x96
        // Призрак:   384x112 => 4 кадра по 96x112
        // Босс:      640x160 => 4 кадра по 160x160
        // Пули:      192x32  => 4 кадра по 48x32 (используем кадр 0)
        // Гемы XP:   96x32   => 3 кадра по 32x32
        // Иконки:    384x64  => 6 кадров по 64x64
        // Vfx искра: 192x48  => 4 кадра по 48x48
        // Сундук: 64x64 один спрайт; взмах: 512x128 => 4 кадра по 128x128;
        // Пол/стены: 128x128 тайлы целиком.
        struct Layout { public int fw, fh, count; }

        static Layout LayoutFor(string name)
        {
            if (name.StartsWith("char_")) return new Layout { fw = 128, fh = 128, count = 4 };
            if (name.Contains("boss")) return new Layout { fw = 160, fh = 160, count = 4 };
            if (name.Contains("ghost")) return new Layout { fw = 96, fh = 112, count = 4 };
            if (name.StartsWith("mob_")) return new Layout { fw = 96, fh = 96, count = 4 };
            if (name == "vfx_spark") return new Layout { fw = 48, fh = 48, count = 4 };
            if (name == "swing") return new Layout { fw = 128, fh = 128, count = 4 };
            if (name == "bullets") return new Layout { fw = 48, fh = 32, count = 4 };
            if (name == "xp_gem") return new Layout { fw = 32, fh = 32, count = 3 };
            if (name == "weapons_icons") return new Layout { fw = 64, fh = 64, count = 6 };
            if (name == "chest") return new Layout { fw = 64, fh = 64, count = 1 };
            if (name == "floor_tile" || name == "wall_tile") return new Layout { fw = 128, fh = 128, count = 1 };
            return new Layout { fw = 64, fh = 64, count = 1 };
        }

        Sprite Cut(Texture2D tex, int frame, int fw, int fh) =>
            Sprite.Create(tex, new Rect(frame * fw, 0, fw, fh), new Vector2(.5f, .5f), GameCfg.PPU);

        Sprite[] Frames(Texture2D tex, Layout l)
        {
            var arr = new Sprite[l.count];
            for (int i = 0; i < l.count; i++) arr[i] = Cut(tex, i, l.fw, l.fh);
            return arr;
        }

        void LoadAll()
        {
            string[] chars = { "char_captain_tuber", "char_engineer_peel", "char_medic_sprout", "char_mage_truffle" };
            string[] mobs = { "mob_slime_green", "mob_slime_red", "mob_slime_blue", "mob_bat", "mob_ghost", "mob_boss_king_gelatin" };

            foreach (var n in chars) sheets[n] = LoadTex(n);
            foreach (var n in mobs) sheets[n] = LoadTex(n);

            var bulletsTex = LoadTex("bullets");
            var gemsTex = LoadTex("xp_gem");
            var iconsTex = LoadTex("weapons_icons");
            var sparkTex = LoadTex("vfx_spark");
            sheets["vfx_spark"] = sparkTex;

            chest = Cut(LoadTex("chest"), 0, 64, 64);
            swing = Cut(LoadTex("swing"), 0, 128, 128);
            floorTile = Cut(LoadTex("floor_tile"), 0, 128, 128);
            wallTile = Cut(LoadTex("wall_tile"), 0, 128, 128);

            bulletSpr = Cut(bulletsTex, 0, 48, 32);
            xpBlue = Cut(gemsTex, 0, 32, 32);
            xpYellow = Cut(gemsTex, 1, 32, 32);
            xpRed = Cut(gemsTex, 2, 32, 32);

            weaponIcons = new Sprite[6];
            for (int i = 0; i < 6; i++) weaponIcons[i] = Cut(iconsTex, i, 64, 64);

            foreach (var n in chars) animFrames[n] = Frames(sheets[n], LayoutFor(n));
            foreach (var n in mobs) animFrames[n] = Frames(sheets[n], LayoutFor(n));
            animFrames["vfx_spark"] = Frames(sparkTex, LayoutFor("vfx_spark"));
            animFrames["swing"] = Frames(LoadTex("swing"), LayoutFor("swing"));

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = LegacyFont.Get();
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
