namespace TomTraduction.Models
{
    public class CreateTranslationRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Francais { get; set; } = string.Empty;
        public string Anglais { get; set; } = string.Empty;
        public string Portugais { get; set; } = string.Empty;
        public string Fichier { get; set; } = string.Empty;
        public bool AutoGenerate { get; set; } = true;
    }
}