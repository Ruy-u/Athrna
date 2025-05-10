/**
 * Enhanced Dark Mode Functionality with better support for City/Site pages
 */
document.addEventListener('DOMContentLoaded', function () {
    console.log("Dark mode script loaded");

    // Create and add dark mode toggle button to header
    createDarkModeToggle();

    // Check for saved theme preference or respect OS preference
    initTheme();

    // Set up toggle button click event
    setupToggleButton();

    // Add observation for City/Site specific elements that might be loaded dynamically
    observeDynamicContent();
});

/**
 * Creates and adds the dark mode toggle button to the header
 */
function createDarkModeToggle() {
    console.log("Creating dark mode toggle");

    // Check if toggle already exists to avoid duplicates
    if (document.getElementById('darkModeToggle')) {
        console.log("Dark mode toggle already exists");
        return;
    }

    // Create the toggle button
    const toggleButton = document.createElement('button');
    toggleButton.className = 'dark-mode-toggle';
    toggleButton.id = 'darkModeToggle';
    toggleButton.setAttribute('aria-label', 'Toggle dark mode');
    toggleButton.innerHTML = '<i class="bi bi-moon"></i>';

    // Find where to insert the button
    const headerActions = document.querySelector('.header-actions');

    if (headerActions) {
        // Insert at beginning of header actions
        headerActions.insertBefore(toggleButton, headerActions.firstChild);
        console.log("Dark mode toggle button added to header");
    } else {
        // Fallback - try to add it to the header directly
        const header = document.querySelector('header');
        if (header) {
            const toggleContainer = document.createElement('div');
            toggleContainer.className = 'ms-auto me-3';
            toggleContainer.appendChild(toggleButton);
            header.appendChild(toggleContainer);
            console.log("Dark mode toggle added to header (fallback)");
        } else {
            console.warn("Could not find header element to add dark mode toggle");
        }
    }
}

/**
 * Checks for saved preference or OS preference to set initial theme
 */
function initTheme() {
    // Check for saved user preference
    const savedTheme = localStorage.getItem('theme');
    console.log("Saved theme:", savedTheme);

    if (savedTheme === 'dark') {
        // User explicitly chose dark mode before
        setTheme('dark');
    } else if (savedTheme === 'light') {
        // User explicitly chose light mode before
        setTheme('light');
    } else {
        // Check OS preference if no saved preference
        const prefersDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;
        console.log("OS prefers dark mode:", prefersDarkMode);

        if (prefersDarkMode) {
            setTheme('dark');
        } else {
            setTheme('light');
        }
    }
}

/**
 * Sets theme and updates toggle button
 */
function setTheme(theme) {
    console.log("Setting theme to:", theme);

    document.documentElement.setAttribute('data-theme', theme);
    updateToggleButton(theme === 'dark');

    // Also add a class to body for additional control
    if (theme === 'dark') {
        document.body.classList.add('dark-theme');
    } else {
        document.body.classList.remove('dark-theme');
    }

    // Apply theme to site-specific elements that might not have been updated
    applySiteSpecificTheme(theme);
}

/**
 * Applies theme to site-specific elements that might be tricky to style with CSS
 */
