/**
 * Client-side translation system for Athrna
 * Works independently without requiring database translations
 */

document.addEventListener('DOMContentLoaded', function () {
    // Static translations for common site elements
    const translations = {
        // Arabic translations
        'ar': {
            // Common UI elements
            'Home': 'الرئيسية',
            'Explore': 'استكشف',
            'Login': 'تسجيل الدخول',
            'Register': 'التسجيل',
            'My Dashboard': 'لوحة التحكم الخاصة بي',
            'My Bookmarks': 'إشاراتي المرجعية',
            'My Ratings': 'تقييماتي',
            'My Profile': 'ملفي الشخصي',
            'Logout': 'تسجيل الخروج',
            'Search historical sites...': 'البحث عن المواقع التاريخية...',

            // City names
            'Madinah': 'المدينة المنورة',
            'Riyadh': 'الرياض',
            'AlUla': 'العلا',

            // Section headings
            'About': 'حول',
            'Cultural Information': 'المعلومات الثقافية',
            'Available Services': 'الخدمات المتاحة',
            'Available Guides': 'المرشدون المتاحون',
            'Visitor Reviews': 'تقييمات الزوار',
            'Opening Hours': 'ساعات العمل',
            'Admission Fees': 'رسوم الدخول',
            'Located in': 'يقع في',
            'Nearby Sites': 'المواقع القريبة',
            'Historical Sites in': 'المواقع التاريخية في',
            'Available Guides in': 'المرشدون المتاحون في',
            'Interactive Map of': 'خريطة تفاعلية لـ',
            'Explore Site': 'استكشف الموقع',
            'Legend': 'مفتاح الخريطة',

            // Service items
            'Parking': 'موقف سيارات',
            'Free parking available for visitors': 'موقف سيارات مجاني متاح للزوار',
            'Information Center': 'مركز المعلومات',
            'Tourist information and guides': 'معلومات سياحية ومرشدين',
            'Café': 'مقهى',
            'Traditional Saudi coffee and refreshments': 'قهوة سعودية تقليدية ومرطبات',
            'Gift Shop': 'متجر هدايا',
            'Local crafts and souvenirs': 'الحرف اليدوية المحلية والهدايا التذكارية',
            'Free Wi-Fi': 'واي فاي مجاني',
            'Available throughout the site': 'متاح في جميع أنحاء الموقع',
            'Accessibility': 'إمكانية الوصول',
            'Wheelchair ramps and accessible facilities': 'منحدرات للكراسي المتحركة ومرافق يمكن الوصول إليها',

            // Review section
            'Leave Your Review': 'اترك تقييمك',
            'Select your rating': 'اختر تقييمك',
            'Your Review (optional)': 'تقييمك (اختياري)',
            'Submit Review': 'إرسال تقييم',
            'Please login to leave a review': 'الرجاء تسجيل الدخول لترك تقييم',

            // Actions
            'Bookmark this Site': 'أضف هذا الموقع للمفضلة',
            'Login to Bookmark': 'تسجيل الدخول للإضافة للمفضلة',
            'Contact Guide': 'تواصل مع المرشد',
            'Send Message': 'إرسال رسالة',
            'Close': 'إغلاق',

            // Form fields
            'Your Name': 'اسمك',
            'Your Email': 'بريدك الإلكتروني',
            'Preferred Visit Date': 'تاريخ الزيارة المفضل',
            'Group Size': 'حجم المجموعة',
            'Message': 'الرسالة',

            // Select options
            '1-2 people': '1-2 أشخاص',
            '3-5 people': '3-5 أشخاص',
            '6-10 people': '6-10 أشخاص',
            'More than 10 people': 'أكثر من 10 أشخاص',

            // Admission fees
            'Adults': 'البالغين',
            'Children (under 12)': 'الأطفال (تحت 12 سنة)',
            'Students': 'الطلاب',
            'Seniors (65+)': 'كبار السن (65+)',
            'Saudi Nationals': 'المواطنين السعوديين',
            'Free': 'مجانا',

            // Other common terms
            'Established': 'تأسس',
            'reviews': 'تقييمات',
            'Discover the rich historical heritage of': 'اكتشف التراث التاريخي الغني لـ',

            // Popular site names in Madinah
            'Prophet\'s Mosque (Al-Masjid an-Nabawi)': 'المسجد النبوي',
            'Quba Mosque': 'مسجد قباء',
            'Battlefield of Uhud': 'ساحة معركة أحد',
            'Al-Baqi Cemetery': 'مقبرة البقيع',

            // Popular site descriptions
            'The second holiest site in Islam, built by Prophet Muhammad in 622 CE. It contains the Prophet\'s tomb and has been expanded extensively throughout history.': 'ثاني أقدس موقع في الإسلام، بناه النبي محمد صلى الله عليه وسلم عام 622 ميلادي. يحتوي على قبر النبي وتم توسيعه بشكل كبير على مر التاريخ.',
            'The first mosque built in Islam. Prophet Muhammad laid its foundation when he arrived in Madinah after his migration from Mecca.': 'أول مسجد بني في الإسلام. وضع النبي محمد صلى الله عليه وسلم أساسه عندما وصل إلى المدينة بعد هجرته من مكة.',
            'Site of the Battle of Uhud in 625 CE, one of the most significant battles in Islamic history. Located at the foot of Mount Uhud.': 'موقع غزوة أحد في عام 625 ميلادي، واحدة من أهم المعارك في التاريخ الإسلامي. تقع عند سفح جبل أحد.',
            'Ancient cemetery containing the graves of many of Prophet Muhammad\'s companions and family members.': 'مقبرة قديمة تحتوي على قبور العديد من صحابة النبي محمد صلى الله عليه وسلم وأفراد عائلته.'
        },

        // French translations
        'fr': {
            // Common UI elements
            'Home': 'Accueil',
            'Explore': 'Explorer',
            'Login': 'Connexion',
            'Register': 'S\'inscrire',
            'My Dashboard': 'Mon tableau de bord',
            'My Bookmarks': 'Mes favoris',
            'My Ratings': 'Mes évaluations',
            'My Profile': 'Mon profil',
            'Logout': 'Déconnexion',
            'Search historical sites...': 'Rechercher des sites historiques...',

            // City names
            'Madinah': 'Médine',
            'Riyadh': 'Riyad',
            'AlUla': 'AlUla',

            // Section headings
            'About': 'À propos',
            'Cultural Information': 'Informations culturelles',
            'Available Services': 'Services disponibles',
            'Available Guides': 'Guides disponibles',
            'Visitor Reviews': 'Avis des visiteurs',
            'Opening Hours': 'Heures d\'ouverture',
            'Admission Fees': 'Frais d\'entrée',
            'Located in': 'Situé à',
            'Nearby Sites': 'Sites à proximité',
            'Historical Sites in': 'Sites historiques à',
            'Available Guides in': 'Guides disponibles à',
            'Interactive Map of': 'Carte interactive de',
            'Explore Site': 'Explorer le site',
            'Legend': 'Légende',

            // Service items
            'Parking': 'Stationnement',
            'Free parking available for visitors': 'Stationnement gratuit disponible pour les visiteurs',
            'Information Center': 'Centre d\'information',
            'Tourist information and guides': 'Informations touristiques et guides',
            'Café': 'Café',
            'Traditional Saudi coffee and refreshments': 'Café saoudien traditionnel et rafraîchissements',
            'Gift Shop': 'Boutique de souvenirs',
            'Local crafts and souvenirs': 'Artisanat local et souvenirs',
            'Free Wi-Fi': 'Wi-Fi gratuit',
            'Available throughout the site': 'Disponible dans tout le site',
            'Accessibility': 'Accessibilité',
            'Wheelchair ramps and accessible facilities': 'Rampes pour fauteuils roulants et installations accessibles',

            // Review section
            'Leave Your Review': 'Laissez votre avis',
            'Select your rating': 'Sélectionnez votre note',
            'Your Review (optional)': 'Votre avis (facultatif)',
            'Submit Review': 'Soumettre l\'avis',
            'Please login to leave a review': 'Veuillez vous connecter pour laisser un avis',

            // Actions
            'Bookmark this Site': 'Ajouter aux favoris',
            'Login to Bookmark': 'Connectez-vous pour ajouter aux favoris',
            'Contact Guide': 'Contacter le guide',
            'Send Message': 'Envoyer le message',
            'Close': 'Fermer',

            // Form fields
            'Your Name': 'Votre nom',
            'Your Email': 'Votre email',
            'Preferred Visit Date': 'Date de visite préférée',
            'Group Size': 'Taille du groupe',
            'Message': 'Message',

            // Select options
            '1-2 people': '1-2 personnes',
            '3-5 people': '3-5 personnes',
            '6-10 people': '6-10 personnes',
            'More than 10 people': 'Plus de 10 personnes',

            // Admission fees
            'Adults': 'Adultes',
            'Children (under 12)': 'Enfants (moins de 12 ans)',
            'Students': 'Étudiants',
            'Seniors (65+)': 'Seniors (65+)',
            'Saudi Nationals': 'Citoyens saoudiens',
            'Free': 'Gratuit',

            // Other common terms
            'Established': 'Établi',
            'reviews': 'avis',
            'Discover the rich historical heritage of': 'Découvrez le riche patrimoine historique de',

            // Popular site names in Madinah
            'Prophet\'s Mosque (Al-Masjid an-Nabawi)': 'Mosquée du Prophète (Al-Masjid an-Nabawi)',
            'Quba Mosque': 'Mosquée de Quba',
            'Battlefield of Uhud': 'Champ de bataille d\'Uhud',
            'Al-Baqi Cemetery': 'Cimetière d\'Al-Baqi',

            // Popular site descriptions
            'The second holiest site in Islam, built by Prophet Muhammad in 622 CE. It contains the Prophet\'s tomb and has been expanded extensively throughout history.': 'Le deuxième site le plus saint de l\'Islam, construit par le Prophète Muhammad en 622 EC. Il contient la tombe du Prophète et a été considérablement agrandi tout au long de l\'histoire.',
            'The first mosque built in Islam. Prophet Muhammad laid its foundation when he arrived in Madinah after his migration from Mecca.': 'La première mosquée construite en Islam. Le Prophète Muhammad a posé sa fondation à son arrivée à Médine après sa migration de La Mecque.',
            'Site of the Battle of Uhud in 625 CE, one of the most significant battles in Islamic history. Located at the foot of Mount Uhud.': 'Site de la bataille d\'Uhud en 625 EC, l\'une des batailles les plus importantes de l\'histoire islamique. Situé au pied du mont Uhud.',
            'Ancient cemetery containing the graves of many of Prophet Muhammad\'s companions and family members.': 'Ancien cimetière contenant les tombes de nombreux compagnons et membres de la famille du Prophète Muhammad.'
        },

        // Spanish translations
        'es': {
            // Common UI elements
            'Home': 'Inicio',
            'Explore': 'Explorar',
            'Login': 'Iniciar sesión',
            'Register': 'Registrarse',
            'My Dashboard': 'Mi panel',
            'My Bookmarks': 'Mis marcadores',
            'My Ratings': 'Mis calificaciones',
            'My Profile': 'Mi perfil',
            'Logout': 'Cerrar sesión',
            'Search historical sites...': 'Buscar sitios históricos...',

            // City names
            'Madinah': 'Medina',
            'Riyadh': 'Riad',
            'AlUla': 'AlUla',

            // Section headings
            'About': 'Acerca de',
            'Cultural Information': 'Información cultural',
            'Available Services': 'Servicios disponibles',
            'Available Guides': 'Guías disponibles',
            'Visitor Reviews': 'Opiniones de visitantes',
            'Opening Hours': 'Horario de apertura',
            'Admission Fees': 'Precios de entrada',
            'Located in': 'Ubicado en',
            'Nearby Sites': 'Sitios cercanos',
            'Historical Sites in': 'Sitios históricos en',
            'Available Guides in': 'Guías disponibles en',
            'Interactive Map of': 'Mapa interactivo de',
            'Explore Site': 'Explorar sitio',
            'Legend': 'Leyenda',

            // Service items
            'Parking': 'Estacionamiento',
            'Free parking available for visitors': 'Estacionamiento gratuito disponible para visitantes',
            'Information Center': 'Centro de información',
            'Tourist information and guides': 'Información turística y guías',
            'Café': 'Café',
            'Traditional Saudi coffee and refreshments': 'Café tradicional saudí y refrescos',
            'Gift Shop': 'Tienda de recuerdos',
            'Local crafts and souvenirs': 'Artesanías locales y recuerdos',
            'Free Wi-Fi': 'Wi-Fi gratuito',
            'Available throughout the site': 'Disponible en todo el sitio',
            'Accessibility': 'Accesibilidad',
            'Wheelchair ramps and accessible facilities': 'Rampas para sillas de ruedas e instalaciones accesibles',

            // Review section
            'Leave Your Review': 'Deje su opinión',
            'Select your rating': 'Seleccione su calificación',
            'Your Review (optional)': 'Su opinión (opcional)',
            'Submit Review': 'Enviar opinión',
            'Please login to leave a review': 'Por favor, inicie sesión para dejar una opinión',

            // Actions
            'Bookmark this Site': 'Guardar en marcadores',
            'Login to Bookmark': 'Iniciar sesión para guardar',
            'Contact Guide': 'Contactar guía',
            'Send Message': 'Enviar mensaje',
            'Close': 'Cerrar',

            // Form fields
            'Your Name': 'Su nombre',
            'Your Email': 'Su correo electrónico',
            'Preferred Visit Date': 'Fecha de visita preferida',
            'Group Size': 'Tamaño del grupo',
            'Message': 'Mensaje',

            // Select options
            '1-2 people': '1-2 personas',
            '3-5 people': '3-5 personas',
            '6-10 people': '6-10 personas',
            'More than 10 people': 'Más de 10 personas',

            // Admission fees
            'Adults': 'Adultos',
            'Children (under 12)': 'Niños (menores de 12)',
            'Students': 'Estudiantes',
            'Seniors (65+)': 'Personas mayores (65+)',
            'Saudi Nationals': 'Ciudadanos saudíes',
            'Free': 'Gratis',

            // Other common terms
            'Established': 'Establecido',
            'reviews': 'opiniones',
            'Discover the rich historical heritage of': 'Descubra el rico patrimonio histórico de',

            // Popular site names in Madinah
            'Prophet\'s Mosque (Al-Masjid an-Nabawi)': 'Mezquita del Profeta (Al-Masjid an-Nabawi)',
            'Quba Mosque': 'Mezquita de Quba',
            'Battlefield of Uhud': 'Campo de batalla de Uhud',
            'Al-Baqi Cemetery': 'Cementerio de Al-Baqi',

            // Popular site descriptions
            'The second holiest site in Islam, built by Prophet Muhammad in 622 CE. It contains the Prophet\'s tomb and has been expanded extensively throughout history.': 'El segundo sitio más sagrado del Islam, construido por el Profeta Muhammad en 622 EC. Contiene la tumba del Profeta y ha sido ampliado extensamente a lo largo de la historia.',
            'The first mosque built in Islam. Prophet Muhammad laid its foundation when he arrived in Madinah after his migration from Mecca.': 'La primera mezquita construida en el Islam. El Profeta Muhammad colocó sus cimientos cuando llegó a Medina después de su migración desde La Meca.',
            'Site of the Battle of Uhud in 625 CE, one of the most significant battles in Islamic history. Located at the foot of Mount Uhud.': 'Sitio de la Batalla de Uhud en 625 EC, una de las batallas más significativas en la historia islámica. Ubicado al pie del Monte Uhud.',
            'Ancient cemetery containing the graves of many of Prophet Muhammad\'s companions and family members.': 'Antiguo cementerio que contiene las tumbas de muchos de los compañeros y familiares del Profeta Muhammad.'
        }
    };

    // Helper functions for translation
    const currentLang = getCookie('language') || 'en';

    // Set the document direction based on language
    if (currentLang === 'ar') {
        document.documentElement.dir = 'rtl';
        document.body.classList.add('rtl');
    } else {
        document.documentElement.dir = 'ltr';
        document.body.classList.remove('rtl');
    }

    // Only proceed with translation if not English
    if (currentLang !== 'en' && translations[currentLang]) {
        // Translate all elements with data-translate attribute
        document.querySelectorAll('[data-translate]').forEach(element => {
            const key = element.getAttribute('data-translate');

            // If it's a direct translation key
            if (translations[currentLang][key]) {
                element.textContent = translations[currentLang][key];
            }
            // If it's the original content that needs translation
            else if (translations[currentLang][element.textContent]) {
                element.textContent = translations[currentLang][element.textContent];
            }
            // Special handling for site elements that might contain the site name
            else if (key.startsWith('site-')) {
                // For site titles, descriptions, etc. we can try a content-based match
                const originalText = element.textContent;
                const translatedText = translateText(originalText, currentLang);
                if (translatedText !== originalText) {
                    element.textContent = translatedText;
                }
            }
        });

        // Scan all text elements for potential translations
        translateAllTextNodes(document.body, currentLang);

        // Handle special cases like city headers
        const cityHeaders = document.querySelectorAll('.city-header h1, .city-header p');
        cityHeaders.forEach(header => {
            const cityName = extractCityName(header.textContent);
            if (cityName && translations[currentLang][cityName]) {
                header.textContent = header.textContent.replace(
                    cityName,
                    translations[currentLang][cityName]
                );
            }
        });

        // Handle section headers that contain city names
        document.querySelectorAll('h2').forEach(heading => {
            for (const key of ['Historical Sites in', 'Available Guides in', 'Interactive Map of']) {
                if (heading.textContent.includes(key)) {
                    const cityName = heading.textContent.replace(key, '').trim();
                    if (translations[currentLang][cityName]) {
                        heading.textContent =
                            (translations[currentLang][key] || key) + ' ' +
                            translations[currentLang][cityName];
                    }
                }
            }
        });
    }

    /**
     * Translates text using the translation dictionary
     */
    function translateText(text, lang) {
        if (!translations[lang]) return text;

        // Direct translation if the entire text matches a key
        if (translations[lang][text]) {
            return translations[lang][text];
        }

        // Try to match the start of longer texts (for headings like "About [SiteName]")
        for (const [key, value] of Object.entries(translations[lang])) {
            if (text.startsWith(key + ' ')) {
                const restOfText = text.substring(key.length).trim();

                // If the rest is a site name we can translate
                if (translations[lang][restOfText]) {
                    return value + ' ' + translations[lang][restOfText];
                }
                // Otherwise just translate the part we know
                return value + ' ' + restOfText;
            }
        }

        // For longer texts like site descriptions, check if we have a direct match
        // This is useful for famous sites with known descriptions
        for (const [key, value] of Object.entries(translations[lang])) {
            // If the key is long enough to be a description and matches
            if (key.length > 50 && text.includes(key)) {
                return text.replace(key, value);
            }
        }

        return text;
    }

    /**
     * Extracts city name from a heading
     */
    function extractCityName(text) {
        // Check for known city names
        const cityNames = ['Madinah', 'Riyadh', 'AlUla'];
        for (const city of cityNames) {
            if (text.includes(city)) {
                return city;
            }
        }
        return null;
    }

    /**
     * Recursively translate all text nodes in an element
     */
    function translateAllTextNodes(element, lang) {
        if (!translations[lang]) return;

        // Skip script and style elements
        if (element.tagName === 'SCRIPT' || element.tagName === 'STYLE') {
            return;
        }

        // Process text nodes
        for (const node of element.childNodes) {
            if (node.nodeType === 3) { // Text node
                const text = node.nodeValue.trim();
                if (text && translations[lang][text]) {
                    node.nodeValue = node.nodeValue.replace(text, translations[lang][text]);
                }
            } else if (node.nodeType === 1) { // Element node
                // Skip elements that already have data-translate
                if (!node.hasAttribute('data-translate')) {
                    translateAllTextNodes(node, lang);
                }
            }
        }
    }

    /**
     * Get a cookie value by name
     */
    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }

    // Add event listeners for language changes
    document.querySelectorAll('.language-selector a').forEach(link => {
        link.addEventListener('click', function () {
            // Show loading overlay while changing language
            showLoadingOverlay();
        });
    });

    /**
     * Shows a loading overlay during language change
     */
    function showLoadingOverlay() {
        const overlay = document.createElement('div');
        overlay.style.position = 'fixed';
        overlay.style.top = '0';
        overlay.style.left = '0';
        overlay.style.width = '100%';
        overlay.style.height = '100%';
        overlay.style.backgroundColor = 'rgba(0,0,0,0.5)';
        overlay.style.zIndex = '9999';
        overlay.style.display = 'flex';
        overlay.style.justifyContent = 'center';
        overlay.style.alignItems = 'center';

        const spinner = document.createElement('div');
        spinner.className = 'spinner-border text-light';
        spinner.setAttribute('role', 'status');

        const span = document.createElement('span');
        span.className = 'visually-hidden';
        span.textContent = 'Loading...';

        spinner.appendChild(span);
        overlay.appendChild(spinner);
        document.body.appendChild(overlay);
    }
});