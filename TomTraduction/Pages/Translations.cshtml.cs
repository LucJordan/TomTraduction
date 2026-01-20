using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TomTraduction.Models;
using TomTraduction.Services;

namespace TomTraduction.Pages
{
    public class TranslationsModel : PageModel
    {
        private readonly ITranslationService _translationService;

        public TranslationsModel(ITranslationService translationService)
        {
            _translationService = translationService;
        }

        public List<Translation> Translations { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public TranslationFilter Filter { get; set; } = new();

        public bool HasSearched { get; set; }

        public async Task OnGetAsync()
        {
            // Appliquer les paramètres globaux aux filtres individuels
            ApplyGlobalSettings();

            // Vérifier si au moins un filtre est spécifié
            HasSearched = !string.IsNullOrWhiteSpace(Filter.Code.Value) ||
                         !string.IsNullOrWhiteSpace(Filter.Francais.Value) ||
                         !string.IsNullOrWhiteSpace(Filter.Anglais.Value) ||
                         !string.IsNullOrWhiteSpace(Filter.Portugais.Value) ||
                         !string.IsNullOrWhiteSpace(Filter.Fichier.Value);

            if (HasSearched)
            {
                Translations = await _translationService.SearchTranslationsAsync(Filter);
            }
            else
            {
                Translations = new List<Translation>();
            }
        }

        private void ApplyGlobalSettings()
        {
            // Appliquer les paramètres globaux aux champs qui n'ont pas de paramètres spécifiques
            if (Filter.GlobalCaseSensitive)
            {
                Filter.Code.CaseSensitive = true;
                Filter.Francais.CaseSensitive = true;
                Filter.Anglais.CaseSensitive = true;
                Filter.Portugais.CaseSensitive = true;
                Filter.Fichier.CaseSensitive = true;
            }

            if (Filter.GlobalSearchType != SearchType.Contains)
            {
                Filter.Code.SearchType = Filter.GlobalSearchType;
                Filter.Francais.SearchType = Filter.GlobalSearchType;
                Filter.Anglais.SearchType = Filter.GlobalSearchType;
                Filter.Portugais.SearchType = Filter.GlobalSearchType;
                Filter.Fichier.SearchType = Filter.GlobalSearchType;
            }
        }
    }
}