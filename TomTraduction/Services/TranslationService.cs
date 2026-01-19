using System.Resources;
using System.Reflection;
using TomTraduction.Models;
using System.Globalization;
using System.Xml.Linq;

namespace TomTraduction.Services
{
    public interface ITranslationService
    {
        Task<List<Translation>> GetAllTranslationsAsync();
        Task<List<Translation>> SearchTranslationsAsync(TranslationFilter filter);
        List<Translation> FilterTranslations(List<Translation> translations, TranslationFilter filter);
    }

    public class TranslationService : ITranslationService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TranslationService> _logger;

        public TranslationService(IWebHostEnvironment environment, ILogger<TranslationService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<List<Translation>> SearchTranslationsAsync(TranslationFilter filter)
        {
            // Ne charger que si au moins un filtre est spécifié
            if (IsFilterEmpty(filter))
            {
                return new List<Translation>();
            }

            var allTranslations = await GetAllTranslationsAsync();
            return FilterTranslations(allTranslations, filter);
        }

        private bool IsFilterEmpty(TranslationFilter filter)
        {
            return string.IsNullOrWhiteSpace(filter.Code) &&
                   string.IsNullOrWhiteSpace(filter.Francais) &&
                   string.IsNullOrWhiteSpace(filter.Anglais) &&
                   string.IsNullOrWhiteSpace(filter.Portugais) &&
                   string.IsNullOrWhiteSpace(filter.Fichier);
        }

        public async Task<List<Translation>> GetAllTranslationsAsync()
        {
            var translations = new List<Translation>();
            var basePath = Path.Combine("C:\\Users\\jordantemp\\projet\\tomweb\\", "Shared", "Localization", "Tomate.Localization", "Resources");

			if (!Directory.Exists(basePath))
            {
                // Si le chemin spécifique n'existe pas, chercher dans le répertoire courant
                basePath = Path.Combine(_environment.ContentRootPath, "Resources");
                if (!Directory.Exists(basePath))
                {
                    _logger.LogWarning($"Répertoire de ressources non trouvé : {basePath}");
                    return translations;
                }
            }

            try
            {
                await Task.Run(() =>
                {
                    var resxFiles = Directory.GetFiles(basePath, "*.resx", SearchOption.AllDirectories);
                    
                    // Grouper les fichiers par nom de base (sans la culture)
                    var fileGroups = resxFiles
                        .GroupBy(f => GetBaseFileName(f))
                        .Where(g => g.Key != null);

                    foreach (var group in fileGroups)
                    {
                        var baseFile = group.Key!;
                        var groupFiles = group.ToList();

                        // Trouver les fichiers pour chaque langue
                        var frenchFile = groupFiles.FirstOrDefault(f => 
                            f.Contains("fr.resx"));
                        
                        var englishFile = groupFiles.FirstOrDefault(f => 
                            f.Contains("en.resx"));
                        
                        var portugueseFile = groupFiles.FirstOrDefault(f => 
                            f.Contains("pt.resx"));

                        if (frenchFile != null)
                        {
                            var frenchTranslations = ReadResxFile(frenchFile);
                            var englishTranslations = englishFile != null ? ReadResxFile(englishFile) : new Dictionary<string, string>();
                            var portugueseTranslations = portugueseFile != null ? ReadResxFile(portugueseFile) : new Dictionary<string, string>();

                            foreach (var kvp in frenchTranslations)
                            {
                                translations.Add(new Translation
                                {
                                    Code = kvp.Key,
                                    Francais = kvp.Value,
                                    Anglais = englishTranslations.GetValueOrDefault(kvp.Key, ""),
                                    Portugais = portugueseTranslations.GetValueOrDefault(kvp.Key, ""),
                                    Fichier = Path.GetFileNameWithoutExtension(baseFile)
                                });
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture des fichiers de traduction");
            }

            return translations.OrderBy(t => t.Fichier).ThenBy(t => t.Code).ToList();
        }

        public List<Translation> FilterTranslations(List<Translation> translations, TranslationFilter filter)
        {
            var query = translations.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Code))
            {
                query = query.Where(t => t.Code.Contains(filter.Code, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(filter.Francais))
            {
                query = query.Where(t => t.Francais.Contains(filter.Francais, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(filter.Anglais))
            {
                query = query.Where(t => t.Anglais.Contains(filter.Anglais, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(filter.Portugais))
            {
                query = query.Where(t => t.Portugais.Contains(filter.Portugais, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(filter.Fichier))
            {
                query = query.Where(t => t.Fichier.Contains(filter.Fichier, StringComparison.OrdinalIgnoreCase));
            }

            return query.ToList();
        }

        private string? GetBaseFileName(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName == null) return null;

            // Retirer les suffixes de culture (.fr, .en, .pt, etc.)
            var cultureSuffixes = new[] { "fr", "en", "pt" };
            
            foreach (var suffix in cultureSuffixes.OrderByDescending(s => s.Length))
            {
                if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    fileName = fileName.Substring(0, fileName.Length - suffix.Length);
                    break;
                }
            }

            return fileName;
        }

        private Dictionary<string, string> ReadResxFile(string filePath)
        {
            var result = new Dictionary<string, string>();
            
            try
            {
                var doc = XDocument.Load(filePath);
                var dataElements = doc.Descendants("data");
                
                foreach (var element in dataElements)
                {
                    var name = element.Attribute("name")?.Value;
                    var value = element.Element("value")?.Value;
                    
                    if (name != null && value != null)
                    {
                        result[name] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la lecture du fichier {filePath}");
            }
            
            return result;
        }
    }
}