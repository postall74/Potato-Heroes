using UnityEngine;

// ===== Глобальные константы и настройки игры "Potato Heroes" (Brotato-like) =====
namespace PotatoHeroes
{
    public enum EnemyType { GreenSlime, RedSlime, BlueSlime, Bat, Ghost, BossKingGelatin }
    public enum WeaponKind { SpudPistol, PeaSMG, PumpkinShotgun, CornRifle, Knife, MagicWand }
    public enum GamePhase { Menu, Wave, Intermission, GameOver, Victory }

    public static class GameCfg
    {
        // Пиксели на единицу мира. Реальные кадры спрайт-шитов ~96x56 px (см. AssetBank),
        // поэтому PPU=48: слизь ≈ 2 юнита, персонаж ≈ 2.7 юнита — крупные, читаемые спрайты.
        public const float PPU = 48f;
        public static Vector2 S(float w, float h) => new Vector2(w / PPU, h / PPU);

        public static readonly Color[] BulletColors =
        {
            new Color(1f, .84f, .31f), new Color(.88f,.88f,.88f), new Color(1f,.44f,.26f),
            new Color(.31f,.76f,.97f), new Color(.73f,.41f,.78f), new Color(.68f,.84f,.5f)
        };

        public const float ArenaW = 26f;   // ширина арены в юнитах
        public const float ArenaH = 16f;
        public const int MaxWaves = 10;
        public const float WaveDuration = 30f;
    }

    [System.Serializable]
    public class WeaponDef
    {
        public WeaponKind kind;
        public string nameRU;
        public int damage = 5;
        public float cooldown = 0.5f;   // сек между выстрелами
        public int projectiles = 1;
        public float spreadDeg = 6f;    // разброс
        public float bulletSpeed = 12f;
        public bool melee = false;
        public float range = 2f;        // для ближнего боя
        public int iconIndex = 0;       // индекс на sheets weapons_icons
        public int tier = 1;

        public static WeaponDef Make(WeaponKind k, int level = 1)
        {
            var w = new WeaponDef { kind = k };
            switch (k)
            {
                case WeaponKind.SpudPistol: w.nameRU="Картофельный пистолет"; w.damage=6; w.cooldown=.55f; w.projectiles=1; w.spreadDeg=5; w.bulletSpeed=13; w.iconIndex=0; break;
                case WeaponKind.PeaSMG: w.nameRU="Гороховый ПП"; w.damage=3; w.cooldown=.16f; w.projectiles=1; w.spreadDeg=12; w.bulletSpeed=15; w.iconIndex=1; break;
                case WeaponKind.PumpkinShotgun: w.nameRU="Тыквенный дробовик"; w.damage=4; w.cooldown=.9f; w.projectiles=5; w.spreadDeg=28; w.bulletSpeed=11; w.iconIndex=2; break;
                case WeaponKind.CornRifle: w.nameRU="Кукурузная винтовка"; w.damage=14; w.cooldown=1.1f; w.projectiles=1; w.spreadDeg=2; w.bulletSpeed=20; w.iconIndex=3; break;
                case WeaponKind.Knife: w.nameRU="Нож «Чистый Картофель»"; w.damage=12; w.cooldown=.5f; w.melee=true; w.range=2.2f; w.iconIndex=4; break;
                case WeaponKind.MagicWand: w.nameRU="Волшебная лоза"; w.damage=8; w.cooldown=.45f; w.projectiles=1; w.spreadDeg=10; w.bulletSpeed=9; w.iconIndex=5; break;
            }
            w.tier = level;
            w.damage = Mathf.RoundToInt(w.damage * (1f + 0.35f * (level - 1)));
            w.cooldown = Mathf.Max(0.06f, w.cooldown * (1f - 0.06f * (level - 1)));
            return w;
        }
    }

    [System.Serializable]
    public class EnemyDef
    {
        public EnemyType type;
        public string nameRU;
        public float hp, speed, damage, xpValue;
        // ЛОГИЧЕСКИЙ размер моба в юнитах мира (для коллайдера). Реальные пиксельные
        // размеры кадров спрайт-шитов задаются один раз в AssetBank.SheetLayout.
        public float spriteW = 2f, spriteH = 2f;
        public int frames;
        public float animFps;
        public bool flying;

