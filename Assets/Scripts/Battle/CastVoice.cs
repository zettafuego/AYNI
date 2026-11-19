using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// One short cry per sheet. Toro keeps the recorded call.
    /// The others are synthesized clips under Battle/Audio.
    /// </summary>
    public static class CastVoice
    {
        public static AudioClip For(CharacterSheet sheet)
        {
            if (sheet == null) return null;
            if (sheet == BattleCast.Toro)
            {
                var recorded = BattleRuntimeAssets.LoadAudio("Battle/Audio/toro");
                if (recorded != null) return recorded;
            }

            return BattleRuntimeAssets.LoadAudio("Battle/Audio/" + sheet.Id);
        }
    }
}
