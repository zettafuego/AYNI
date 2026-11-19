namespace AYNI.Player
{
    public enum AnimalForm
    {
        Humano,
        Zorro,
        Condor,
        Vizcacha
    }

    public static class AnimalFormData
    {
        public static string GetDisplayName(AnimalForm form) => form switch
        {
            AnimalForm.Humano => "Joven",
            AnimalForm.Zorro => "Zorro",
            AnimalForm.Condor => "Cóndor",
            AnimalForm.Vizcacha => "Vizcacha",
            _ => form.ToString()
        };

        public static string GetVirtue(AnimalForm form) => form switch
        {
            AnimalForm.Zorro => "Astucia",
            AnimalForm.Condor => "Visión espiritual",
            AnimalForm.Vizcacha => "Agilidad y adaptación",
            _ => "Equilibrio"
        };

        public static string GetDescription(AnimalForm form) => form switch
        {
            AnimalForm.Zorro => "El zorro enseña a observar antes de actuar. Con astucia, encuentras caminos ocultos.",
            AnimalForm.Condor => "El cóndor eleva la mirada. Desde lo alto, ves lo que otros no ven.",
            AnimalForm.Vizcacha => "La vizcacha sobrevive adaptándose. Donde otros no caben, tú pasas.",
            _ => "Aún eres humano. Rompiste el equilibrio, pero puedes restaurarlo."
        };
    }
}