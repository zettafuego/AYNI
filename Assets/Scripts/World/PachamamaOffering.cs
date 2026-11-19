using AYNI.Core;
using AYNI.Narrative;
using UnityEngine;

namespace AYNI.World
{
    public class PachamamaOffering : MonoBehaviour
    {
        [SerializeField] string offeringName = "Ofrenda";
        [SerializeField] float reciprocidadReward = 10f;
        [SerializeField] float respetoReward = 15f;
        [SerializeField] bool consumedOnUse = true;

        bool used;

        void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player") || used) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                used = true;
                var balance = AyniBalanceSystem.Instance;
                balance?.Modify(AyniValue.Reciprocidad, reciprocidadReward);
                balance?.Modify(AyniValue.RespetoPachamama, respetoReward);

                NarratorSystem.Instance?.Say(
                    $"Ofrenda entregada a la Pachamama. El ayni se restaura, poquito a poquito.");

                if (consumedOnUse)
                    gameObject.SetActive(false);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}