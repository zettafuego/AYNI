using System.Collections.Generic;

namespace AYNI.Battle
{
    /// <summary>
    /// Algunos dibujos traen mucho lienzo transparente alrededor del cuerpo.
    /// El lienzo decide la escala y el pivote, así que esos cuerpos salen más
    /// bajos y con los pies flotando. Estas medidas recortan el aire: el cuerpo
    /// vuelve a medir lo mismo que el resto y los pies tocan el suelo.
    ///
    /// Las dos tablas guardan los pies medidos desde el borde superior del
    /// lienzo; PivotFor los convierte al pivote de Unity (desde abajo).
    /// </summary>
    public static class SpriteMetrics
    {
        public readonly struct Measure
        {
            /// <summary>
            /// Pies medidos desde el borde SUPERIOR del lienzo (1 = borde superior).
            /// Ojo: no es el pivote de Unity, que cuenta desde el borde inferior;
            /// SpriteMetrics.PivotFor invierte este valor antes de crear el sprite.
            /// </summary>
            public readonly float FeetFromTop;

            /// <summary>Cuerpo visible en reposo, en píxeles.</summary>
            public readonly float StandWidth;
            public readonly float StandHeight;

            public readonly float CanvasWidth;
            public readonly float CanvasHeight;

            public Measure(float feetFromTop, float standWidth, float standHeight,
                float canvasWidth, float canvasHeight)
            {
                FeetFromTop = feetFromTop;
                StandWidth = standWidth;
                StandHeight = standHeight;
                CanvasWidth = canvasWidth;
                CanvasHeight = canvasHeight;
            }

            /// <summary>Qué parte del lienzo ocupa el cuerpo visible.</summary>
            public float HeightRatio => CanvasHeight > 0f ? StandHeight / CanvasHeight : 1f;
            public float WidthRatio => CanvasWidth > 0f ? StandWidth / CanvasWidth : 1f;
        }

        static readonly Dictionary<string, Measure> byFolder = new Dictionary<string, Measure>
        {
            // Los ocho jugables: cuerpo visible en reposo y línea de los pies.
            // Regenerado con docs/herramientas/medir_lienzo.py (alfa >= 50 %).
            { "Vizcacha", new Measure(0.9861f, 272f, 349f, 286f, 361f) },
            { "Atoq", new Measure(0.9851f, 541f, 479f, 557f, 537f) },
            { "Conejo", new Measure(0.9790f, 430f, 731f, 1010f, 763f) },
            { "Brayan", new Measure(1.0000f, 564f, 700f, 564f, 700f) },
            { "Cuy", new Measure(1.0000f, 492f, 680f, 520f, 680f) },
            { "Calavera", new Measure(0.9846f, 322f, 1010f, 1003f, 1042f) },
            // campesina: lienzo 489x931, cuerpo 342x725.
            { "campesina", new Measure(0.9495f, 342f, 725f, 489f, 931f) },
            // Juan de Oso: lienzo 668x1489, cuerpo 488x989. El PNG trae motas
            // casi transparentes sobre la cabeza; contarlas lo encogía un 20 %.
            { "Juan de Oso", new Measure(0.9523f, 488f, 989f, 668f, 1489f) },
            // Gallina-taxi: lienzo 640x467, ruedas 10 px sobre el borde.
            { "Gallina", new Measure(0.9786f, 604f, 394f, 640f, 467f) },
        };

