using System;
using AYNI.Core;
using AYNI.Player;
using UnityEngine;

namespace AYNI.Narrative
{
    public class MoralChoiceSystem : MonoBehaviour
    {
        public static MoralChoiceSystem Instance { get; private set; }

        public MoralChoice ActiveChoice { get; private set; }
        public bool IsChoosing { get; private set; }

        public event Action<MoralChoice> OnChoicePresented;
        public event Action<MoralChoiceOption> OnChoiceResolved;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Present(MoralChoice choice)
        {
            ActiveChoice = choice;
            IsChoosing = true;
            OnChoicePresented?.Invoke(choice);
        }

        public void Resolve(int optionIndex)
        {
            if (!IsChoosing || ActiveChoice == null) return;
            if (optionIndex < 0 || optionIndex >= ActiveChoice.options.Length) return;

            var option = ActiveChoice.options[optionIndex];
            var balance = AyniBalanceSystem.Instance;

            if (balance != null)
            {
                balance.Modify(AyniValue.Reciprocidad, option.reciprocidad);
                balance.Modify(AyniValue.Equilibrio, option.equilibrio);
                balance.Modify(AyniValue.RespetoPachamama, option.respetoPachamama);
            }

            if (option.unlocksForm.HasValue)
            {
                var transform = FindFirstObjectByType<AnimalTransformationController>();
                transform?.UnlockForm(option.unlocksForm.Value);
            }

            if (!string.IsNullOrEmpty(option.narratorReaction))
                NarratorSystem.Instance?.Say(option.narratorReaction);

            IsChoosing = false;
            OnChoiceResolved?.Invoke(option);
            ActiveChoice = null;
        }
    }
}