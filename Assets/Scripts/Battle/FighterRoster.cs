namespace AYNI.Battle
{
    /// <summary>
    /// Who the player can take into the mountain.
    /// </summary>
    public static class FighterRoster
    {
        public readonly struct Option
        {
            public readonly CharacterSheet Sheet;
            public readonly bool FacesRight;
            public readonly string Label;

            public Option(CharacterSheet sheet, bool facesRight, string label)
            {
                Sheet = sheet;
                FacesRight = facesRight;
                Label = label;
            }

            /// The heroes share one standing height. The rival toro keeps his own.
            public float Scale => Sheet == BattleCast.Toro ? CastScale.Toro : CastScale.Of(Sheet);
        }

        /// <summary>La ficha se dibuja en una grilla de cuatro columnas.</summary>
        public const int RosterColumns = 4;

        public static readonly Option[] Choices =
        {
            new Option(BattleCast.Vizcacha, true, "VIZCACHA"),
            new Option(BattleCast.Atoq, true, "ATOQ"),
            new Option(BattleCast.Conejo, true, "CONEJO"),
            new Option(BattleCast.Brayan, true, "BRAYAN"),
            new Option(BattleCast.Cuy, true, "CUY"),
            new Option(BattleCast.Calavera, true, "CALAVERA"),
            new Option(BattleCast.Campesina, true, "CAMPESINA"),
            new Option(BattleCast.JuanDeOso, true, "JUAN DE OSO"),
        };

        public static int Index { get; private set; }

        public static Option Current
        {
            get
            {
                int i = Index;
                if (i < 0 || i >= Choices.Length) i = 0;
                return Choices[i];
            }
        }

        public static void Select(int index)
        {
            if (Choices.Length == 0) return;
            Index = ((index % Choices.Length) + Choices.Length) % Choices.Length;
        }

        public static Option Rival()
        {
            if (Current.Sheet == BattleCast.Toro)
                return new Option(BattleCast.Vizcacha, true, "VIZCACHA");
            return new Option(BattleCast.Toro, false, "TORO");
        }

        public readonly struct Ficha
        {
            public readonly string Nombre;
            public readonly string Oficio;
            public readonly string Cuerpo;
            public readonly string Hace;

            public Ficha(string nombre, string oficio, string cuerpo, string hace)
            {
                Nombre = nombre;
                Oficio = oficio;
                Cuerpo = cuerpo;
                Hace = hace;
            }
        }

        public static Ficha Card(Option option)
        {
            var sheet = option.Sheet;
            string hace = "Espacio salta. En el río nada. F golpea. A y D caminan.";
            if (sheet == BattleCast.Vizcacha)
                return new Ficha("VIZCACHA", "Cachorro", "Ágil. El 5 es el coletazo.", hace);
            if (sheet == BattleCast.Atoq)
                return new Ficha("ATOQ", "Maestro", "Astucia. Entra al monte y pega de cerca.", hace);
            if (sheet == BattleCast.Brayan)
                return new Ficha("BRAYAN", "Cóndor", "Visión. Mira el sendero desde arriba.", hace);
            if (sheet == BattleCast.Cuy)
                return new Ficha("CUY", "Narrador", "Sale del batán y cuenta el camino.", hace);
            if (sheet == BattleCast.Calavera)
                return new Ficha("CALAVERA", "Danzante", "Máscara, pollera y sandalias. Baila y pelea.", hace);
            if (sheet == BattleCast.Conejo)
                return new Ficha("CONEJO", "Maíz", "Aretes, un diente y una mazorca. Cruza el agua.", hace);
            if (sheet == BattleCast.Campesina)
                return new Ficha("CAMPESINA", "Guardián de la Tierra", "Manta, pollera y sombrero. Conoce el camino.", hace);
            if (sheet == BattleCast.JuanDeOso)
                return new Ficha("JUAN DE OSO", "Fuerza del Oso", "Peso y aguante. Abre paso en la montaña.", hace);
            if (sheet == BattleCast.AlmaOro)
                return new Ficha("ALMA DE ORO", "Alma", "Flota y baila. El cuerpo es el ciclo.", hace);
            if (sheet == BattleCast.AlmaRosa)
                return new Ficha("ALMA ROSA", "Alma", "Flota y baila. El cuerpo es el ciclo.", hace);
            if (sheet == BattleCast.AlmaAzul)
                return new Ficha("ALMA AZUL", "Alma", "Flota y baila. El cuerpo es el ciclo.", hace);
            return new Ficha(option.Label, sheet != null ? sheet.Role : "", "", hace);
        }
    }
}
