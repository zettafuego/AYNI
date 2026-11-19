using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Marca el distrito de Cajamarca ya construido con GameObjects reales.
    /// Cuando existe, CajamarcaCalles usa esta copia en lugar de generar
    /// la ciudad desde código al abrir el capítulo.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AyniBakedCajamarca : MonoBehaviour
    {
        public const string RootName = "__CAJAMARCA_EDITABLE__";
        public const string DistrictName = "Distrito";
        public const string PlanName = "Plano";
    }
}
