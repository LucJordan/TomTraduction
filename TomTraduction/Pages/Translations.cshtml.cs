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
            // Vérifier si au moins un filtre est spécifié
            HasSearched = !string.IsNullOrWhiteSpace(Filter.Code) ||
                         !string.IsNullOrWhiteSpace(Filter.Francais) ||
                         !string.IsNullOrWhiteSpace(Filter.Anglais) ||
                         !string.IsNullOrWhiteSpace(Filter.Portugais) ||
                         !string.IsNullOrWhiteSpace(Filter.Fichier);

            if (HasSearched)
            {
                Translations = await _translationService.SearchTranslationsAsync(Filter);
            }
            else
            {
                // Au premier chargement, liste vide
                Translations = new List<Translation>();
            }
        }
    }
}