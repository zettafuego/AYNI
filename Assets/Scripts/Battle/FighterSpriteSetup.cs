using UnityEngine;

namespace AYNI.Battle
{
    public class FighterSpriteSetup : MonoBehaviour
    {
        void Awake()
        {
            var anim = GetComponent<SpriteFrameAnimator>();
            if (anim == null) return;

            var sheet = GetComponent<ToroEnemyAI>() != null ? BattleCast.Toro : BattleCast.Vizcacha;
            BattleSprites.Apply(anim, sheet);
        }
    }
}
