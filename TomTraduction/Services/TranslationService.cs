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
            return string.IsNullOrWhiteSpace(filter.Code.Value) &&
                   string.IsNullOrWhiteSpace(filter.Francais.Value) &&
                   string.IsNullOrWhiteSpace(filter.Anglais.Value) &&
                   string.IsNullOrWhiteSpace(filter.Portugais.Value) &&
                   string.IsNullOrWhiteSpace(filter.Fichier.Value);
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
                                    Fichier = Path.GetFileNameWithoutExtension(baseFile),
                                    CheminComplet = Path.GetDirectoryName(frenchFile)
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

            if (!string.IsNullOrEmpty(filter.Code.Value))
            {
                query = query.Where(t => MatchesFilter(t.Code, filter.Code));
            }

            if (!string.IsNullOrEmpty(filter.Francais.Value))
            {
                query = query.Where(t => MatchesFilter(t.Francais, filter.Francais));
            }

            if (!string.IsNullOrEmpty(filter.Anglais.Value))
            {
                query = query.Where(t => MatchesFilter(t.Anglais, filter.Anglais));
            }

            if (!string.IsNullOrEmpty(filter.Portugais.Value))
            {
                query = query.Where(t => MatchesFilter(t.Portugais, filter.Portugais));
            }

            if (!string.IsNullOrEmpty(filter.Fichier.Value))
            {
                query = query.Where(t => MatchesFilter(t.Fichier, filter.Fichier));
            }

            return query.ToList();
        }

        private bool MatchesFilter(string fieldValue, FieldFilter filter)
        {
            if (string.IsNullOrEmpty(filter.Value))
                return true;

            var comparison = filter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            return filter.SearchType switch
            {
                SearchType.Contains => fieldValue.Contains(filter.Value, comparison),
                SearchType.BeginsWith => fieldValue.StartsWith(filter.Value, comparison),
                SearchType.EndsWith => fieldValue.EndsWith(filter.Value, comparison),
                SearchType.Equals => fieldValue.Equals(filter.Value, comparison),
                _ => fieldValue.Contains(filter.Value, comparison)
            };
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
                Anglais = await GenerateBasicTranslationAsync(frenchText, "en"),
                Portugais = await GenerateBasicTranslationAsync(frenchText, "pt")
            };

            return translation;
        }

        private async Task<string> GenerateBasicTranslationAsync(string text, string targetLanguage)
        {
            await Task.Delay(1);
            
            return targetLanguage switch
            {
                "en" => $"[EN] {text}",
                "pt" => $"[PT] {text}",
                _ => text
            };
        }

        private async Task<bool> WriteToResxFileAsync(string basePath, string fileName, string culture, string key, string value)
        {
            string? filePath = await FindExistingFilePathAsync(basePath, fileName, culture);
            
            if (filePath == null)
            {
                filePath = Path.Combine(basePath, $"{fileName}{culture}.resx");
            }
            
            try
            {
                XDocument doc;
                
                if (File.Exists(filePath))
                {
                    doc = XDocument.Load(filePath);
                }
                else
                {
                    doc = CreateBaseResxDocument();
                    
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
                    throw new Exception("Ce code existe déjà");
                }
                else
                {
                    var dataElement = new XElement("data",
                        new XAttribute("name", key),
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

        private async Task<string?> FindExistingFilePathAsync(string basePath, string fileName, string culture)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(basePath))
                    return null;

                var searchPattern = $"{fileName}{culture}.resx";
                var foundFiles = Directory.GetFiles(basePath, searchPattern, SearchOption.AllDirectories);
                
                return foundFiles.FirstOrDefault();
            });
        }

        private async Task<bool> RemoveFromResxFileAsync(string basePath, string fileName, string culture, string key)
        {
            var filePath = Path.Combine(basePath, $"{fileName}{culture}.resx");
            
            if (!File.Exists(filePath))
            {
                return true;
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