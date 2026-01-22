namespace TomTraduction.Models
{
    public class Translation
    {
        public string Code { get; set; } = string.Empty;
        public string Francais { get; set; } = string.Empty;
        public string Anglais { get; set; } = string.Empty;
        public string Portugais { get; set; } = string.Empty;
        public string Fichier { get; set; } = string.Empty;
        public string? CheminComplet { get; set; }
        public string CodeACopier 
        { 
            get 
            { 
                return $"Localization.Get(ResXEnum.{Fichier},\"{Code}\")"; 
            }
		}
	}

    public enum SearchType
    {
        Contains,
        BeginsWith,
        EndsWith,
        Equals
    }

    public class FieldFilter
    {
        public string? Value { get; set; }
        public SearchType SearchType { get; set; } = SearchType.Contains;
        public bool CaseSensitive { get; set; } = false;
    }

    public class TranslationFilter
    {
        public FieldFilter Code { get; set; } = new();
        public FieldFilter Francais { get; set; } = new();
        public FieldFilter Anglais { get; set; } = new();
        public FieldFilter Portugais { get; set; } = new();
        public FieldFilter Fichier { get; set; } = new();

        // Propriétés de compatibilité pour le binding simple
        public string? CodeValue
        {
            get => Code.Value;
            set => Code.Value = value;
        }
        
        public string? FrancaisValue
        {
            get => Francais.Value;
            set => Francais.Value = value;
        }
        
        public string? AnglaisValue
        {
            get => Anglais.Value;
            set => Anglais.Value = value;
        }
        
        public string? PortugaisValue
        {
            get => Portugais.Value;
            set => Portugais.Value = value;
        }
        
        public string? FichierValue
        {
            get => Fichier.Value;
            set => Fichier.Value = value;
        }

        public bool GlobalCaseSensitive { get; set; } = false;
        public SearchType GlobalSearchType { get; set; } = SearchType.Contains;
    }
}