using System.Resources;
using System.Reflection;
using TomTraduction.Models;
using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using System.Text;

namespace TomTraduction.Services
{
    public interface ITranslationService
    {
        Task<List<Translation>> GetAllTranslationsAsync();
        Task<List<Translation>> SearchTranslationsAsync(TranslationFilter filter);
        List<Translation> FilterTranslations(List<Translation> translations, TranslationFilter filter);
        Task<List<string>> GetAvailableFilesAsync();
        Task<bool> CreateTranslationAsync(Translation translation);
        Task<bool> UpdateTranslationAsync(Translation translation);
        Task<bool> DeleteTranslationAsync(string code, string fileName);
        Task<Translation> GenerateTranslationAsync(string code, string frenchText, string fileName);
    }

    public class TranslationService : ITranslationService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TranslationService> _logger;
        private readonly TranslationOptions _translationOptions;

        public TranslationService(
            IWebHostEnvironment environment, 
            ILogger<TranslationService> logger,
            IOptions<TranslationOptions> translationOptions)
        {
            _environment = environment;
            _logger = logger;
            _translationOptions = translationOptions.Value;
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
            var basePath = GetBasePath();

            if (!Directory.Exists(basePath))
            {
                _logger.LogWarning($"Répertoire de ressources non trouvé : {basePath}");
                return translations;
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

        public async Task<List<string>> GetAvailableFilesAsync()
        {
            var basePath = GetBasePath();
            var files = new HashSet<string>();

            if (Directory.Exists(basePath))
            {
                await Task.Run(() =>
                {
                    var resxFiles = Directory.GetFiles(basePath, "*.resx", SearchOption.AllDirectories);
                    
                    foreach (var file in resxFiles)
                    {
                        var baseFileName = GetBaseFileName(file);
                        if (!string.IsNullOrEmpty(baseFileName))
                        {
                            files.Add(baseFileName);
                        }
                    }
                });
            }

            return files.OrderBy(f => f).ToList();
        }

        public async Task<bool> CreateTranslationAsync(Translation translation)
        {
            try
            {
                var basePath = GetBasePath();
                
                // Créer les fichiers pour chaque langue
                var success = true;
                
                if (!string.IsNullOrEmpty(translation.Francais))
                {
                    success &= await WriteToResxFileAsync(basePath, translation.Fichier, "fr", translation.Code, translation.Francais);
                }
                
                if (!string.IsNullOrEmpty(translation.Anglais))
                {
                    success &= await WriteToResxFileAsync(basePath, translation.Fichier, "en", translation.Code, translation.Anglais);
                }
                
                if (!string.IsNullOrEmpty(translation.Portugais))
                {
                    success &= await WriteToResxFileAsync(basePath, translation.Fichier, "pt", translation.Code, translation.Portugais);
                }

                if (success)
                {
                    _logger.LogInformation($"Traduction créée avec succès : {translation.Code} dans {translation.Fichier}");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la création de la traduction : {translation.Code}");
                return false;
            }
        }

        public async Task<bool> UpdateTranslationAsync(Translation translation)
        {
            // Même logique que CreateTranslationAsync - les fichiers RESX peuvent être mis à jour de la même manière
            return await CreateTranslationAsync(translation);
        }

        public async Task<bool> DeleteTranslationAsync(string code, string fileName)
        {
            try
            {
                var basePath = GetBasePath();
                var success = true;
                
                var cultures = new[] { "fr", "en", "pt" };
                
                foreach (var culture in cultures)
                {
                    success &= await RemoveFromResxFileAsync(basePath, fileName, culture, code);
                }

                if (success)
                {
                    _logger.LogInformation($"Traduction supprimée avec succès : {code} dans {fileName}");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression de la traduction : {code}");
                return false;
            }
        }

        public async Task<Translation> GenerateTranslationAsync(string code, string frenchText, string fileName)
        {
            var translation = new Translation
            {
                Code = code,
                Francais = frenchText,
                Fichier = fileName,
                // Générations basiques - à améliorer avec un service de traduction
                Anglais = await GenerateBasicTranslationAsync(frenchText, "en"),
                Portugais = await GenerateBasicTranslationAsync(frenchText, "pt")
            };

            return translation;
        }

        private async Task<string> GenerateBasicTranslationAsync(string text, string targetLanguage)
        {
            // Génération basique - vous pouvez intégrer un service de traduction ici
            await Task.Delay(1); // Simulation async
            
            return targetLanguage switch
            {
                "en" => $"[EN] {text}",  // Placeholder - remplacez par un vrai service de traduction
                "pt" => $"[PT] {text}",  // Placeholder - remplacez par un vrai service de traduction
                _ => text
            };
        }

        private async Task<bool> WriteToResxFileAsync(string basePath, string fileName, string culture, string key, string value)
        {
            var filePath = Path.Combine(basePath, $"{fileName}.{culture}.resx");
            
            try
            {
                XDocument doc;
                
                if (File.Exists(filePath))
                {
                    doc = XDocument.Load(filePath);
                }
                else
                {
                    // Créer la structure de base du fichier RESX
                    doc = CreateBaseResxDocument();
                    
                    // Créer le répertoire si nécessaire
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }

                var root = doc.Root!;
                var existingData = root.Elements("data").FirstOrDefault(x => x.Attribute("name")?.Value == key);
                
                if (existingData != null)
                {
                    // Mettre à jour la valeur existante
                    var valueElement = existingData.Element("value");
                    if (valueElement != null)
                    {
                        valueElement.Value = value;
                    }
                }
                else
                {
                    // Ajouter une nouvelle entrée
                    var dataElement = new XElement("data",
                        new XAttribute("name", key),
                        new XAttribute("xml:space", "preserve"),
                        new XElement("value", value)
                    );
                    
                    root.Add(dataElement);
                }

                await Task.Run(() => doc.Save(filePath));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'écriture dans le fichier {filePath}");
                return false;
            }
        }

        private async Task<bool> RemoveFromResxFileAsync(string basePath, string fileName, string culture, string key)
        {
            var filePath = Path.Combine(basePath, $"{fileName}.{culture}.resx");
            
            if (!File.Exists(filePath))
            {
                return true; // Le fichier n'existe pas, donc la clé n'existe pas non plus
            }

            try
            {
                var doc = XDocument.Load(filePath);
                var root = doc.Root!;
                var existingData = root.Elements("data").FirstOrDefault(x => x.Attribute("name")?.Value == key);
                
                if (existingData != null)
                {
                    existingData.Remove();
                    await Task.Run(() => doc.Save(filePath));
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression dans le fichier {filePath}");
                return false;
            }
        }

        private string GetBasePath()
        {
            var basePath = Path.Combine(_translationOptions.ResourcesBasePath, "Shared", "Localization", "Tomate.Localization", "Resources");

            if (!Directory.Exists(basePath))
            {
                basePath = Path.Combine(_environment.ContentRootPath, "Resources");
            }

            return basePath;
        }

        private XDocument CreateBaseResxDocument()
        {
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("root",
                    new XElement("xsd:schema",
                        new XAttribute("id", "root"),
                        new XAttribute("targetNamespace", ""),
                        new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                        new XAttribute(XNamespace.Xmlns + "msdata", "urn:schemas-microsoft-com:xml-msdata"),
                        new XElement(XNamespace.Get("http://www.w3.org/2001/XMLSchema") + "import",
                            new XAttribute("namespace", "http://www.w3.org/XML/1998/namespace")),
                        new XElement(XNamespace.Get("http://www.w3.org/2001/XMLSchema") + "element",
                            new XAttribute("name", "root"),
                            new XAttribute("msdata:IsDataSet", "true"))
                    ),
                    new XElement("resheader",
                        new XAttribute("name", "resmimetype"),
                        new XElement("value", "text/microsoft-resx")),
                    new XElement("resheader",
                        new XAttribute("name", "version"),
                        new XElement("value", "2.0")),
                    new XElement("resheader",
                        new XAttribute("name", "reader"),
                        new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")),
                    new XElement("resheader",
                        new XAttribute("name", "writer"),
                        new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"))
                )
            );
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