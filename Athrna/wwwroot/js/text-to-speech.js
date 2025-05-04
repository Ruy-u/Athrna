// text-to-speech.js

/**
 * Text-to-Speech functionality for Athrna website
 * Uses Google Cloud Text-to-Speech API to read text content in the selected language
 */

class AthrnaTextToSpeech {
    constructor(options = {}) {
        // Configuration
        this.apiKey = options.apiKey || ''; // Will be set server-side
        this.endpointUrl = '/api/texttospeech';
        this.languageSelector = document.cookie.replace(/(?:(?:^|.*;\s*)Athrna_Language\s*\=\s*([^;]*).*$)|^.*$/, "$1") || 'en';
        
        // Default voice settings
        this.voiceSettings = {
            'en': { name: 'en-US-Neural2-F', gender: 'FEMALE' },
            'ar': { name: 'ar-XA-Wavenet-B', gender: 'MALE' },
            'fr': { name: 'fr-FR-Neural2-A', gender: 'FEMALE' },
            'de': { name: 'de-DE-Neural2-B', gender: 'MALE' },
            'es': { name: 'es-ES-Neural2-A', gender: 'FEMALE' }
        };

        // Cache for audio data
        this.audioCache = new Map();
        
        // State
        this.isPlaying = false;
        this.currentAudio = null;
        
        // DOM elements to add speech buttons to
        this.targetSelectors = options.targetSelectors || [
            // Only target elements on the Site Details page
            '.site-description-section p.site-description', 
            '.site-description-section .cultural-info',
            '.site-description-section p[data-translate="site-description"]',
            '.site-description-section div.cultural-info-section p.cultural-info',
            '.site-description-section [data-translate="cultural-info"]'
        ];
        
        // Initialize
        this.init();
    }
    
    init() {
        // Check if we're on a site details page before initializing
        if (this.isSiteDetailsPage()) {
            // Add speech buttons to target elements
            this.addSpeechButtons();
            
            // Listen for language changes
            this.setupLanguageChangeListener();
        } else {
            console.log('Not on site details page, skipping text-to-speech initialization');
        }
    }
    
    /**
     * Checks if current page is a site details page
     * This ensures TTS only appears on site detail pages, not on explore pages
     */
    isSiteDetailsPage() {
        // Check URL pattern to see if we're on a site details page
        const path = window.location.pathname;
        return path.includes('/City/Site/');
    }
    
