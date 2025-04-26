using Athrna.Data;
using Athrna.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Athrna.Services
{
    public class TranslationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TranslationService> _logger;
        private readonly IWebHostEnvironment _environment;
        private static Dictionary<string, Dictionary<string, string>> _translations;

        public TranslationService(
            ApplicationDbContext context,
            ILogger<TranslationService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;

            // Initialize translations if they haven't been loaded yet
            if (_translations == null)
            {
                _translations = new Dictionary<string, Dictionary<string, string>>();
                LoadTranslations();
            }
        }

        /// <summary>
        /// Load translation files for all supported languages
        /// </summary>
        private void LoadTranslations()
        {
            try
            {
                var translationsPath = Path.Combine(_environment.WebRootPath, "translations");
                if (!Directory.Exists(translationsPath))
                {
                    Directory.CreateDirectory(translationsPath);
                    CreateDefaultTranslationFiles(translationsPath);
                }

                // Load all translation files
                foreach (var file in Directory.GetFiles(translationsPath, "*.json"))
                {
                    var languageCode = Path.GetFileNameWithoutExtension(file);
                    var translationJson = File.ReadAllText(file);

                    var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(
                        translationJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (translations != null)
                    {
                        _translations[languageCode] = translations;
                        _logger.LogInformation($"Loaded {translations.Count} translations for language: {languageCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading translation files");
            }
        }

        /// <summary>
        /// Create default translation files if they don't exist
        /// </summary>
        private void CreateDefaultTranslationFiles(string translationsPath)
        {
            // Create Arabic translations
            var arTranslations = new Dictionary<string, string>
            {
                { "Home", "الرئيسية" },
                { "Discover Saudi Arabia's Rich Heritage", "اكتشف التراث الغني للمملكة العربية السعودية" },
                { "Explore the historical wonders and cultural treasures of the Kingdom's most iconic cities",
                  "استكشف العجائب التاريخية والكنوز الثقافية لأشهر مدن المملكة" },
                { "Explore", "استكشف" },
                { "Login", "تسجيل الدخول" },
                { "Register", "التسجيل" },
                { "Search historical sites...", "البحث عن المواقع التاريخية..." },
                { "Explore Historical Sites", "استكشف المواقع التاريخية" },
                { "My Dashboard", "لوحة التحكم الخاصة بي" },
                { "My Bookmarks", "إشاراتي المرجعية" },
                { "My Ratings", "تقييماتي" },
                { "My Profile", "ملفي الشخصي" },
                { "Logout", "تسجيل الخروج" },
                { "Madinah", "المدينة المنورة" },
                { "Riyadh", "الرياض" },
                { "AlUla", "العلا" },
                { "About", "حول" },
                { "Historical Sites in", "المواقع التاريخية في" },
                { "Available Guides in", "المرشدون المتاحون في" },
                { "Interactive Map of", "خريطة تفاعلية لـ" },
                { "Opening Hours", "ساعات العمل" },
                { "Admission Fees", "رسوم الدخول" },
                { "Located in", "يقع في" },
                { "Nearby Sites", "المواقع القريبة" },
                { "Visitor Reviews", "تقييمات الزوار" },
                { "Submit Review", "إرسال تقييم" },
                { "Contact Guide", "تواصل مع المرشد" },
                { "Privacy Policy", "سياسة الخصوصية" },
                { "Contact Us", "اتصل بنا" },
                { "All rights reserved", "جميع الحقوق محفوظة" }
            };
            SaveTranslationFile("ar", arTranslations, translationsPath);

            // Create French translations
            var frTranslations = new Dictionary<string, string>
            {
                { "Home", "Accueil" },
                { "Discover Saudi Arabia's Rich Heritage", "Découvrez le riche patrimoine de l'Arabie Saoudite" },
                { "Explore the historical wonders and cultural treasures of the Kingdom's most iconic cities",
                  "Explorez les merveilles historiques et les trésors culturels des villes les plus emblématiques du Royaume" },
                { "Explore", "Explorer" },
                { "Login", "Connexion" },
                { "Register", "S'inscrire" },
                { "Search historical sites...", "Rechercher des sites historiques..." },
                { "Explore Historical Sites", "Explorer les sites historiques" },
                { "My Dashboard", "Mon tableau de bord" },
                { "My Bookmarks", "Mes favoris" },
                { "My Ratings", "Mes évaluations" },
                { "My Profile", "Mon profil" },
                { "Logout", "Déconnexion" },
                { "Madinah", "Médine" },
                { "Riyadh", "Riyad" },
                { "AlUla", "AlUla" },
                { "About", "À propos" },
                { "Historical Sites in", "Sites historiques à" },
                { "Available Guides in", "Guides disponibles à" },
                { "Interactive Map of", "Carte interactive de" },
                { "Opening Hours", "Heures d'ouverture" },
                { "Admission Fees", "Frais d'entrée" },
                { "Located in", "Situé à" },
                { "Nearby Sites", "Sites à proximité" },
                { "Visitor Reviews", "Avis des visiteurs" },
                { "Submit Review", "Soumettre un avis" },
                { "Contact Guide", "Contacter le guide" },
                { "Privacy Policy", "Politique de confidentialité" },
                { "Contact Us", "Contactez-nous" },
                { "All rights reserved", "Tous droits réservés" }
            };
            SaveTranslationFile("fr", frTranslations, translationsPath);

            // Create Spanish translations
            var esTranslations = new Dictionary<string, string>
            {
                { "Home", "Inicio" },
                { "Discover Saudi Arabia's Rich Heritage", "Descubra el rico patrimonio de Arabia Saudita" },
                { "Explore the historical wonders and cultural treasures of the Kingdom's most iconic cities",
                  "Explore las maravillas históricas y los tesoros culturales de las ciudades más emblemáticas del Reino" },
                { "Explore", "Explorar" },
                { "Login", "Iniciar sesión" },
                { "Register", "Registrarse" },
                { "Search historical sites...", "Buscar sitios históricos..." },
                { "Explore Historical Sites", "Explorar sitios históricos" },
                { "My Dashboard", "Mi panel" },
                { "My Bookmarks", "Mis marcadores" },
                { "My Ratings", "Mis calificaciones" },
                { "My Profile", "Mi perfil" },
                { "Logout", "Cerrar sesión" },
                { "Madinah", "Medina" },
                { "Riyadh", "Riad" },
                { "AlUla", "AlUla" },
                { "About", "Acerca de" },
                { "Historical Sites in", "Sitios históricos en" },
                { "Available Guides in", "Guías disponibles en" },
                { "Interactive Map of", "Mapa interactivo de" },
                { "Opening Hours", "Horario de apertura" },
                { "Admission Fees", "Precios de entrada" },
                { "Located in", "Ubicado en" },
                { "Nearby Sites", "Sitios cercanos" },
                { "Visitor Reviews", "Opiniones de visitantes" },
                { "Submit Review", "Enviar opinión" },
                { "Contact Guide", "Contactar guía" },
                { "Privacy Policy", "Política de privacidad" },
                { "Contact Us", "Contáctenos" },
                { "All rights reserved", "Todos los derechos reservados" }
            };
            SaveTranslationFile("es", esTranslations, translationsPath);
        }

        /// <summary>
        /// Save translation dictionary to a JSON file
        /// </summary>
        private void SaveTranslationFile(string languageCode, Dictionary<string, string> translations, string translationsPath)
        {
            var filePath = Path.Combine(translationsPath, $"{languageCode}.json");
            var json = JsonSerializer.Serialize(translations, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
            _logger.LogInformation($"Created translation file for {languageCode} with {translations.Count} entries");
        }

        /// <summary>
        /// Get translation for a key in the specified language
        /// </summary>
        public string Translate(string key, string languageCode = "en")
        {
            // Default to English (no translation needed)
            if (languageCode == "en" || string.IsNullOrEmpty(languageCode))
            {
                return key;
            }

            // Check if we have translations for this language
            if (_translations.TryGetValue(languageCode, out var languageTranslations))
            {
                // Check if we have a translation for this key
                if (languageTranslations.TryGetValue(key, out var translation))
                {
                    return translation;
                }
            }

            // Return the original key if no translation is found
            return key;
        }

        /// <summary>
        /// Get all available languages
        /// </summary>
        public async Task<List<Language>> GetAvailableLanguagesAsync()
        {
            return await _context.Language.ToListAsync();
        }

        /// <summary>
        /// Get current language from request cookies
        /// </summary>
        public string GetCurrentLanguage(HttpContext httpContext)
        {
            if (httpContext.Request.Cookies.TryGetValue("language", out var language))
            {
                return language;
            }

            return "en"; // Default to English
        }
    }
}