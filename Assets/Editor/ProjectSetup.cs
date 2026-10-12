#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PotatoHeroes.EditorTools
{
    // Автоматическая настройка проекта при первом открытии:
    //  - Point-фильтрация для всех PNG
    //  - слои Enemy/Player/Bullet
    //  - сцена SampleScene в Build Settings
    public static class ProjectSetup
    {
        [MenuItem("Potato Heroes/1. Setup Project (auto)")]
        public static void Setup()
        {
            ConfigureSpriteImporters();
            EnsureLayers();
            Debug.Log("[PotatoHeroes] Проект настроен. Откройте Assets/Scenes/SampleScene.unity и нажмите Play.");
        }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            // Однократно после импорта ассетов
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("ph_setup_done", false))
                {
                    SessionState.SetBool("ph_setup_done", true);
                    Setup();
                }
            };
        }


        [InitializeOnLoadMethod]
        static void EnsureSceneHasEntry()
        {
            EditorApplication.delayCall += () =>
            {
                var bootstrapType = System.Type.GetType("PotatoHeroes.Bootstrap, Assembly-CSharp");
                if (bootstrapType == null) return; // скрипты ещё не скомпилированы — повторим при следующем открытии

                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (!scene.IsValid()) return;

                bool dirty = false;
                var roots = scene.GetRootGameObjects();

                // 1) Чиним "потерянные" скрипты: объект [GAME] без компонента-скрипта
                foreach (var r in roots)
                {
                    if (r.name != "[GAME]") continue;
                    var comps = r.GetComponents<Component>();
                    bool hasBootstrap = false, hasMissing = false;
                    foreach (var c in comps)
                    {
                        if (c == null) { hasMissing = true; continue; }
                        if (c.GetType() == bootstrapType) hasBootstrap = true;
                    }
                    if (!hasBootstrap)
                    {
                        if (hasMissing)
                            Debug.LogWarning("[PotatoHeroes] Найдена потерянная ссылка на скрипт на [GAME] — переподключаем Bootstrap.");
                        r.AddComponent(bootstrapType);
                        dirty = true;
                    }
                }

                // 2) Если объекта [GAME] вовсе нет — создаём
                bool any = false;
                foreach (var r in roots) if (r.name == "[GAME]") { any = true; break; }
                if (!any)
                {
                    var go = new GameObject("[GAME]");
                    go.AddComponent(bootstrapType);
                    dirty = true;
                }

                if (dirty)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log("[PotatoHeroes] Сцена исправлена: на объекте [GAME] висит Bootstrap. Нажмите Play.");
                }
            };
        }

        static void ConfigureSpriteImporters()
        {
            string dir = Path.Combine(Application.dataPath, "Sprites");
            foreach (var f in Directory.GetFiles(dir, "*.png"))
            {
                var rp = "Assets" + f.Substring(Application.dataPath.Length).Replace("\\", "/");
                var imp = AssetImporter.GetAtPath(rp) as TextureImporter;
                if (imp == null) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit = 20f;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
        }

        static void EnsureLayers()
        {
            var am = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = am.FindProperty("layers");
            string[] want = { "Enemy", "Player", "Bullet" };
            int[] idx = { 7, 8, 9 };
            for (int i = 0; i < want.Length; i++)
            {
                var p = layers.GetArrayElementAtIndex(idx[i]);
                if (string.IsNullOrEmpty(p.stringValue)) p.stringValue = want[i];
            }
            am.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
