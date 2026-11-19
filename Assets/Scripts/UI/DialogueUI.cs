using AYNI.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.UI
{
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text speakerText;
        [SerializeField] Text dialogueText;

        void Start()
        {
            if (NarratorSystem.Instance != null)
            {
                NarratorSystem.Instance.OnLineStarted += OnLineStarted;
                NarratorSystem.Instance.OnDialogueFinished += OnFinished;
            }
            panel?.SetActive(false);
        }

        void OnDestroy()
        {
            if (NarratorSystem.Instance == null) return;
            NarratorSystem.Instance.OnLineStarted -= OnLineStarted;
            NarratorSystem.Instance.OnDialogueFinished -= OnFinished;
        }

        void Update()
        {
            if (NarratorSystem.Instance == null || dialogueText == null) return;
            if (NarratorSystem.Instance.IsSpeaking)
                dialogueText.text = NarratorSystem.Instance.CurrentText;
        }

        void OnLineStarted(DialogueLine line)
        {
            panel?.SetActive(true);
            if (speakerText != null)
                speakerText.text = line.speaker;
        }

        void OnFinished()
        {
            if (MoralChoiceSystem.Instance != null && MoralChoiceSystem.Instance.IsChoosing)
                return;
            panel?.SetActive(false);
        }
    }
}