function applySiteSpecificTheme(theme) {
    // City/Site page specific elements
    if (window.location.pathname.includes('/City/Site/') ||
        window.location.pathname.includes('/City/Explore/')) {

        // Force update for elements that might be resistant to CSS-only styling
        if (theme === 'dark') {
            // Apply dark mode styling to specific elements
            document.querySelectorAll('.site-card, .service-item, .guide-profile, .sidebar-widget').forEach(el => {
                el.style.backgroundColor = '#1e1e1e';
                el.style.color = '#e0e0e0';
                el.style.borderColor = '#333333';
            });

            // Fix headings within cards
            document.querySelectorAll('.site-card h3, .service-details h4, .guide-info h4, .sidebar-widget h4').forEach(el => {
                el.style.color = '#e0e0e0';
            });

            // Fix descriptions and paragraphs
            document.querySelectorAll('.site-description, .cultural-info, .service-details p, .guide-details').forEach(el => {
                el.style.color = '#e0e0e0';
            });

            // Fix sidebar lists
            document.querySelectorAll('.nearby-sites li, .hours-list li, .fees-list li').forEach(el => {
                el.style.borderColor = '#333333';
            });
        } else {
            // Remove inline styles when returning to light mode
            document.querySelectorAll('.site-card, .service-item, .guide-profile, .sidebar-widget, ' +
                '.site-card h3, .service-details h4, .guide-info h4, .sidebar-widget h4, ' +
                '.site-description, .cultural-info, .service-details p, .guide-details, ' +
                '.nearby-sites li, .hours-list li, .fees-list li').forEach(el => {
                    el.style.backgroundColor = '';
                    el.style.color = '';
                    el.style.borderColor = '';
                });
        }
    }
}

/**
 * Sets up the toggle button click event
 */
function setupToggleButton() {
    const toggleButton = document.getElementById('darkModeToggle');

    if (toggleButton) {
        toggleButton.addEventListener('click', function () {
            // Check current theme
            const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
            const newTheme = currentTheme === 'light' ? 'dark' : 'light';

            console.log("Toggling theme from", currentTheme, "to", newTheme);

            // Update the theme
            setTheme(newTheme);

            // Save preference
            localStorage.setItem('theme', newTheme);
        });

        console.log("Toggle button event handler set up");
    } else {
        console.warn("Could not find dark mode toggle button");
    }
}

/**
 * Updates the toggle button icon based on current mode
 */
function updateToggleButton(isDarkMode) {
    const toggleButton = document.getElementById('darkModeToggle');

    if (toggleButton) {
        if (isDarkMode) {
            toggleButton.innerHTML = '<i class="bi bi-sun"></i>';
            toggleButton.setAttribute('aria-label', 'Switch to light mode');
        } else {
            toggleButton.innerHTML = '<i class="bi bi-moon"></i>';
            toggleButton.setAttribute('aria-label', 'Switch to dark mode');
        }
    }
}

/**
 * Sets up a MutationObserver to detect dynamic content changes,
 * especially helpful for City/Site pages where content may load dynamically
 */
function observeDynamicContent() {
    // Only set up on City/Site pages
    if (window.location.pathname.includes('/City/Site/') ||
        window.location.pathname.includes('/City/Explore/')) {

        console.log("Setting up observer for dynamic content on City/Site page");

        const observer = new MutationObserver(function (mutations) {
            // Check if we need to reapply site-specific theming
            const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
            applySiteSpecificTheme(currentTheme);
        });

        // Start observing the main content area for changes
        const targetNode = document.querySelector('main') || document.body;
        const config = { childList: true, subtree: true };
        observer.observe(targetNode, config);
    }
}

/**
 * Check and handle OS theme preference changes
 */
const prefersDarkModeMedia = window.matchMedia('(prefers-color-scheme: dark)');
prefersDarkModeMedia.addEventListener('change', function (e) {
    // Only apply OS preference if user hasn't set a preference
    if (!localStorage.getItem('theme')) {
        console.log("OS preference changed, updating theme");
        setTheme(e.matches ? 'dark' : 'light');
    }
});

// Add a function to force a check/refresh of theme
window.refreshDarkMode = function () {
    const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
    console.log("Manually refreshing dark mode. Current theme:", currentTheme);
    setTheme(currentTheme);
};

// Execute a refresh after a short delay to ensure everything is loaded
setTimeout(function () {
    window.refreshDarkMode();
    console.log("Delayed theme refresh executed");
}, 500);

// Add second refresh for site-specific content that might load later
setTimeout(function () {
    // Check for site-specific page
    if (window.location.pathname.includes('/City/Site/') ||
        window.location.pathname.includes('/City/Explore/')) {
        console.log("Second refresh for site-specific content");
        window.refreshDarkMode();
    }
}, 1500);