        public static EnemyDef Make(EnemyType t, int wave)
        {
            float scale = 1f + 0.18f * (wave - 1); // рост сложности по волнам
            var d = new EnemyDef { type = t };
            switch (t)
            {
                case EnemyType.GreenSlime: d.nameRU="Зелёная слизь"; d.hp=10; d.speed=2.4f; d.damage=4; d.xpValue=2; d.spriteW=d.spriteH=2f; d.frames=4; d.animFps=6; break;
                case EnemyType.RedSlime:   d.nameRU="Красная ярость"; d.hp=18; d.speed=3.1f; d.damage=6; d.xpValue=3; d.spriteW=d.spriteH=2f; d.frames=4; d.animFps=7; break;
                case EnemyType.BlueSlime:  d.nameRU="Ледяная слизь"; d.hp=14; d.speed=2.0f; d.damage=5; d.xpValue=3; d.spriteW=d.spriteH=2f; d.frames=4; d.animFps=6; break;
                case EnemyType.Bat:        d.nameRU="Летучая мышь"; d.hp=8; d.speed=4.2f; d.damage=3; d.xpValue=2; d.spriteW=2f; d.spriteH=1.5f; d.frames=4; d.animFps=12; d.flying=true; break;
                case EnemyType.Ghost:      d.nameRU="Призрак огорода"; d.hp=26; d.speed=2.2f; d.damage=8; d.xpValue=5; d.spriteW=2f; d.spriteH=2.4f; d.frames=4; d.animFps=8; d.flying=true; break;
                case EnemyType.BossKingGelatin: d.nameRU="КОРОЛЬ ЖЕЛЕ"; d.hp=420; d.speed=1.7f; d.damage=14; d.xpValue=60; d.spriteW=d.spriteH=3.4f; d.frames=4; d.animFps=5; break;
            }
            d.hp = Mathf.RoundToInt(d.hp * scale);
            return d;
        }
    }

    [System.Serializable]
    public class UpgradeDef
    {
        public string id, nameRU, descRU;
        public int maxStacks = 5;
        // Применяется к игроку
        public const string HP="hp", DMG="dmg", SPD="spd", AS="as", CRIT="crit", LIFESTEAL="ls", RANGE="range", PROJ="proj", ARMOR="armor", XP="xp";
        public static UpgradeDef Make(string id)
        {
            switch (id)
            {
                case HP: return new UpgradeDef{id=HP,nameRU="Здоровье",descRU="+15 максимального ОЗ",maxStacks=9};
                case DMG: return new UpgradeDef{id=DMG,nameRU="Сила",descRU="+2 к урону",maxStacks=9};
                case SPD: return new UpgradeDef{id=SPD,nameRU="Скорость",descRU="+8% скорости бега",maxStacks=7};
                case AS: return new UpgradeDef{id=AS,nameRU="Скорострельность",descRU="+10% скорости атаки",maxStacks=9};
                case CRIT: return new UpgradeDef{id=CRIT,nameRU="Крит",descRU="+5% шанс критического удара x2",maxStacks=8};
                case LIFESTEAL: return new UpgradeDef{id=LIFESTEAL,nameRU="Вампиризм",descRU="+1 ОЗ за каждое убийство",maxStacks=5};
                case RANGE: return new UpgradeDef{id=RANGE,nameRU="Дальность",descRU="+15% дальности/дальности боя",maxStacks=5};
                case PROJ: return new UpgradeDef{id=PROJ,nameRU="+1 снаряд",descRU="+1 снаряд в залпе",maxStacks=3};
                case ARMOR: return new UpgradeDef{id=ARMOR,nameRU="Броня",descRU="-10% получаемого урона",maxStacks=6};
                case XP: return new UpgradeDef{id=XP,nameRU="Жадность",descRU="+20% опыта с кристаллов",maxStacks=5};
            }
            return null;
        }
        public static string[] Pool => new[]{HP,DMG,SPD,AS,CRIT,LIFESTEAL,RANGE,PROJ,ARMOR,XP};
    }

    [System.Serializable]
    public class CharacterDef
    {
        public string id, nameRU, descRU, sprite;
        public float hpMul=1f, dmgMul=1f, spdMul=1f, armorMul=1f;
        public static CharacterDef[] All = new[]
        {
            new CharacterDef{id="captain",nameRU="Капитан Клубень",descRU="Ветеран огородных войн. +15% урона.",sprite="char_captain_tuber",dmgMul=1.15f},
            new CharacterDef{id="engineer",nameRU="Инженер Очисток",descRU="Быстрый и шустрый. +15% скорости.",sprite="char_engineer_peel",spdMul=1.15f},
            new CharacterDef{id="medic",nameRU="Медсестра Росток",descRU="Крепкая. +30 ОЗ и немного брони.",sprite="char_medic_sprout",hpMul=1.3f,armorMul=0.9f},
            new CharacterDef{id="mage",nameRU="Маг Трюфель",descRU="Стеклянная пушка: +25% урона, -20% ОЗ.",sprite="char_mage_truffle",dmgMul=1.25f,hpMul=0.8f},
        };
    }
}
