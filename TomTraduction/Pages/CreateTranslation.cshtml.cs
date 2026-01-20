using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TomTraduction.Models;
using TomTraduction.Services;

namespace TomTraduction.Pages
{
    public class CreateTranslationModel : PageModel
    {
        private readonly ITranslationService _translationService;
        private readonly ILogger<CreateTranslationModel> _logger;

        [BindProperty]
        public CreateTranslationRequest Translation { get; set; } = new();

        public List<string> AvailableFiles { get; set; } = new();
        public bool? Success { get; set; }

        public CreateTranslationModel(ITranslationService translationService, ILogger<CreateTranslationModel> logger)
        {
            _translationService = translationService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadAvailableFiles();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadAvailableFiles();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var translation = new Translation
                {
                    Code = Translation.Code,
                    Francais = Translation.Francais,
                    Fichier = Translation.Fichier
                };

                translation.Anglais = Translation.Anglais;
                translation.Portugais = Translation.Portugais;

                Success = await _translationService.CreateTranslationAsync(translation);

                if (Success == true)
                {
                    _logger.LogInformation($"Nouvelle traduction créée : {translation.Code} dans {translation.Fichier}");
                    
                    // Optionnel : rediriger vers la page de traductions avec un filtre sur la nouvelle traduction
                    // return RedirectToPage("/Translations", new { Code = translation.Code });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la traduction");
                Success = false;
            }

            return Page();
        }

        private async Task LoadAvailableFiles()
        {
            try
            {
                AvailableFiles = await _translationService.GetAvailableFilesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des fichiers disponibles");
                AvailableFiles = new List<string>();
            }
        }
    }
}