using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Marca el distrito de Cajamarca Real horneado con la topografía fidedigna,
    /// Cerro Santa Apolonia en su ubicación real, mirador, escalinatas y la paleta
    /// de iluminación violeta con cielo azul andino.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AyniBakedCajamarcaReal : MonoBehaviour
    {
        public const string RootName = "__CAJAMARCA_REAL_EDITABLE__";
        public const string DistrictName = "Distrito";
        public const string PlanName = "Plano";
        public const string TerrainName = "TerrenoReal";
        public const string SantaApoloniaName = "SantaApolonia";
        public const string StairsName = "Escalinatas";
        public const string MiradorName = "Mirador";
        public const string CapillaName = "Capilla";
    }
}
