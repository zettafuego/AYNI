using AYNI.Battle;
using AYNI.Narrative;
using UnityEngine;

namespace AYNI.Editor
{
    public static class AyniCompile
    {
        public static void Check()
        {
            var calle = Shader.Find("AYNI/Calle");
            Debug.Log("[AYNI] CompileCheck OK ley=" + LeyDelMonte.BookTitle
                + " caps=" + LeyDelMonte.Chapters.Length
                + " cast=" + BattleCast.All.Length
                + " golpes=" + PeleaComandos.OptionCount
                + " vistas=" + CajamarcaCalles.VistaCount
                + " calle=" + (calle != null)
                + " bosque=" + (Shader.Find("AYNI/Bosque") != null)
                + " pino=" + (Resources.Load<GameObject>("Bosque/tree_pineTallA") != null)
                + " rio=" + (Shader.Find("AYNI/Rio") != null)
                + " nadar=" + BattleRuntimeAssets.LoadClip(BattleCast.Vizcacha.Clip("Nadar")).Length
                + " | " + Counts(BattleCast.Vizcacha)
                + " | " + Counts(BattleCast.Toro)
                + " | " + Counts(BattleCast.Atoq)
                + " | " + Counts(BattleCast.Brayan));
        }

        static string Counts(CharacterSheet sheet)
        {
            return sheet.Id
                + " Q" + BattleRuntimeAssets.LoadClip(sheet.Clip("Quieto")).Length
                + " C" + BattleRuntimeAssets.LoadClip(sheet.Clip("Caminar")).Length
                + " S" + BattleRuntimeAssets.LoadClip(sheet.Clip("Salto")).Length
                + " A" + BattleRuntimeAssets.LoadClip(sheet.Clip("Ataque")).Length
                + " G" + BattleRuntimeAssets.LoadClip(sheet.Clip("Golpe")).Length
                + " D" + BattleRuntimeAssets.LoadClip(sheet.Clip("Caida")).Length;
        }
    }
}
