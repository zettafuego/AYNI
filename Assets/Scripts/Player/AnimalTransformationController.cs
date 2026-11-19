using System;
using System.Collections.Generic;
using UnityEngine;

namespace AYNI.Player
{
    public class AnimalTransformationController : MonoBehaviour
    {
        [SerializeField] Renderer bodyRenderer;
        [SerializeField] AnimalForm currentForm = AnimalForm.Humano;

        readonly HashSet<AnimalForm> unlockedForms = new() { AnimalForm.Humano };

        public AnimalForm CurrentForm => currentForm;
        public event Action<AnimalForm> OnFormChanged;
        public event Action<AnimalForm> OnFormUnlocked;

        static readonly Dictionary<AnimalForm, Color> FormColors = new()
        {
            { AnimalForm.Humano, new Color(0.85f, 0.65f, 0.45f) },
            { AnimalForm.Zorro, new Color(0.9f, 0.45f, 0.15f) },
            { AnimalForm.Condor, new Color(0.2f, 0.25f, 0.35f) },
            { AnimalForm.Vizcacha, new Color(0.6f, 0.55f, 0.4f) }
        };

        void Awake()
        {
            if (bodyRenderer == null)
                bodyRenderer = GetComponentInChildren<Renderer>();
            ApplyVisuals();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryTransform(AnimalForm.Humano);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryTransform(AnimalForm.Zorro);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryTransform(AnimalForm.Condor);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TryTransform(AnimalForm.Vizcacha);
        }

        public bool IsUnlocked(AnimalForm form) => unlockedForms.Contains(form);

        public void UnlockForm(AnimalForm form)
        {
            if (unlockedForms.Add(form))
            {
                OnFormUnlocked?.Invoke(form);
                Narrative.NarratorSystem.Instance?.Say(
                    $"Has desbloqueado la forma del {AnimalFormData.GetDisplayName(form)}. " +
                    AnimalFormData.GetDescription(form));
            }
        }

        public void TryTransform(AnimalForm form)
        {
            if (!unlockedForms.Contains(form)) return;
            if (currentForm == form) return;

            currentForm = form;
            ApplyVisuals();
            OnFormChanged?.Invoke(form);
        }

        void ApplyVisuals()
        {
            if (bodyRenderer == null) return;

            var mat = bodyRenderer.material;
            mat.color = FormColors[currentForm];

            float scaleY = currentForm switch
            {
                AnimalForm.Condor => 0.8f,
                AnimalForm.Vizcacha => 0.6f,
                AnimalForm.Zorro => 0.9f,
                _ => 1f
            };
            transform.localScale = new Vector3(1f, scaleY, 1f);
        }

        public float GetSpeedMultiplier() => currentForm switch
        {
            AnimalForm.Zorro => 1.4f,
            AnimalForm.Vizcacha => 1.2f,
            AnimalForm.Condor => 1.0f,
            _ => 1.0f
        };

        public bool CanGlide() => currentForm == AnimalForm.Condor;
        public bool CanSneak() => currentForm == AnimalForm.Zorro;
        public bool CanBurrow() => currentForm == AnimalForm.Vizcacha;
    }
}