using AYNI.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.UI
{
    public class ChoiceUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text promptText;
        [SerializeField] Text contextText;
        [SerializeField] Button[] optionButtons;
        [SerializeField] Text[] optionLabels;

        void Start()
        {
            if (MoralChoiceSystem.Instance != null)
                MoralChoiceSystem.Instance.OnChoicePresented += OnChoicePresented;
            panel?.SetActive(false);
        }

        void OnDestroy()
        {
            if (MoralChoiceSystem.Instance == null) return;
            MoralChoiceSystem.Instance.OnChoicePresented -= OnChoicePresented;
        }

        void OnChoicePresented(MoralChoice choice)
        {
            panel?.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (promptText != null) promptText.text = choice.prompt;
            if (contextText != null) contextText.text = choice.context;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool active = i < choice.options.Length;
                optionButtons[i].gameObject.SetActive(active);
                if (active)
                {
                    int idx = i;
                    optionLabels[i].text = choice.options[i].text;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => Select(idx));
                }
            }
        }

        void Select(int index)
        {
            MoralChoiceSystem.Instance?.Resolve(index);
            panel?.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}