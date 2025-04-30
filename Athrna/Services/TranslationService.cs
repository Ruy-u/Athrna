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
                { "All rights reserved", "جميع الحقوق محفوظة" },
                // Site details translations
                { "About", "حول" },
                { "Cultural Information", "المعلومات الثقافية" },
                { "Available Services", "الخدمات المتاحة" },
                { "Available Guides", "المرشدون المتاحون" },
                { "Visitor Reviews", "تقييمات الزوار" },
                { "Parking", "موقف سيارات" },
                { "Free parking available for visitors", "موقف سيارات مجاني متاح للزوار" },
                { "Information Center", "مركز المعلومات" },
                { "Tourist information and guides", "معلومات سياحية ومرشدين" },
                { "Café", "مقهى" },
                { "Traditional Saudi coffee and refreshments", "قهوة سعودية تقليدية ومرطبات" },
                { "Gift Shop", "متجر هدايا" },
                { "Local crafts and souvenirs", "الحرف اليدوية المحلية والهدايا التذكارية" },
                { "Free Wi-Fi", "واي فاي مجاني" },
                { "Available throughout the site", "متاح في جميع أنحاء الموقع" },
                { "Accessibility", "إمكانية الوصول" },
                { "Wheelchair ramps and accessible facilities", "منحدرات للكراسي المتحركة ومرافق يمكن الوصول إليها" },
                // New site-specific translations for Madinah
                { "Prophet's Mosque (Al-Masjid an-Nabawi)", "المسجد النبوي" },
                { "The second holiest site in Islam, built by Prophet Muhammad in 622 CE. It contains the Prophet's tomb and has been expanded extensively throughout history.", "ثاني أقدس موقع في الإسلام، بناه النبي محمد صلى الله عليه وسلم عام 622 ميلادي. يحتوي على قبر النبي وتم توسيعه بشكل كبير على مر التاريخ." },
                { "Quba Mosque", "مسجد قباء" },
                { "The first mosque built in Islam. Prophet Muhammad laid its foundation when he arrived in Madinah after his migration from Mecca.", "أول مسجد بني في الإسلام. وضع النبي محمد صلى الله عليه وسلم أساسه عندما وصل إلى المدينة بعد هجرته من مكة." },
                { "Battlefield of Uhud", "ساحة معركة أحد" },
                { "Site of the Battle of Uhud in 625 CE, one of the most significant battles in Islamic history. Located at the foot of Mount Uhud.", "موقع غزوة أحد في عام 625 ميلادي، واحدة من أهم المعارك في التاريخ الإسلامي. تقع عند سفح جبل أحد." },
                { "Al-Baqi Cemetery", "مقبرة البقيع" },
                { "Ancient cemetery containing the graves of many of Prophet Muhammad's companions and family members.", "مقبرة قديمة تحتوي على قبور العديد من صحابة النبي محمد صلى الله عليه وسلم وأفراد عائلته." }
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
                { "All rights reserved", "Tous droits réservés" },
                // Site details translations
                { "About", "À propos de" },
                { "Cultural Information", "Informations culturelles" },
                { "Available Services", "Services disponibles" },
                { "Available Guides", "Guides disponibles" },
                { "Visitor Reviews", "Avis des visiteurs" },
                { "Parking", "Stationnement" },
                { "Free parking available for visitors", "Stationnement gratuit disponible pour les visiteurs" },
                { "Information Center", "Centre d'information" },
                { "Tourist information and guides", "Informations touristiques et guides" },
                { "Café", "Café" },
                { "Traditional Saudi coffee and refreshments", "Café saoudien traditionnel et rafraîchissements" },
                { "Gift Shop", "Boutique de souvenirs" },
                { "Local crafts and souvenirs", "Artisanat local et souvenirs" },
                { "Free Wi-Fi", "Wi-Fi gratuit" },
                { "Available throughout the site", "Disponible dans tout le site" },
                { "Accessibility", "Accessibilité" },
                { "Wheelchair ramps and accessible facilities", "Rampes pour fauteuils roulants et installations accessibles" },
                // New site-specific translations for Madinah
                { "Prophet's Mosque (Al-Masjid an-Nabawi)", "Mosquée du Prophète (Al-Masjid an-Nabawi)" },
                { "The second holiest site in Islam, built by Prophet Muhammad in 622 CE. It contains the Prophet's tomb and has been expanded extensively throughout history.", "Le deuxième site le plus saint de l'Islam, construit par le Prophète Muhammad en 622 EC. Il contient la tombe du Prophète et a été considérablement agrandi tout au long de l'histoire." },
                { "Quba Mosque", "Mosquée de Quba" },
                { "The first mosque built in Islam. Prophet Muhammad laid its foundation when he arrived in Madinah after his migration from Mecca.", "La première mosquée construite en Islam. Le Prophète Muhammad a posé sa fondation à son arrivée à Médine après sa migration de La Mecque." },
                { "Battlefield of Uhud", "Champ de bataille d'Uhud" },
                { "Site of the Battle of Uhud in 625 CE, one of the most significant battles in Islamic history. Located at the foot of Mount Uhud.", "Site de la bataille d'Uhud en 625 EC, l'une des batailles les plus importantes de l'histoire islamique. Situé au pied du mont Uhud." },
                { "Al-Baqi Cemetery", "Cimetière d'Al-Baqi" },
                { "Ancient cemetery containing the graves of many of Prophet Muhammad's companions and family members.", "Ancien cimetière contenant les tombes de nombreux compagnons et membres de la famille du Prophète Muhammad." }
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
                { "All rights reserved", "Todos los derechos reservados" },
                // Site details translations
                { "About", "Acerca de" },
                { "Cultural Information", "Información cultural" },
                { "Available Services", "Servicios disponibles" },
                { "Available Guides", "Guías disponibles" },
                { "Visitor Reviews", "Opiniones de visitantes" },
                { "Parking", "Estacionamiento" },
                { "Free parking available for visitors", "Estacionamiento gratuito disponible para visitantes" },
                { "Information Center", "Centro de información" },
                { "Tourist information and guides", "Información turística y guías" },
                { "Café", "Café" },
                { "Traditional Saudi coffee and refreshments", "Café tradicional saudí y refrescos" },
                { "Gift Shop", "Tienda de recuerdos" },
                { "Local crafts and souvenirs", "Artesanías locales y recuerdos" },
                { "Free Wi-Fi", "Wi-Fi gratuito" },
                { "Available throughout the site", "Disponible en todo el sitio" },
                { "Accessibility", "Accesibilidad" },
                { "Wheelchair ramps and accessible facilities", "Rampas para sillas de ruedas e instalaciones accesibles" },
                // New site-specific translations for Madinah
                { "Prophet's Mosque (Al-Masjid an-Nabawi)", "Mezquita del Profeta (Al-Masjid an-Nabawi)" },
                { "The second holiest site in Islam, built by Prophet Muhammad in 622 CE. It contains the Prophet's tomb and has been expanded extensively throughout history.", "El segundo sitio más sagrado del Islam, construido por el Profeta Muhammad en 622 EC. Contiene la tumba del Profeta y ha sido ampliado extensamente a lo largo de la historia." },
                { "Quba Mosque", "Mezquita de Quba" },
                { "The first mosque built in Islam. Prophet Muhammad laid its foundation when he arrived in Madinah after his migration from Mecca.", "La primera mezquita construida en el Islam. El Profeta Muhammad colocó sus cimientos cuando llegó a Medina después de su migración desde La Meca." },
                { "Battlefield of Uhud", "Campo de batalla de Uhud" },
                { "Site of the Battle of Uhud in 625 CE, one of the most significant battles in Islamic history. Located at the foot of Mount Uhud.", "Sitio de la Batalla de Uhud en 625 EC, una de las batallas más significativas en la historia islámica. Ubicado al pie del Monte Uhud." },
                { "Al-Baqi Cemetery", "Cementerio de Al-Baqi" },
                { "Ancient cemetery containing the graves of many of Prophet Muhammad's companions and family members.", "Antiguo cementerio que contiene las tumbas de muchos de los compañeros y familiares del Profeta Muhammad." }
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
        /// Get all translations for a specific language
        /// </summary>
        public Dictionary<string, string> GetAllTranslations(string languageCode)
        {
            if (languageCode == "en" || string.IsNullOrEmpty(languageCode))
            {
                return new Dictionary<string, string>();
            }

            if (_translations.TryGetValue(languageCode, out var translations))
            {
                return translations;
            }

            return new Dictionary<string, string>();
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