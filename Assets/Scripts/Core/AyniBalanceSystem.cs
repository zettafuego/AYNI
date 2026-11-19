using System;
using UnityEngine;

namespace AYNI.Core
{
    public enum AyniValue
    {
        Reciprocidad,
        Equilibrio,
        RespetoPachamama
    }

    [Serializable]
    public class AyniSnapshot
    {
        public float reciprocidad;
        public float equilibrio;
        public float respetoPachamama;
    }

    public class AyniBalanceSystem : MonoBehaviour
    {
        public static AyniBalanceSystem Instance { get; private set; }

        [SerializeField] float reciprocidad = 50f;
        [SerializeField] float equilibrio = 50f;
        [SerializeField] float respetoPachamama = 50f;

        public event Action<AyniSnapshot> OnBalanceChanged;

        public float Reciprocidad => reciprocidad;
        public float Equilibrio => equilibrio;
        public float RespetoPachamama => respetoPachamama;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Modify(AyniValue value, float delta)
        {
            switch (value)
            {
                case AyniValue.Reciprocidad:
                    reciprocidad = Mathf.Clamp(reciprocidad + delta, 0f, 100f);
                    break;
                case AyniValue.Equilibrio:
                    equilibrio = Mathf.Clamp(equilibrio + delta, 0f, 100f);
                    break;
                case AyniValue.RespetoPachamama:
                    respetoPachamama = Mathf.Clamp(respetoPachamama + delta, 0f, 100f);
                    break;
            }

            OnBalanceChanged?.Invoke(GetSnapshot());
        }

        public AyniSnapshot GetSnapshot() => new AyniSnapshot
        {
            reciprocidad = reciprocidad,
            equilibrio = equilibrio,
            respetoPachamama = respetoPachamama
        };

        public string GetSpiritualState()
        {
            float avg = (reciprocidad + equilibrio + respetoPachamama) / 3f;
            if (avg >= 75f) return "La Pachamama sonríe contigo";
            if (avg >= 50f) return "El ayni fluye, pero hay trabajo por hacer";
            if (avg >= 25f) return "El equilibrio tiembla como un puente de quena";
            return "Los apus guardan silencio...";
        }
    }
}