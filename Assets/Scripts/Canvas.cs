using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PotatoHeroes
{
    // UI строится полностью кодом — сцена не требует ручной расстановки.
    public class HUD : MonoBehaviour
    {
        public static HUD I { get; private set; }

        Canvas _canvas;
        Text _hpText, _waveText, _timerText, _xpText, _killsText, _weaponBar;
        Image _hpBar, _xpBar;
        RectTransform _root;
        readonly Queue<Text> _floatPool = new Queue<Text>();
        readonly List<(Text t, float life, Vector2 v)> _floats = new List<(Text t, float life, Vector2 v)>();
        GameObject _upgradePanel, _menuPanel, _endPanel;

        void Awake()
        {
            // Защита от дубликатов синглтона
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            BuildCanvas();
        }

        void BuildCanvas()
        {
            // Screen Space Overlay — не требует UI-камеры и трансформации позиции.
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1280, 720);
            _root = go.GetComponent<RectTransform>();

            // КРИТИЧНО для кликабельности кнопок: без EventSystem GraphicRaycaster
            // ничего не доставляет в Button.onClick. Создаём вручную (в сцене его нет).
            EnsureEventSystem();
        }

        static void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // Legacy Input Manager (Input.GetAxis) — проект использует его, New Input System не ставим.
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        static Font Font => AssetBank.I ? AssetBank.I.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Text MkText(string name, Transform parent, int size, Color col, Vector2 anchor, Vector2 pos, Vector2? sz = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font; t.fontSize = size; t.color = col; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sz ?? new Vector2(400, 40);
            return t;
        }

        Image MkBar(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color col)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = col;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return img;
        }

        public void Refresh()
        {
            if (_canvas == null) return;
            var p = Player.I;
            if (p == null) return;
            _hpText.text = $"{Mathf.CeilToInt(p.hp)} / {Mathf.RoundToInt(p.maxHp)}";
            float hpFrac = Mathf.Clamp01(p.hp / p.maxHp);
            _hpBar.rectTransform.sizeDelta = new Vector2(300f * hpFrac, 22);
        }

        public void BindGame()
        {
            // нижняя панель XP и инфо о волне создаются один раз
            if (_waveText != null) return;
            _waveText = MkText("wave", _root, 26, Color.white, new Vector2(.5f, 1), new Vector2(0, -18));
            _timerText = MkText("timer", _root, 40, new Color(1f,.9f,.5f), new Vector2(.5f, 1), new Vector2(0, -52));
            _hpText = MkText("hp", _root, 20, Color.white, new Vector2(0, 1), new Vector2(160, -28), new Vector2(200,30));
            MkBar("hpBg", _root, new Vector2(0,1), new Vector2(160,-52), new Vector2(300,22), new Color(0,0,0,.5f));
            _hpBar = MkBar("hpFill", _root, new Vector2(0,1), new Vector2(160,-52), new Vector2(300,22), new Color(.9f,.2f,.2f,1));
            _hpBar.rectTransform.anchorMin = new Vector2(0,1); _hpBar.rectTransform.anchorMax = new Vector2(0,1);
            _hpBar.rectTransform.pivot = new Vector2(0,.5f);
            _hpBar.rectTransform.anchoredPosition = new Vector2(10,-52);
            _xpText = MkText("lvl", _root, 20, new Color(.5f,.9f,1f), new Vector2(0,1), new Vector2(160, -84), new Vector2(200,30));
            MkBar("xpBg", _root, new Vector2(0,1), new Vector2(160,-106), new Vector2(300,12), new Color(0,0,0,.5f));
            _xpBar = MkBar("xpFill", _root, new Vector2(0,1), new Vector2(10,-106), new Vector2(300,12), new Color(.3f,.7f,1f));
            _xpBar.rectTransform.pivot = new Vector2(0,.5f);
            _killsText = MkText("kills", _root, 18, new Color(1f,.6f,.6f), new Vector2(1,1), new Vector2(-90,-28), new Vector2(160,30));
            _weaponBar = MkText("weapons", _root, 16, Color.white, new Vector2(.5f, 0), new Vector2(0, 20), new Vector2(900,30));
        }

        public void UpdateHud(int wave, float timeLeft, int kills, int level, int xp, int xpNext, List<WeaponDef> weapons)
        {
            if (_waveText == null) BindGame();
            var p = Player.I;
            _waveText.text = $"ВОЛНА {wave} / {GameCfg.MaxWaves}";
            _timerText.text = timeLeft >= 0 ? $"{Mathf.CeilToInt(timeLeft)}" : "ЗАЧИСТИТЬ АРЕНУ!";
            _killsText.text = $"Убийств: {kills}";
            _xpText.text = $"Уровень {level}";
            float xf = xpNext > 0 ? Mathf.Clamp01((float)xp / xpNext) : 1;
            _xpBar.rectTransform.sizeDelta = new Vector2(300 * xf, 12);
            string wnames = "";
            foreach (var w in weapons) wnames += $"[{w.nameRU} ур.{w.tier}]  ";
            _weaponBar.text = wnames;
            if (p)
            {
                _hpText.text = $"{Mathf.CeilToInt(p.hp)} / {Mathf.RoundToInt(p.maxHp)}";
                _hpBar.rectTransform.sizeDelta = new Vector2(300 * Mathf.Clamp01(p.hp / p.maxHp), 22);
            }
        }

        // ===== Всплывающий текст мира -> UI =====
        public void ShowFloatingText(Vector3 worldPos, string text, bool isLabel)
        {
            if (_canvas == null) return;
            Text t;
            if (_floatPool.Count > 0) t = _floatPool.Dequeue();
            else
            {
                t = MkText("ft", _root, 22, Color.white, new Vector2(.5f,.5f), Vector2.zero, new Vector2(200,30));
            }
            t.gameObject.SetActive(true);
            t.text = text;
            t.color = isLabel ? new Color(.6f,1f,.6f) : (text.Contains("КРИТ") ? Color.yellow : Color.white);
            t.fontSize = isLabel ? 20 : 24;
            var cam = Camera.main;
            Vector2 sp = cam ? WorldToUiPoint(worldPos) : Vector2.zero;
            t.rectTransform.anchoredPosition = sp;
            _floats.Add((t, .7f, new Vector2(Random.Range(-8f,8f), 60f)));
        }

        Vector2 WorldToUiPoint(Vector3 world)
        {
            var cam = Camera.main;
            var mp = RectTransformUtility.WorldToScreenPoint(cam, world);
            // worldCamera = null: для Screen Space Overlay это правильный параметр.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, mp, null, out var lp);
            return lp;
        }

        void LateUpdate()
        {
            for (int i = _floats.Count - 1; i >= 0; i--)
            {
                var (t, life, v) = _floats[i];
                life -= Time.deltaTime;
                t.rectTransform.anchoredPosition += v * Time.deltaTime;
                var c = t.color; c.a = Mathf.Clamp01(life / .4f); t.color = c;
                if (life <= 0)
                {
                    t.gameObject.SetActive(false);
                    _floatPool.Enqueue(t);
                    _floats.RemoveAt(i);
                }
                else _floats[i] = (t, life, v);
            }
        }

        // ===== Панели =====
        public void ShowMenu(System.Action<CharacterDef> onStart)
        {
            HideAll();
            _menuPanel = new GameObject("Menu", typeof(RectTransform));
            _menuPanel.transform.SetParent(_root, false);
            var full = _menuPanel.GetComponent<RectTransform>();
            Stretch(full);
            var bg = _menuPanel.AddComponent<Image>(); bg.color = new Color(.06f,.07f,.1f,.96f);

            MkText("title", full, 54, new Color(1f,.85f,.4f), new Vector2(.5f,1), new Vector2(0,-90)).text =
                "🥔 POTATO HEROES 🥔";
            MkText("sub", full, 22, Color.gray, new Vector2(.5f,1), new Vector2(0,-140)).text =
                "Roguelike-арена в духе Brotato · WASD — движение · мышь — прицел · автоатака по ближайшему врагу";

            var chars = CharacterDef.All;
            for (int i = 0; i < chars.Length; i++)
            {
                var def = chars[i];
                var b = MakeButton(full, def.nameRU + "\n" + def.descRU,
                    new Vector2(.5f + (i % 2 == 0 ? -.26f : .26f), .55f - (i / 2) * .22f),
                    new Vector2(320, 110),
                    () => { HideAll(); onStart?.Invoke(def); });
            }
        }

        public void ShowUpgradeChoices(List<UpgradeDef> opts, System.Action<UpgradeDef> onPick)
        {
            HideAll();
            _upgradePanel = new GameObject("Upg", typeof(RectTransform));
            _upgradePanel.transform.SetParent(_root, false);
            var full = _upgradePanel.GetComponent<RectTransform>(); Stretch(full);
            var bg = _upgradePanel.AddComponent<Image>(); bg.color = new Color(0,0,0,.75f);
            MkText("t", full, 34, new Color(.6f,1f,.6f), new Vector2(.5f,1), new Vector2(0,-80)).text =
                "УРОВЕНЬ ПОВЫСИЛСЯ! Выбери улучшение:";
            for (int i = 0; i < opts.Count; i++)
            {
                var u = opts[i];
                float x = (i - (opts.Count - 1) / 2f) * .3f;
                MakeButton(full, $"{u.nameRU}\n{u.descRU}", new Vector2(.5f + x, .45f), new Vector2(300, 130),
                    () => { HideAll(); onPick?.Invoke(u); });
            }
        }

        public void ShowWaveSummary(int wave, System.Action onContinue)
        {
            HideAll();
            _upgradePanel = new GameObject("WaveEnd", typeof(RectTransform));
            _upgradePanel.transform.SetParent(_root, false);
            var full = _upgradePanel.GetComponent<RectTransform>(); Stretch(full);
            var bg = _upgradePanel.AddComponent<Image>(); bg.color = new Color(0,0,0,.7f);
            MkText("t", full, 40, new Color(1f,.85f,.4f), new Vector2(.5f,.6f), Vector2.zero).text =
                $"Волна {wave} пройдена!";
            MkText("h", full, 20, Color.gray, new Vector2(.5f,.5f), Vector2.zero).text =
                "Подбери выпавшие кристаллы опыта и сундуки — затем продолжай.";
            MakeButton(full, "ПРОДОЛЖИТЬ ▶", new Vector2(.5f,.35f), new Vector2(260,70),
                () => { HideAll(); onContinue?.Invoke(); });
        }

        public void ShowEnd(bool victory, System.Action onRestart)
        {
            HideAll();
            _endPanel = new GameObject("End", typeof(RectTransform));
            _endPanel.transform.SetParent(_root, false);
            var full = _endPanel.GetComponent<RectTransform>(); Stretch(full);
            var bg = _endPanel.AddComponent<Image>(); bg.color = new Color(.05f,.05f,.08f,.95f);
            MkText("t", full, 52, victory ? Color.green : Color.red, new Vector2(.5f,.62f), Vector2.zero).text =
                victory ? "🏆 ПОБЕДА! Огород спасён!" : "☠️ Поражение";
            MkText("k", full, 24, Color.white, new Vector2(.5f,.5f), Vector2.zero).text =
                GameManager.I.StatsLine;
            MakeButton(full, "ЗАНОВО", new Vector2(.5f,.3f), new Vector2(240,70),
                () => { HideAll(); onRestart?.Invoke(); });
        }

        void HideAll()
        {
            if (_menuPanel) Destroy(_menuPanel);
            if (_upgradePanel) Destroy(_upgradePanel);
            if (_endPanel) Destroy(_endPanel);
            _menuPanel = _upgradePanel = _endPanel = null;
        }

        Button MakeButton(RectTransform parent, string label, Vector2 anchorPos, Vector2 size, System.Action onClick)
        {
            var go = new GameObject("btn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchorPos;
            rt.pivot = new Vector2(.5f,.5f);
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = new Color(.2f,.35f,.55f);
            var btn = go.GetComponent<Button>();
            var cb = btn.colors; cb.highlightedColor = new Color(.3f,.5f,.75f); cb.pressedColor = new Color(.15f,.25f,.4f);
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var txt = MkText("l", rt, 20, Color.white, new Vector2(.5f,.5f), Vector2.zero, size);
            txt.text = label;
            return btn;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
