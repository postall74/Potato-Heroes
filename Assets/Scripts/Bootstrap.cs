using UnityEngine;

namespace PotatoHeroes
{
    // Точка входа. Создаёт HUD раньше GameManager (HUD.I нужен ему в Start).
    public class Bootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoCreate()
        {
            var go = new GameObject("[GAME]");
            DontDestroyOnLoad(go);
            go.AddComponent<Bootstrap>();
        }

        void Awake()
        {
            if (FindAnyObjectByType<GameManager>() != null) { Destroy(gameObject); return; }
            gameObject.AddComponent<HUD>();          // сначала UI-синглтон
            gameObject.AddComponent<GameManager>();  // затем игра
        }
    }
}
