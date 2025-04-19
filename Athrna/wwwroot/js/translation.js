// Translation functionality for Athrna
document.addEventListener('DOMContentLoaded', function () {
    // Get current language from cookie
    const getCurrentLanguage = () => {
        const matches = document.cookie.match(/language=([^;]+)/);
        return matches ? matches[1] : 'en';
    };

    const currentLanguage = getCurrentLanguage();

    // Apply translations if we're on a site details page
    const siteDetailsContainer = document.querySelector('.site-description-section');
    if (siteDetailsContainer && currentLanguage !== 'en') {
        // Extract site ID from URL
        const urlParts = window.location.pathname.split('/');
        const siteId = urlParts[urlParts.length - 1];

        if (siteId && !isNaN(siteId)) {
            applySiteTranslation(siteId, currentLanguage);
        }
    }

    /**
     * Applies translations to the site details page
     * @param {string} siteId - The ID of the site
     * @param {string} languageCode - The language code (e.g., 'ar', 'fr')
     */
    function applySiteTranslation(siteId, languageCode) {
        // Fetch site translation
        fetch(`/Translation/GetSiteTranslation/${siteId}?lang=${languageCode}`)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                // Update site name
                const siteTitleElement = document.querySelector('.site-title');
                if (siteTitleElement && data.Name) {
                    siteTitleElement.textContent = data.Name;
                }

                // Update site description
                const siteDescriptionElement = document.querySelector('.site-description-section p:first-of-type');
                if (siteDescriptionElement && data.Description) {
                    siteDescriptionElement.textContent = data.Description;
                }

                // After updating site data, fetch cultural info translation
                return fetch(`/Translation/GetCulturalInfoTranslation/${siteId}?lang=${languageCode}`);
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                // Update cultural info summary
                const culturalInfoElement = document.querySelector('.site-description-section p:last-of-type');
                if (culturalInfoElement && data.Summary) {
                    culturalInfoElement.textContent = data.Summary;
                }
            })
            .catch(error => {
                console.error('Error fetching translation:', error);
            });
    }

    // Handle RTL layout for Arabic language
    if (currentLanguage === 'ar') {
        document.documentElement.setAttribute('dir', 'rtl');
        // Add RTL Bootstrap CSS
        const rtlStyles = document.createElement('link');
        rtlStyles.rel = 'stylesheet';
        rtlStyles.href = 'https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.rtl.min.css';
        document.head.appendChild(rtlStyles);
    } else {
        document.documentElement.setAttribute('dir', 'ltr');
    }

    // Add translation buttons to admin interface if we're on the edit site page
    const adminEditForm = document.querySelector('form[asp-action="EditSite"]');
    if (adminEditForm) {
        setupAdminTranslationInterface();
    }

    /**
     * Sets up the translation interface for admin users editing sites
     */
    function setupAdminTranslationInterface() {
        // Find the cultural info section
        const culturalInfoSection = document.querySelector('.form-section:has(label[for="CulturalInfo.Summary"])');
        if (!culturalInfoSection) return;

        // Create translation section
        const translationSection = document.createElement('div');
        translationSection.className = 'form-section mt-4';
        translationSection.innerHTML = `
            <h5>Translations</h5>
            <div class="alert alert-info">
                Add translations for site content in different languages.
            </div>
            <div class="translation-tabs">
                <ul class="nav nav-tabs" id="translationTabs" role="tablist"></ul>
                <div class="tab-content mt-3" id="translationTabContent"></div>
            </div>
        `;

        // Insert after cultural info section
        culturalInfoSection.parentNode.insertBefore(translationSection, culturalInfoSection.nextSibling);

        // Fetch available languages
        fetch('/Translation/GetAvailableLanguages')
            .then(response => response.json())
            .then(languages => {
                populateTranslationTabs(languages);
            })
            .catch(error => {
                console.error('Error fetching languages:', error);
            });
    }

    /**
     * Populates the translation tabs with language options
     * @param {Array} languages - List of language objects
     */
    function populateTranslationTabs(languages) {
        const tabsList = document.getElementById('translationTabs');
        const tabContent = document.getElementById('translationTabContent');

        if (!tabsList || !tabContent) return;

        // Skip English (default language)
        const translationLanguages = languages.filter(lang => lang.code !== 'en');

        // Create a tab for each language
        translationLanguages.forEach((lang, index) => {
            // Create tab
            const tabItem = document.createElement('li');
            tabItem.className = 'nav-item';
            tabItem.role = 'presentation';
            tabItem.innerHTML = `
                <button class="nav-link ${index === 0 ? 'active' : ''}" 
                        id="lang-${lang.code}-tab" 
                        data-bs-toggle="tab" 
                        data-bs-target="#lang-${lang.code}" 
                        type="button" 
                        role="tab" 
                        aria-controls="lang-${lang.code}" 
                        aria-selected="${index === 0}">
                    ${lang.name}
                </button>
            `;
            tabsList.appendChild(tabItem);

            // Create tab content
            const tabPane = document.createElement('div');
            tabPane.className = `tab-pane fade ${index === 0 ? 'show active' : ''}`;
            tabPane.id = `lang-${lang.code}`;
            tabPane.role = 'tabpanel';
            tabPane.setAttribute('aria-labelledby', `lang-${lang.code}-tab`);

            // Create form fields for this language
            tabPane.innerHTML = `
                <form id="translation-form-${lang.code}" class="translation-form">
                    <input type="hidden" name="languageId" value="${lang.id}">
                    
                    <div class="form-group mb-3">
                        <label class="form-label">Name (${lang.name})</label>
                        <input type="text" name="translatedName" class="form-control">
                    </div>
                    
                    <div class="form-group mb-3">
                        <label class="form-label">Description (${lang.name})</label>
                        <textarea name="translatedDescription" class="form-control" rows="5"></textarea>
                    </div>
                    
                    <div class="form-group mb-3">
                        <label class="form-label">Cultural Summary (${lang.name})</label>
                        <textarea name="translatedSummary" class="form-control" rows="4"></textarea>
                    </div>
                    
                    <button type="button" class="btn btn-primary save-translation" data-lang="${lang.code}">
                        Save ${lang.name} Translation
                    </button>
                </form>
            `;

            tabContent.appendChild(tabPane);
        });

        // Add event listeners to save buttons
        document.querySelectorAll('.save-translation').forEach(button => {
            button.addEventListener('click', function () {
                const langCode = this.getAttribute('data-lang');
                saveTranslation(langCode);
            });
        });
    }

    /**
     * Saves a translation for a specific language
     * @param {string} langCode - The language code
     */
    function saveTranslation(langCode) {
        const form = document.getElementById(`translation-form-${langCode}`);
        if (!form) return;

        // Get form data
        const formData = new FormData(form);
        const siteId = document.querySelector('input[name="Id"]').value;

        // Create site translation
        fetch('/Translation/AddSiteTranslation', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: new URLSearchParams({
                'siteId': siteId,
                'languageId': formData.get('languageId'),
                'translatedName': formData.get('translatedName'),
                'translatedDescription': formData.get('translatedDescription')
            })
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Failed to save site translation');
                }

                // Get cultural info ID
                const culturalInfoId = getCulturalInfoId();

                if (culturalInfoId) {
                    // Create cultural info translation
                    return fetch('/Translation/AddCulturalInfoTranslation', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/x-www-form-urlencoded',
                            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                        },
                        body: new URLSearchParams({
                            'culturalInfoId': culturalInfoId,
                            'languageId': formData.get('languageId'),
                            'translatedSummary': formData.get('translatedSummary')
                        })
                    });
                }
            })
            .then(response => {
                if (!response || !response.ok) {
                    throw new Error('Failed to save cultural info translation');
                }

                alert(`${langCode.toUpperCase()} translation saved successfully!`);
            })
            .catch(error => {
                console.error('Error saving translation:', error);
                alert('Error saving translation. Please try again.');
            });
    }

    /**
     * Gets the cultural info ID from the page
     * @returns {string|null} The cultural info ID or null if not found
     */
    function getCulturalInfoId() {
        // This would depend on how the cultural info ID is stored on the page
        // For now, we'll assume it's stored in a hidden input
        const culturalInfoInput = document.querySelector('input[name="CulturalInfo.Id"]');
        return culturalInfoInput ? culturalInfoInput.value : null;
    }
});