using UnityEngine;

namespace AYNI.Battle
{
    public static class BattleSprites
    {
        public static void Apply(SpriteFrameAnimator anim, CharacterSheet sheet)
        {
            if (anim == null || sheet == null || !sheet.HasSprites) return;

            var walk = BattleRuntimeAssets.LoadClip(sheet.WalkFolder);
            if (walk.Length == 0) return;

            anim.SetClips(
                Or(sheet, "Quieto", walk[0]),
                walk,
                BattleRuntimeAssets.LoadClip(sheet.Clip("Salto")),
                Or(sheet, "Ataque", null),
                BattleRuntimeAssets.LoadClip(sheet.Clip("Golpe")),
                BattleRuntimeAssets.LoadClip(sheet.Clip("Caida")));
            anim.SetAxialWalks(
                BattleRuntimeAssets.LoadClip(sheet.Clip("CaminarFrente")),
                BattleRuntimeAssets.LoadClip(sheet.Clip("CaminarDetras")));
            anim.SetSwim(BattleRuntimeAssets.LoadClip(sheet.Clip("Nadar")));
            anim.SetSpecial(BattleRuntimeAssets.LoadClip(sheet.Clip("Especial")));
            anim.SetEthereal(IsAlma(sheet), Aura(sheet));
        }

        public static void WearDevil(SpriteFrameAnimator anim, bool pointing)
        {
            if (anim == null) return;
            var walk = BattleRuntimeAssets.LoadClip(BattleCast.Diablo.WalkFolder);
            var idle = BattleRuntimeAssets.LoadClip(BattleCast.Diablo.Clip(pointing ? "Senala" : "Quieto"));
            if (walk.Length == 0) return;
            var still = idle.Length > 0 ? idle : walk;
            anim.SetEthereal(false);
            anim.SetClips(still, walk, still, still, still, still);
            anim.SetAxialWalks(
                BattleRuntimeAssets.LoadClip(BattleCast.Diablo.Clip("CaminarFrente")),
                BattleRuntimeAssets.LoadClip(BattleCast.Diablo.Clip("CaminarDetras")));
        }

        /// <summary>Las almas flotan: aura propia y movimiento mas suave.</summary>
        public static bool IsAlma(CharacterSheet sheet)
        {
            return sheet == BattleCast.AlmaOro
                || sheet == BattleCast.AlmaRosa
                || sheet == BattleCast.AlmaAzul;
        }

        public static Color Aura(CharacterSheet sheet)
        {
            if (sheet == BattleCast.AlmaOro) return new Color(1f, 0.82f, 0.36f);
            if (sheet == BattleCast.AlmaRosa) return new Color(1f, 0.48f, 0.72f);
            if (sheet == BattleCast.AlmaAzul) return new Color(0.44f, 0.66f, 1f);
            return Color.white;
        }

        static Sprite[] Or(CharacterSheet sheet, string clip, Sprite fallback)
        {
            var frames = BattleRuntimeAssets.LoadClip(sheet.Clip(clip));
            if (frames.Length > 0) return frames;
            return fallback != null ? new[] { fallback } : null;
        }
    }
}