    /**
     * Adds speech buttons to all target elements
     */
    addSpeechButtons() {
        // Find all target elements
        this.targetSelectors.forEach(selector => {
            const elements = document.querySelectorAll(selector);
            
            elements.forEach(element => {
                // Skip if already has a speech button
                if (element.querySelector('.tts-button') || element.parentNode.querySelector('.tts-button-container')) return;
                
                // Create button container
                const buttonContainer = document.createElement('div');
                buttonContainer.className = 'tts-button-container';
                
                // Create the button
                const button = document.createElement('button');
                button.className = 'tts-button';
                button.setAttribute('aria-label', 'Listen to text');
                button.setAttribute('title', 'Listen to text');
                button.innerHTML = '<i class="bi bi-volume-up"></i>';
                
                // Add click event
                button.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.handleSpeechButtonClick(element, button);
                });
                
                // Add button to container
                buttonContainer.appendChild(button);
                
                // Position button based on element type
                if (element.classList.contains('cultural-info') || element.getAttribute('data-translate') === 'cultural-info') {
                    // For cultural info, place button at the beginning of the element
                    element.insertBefore(buttonContainer, element.firstChild);
                    buttonContainer.style.float = 'left';
                    buttonContainer.style.marginRight = '10px';
                    buttonContainer.style.marginTop = '3px';
                } else {
                    // For regular description, place button before the element
                    element.parentNode.insertBefore(buttonContainer, element);
                    
                    // Add margin to text element if in block mode
                    if (window.getComputedStyle(element).display === 'block') {
                        element.style.marginLeft = '40px';
                    } else {
                        buttonContainer.style.marginRight = '10px';
                    }
                }
            });
        });
        
        // Add styles
        this.addStyles();
    }
    
    /**
     * Handles speech button click
     */
    async handleSpeechButtonClick(element, button) {
        // Get text to speak
        const textToSpeak = element.innerText.trim();
        
        if (!textToSpeak) {
            console.warn('No text found to speak');
            return;
        }
        
        // Stop current audio if playing
        if (this.isPlaying && this.currentAudio) {
            this.currentAudio.pause();
            this.isPlaying = false;
            this.resetAllButtons();
            
            // If clicking the same button that's currently playing, just stop
            if (button.classList.contains('playing')) {
                return;
            }
        }
        
        // Update button state to show loading
        button.classList.add('loading');
        button.innerHTML = '<i class="bi bi-hourglass-split"></i>';
        
        try {
            // Get audio data
            const audioData = await this.getAudioForText(textToSpeak);
            
            // Create and play audio
            const audio = new Audio(audioData);
            this.currentAudio = audio;
            
            // Set up audio events
            audio.addEventListener('playing', () => {
                this.isPlaying = true;
                button.classList.remove('loading');
                button.classList.add('playing');
                button.innerHTML = '<i class="bi bi-pause-fill"></i>';
            });
            
            audio.addEventListener('ended', () => {
                this.isPlaying = false;
                button.classList.remove('playing');
                button.innerHTML = '<i class="bi bi-volume-up"></i>';
            });
            
            audio.addEventListener('pause', () => {
                this.isPlaying = false;
                button.classList.remove('playing');
                button.innerHTML = '<i class="bi bi-volume-up"></i>';
            });
            
            audio.addEventListener('error', (e) => {
                console.error('Audio playback error:', e);
                this.isPlaying = false;
                button.classList.remove('loading', 'playing');
                button.innerHTML = '<i class="bi bi-volume-up"></i>';
                
                // Show error message
                this.showNotification('Failed to play audio. Please try again.', 'error');
            });
            
            // Play the audio
            audio.play();
            
        } catch (error) {
            console.error('Error generating speech:', error);
            button.classList.remove('loading');
            button.innerHTML = '<i class="bi bi-volume-up"></i>';
            
            // Show error notification
            this.showNotification('Failed to generate speech. Please try again.', 'error');
        }
    }
    
    /**
     * Resets all buttons to default state
     */
    resetAllButtons() {
        document.querySelectorAll('.tts-button').forEach(btn => {
            btn.classList.remove('loading', 'playing');
            btn.innerHTML = '<i class="bi bi-volume-up"></i>';
        });
    }
    
    /**
     * Gets audio data for the given text
     * Uses cache if available, otherwise calls API
     */
    async getAudioForText(text) {
        const language = this.languageSelector;
        const cacheKey = `${text}_${language}`;
        
        // Check cache first
        if (this.audioCache.has(cacheKey)) {
            return this.audioCache.get(cacheKey);
        }
        
        // Get voice settings for the current language
        const voiceSettings = this.voiceSettings[language] || this.voiceSettings['en'];
        
        // Call Text-to-Speech API
        const audioData = await this.callTextToSpeechAPI(text, language, voiceSettings);
        
        // Cache the result
        this.audioCache.set(cacheKey, audioData);
        
        return audioData;
    }
    
    /**
     * Calls server-side Text-to-Speech API endpoint
     */
    async callTextToSpeechAPI(text, language, voiceSettings) {
        try {
            // Get CSRF token if available
            const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            const headers = {
                'Content-Type': 'application/json'
            };
            
            if (csrfToken) {
                headers['RequestVerificationToken'] = csrfToken;
            }
            
            const response = await fetch(this.endpointUrl, {
                method: 'POST',
                headers: headers,
                body: JSON.stringify({
                    text: text,
                    languageCode: language,
                    voiceName: voiceSettings.name,
                    ssmlGender: voiceSettings.gender
                })
            });
            
            if (!response.ok) {
                throw new Error(`API error: ${response.status} ${response.statusText}`);
            }
            
            const data = await response.json();
            return `data:audio/mp3;base64,${data.audioContent}`;
            
        } catch (error) {
            console.error('Error calling Text-to-Speech API:', error);
            throw error;
        }
    }
    
    /**
     * Displays a notification message
     */
    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `tts-notification ${type}`;
        notification.textContent = message;
        
        // Add to body
        document.body.appendChild(notification);
        
        // Show with animation
        setTimeout(() => {
            notification.classList.add('show');
        }, 10);
        
        // Hide after 3 seconds
        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }
    
    /**
     * Sets up listener for language changes
     */
    setupLanguageChangeListener() {
        // Check for language changes in the cookie
        setInterval(() => {
            const currentLanguage = document.cookie.replace(/(?:(?:^|.*;\s*)Athrna_Language\s*\=\s*([^;]*).*$)|^.*$/, "$1") || 'en';
            
            if (currentLanguage !== this.languageSelector) {
                this.languageSelector = currentLanguage;
                
                // Clear audio cache when language changes
                this.audioCache.clear();
                
                // Stop any playing audio
                if (this.isPlaying && this.currentAudio) {
                    this.currentAudio.pause();
                    this.isPlaying = false;
                    this.resetAllButtons();
                }
            }
        }, 1000);
    }
    
    /**
     * Adds CSS styles for speech buttons
     */
    addStyles() {
        // Check if styles already exist
        if (document.getElementById('tts-styles')) return;
        
        // Create style element
        const style = document.createElement('style');
        style.id = 'tts-styles';
        style.textContent = `
            .tts-button-container {
                display: inline-block;
                margin-right: 8px;
                vertical-align: middle;
            }
            
            .tts-button {
                width: 36px;
                height: 36px;
                border-radius: 50%;
                background-color: #1a3b29;
                color: white;
                display: flex;
                align-items: center;
                justify-content: center;
                border: none;
                cursor: pointer;
                transition: all 0.3s ease;
                box-shadow: 0 2px 5px rgba(0, 0, 0, 0.2);
                padding: 0;
                margin: 0 5px;
            }
            
            .tts-button:hover {
                background-color: #2c5f46;
                transform: translateY(-2px);
                box-shadow: 0 4px 8px rgba(0, 0, 0, 0.3);
            }
            
            .tts-button.playing {
                background-color: #f8c15c;
                color: #1a3b29;
            }
            
            .tts-button.loading {
                opacity: 0.7;
            }
            
            .tts-button i {
                font-size: 18px;
            }
            
            .tts-notification {
                position: fixed;
                bottom: 20px;
                right: 20px;
                padding: 10px 20px;
                background-color: #333;
                color: white;
                border-radius: 4px;
                box-shadow: 0 2px 10px rgba(0, 0, 0, 0.3);
                opacity: 0;
                transform: translateY(20px);
                transition: all 0.3s ease;
                z-index: 1000;
            }
            
            .tts-notification.show {
                opacity: 1;
                transform: translateY(0);
            }
            
            .tts-notification.error {
                background-color: #dc3545;
            }
            
            .tts-notification.success {
                background-color: #28a745;
            }
            
            /* Dark mode support */
            [data-theme="dark"] .tts-button {
                background-color: #2c5f46;
            }
            
            [data-theme="dark"] .tts-button:hover {
                background-color: #3a7d5d;
            }
            
            [data-theme="dark"] .tts-button.playing {
                background-color: #f8c15c;
                color: #2c5f46;
            }
            
            /* Specific styling for cultural info section */
            .cultural-info-section .tts-button-container {
                float: left;
                margin-right: 12px;
                margin-top: 2px;
            }
            
            /* Add a clear fix for floating elements */
            .cultural-info-section:after {
                content: "";
                display: table;
                clear: both;
            }
            
            /* Heading with button styling */
            h3.mt-4 + .cultural-info-section .tts-button-container {
                margin-top: -30px;
                margin-bottom: 10px;
            }
            
            /* Responsive adjustments */
            @media (max-width: 768px) {
                .tts-button {
                    width: 32px;
                    height: 32px;
                }
                
                .tts-button i {
                    font-size: 16px;
                }
                
                .cultural-info-section .tts-button-container {
                    display: block;
                    margin-bottom: 10px;
                }
            }
            
            /* Spinner animation for loading state */
            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }
            
            .tts-button.loading i {
                animation: spin 1.5s linear infinite;
            }
        `;
        
        // Add to head
        document.head.appendChild(style);
    }
}

// Initialize the text-to-speech functionality when the DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    // Create text-to-speech instance
    window.athrnaTextToSpeech = new AthrnaTextToSpeech();
});