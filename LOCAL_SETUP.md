# 🥔 Potato Heroes — установка и первый запуск (локально на ПК)

## Шаг 0. Если проект уже скачан с GitHub
```
git clone https://github.com/postall74/Potato-Heroes.git
```
(или кнопка Code → Download ZIP и распаковать в удобную папку, например `D:\Games\Potato-Heroes`)

## Шаг 1. Установите Unity 6 через Unity Hub
1. Скачайте Unity Hub: https://unity.com/download
2. В Hub: **Installs → Install Editor → Versions → Unity 6** (любая актуальная, например 6000.0.x / LTS).
3. В списке модулей обязательно отметьте:
   - **Microsoft Visual Studio Community** (компонент внутри установщика Unity) — нужен для редактора скриптов;
   - Platform: Windows/Mac/Linux — ваша ОС.
4. Остальные модули (AI, DOTS, WebGL и т.п.) не нужны — игра использует только встроенные API (Sprites, UI, Physics 2D).

## Шаг 2. Откройте проект
1. Unity Hub → **Add project from disk** → выберите **корневую папку репозитория** (там, где лежат `Assets/`, `Packages/`, `ProjectSettings/`).
2. Hub сам определит версию по `ProjectSettings/ProjectVersion.txt`. Если предложит другую версию — соглашайтесь, проект совместим с любой Unity 6.
3. Дождитесь первой импорта ассетов (Library генерируется 1–3 минуты).

## Шаг 3. Автоматическая настройка (произойдёт сама)
При первом открытии редактор выполнит `Assets/Editor/ProjectSetup.cs`:
- сконфигурирует все PNG как спрайты (Point-фильтр, 20 ppu);
- создаст слои Enemy / Player / Bullet;
- добавит `SampleScene` в Build Settings;
- проверит объект **[GAME]** в сцене и при необходимости починит его (см. «Если снова потерянный скрипт»).

В Console должны появиться сообщения `[PotatoHeroes] ...` — это нормально.

## Шаг 4. Запуск
1. Откройте сцену: **Assets/Scenes/SampleScene.unity** (двойной клик).
2. Нажмите ▶ **Play** (или Ctrl+P).
3. Управление: **WASD / стрелки** — движение, **мышь** — прицел, ЛКМ — выстрел (автоатака тоже работает), **Enter** — продолжить после волны.

## Если снова видно "[GAME] с потерянным скриптом"
Это значит сцена открылась до того, как Unity успела скомпилировать скрипты. Лечение (по порядку):
1. Посмотрите Console: если есть красные ошибки компиляции C# — исправьте их сначала (без них скрипты не подгрузятся никогда).
2. Перезапустите Unity полностью (Close → заново открыть проект) — автопочинка `EnsureSceneHasEntry` сама заменит Missing Script на компонент Bootstrap и сохранит сцену как грязную; затем **Ctrl+S**.
3. Ручной вариант: выделите [GAME] → Add Component → найдите **Bootstrap** → удалите красный `Missing Script (MonoScript)` → Ctrl+S.

## Сборка standalone-экзешника (опционально)
1. **File → Build Settings** (в Unity 6: File → Build Profiles → Platforms).
2. Убедитесь, что `Assets/Scenes/SampleScene.unity` в списке **Scenes In Build** (ProjectSetup добавляет его автоматически).
3. Выберите платформу → **Build** → укажите пустую папку (например `Build/Windows`) → Ready.

## Частые проблемы
| Симптом | Решение |
|---|---|
| Hub не видит папку как проект | Открывайте именно корень с `Assets/` и `ProjectSettings/`, а не `Assets/` отдельно |
| Долгий первый импорт / белый экран | Это генерация Library — не прерывайте, 1–5 минут |
| Нет пункта Unity 6 в Hub | Обновите Hub до последней версии |
| После Play ничего не происходит | Проверьте, что открыта SampleScene, а не пустая New Scene; в Hierarchy должен быть [GAME] с компонентом Bootstrap |
| Ошибка про Input System | Не нужна: игра использует старый Input Manager (встроен в Unity) |
