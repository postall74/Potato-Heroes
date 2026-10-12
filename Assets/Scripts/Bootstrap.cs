using UnityEngine;

namespace PotatoHeroes
{
    // Точка входа. Создаёт HUD раньше GameManager (HUD.I нужен ему в Start).
    public class Bootstrap : MonoBehaviour
    {
        void Awake()
        {
            if (FindAnyObjectByType<GameManager>() != null) { Destroy(gameObject); return; }
            gameObject.name = "[GAME]";
            gameObject.AddComponent<HUD>();          // сначала UI-синглтон
            gameObject.AddComponent<GameManager>();  // затем игра
        }

    }
}
