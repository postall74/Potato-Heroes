using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PotatoHeroes
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public GamePhase phase = GamePhase.Menu;
        public int wave = 1;
        public float waveTimer;
        public bool bossWave => wave == GameCfg.MaxWaves;
        public int kills;
        public int level = 1, xp, xpNext = 8;
        readonly Dictionary<string,int> _stacks = new Dictionary<string,int>();
        CharacterDef _selectedChar;
        Transform _spawnRoot;
        int _toSpawn; float _spawnCd;
        bool _bossAlive;

        static GameManager _createdThisSession;

        void Awake()
        {
            // Защита от дубликата: если компонент был добавлен на [GAME] вручную (или Bootstrap
            // сработал повторно), а такой менеджер уже есть в этом рантайме — удаляем лишний.
            if (_createdThisSession != null && _createdThisSession != this)
            {
                Destroy(this);          // снимаем лишний компонент, объект [GAME] не трогаем
                return;
            }
            if (I != null && I != this) { Destroy(gameObject); return; }
            _createdThisSession = I = this;
            Application.targetFrameRate = 60;
            SetupLayers();
        }

        void SetupLayers()
        {
            EnsureLayer("Enemy", 7); EnsureLayer("Player", 8); EnsureLayer("Bullet", 9);
            // Физический шаг движка нам не нужен: ВСЕ тела RigidbodyType2D.Kinematic,
            // движение — трансформом/MovePosition, попадания — Physics2D.OverlapCircleAll.
            // Поэтому просто не вызываем никаких физических шагов; менять simulationMode необязательно
            // (в разных сборках Unity 6 имя enum/свойства отличается — отсюда была ошибка CS0103).
        }

        void EnsureLayer(string name, int idx)
        {
            if (LayerMask.NameToLayer(name) < 0)
                UnityEditorTagLayer.Assign(name, idx);
        }

        void Start()
        {
            BuildWorld();
            HUD.I.ShowMenu(StartRun);
        }

        void BuildWorld()
        {
            var ab = gameObject.AddComponent<AssetBank>();
            _ = ab; // Awake загрузит ассеты
            // Камера
            var camGo = new GameObject("MainCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.05f,.06f,.09f);
            cam.orthographicSize = GameCfg.ArenaH / 2f + 1.2f;
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);
            var follow = camGo.AddComponent<CameraFollow>();

            // Пол арены: тайлы floor_tile
            _spawnRoot = new GameObject("Arena").transform;
            var bank = AssetBank.I;
            float tileW = 32 / GameCfg.PPU, tileH = 32 / GameCfg.PPU;
            int cols = Mathf.CeilToInt(GameCfg.ArenaW / tileW), rows = Mathf.CeilToInt(GameCfg.ArenaH / tileH);
            for (int y = 0; y < rows; y++) for (int x = 0; x < cols; x++)
            {
                var go = new GameObject($"t{x}_{y}");
                go.transform.SetParent(_spawnRoot, false);
                go.transform.position = new Vector3(-GameCfg.ArenaW/2 + x*tileW + tileW/2, -GameCfg.ArenaH/2 + y*tileH + tileH/2);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = bank.floorTile; sr.sortingOrder = -10;
            }
            // Стены по периметру
            for (int x = -1; x <= cols; x++) for (int y = -1; y <= rows; y++)
            {
                bool edge = x == -1 || y == -1 || x == cols || y == rows;
                if (!edge) continue;
                var go = new GameObject($"w{x}_{y}");
                go.transform.SetParent(_spawnRoot, false);
                go.transform.position = new Vector3(-GameCfg.ArenaW/2 + x*tileW + tileW/2, -GameCfg.ArenaH/2 + y*tileH + tileH/2);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = bank.wallTile; sr.sortingOrder = -9;
            }
        }

        void StartRun(CharacterDef def)
        {
            _selectedChar = def;
            wave = 1; kills = 0; level = 1; xp = 0; xpNext = 8;
            _stacks.Clear();

            // Игрок
            var pgo = new GameObject("Player");
            pgo.layer = LayerMask.NameToLayer("Player") >= 0 ? LayerMask.NameToLayer("Player") : 8;
            var psr = pgo.AddComponent<SpriteRenderer>();
            psr.sortingOrder = 8;
            pgo.AddComponent<SpriteAnimator>();
            var pl = pgo.AddComponent<Player>();
            pl.Setup(def, AssetBank.I.animFrames[def.sprite]);
            pl.weapons.Add(WeaponDef.Make(WeaponKind.SpudPistol));
            HUD.I.BindGame();

            BeginWave();
        }

        void BeginWave()
        {
            phase = GamePhase.Wave;
            waveTimer = bossWave ? -1 : GameCfg.WaveDuration;
            _toSpawn = TotalEnemiesForWave(wave);
            _spawnCd = .5f;
            _bossAlive = false;
            if (bossWave) SpawnBoss();
        }

        int TotalEnemiesForWave(int w)
        {
            if (bossWave) return 14; // свита босса
            return Mathf.RoundToInt(10 + w * 7 + w * w * .8f);
        }

        void Update()
        {
            if (phase == GamePhase.Wave)
            {
                if (waveTimer > 0)
                {
                    waveTimer -= Time.deltaTime;
                    if (waveTimer <= 0) { waveTimer = 0; }
                }
                // Спавн порциями
                if (_toSpawn > 0)
                {
                    _spawnCd -= Time.deltaTime;
                    if (_spawnCd <= 0)
                    {
                        int n = Mathf.Min(Random.Range(2, 5), _toSpawn);
                        for (int i = 0; i < n; i++) SpawnEnemy(PickEnemyType());
                        _toSpawn -= n;
                        _spawnCd = Mathf.Lerp(.9f, .25f, wave / (float)GameCfg.MaxWaves);
                    }
                }
                // Конец волны: время вышло и все живые убиты (или босс мёртв)
                bool cleared = waveTimer <= 0 && _toSpawn <= 0 &&
                               CountLiveNonBoss() == 0 && (!_bossAlive || bossKilledFlag);
                if (cleared) EndWave();
            }
            else if (phase == GamePhase.Intermission)
            {
                // фаза подбора лута: жмём Enter или подбираем всё
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    ContinueAfterLoot();
            }
            HUD.I?.UpdateHud(wave, phase == GamePhase.Wave ? waveTimer : -1, kills, level, xp, xpNext,
                Player.I ? Player.I.weapons : new List<WeaponDef>());
        }

        bool bossKilledFlag;
        int CountLiveNonBoss()
        {
            int c = 0;
            foreach (var e in Enemy.All) if (e.Alive && e.def.type != EnemyType.BossKingGelatin) c++;
            return c;
        }

        EnemyType PickEnemyType()
        {
            var list = new List<EnemyType> { EnemyType.GreenSlime, EnemyType.GreenSlime, EnemyType.RedSlime };
            if (wave >= 2) list.Add(EnemyType.Bat);
            if (wave >= 3) list.Add(EnemyType.BlueSlime);
            if (wave >= 4) list.Add(EnemyType.Ghost);
            if (wave >= 5) { list.Add(EnemyType.RedSlime); list.Add(EnemyType.Ghost); }
            return list[Random.Range(0, list.Count)];
        }

        string SheetFor(EnemyType t) => t switch
        {
            EnemyType.GreenSlime => "mob_slime_green",
            EnemyType.RedSlime => "mob_slime_red",
            EnemyType.BlueSlime => "mob_slime_blue",
            EnemyType.Bat => "mob_bat",
            EnemyType.Ghost => "mob_ghost",
            EnemyType.BossKingGelatin => "mob_boss_king_gelatin",
            _ => "mob_slime_green"
        };

        void SpawnEnemy(EnemyType t)
        {
            var def = EnemyDef.Make(t, wave);
            var go = new GameObject(def.nameRU);
            go.transform.position = RandomEdgePos();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 6;
            go.AddComponent<SpriteAnimator>();
            var e = go.AddComponent<Enemy>();
            e.Setup(def, AssetBank.I.animFrames[SheetFor(t)], def.animFps);
        }

        void SpawnBoss()
        {
            var def = EnemyDef.Make(EnemyType.BossKingGelatin, wave);
            var go = new GameObject("BOSS");
            go.transform.position = new Vector3(0, GameCfg.ArenaH/2 - 2f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 7;
            go.AddComponent<SpriteAnimator>();
            var e = go.AddComponent<Enemy>();
            e.Setup(def, AssetBank.I.animFrames[SheetFor(EnemyType.BossKingGelatin)], def.animFps);
            _bossAlive = true; bossKilledFlag = false;
        }

        public void OnBossKilled() { bossKilledFlag = true; _bossAlive = false; }

        Vector2 RandomEdgePos()
        {
            int s = Random.Range(0, 4);
            float hw = GameCfg.ArenaW/2 - .6f, hh = GameCfg.ArenaH/2 - .6f;
            return s switch
            {
                0 => new Vector2(Random.Range(-hw,hw), hh),
                1 => new Vector2(Random.Range(-hw,hw), -hh),
                2 => new Vector2(-hw, Random.Range(-hh,hh)),
                _ => new Vector2(hw, Random.Range(-hh,hh)),
            };
        }

        void EndWave()
        {
            if (bossWave) { Victory(); return; }
            phase = GamePhase.Intermission;
            // Бонусный сундук с оружием
            var kinds = (WeaponKind[])System.Enum.GetValues(typeof(WeaponKind));
            ChestPickup.Spawn(RandomEdgePos() * .6f, kinds[Random.Range(0, kinds.Length)], Mathf.Clamp(1 + wave / 3, 1, 4));
            HUD.I.ShowWaveSummary(wave, ContinueAfterLoot);
        }

        void ContinueAfterLoot()
        {
            wave++;
            HealBetweenWaves();
            BeginWave();
        }

        void HealBetweenWaves()
        {
            if (Player.I) Player.I.Heal(Mathf.RoundToInt(Player.I.maxHp * .25f));
        }

        public void CountKill() { kills++; }

        // ===== XP / Уровни =====
        public void AddXP(int v)
        {
            xp += Mathf.RoundToInt(v * (Player.I ? Player.I.xpMul : 1f));
            while (xp >= xpNext)
            {
                xp -= xpNext; level++;
                xpNext = Mathf.RoundToInt(xpNext * 1.35f + 4);
                OfferUpgrade();
            }
        }

        void OfferUpgrade()
        {
            var pool = new List<UpgradeDef>();
            foreach (var id in UpgradeDef.Pool)
            {
                var u = UpgradeDef.Make(id);
                if (_stacks.TryGetValue(id, out var st) && st >= u.maxStacks) continue;
                pool.Add(u);
            }
            // Перемешать, взять 3
            for (int i = pool.Count - 1; i > 0; i--) { int j = Random.Range(0, i+1); (pool[i], pool[j]) = (pool[j], pool[i]); }
            var opts = pool.GetRange(0, Mathf.Min(3, pool.Count));
            if (opts.Count == 0) return;
            Time.timeScale = 1f; // паузы нет; выбор мгновенный (упрощение для аркадности)
            HUD.I.ShowUpgradeChoices(opts, ApplyUpgrade);
        }

        void ApplyUpgrade(UpgradeDef u)
        {
            _stacks[u.id] = _stacks.TryGetValue(u.id, out var s) ? s + 1 : 1;
            var p = Player.I; if (!p) return;
            switch (u.id)
            {
                case UpgradeDef.HP: p.maxHp += 15; p.hp += 15; break;
                case UpgradeDef.DMG: p.bonusDamage += 2; break;
                case UpgradeDef.SPD: p.baseSpeed *= 1.08f; break;
                case UpgradeDef.AS: p.atkSpeedMul *= 1.1f; break;
                case UpgradeDef.CRIT: p.critChance += .05f; break;
                case UpgradeDef.LIFESTEAL: p.lifeSteal += 1; break;
                case UpgradeDef.RANGE: p.rangeMul += .15f; break;
                case UpgradeDef.PROJ: p.bonusProjectiles += 1; break;
                case UpgradeDef.ARMOR: p.armorPct = Mathf.Min(60, p.armorPct + 10); break;
                case UpgradeDef.XP: p.xpMul += .2f; break;
            }
        }

        public void GrantWeapon(WeaponKind k, int tier)
        {
            var p = Player.I; if (!p) return;
            if (p.weapons.Count >= 6) // апгрейд существующего такого же типа
            {
                for (int i = 0; i < p.weapons.Count; i++)
                    if (p.weapons[i].kind == k) { p.weapons[i] = WeaponDef.Make(k, tier + 1); return; }
                p.weapons[^1] = WeaponDef.Make(k, tier); // заменяем последний
            }
            else p.weapons.Add(WeaponDef.Make(k, tier));
        }

        public string StatsLine => $"Волна: {wave} · Убийств: {kills} · Уровень: {level}";

        public void OnPlayerDeath()
        {
            if (phase == GamePhase.GameOver) return;
            phase = GamePhase.GameOver;
            HUD.I.ShowEnd(false, Restart);
        }

        void Victory()
        {
            phase = GamePhase.Victory;
            HUD.I.ShowEnd(true, Restart);
        }

        void Restart()
        {
            bossKilledFlag = false; _bossAlive = false;
            wave = 1; level = 1; xp = 0; xpNext = 8; kills = 0;
            foreach (var e in Enemy.All.ToArray()) Destroy(e.gameObject);
            var p = Player.I; if (p) Destroy(p.gameObject);
            DestroyAllChildren();
            phase = GamePhase.Menu;
            HUD.I.ShowMenu(StartRun);
        }

        void DestroyAllChildren()
        {
            foreach (var o in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (o.name is "Bullet" or "xp" or "chest" or "spark" or "swing" or "BOSS" or "MainCamera" or "Canvas" or "UICamera" or "Arena")
                    if (o.name != "MainCamera" && o.name != "Canvas" && o.name != "UICamera" && o.name != "Arena")
                        Destroy(o);
            }
        }
    }

    // Плавное следование камеры за игроком
    public class CameraFollow : MonoBehaviour
    {
        void LateUpdate()
        {
            var target = Player.I ? (Vector2)Player.I.Position : Vector2.zero;
            float x = Mathf.Clamp(Mathf.Lerp(transform.position.x, target.x, Time.deltaTime * 6f), -2f, 2f);
            float y = Mathf.Clamp(Mathf.Lerp(transform.position.y, target.y, Time.deltaTime * 6f), -1.5f, 1.5f);
            transform.position = new Vector3(x, y, transform.position.z); // z остаётся -10
        }
    }

    // Присвоение слоёв без ручных ProjectSettings (через tag manager API редактора во время игры недоступно,
    // поэтому просто резервируем индексы — NameToLayer использует TagManager из проекта)
    public static class UnityEditorTagLayer
    {
        public static void Assign(string name, int idx) { /* слои заданы в TagManager.asset */ }
    }
}