        /// <summary>
        /// Excepciones por clip, clave "Personaje/Clip" del recurso.
        /// Cada animación se dibuja en un lienzo distinto y con otro aire abajo:
        /// la campesina apoya a 47 px en Quieto, 49 en Caminar y 94 en Salto.
        /// Con un solo pivote por personaje los pies bailan entre clips.
        /// Mismo convenio que Measure: pies medidos desde el borde superior.
        /// Regenerado con docs/herramientas/pivotes_clips.py.
        /// </summary>
        static readonly Dictionary<string, float> byClip = new Dictionary<string, float>
        {
            { "AlmaAzul/Ataque", 0.9775f },  // gap 6 / 267 px
            { "AlmaAzul/Caida", 0.9775f },  // gap 6 / 267 px
            { "AlmaAzul/Caminar", 0.9775f },  // gap 6 / 267 px
            { "AlmaAzul/Especial", 0.9775f },  // gap 6 / 267 px
            { "AlmaAzul/Feliz", 0.9775f },  // gap 6 / 267 px
            { "AlmaAzul/Golpe", 0.9775f },  // gap 6 / 267 px
            { "AlmaAzul/Nadar", 0.9775f },  // gap 6 / 267 px
            { "AlmaAzul/Quieto", 0.9775f },  // gap 6 / 267 px
            { "AlmaAzul/Salto", 0.9775f },  // gap 6 / 267 px
            { "AlmaOro/Ataque", 0.9834f },  // gap 6 / 361 px
            { "AlmaOro/Caida", 0.9834f },  // gap 6 / 361 px
            { "AlmaOro/Caminar", 0.9834f },  // gap 6 / 361 px
            { "AlmaOro/Especial", 0.9834f },  // gap 6 / 361 px
            { "AlmaOro/Feliz", 0.9834f },  // gap 6 / 361 px
            { "AlmaOro/Golpe", 0.9834f },  // gap 6 / 361 px
            { "AlmaOro/Nadar", 0.9834f },  // gap 6 / 361 px
            { "AlmaOro/Quieto", 0.9834f },  // gap 6 / 361 px
            { "AlmaOro/Salto", 0.9834f },  // gap 6 / 361 px
            { "AlmaRosa/Ataque", 0.9780f },  // gap 6 / 273 px
            { "AlmaRosa/Caida", 0.9780f },  // gap 6 / 273 px
            { "AlmaRosa/Caminar", 0.9780f },  // gap 6 / 273 px
            { "AlmaRosa/Especial", 0.9780f },  // gap 6 / 273 px
            { "AlmaRosa/Feliz", 0.9780f },  // gap 6 / 273 px
            { "AlmaRosa/Golpe", 0.9780f },  // gap 6 / 273 px
            { "AlmaRosa/Nadar", 0.9780f },  // gap 6 / 273 px
            { "AlmaRosa/Quieto", 0.9780f },  // gap 6 / 273 px
            { "AlmaRosa/Salto", 0.9780f },  // gap 6 / 273 px
            { "Atoq/CaminarDetras", 0.9926f },  // gap 4 / 537 px
            { "Atoq/CaminarFrente", 0.9926f },  // gap 4 / 537 px
            { "Atoq/Feliz", 0.9904f },  // gap 7 / 727 px
            { "Brayan/CaminarDetras", 0.9943f },  // gap 4 / 700 px
            { "Brayan/CaminarFrente", 0.9943f },  // gap 4 / 700 px
            { "Brayan/Feliz", 0.9891f },  // gap 8 / 732 px
            { "Brayan/Nadar", 0.9750f },  // gap 8 / 320 px
            { "ChanchoViolin/Caminar", 0.9927f },  // gap 6 / 818 px
            { "ChanchoViolin/Quieto", 0.9927f },  // gap 6 / 818 px
            { "Cuy/Caminar", 0.9784f },  // gap 12 / 556 px
            { "Cuy/CaminarDetras", 0.9928f },  // gap 4 / 556 px
            { "Cuy/CaminarFrente", 0.9928f },  // gap 4 / 556 px
            { "Cuy/Feliz", 0.9904f },  // gap 7 / 730 px
            { "Cuy/Nadar", 0.9750f },  // gap 8 / 320 px
            { "CuyBajo/Caminar", 0.9920f },  // gap 4 / 497 px
            { "CuyBajo/Quieto", 0.9920f },  // gap 4 / 497 px
            { "CuyBateria/Caminar", 0.9920f },  // gap 4 / 497 px
            { "CuyBateria/Quieto", 0.9920f },  // gap 4 / 497 px
            { "CuyFan/Caminar", 0.9897f },  // gap 9 / 871 px
            { "CuyFan/Quieto", 0.9897f },  // gap 9 / 871 px
            { "CuyFanPunio/Caminar", 0.9900f },  // gap 8 / 799 px
            { "CuyFanPunio/Quieto", 0.9900f },  // gap 8 / 799 px
            { "CuyFanRojo/Caminar", 0.9909f },  // gap 8 / 876 px
            { "CuyFanRojo/Quieto", 0.9909f },  // gap 8 / 876 px
            { "CuyGuitarra/Caminar", 0.9920f },  // gap 4 / 497 px
            { "CuyGuitarra/Quieto", 0.9920f },  // gap 4 / 497 px
            { "Diablo/Caminar", 0.9918f },  // gap 4 / 486 px
            { "Diablo/Quieto", 0.9918f },  // gap 4 / 486 px
            { "Diablo/Senala", 0.9918f },  // gap 4 / 486 px
            { "GatoZeta/Caminar", 0.9965f },  // gap 4 / 1154 px
            { "Juan de Oso/Ataque", 0.9617f },  // gap 57 / 1489 px
            { "Juan de Oso/Caida", 1.0000f },  // gap 0 / 1489 px
            { "Juan de Oso/Feliz", 0.9328f },  // gap 100 / 1489 px
            { "Juan de Oso/Golpe", 0.9469f },  // gap 79 / 1489 px
            { "Juan de Oso/Nadar", 1.0000f },  // gap 0 / 1489 px
            { "Juan de Oso/Quieto", 0.9483f },  // gap 77 / 1489 px
            { "Juan de Oso/Salto", 0.9201f },  // gap 119 / 1489 px
            { "MartinVera/Caminar", 0.9962f },  // gap 4 / 1046 px
            { "MartinVera/Quieto", 0.9962f },  // gap 4 / 1046 px
            { "Mineros/Sentado", 0.9878f },  // gap 8 / 656 px
            { "Toro/Ataque", 0.9721f },  // gap 7 / 251 px
            { "Toro/Caida", 0.9817f },  // gap 6 / 327 px
            { "Toro/Caminar", 0.9448f },  // gap 26 / 471 px
            { "Toro/Correr", 0.9448f },  // gap 26 / 471 px
            { "Toro/Feliz", 0.9917f },  // gap 6 / 725 px
            { "Toro/Golpe", 0.9786f },  // gap 7 / 327 px
            { "Toro/Nadar", 0.9750f },  // gap 8 / 320 px
            { "Toro/Quieto", 0.8854f },  // gap 54 / 471 px
            { "Toro/Salto", 0.9855f },  // gap 6 / 414 px
            { "Vizcacha/Ataque", 0.9973f },  // gap 1 / 368 px
            { "Vizcacha/Caminar", 0.9674f },  // gap 12 / 368 px
            { "Vizcacha/Especial", 0.9683f },  // gap 17 / 536 px
            { "Vizcacha/Nadar", 0.9750f },  // gap 8 / 320 px
            { "Vizcacha/Patada", 0.9973f },  // gap 1 / 368 px
            { "campesina/Caida", 1.0000f },  // gap 0 / 931 px
            { "campesina/Feliz", 0.9248f },  // gap 70 / 931 px
            { "campesina/Golpe", 0.9538f },  // gap 43 / 931 px
            { "campesina/Nadar", 1.0000f },  // gap 0 / 931 px
            { "campesina/Salto", 0.8990f },  // gap 94 / 931 px
        };

