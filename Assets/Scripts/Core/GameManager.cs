using AYNI.Narrative;
using AYNI.Player;
using AYNI.UI;
using UnityEngine;

namespace AYNI.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] bool pauseOnDialogue;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var narrator = NarratorSystem.Instance;
            if (narrator != null)
                narrator.OnDialogueFinished += OnDialogueFinished;
        }

        void OnDestroy()
        {
            if (NarratorSystem.Instance != null)
                NarratorSystem.Instance.OnDialogueFinished -= OnDialogueFinished;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }

            if (Input.GetKeyDown(KeyCode.F1))
                ShowHelp();
        }

        void OnDialogueFinished()
        {
            if (!pauseOnDialogue) return;
            // Hook para pausar gameplay durante narración si se desea
        }

        void ShowHelp()
        {
            NarratorSystem.Instance?.Say(
                "Controles: WASD mover | Espacio saltar | Shift+Zorro=sigilo | " +
                "Espacio+Cóndor=planear | 1-4 cambiar forma | E interactuar | ESC cursor");
        }
    }
}