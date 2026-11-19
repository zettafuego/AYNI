using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Marca la arboleda del capítulo del bosque ya construida con GameObjects reales.
    /// Cuando existe, BosqueCapitulo usa esta copia en lugar de generar
    /// el bosque desde código al abrir el capítulo.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AyniBakedBosque : MonoBehaviour
    {
        public const string RootName = "__BOSQUE_EDITABLE__";
        public const string GroveName = "Arboleda";
    }
}
