using System.Linq;
using UnityEngine;

namespace AYNI.Battle
{
    public static class BattleRuntimeAssets
    {
        public static Sprite[] LoadClip(string resourceFolder)
        {
            if (string.IsNullOrEmpty(resourceFolder)) return System.Array.Empty<Sprite>();

            var textures = Resources.LoadAll<Texture2D>(resourceFolder);
            if (textures == null || textures.Length == 0) return System.Array.Empty<Sprite>();

            // Pivote en la línea de los pies. PivotFor ya devuelve el convenio
            // de Unity (0 = borde inferior): no pasarle una medida en crudo.
            float pivotY = SpriteMetrics.PivotFor(resourceFolder, 0f);

            return textures
                .OrderBy(tex => FrameNumber(tex.name))
                .ThenBy(tex => tex.name, System.StringComparer.Ordinal)
                .Select(tex => CreateSprite(tex, pivotY))
                .ToArray();
        }

        public static int FrameNumber(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            int end = name.Length - 1;
            int start = end;
            while (start >= 0 && char.IsDigit(name[start])) start--;
            if (start == end) return 0;
            return int.Parse(name.Substring(start + 1));
        }

        public static Sprite LoadBackground(string resourcePath)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            return tex != null ? CreateSprite(tex) : null;
        }

        public static AudioClip LoadAudio(string resourcePath)
        {
            return Resources.Load<AudioClip>(resourcePath);
        }

        static Sprite CreateSprite(Texture2D tex, float pivotY = 0f)
        {
            // FullRect: el sprite cubre todo el lienzo y la malla es un cuadro simple.
            // Con la malla Tight, Unity recorta el cuadro por el aire transparente y
            // algunas hojas (Juan de Oso) se dibujaban ~20 % más bajas.
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, pivotY), 100f, 0, SpriteMeshType.FullRect);
        }
    }
}