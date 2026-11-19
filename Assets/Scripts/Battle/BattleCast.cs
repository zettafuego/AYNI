using System.Collections.Generic;
using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Who is in the laguna fight. Each clip is a folder of 00.png, 01.png, and so on.
    /// </summary>
    public sealed class CharacterSheet
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Role;
        public readonly string RootFolder;

        public CharacterSheet(string id, string name, string role, string rootFolder)
        {
            Id = id;
            Name = name;
            Role = role;
            RootFolder = rootFolder;
        }

        public string Clip(string clip) => string.IsNullOrEmpty(RootFolder) ? null : RootFolder + "/" + clip;

        public string WalkFolder => Clip("Caminar");

        public string ActionFolder => Clip("Ataque");

        public bool HasSprites => !string.IsNullOrEmpty(RootFolder);
    }

    public static class BattleCast
    {
        public const string Root = "Battle/Personajes";

        public static readonly CharacterSheet Vizcacha = new CharacterSheet(
            "vizcacha", "Vizcacha", "Cachorro", Root + "/Vizcacha");

        public static readonly CharacterSheet Toro = new CharacterSheet(
            "toro", "Toro", "Codicia", Root + "/Toro");

        public static readonly CharacterSheet Atoq = new CharacterSheet(
            "atoq", "Atoq", "Maestro", Root + "/Atoq");

        public static readonly CharacterSheet Brayan = new CharacterSheet(
            "brayan", "Brayan", "Cóndor", Root + "/Brayan");

        public static readonly CharacterSheet Cuy = new CharacterSheet(
            "cuy", "Cuy", "Narrador", Root + "/Cuy");

        public static readonly CharacterSheet Calavera = new CharacterSheet(
            "calavera", "Calavera", "Danzante", Root + "/Calavera");

        public static readonly CharacterSheet Conejo = new CharacterSheet(
            "conejo", "Conejo", "Maíz", Root + "/Conejo");

        public static readonly CharacterSheet GatoGuitarra = new CharacterSheet(
            "gatoguitarra", "Gato de guitarra", "Carnaval", Root + "/GatoGuitarra");

        public static readonly CharacterSheet GatoCaja = new CharacterSheet(
            "gatocaja", "Gato de caja", "Carnaval", Root + "/GatoCaja");

        public static readonly CharacterSheet GataPollera = new CharacterSheet(
            "gatapollera", "Gata de pollera", "Carnaval", Root + "/GataPollera");

        public static readonly CharacterSheet GatoBombo = new CharacterSheet(
            "gatobombo", "Gato de bombo", "Carnaval", Root + "/GatoBombo");

        /// <summary>Alumbra a la comparsa y convida un vaso de chicha. No se elige en el título.</summary>
        public static readonly CharacterSheet GatoChicha = new CharacterSheet(
            "gatochicha", "Gato de la chicha", "Carnaval", Root + "/GatoChicha");

        /// <summary>Lidera la comparsa de la plaza. No se elige en el título.</summary>
        public static readonly CharacterSheet ChanchoViolin = new CharacterSheet(
            "chanchoviolin", "Chanchito violinista", "Carnaval", Root + "/ChanchoViolin");

        public static readonly CharacterSheet CuyGuitarra = new CharacterSheet(
            "cuyguitarra", "Cuy de guitarra", "Rock", Root + "/CuyGuitarra");

        public static readonly CharacterSheet CuyBateria = new CharacterSheet(
            "cuybateria", "Cuy de batería", "Rock", Root + "/CuyBateria");

        public static readonly CharacterSheet CuyBajo = new CharacterSheet(
            "cuybajo", "Cuy de bajo", "Rock", Root + "/CuyBajo");

        public static readonly CharacterSheet MartinVera = new CharacterSheet(
            "martinvera", "Martín Vera", "Chicha", Root + "/MartinVera");

        public static readonly CharacterSheet CuyFan = new CharacterSheet(
            "cuyfan", "Fan de Eterno", "Pogo", Root + "/CuyFan");

        public static readonly CharacterSheet CuyFanRojo = new CharacterSheet(
            "cuyfanrojo", "Fan de polo rojo", "Pogo", Root + "/CuyFanRojo");

        public static readonly CharacterSheet CuyFanPunio = new CharacterSheet(
            "cuyfanpunio", "Fan de puño", "Pogo", Root + "/CuyFanPunio");

        public static readonly CharacterSheet Diablo = new CharacterSheet(
            "diablo", "Diablito", "El que señala", Root + "/Diablo");

        public static readonly CharacterSheet Campesina = new CharacterSheet(
            "campesina", "Campesina", "Guardián de la Tierra", Root + "/campesina");

        public static readonly CharacterSheet JuanDeOso = new CharacterSheet(
            "juandeoso", "Juan de Oso", "Fuerza del Oso", Root + "/Juan de Oso");

        /// <summary>Pide para la gasolina en la plaza. Usa el cuerpo de Brayan, teñido.</summary>
        public static readonly CharacterSheet Policia = new CharacterSheet(
            "policia", "Policía", "Gasolina", Root + "/Brayan");

        /// <summary>Raza del comedor. Pelaje corto, sin barba y sin túnica. No se eligen en el título.</summary>
        public static readonly CharacterSheet CuyMesero = new CharacterSheet(
            "cuymesero", "Cuy mesero", "Comedor", Root + "/CuyMesero");

        public static readonly CharacterSheet CuyCocinero = new CharacterSheet(
            "cuycocinero", "Cuy cocinero", "Comedor", Root + "/CuyCocinero");

        public static readonly CharacterSheet CuyPregon = new CharacterSheet(
            "cuypregon", "Cuy del pregón", "Comedor", Root + "/CuyPregon");

        /// <summary>Zetafuego, hecho gato. Canta con Cállate y Baila. No se elige en el título.</summary>
        public static readonly CharacterSheet GatoZeta = new CharacterSheet(
            "gatozeta", "Zetafuego", "Cállate y Baila", Root + "/GatoZeta");

        /// <summary>Raza de los ladrones. No se elige en el título.</summary>
        public static readonly CharacterSheet GatoLadron = new CharacterSheet(
            "gatoladron", "Ladrón", "Calle", Root + "/GatoLadron");

        public static readonly CharacterSheet AlmaOro = new CharacterSheet(
            "almaoro", "Alma de oro", "Alma", Root + "/AlmaOro");

        public static readonly CharacterSheet AlmaRosa = new CharacterSheet(
            "almarosa", "Alma rosa", "Alma", Root + "/AlmaRosa");

        public static readonly CharacterSheet AlmaAzul = new CharacterSheet(
            "almaazul", "Alma azul", "Alma", Root + "/AlmaAzul");

        public static readonly CharacterSheet[] All = {
            Vizcacha, Toro, Atoq, Brayan, Cuy, Calavera, Conejo, Campesina, JuanDeOso,
            AlmaOro, AlmaRosa, AlmaAzul
        };

        /// <summary>Comparsa de la plaza. Caminan juntos. No se eligen en el título.</summary>
        public static readonly CharacterSheet[] Comparsa =
        {
            ChanchoViolin, GatoChicha, GatoGuitarra, GatoCaja, GataPollera, GatoBombo,
        };

        /// <summary>Eterno, en el Batán cerrado. No caminan y no se eligen en el título.</summary>
        public static readonly CharacterSheet[] Concierto =
        {
            CuyGuitarra, CuyBateria, CuyBajo,
        };

        /// <summary>Pozo del Batán. Poguean en círculo y no se eligen en el título.</summary>
        public static readonly CharacterSheet[] Fanes =
        {
            CuyFan, CuyFanRojo, CuyFanPunio,
        };

        /// <summary>Gente que camina por el mundo. No se elige en el título.</summary>
        public static readonly CharacterSheet[] Vecinos =
        {
            Brayan, Cuy, Calavera, Campesina, JuanDeOso,
        };

        /// <summary>De estos se huye. Persiguen como en Pac-Man.</summary>
        public static readonly CharacterSheet[] Cazadores =
        {
            Toro, Diablo, AlmaOro, AlmaRosa, AlmaAzul,
        };

        /// <summary>Atienden el comedor de la cumbre. No caminan y no se eligen en el título.</summary>
        public static readonly CharacterSheet[] Puestos =
        {
            CuyCocinero, CuyMesero, CuyPregon,
        };

        /// <summary>Cállate y Baila. Un gato canta. No se elige en el título.</summary>
        public static readonly CharacterSheet[] Banda =
        {
            GatoZeta,
        };

        public static float BodyScale(CharacterSheet sheet)
        {
            if (sheet == Toro) return CastScale.Toro;
            if (sheet == Diablo) return CastScale.Diablo;
            return CastScale.Of(sheet);
        }
    }

    /// <summary>
    /// The playable cast shares one standing height.
    /// Their drawings are different pixel heights, so the scale is fitted
    /// from the Quieto canvas. The rival toro keeps his previous size.
    /// </summary>
    public static class CastScale
    {
        /// Quieto canvas height, in world units, for the playable bodies.
        public const float Stand = 6f;

        public static float Vizcacha => Of(BattleCast.Vizcacha);
        public static float Atoq => Of(BattleCast.Atoq);
        public static float Cuy => Of(BattleCast.Cuy);
        public static float Brayan => Of(BattleCast.Brayan);
        public static float Campesina => Of(BattleCast.Campesina);
        public static float JuanDeOso => Of(BattleCast.JuanDeOso);

        public const float Toro = 1.617f * 1.22f * (6f / 8.5f);
        public const float Diablo = 0.9f;

        static readonly Dictionary<string, float> native = new Dictionary<string, float>();

        public static float Of(CharacterSheet sheet)
        {
            float height = Native(sheet);
            return height > 0.05f ? Stand / height : 1f;
        }

        public static float Native(CharacterSheet sheet)
        {
            if (sheet == null || string.IsNullOrEmpty(sheet.Id)) return 1f;
            if (native.TryGetValue(sheet.Id, out float cached)) return cached;
            float height = Measure(sheet);
            if (height > 0.05f) native[sheet.Id] = height;
            return height > 0.05f ? height : 1f;
        }

        static float Measure(CharacterSheet sheet)
        {
            var frames = BattleRuntimeAssets.LoadClip(sheet.Clip("Quieto"));
            if (frames == null || frames.Length == 0)
                frames = BattleRuntimeAssets.LoadClip(sheet.WalkFolder);
            if (frames == null || frames.Length == 0 || frames[0] == null) return 0f;

            float height = frames[0].rect.height / frames[0].pixelsPerUnit;
            // El lienzo puede traer aire arriba y abajo: medimos el cuerpo visible.
            if (SpriteMetrics.TryGetSheet(sheet, out SpriteMetrics.Measure measure))
                height *= measure.HeightRatio;
            return height;
        }
    }
}
