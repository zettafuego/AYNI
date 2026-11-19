using AYNI.Narrative;
using UnityEngine;

namespace AYNI.World
{
    public class ExplorationZone : MonoBehaviour
    {
        [SerializeField] string zoneName = "Zona sagrada";
        [SerializeField] string discoveryLine;
        [SerializeField] bool triggerOnce = true;

        bool triggered;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (triggerOnce && triggered) return;

            triggered = true;
            var narrator = NarratorSystem.Instance;
            if (narrator != null)
            {
                narrator.Say(string.IsNullOrEmpty(discoveryLine)
                    ? $"Has llegado a {zoneName}. Escucha lo que la tierra tiene que decirte."
                    : discoveryLine);
            }
        }
    }
}