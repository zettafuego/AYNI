using AYNI.Core;
using AYNI.Player;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.UI
{
    public class AyniHUD : MonoBehaviour
    {
        [SerializeField] Slider reciprocidadBar;
        [SerializeField] Slider equilibrioBar;
        [SerializeField] Slider pachamamaBar;
        [SerializeField] Text formText;
        [SerializeField] Text stateText;
        [SerializeField] Text controlsText;

        AnimalTransformationController transformation;

        void Start()
        {
            transformation = FindFirstObjectByType<AnimalTransformationController>();

            if (AyniBalanceSystem.Instance != null)
            {
                AyniBalanceSystem.Instance.OnBalanceChanged += UpdateBars;
                UpdateBars(AyniBalanceSystem.Instance.GetSnapshot());
            }

            if (transformation != null)
                transformation.OnFormChanged += OnFormChanged;

            if (controlsText != null)
                controlsText.text = "WASD Mover | E Ofrenda | 1-4 Formas | F1 Ayuda";
        }

        void OnDestroy()
        {
            if (AyniBalanceSystem.Instance != null)
                AyniBalanceSystem.Instance.OnBalanceChanged -= UpdateBars;
            if (transformation != null)
                transformation.OnFormChanged -= OnFormChanged;
        }

        void Update()
        {
            if (stateText != null && AyniBalanceSystem.Instance != null)
                stateText.text = AyniBalanceSystem.Instance.GetSpiritualState();
        }

        void UpdateBars(AyniSnapshot snap)
        {
            if (reciprocidadBar != null) reciprocidadBar.value = snap.reciprocidad / 100f;
            if (equilibrioBar != null) equilibrioBar.value = snap.equilibrio / 100f;
            if (pachamamaBar != null) pachamamaBar.value = snap.respetoPachamama / 100f;
        }

        void OnFormChanged(AnimalForm form)
        {
            if (formText != null)
                formText.text = $"{AnimalFormData.GetDisplayName(form)} — {AnimalFormData.GetVirtue(form)}";
        }
    }
}