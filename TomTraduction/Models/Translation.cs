namespace TomTraduction.Models
{
    public class Translation
    {
        public string Code { get; set; } = string.Empty;
        public string Francais { get; set; } = string.Empty;
        public string Anglais { get; set; } = string.Empty;
        public string Portugais { get; set; } = string.Empty;
        public string Fichier { get; set; } = string.Empty;
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