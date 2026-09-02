using UnityEngine;

namespace PokeIdle
{
    public sealed class PokeIdleApp : MonoBehaviour
    {
        private void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 30;

            // Mantém cenas antigas jogáveis enquanto o bootstrap de editor é atualizado.
            if (GetComponent<BattleArenaView>() == null)
            {
                gameObject.AddComponent<BattleArenaView>();
            }
        }
    }
}