        /// <summary>Carpeta del personaje dentro de una ruta de clip: ".../campesina/Quieto".</summary>
        public static string FolderOf(string resourceFolder)
        {
            if (string.IsNullOrEmpty(resourceFolder)) return null;
            int end = resourceFolder.LastIndexOf('/');
            if (end <= 0) return resourceFolder;
            int start = resourceFolder.LastIndexOf('/', end - 1);
            return resourceFolder.Substring(start + 1, end - start - 1);
        }

        /// <summary>Clave "Personaje/Clip": ".../campesina/Caminar" → "campesina/Caminar".</summary>
        public static string ClipOf(string resourceFolder)
        {
            string folder = FolderOf(resourceFolder);
            if (string.IsNullOrEmpty(folder)) return null;
            int end = resourceFolder.LastIndexOf('/');
            if (end < 0 || end == resourceFolder.Length - 1) return null;
            return folder + "/" + resourceFolder.Substring(end + 1);
        }

        public static bool TryGet(string resourceFolder, out Measure measure)
        {
            measure = default;
            string folder = FolderOf(resourceFolder);
            return !string.IsNullOrEmpty(folder) && byFolder.TryGetValue(folder, out measure);
        }

        public static bool TryGetSheet(CharacterSheet sheet, out Measure measure)
        {
            measure = default;
            return sheet != null && TryGet(sheet.WalkFolder, out measure);
        }

        /// <summary>
        /// Pivote vertical para un clip, en el convenio de Unity (0 = borde
        /// inferior del lienzo, 1 = borde superior).
        ///
        /// Las tablas guardan los pies medidos desde el borde superior, así que
        /// aquí se invierten: 47 px de aire abajo son pivote 0.0505, no 0.9495.
        /// Alimentar Sprite.Create con el valor sin invertir ancla el dibujo en
        /// la cabeza y hunde el cuerpo entero bajo el suelo.
        /// </summary>
        public static float PivotFor(string resourceFolder, float fallback)
        {
            string clip = ClipOf(resourceFolder);
            if (!string.IsNullOrEmpty(clip) && byClip.TryGetValue(clip, out float feetFromTop))
                return 1f - feetFromTop;

            return TryGet(resourceFolder, out Measure measure) ? 1f - measure.FeetFromTop : fallback;
        }
    }
}
