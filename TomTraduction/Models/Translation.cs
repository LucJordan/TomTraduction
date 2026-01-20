namespace TomTraduction.Models
{
    public class Translation
    {
        public string Code { get; set; } = "";
        public string Francais { get; set; } = "";
        public string Anglais { get; set; } = "";
        public string Portugais { get; set; } = "";
        public string Fichier { get; set; } = "";
        public string? CheminComplet { get; set; } // Nouveau : stocker le chemin complet
    }

    public class TranslationFilter
    {
        public string? Code { get; set; }
        public string? Francais { get; set; }
        public string? Anglais { get; set; }
        public string? Portugais { get; set; }
        public string? Fichier { get; set; }
    }
}