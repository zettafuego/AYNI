using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Marca una escena cuyo mundo ya está construido con GameObjects reales.
    /// Cuando existe, LagunaBattleManager no vuelve a generar el nivel en runtime:
    /// lo que ves en el editor es lo que se juega.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AyniBakedWorld : MonoBehaviour
    {
        public const string RootName = "__MUNDO_EDITABLE__";
    }
}
