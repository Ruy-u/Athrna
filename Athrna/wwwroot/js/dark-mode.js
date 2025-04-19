/**
 * Dark mode functionality for Athrna
 */
document.addEventListener('DOMContentLoaded', function () {
    // Create toggle button
    const toggleButton = document.createElement('button');
    toggleButton.className = 'theme-toggle';
    toggleButton.setAttribute('aria-label', 'Toggle dark mode');
    toggleButton.innerHTML = '<i class="bi bi-moon-fill" aria-hidden="true"></i>';
    document.body.appendChild(toggleButton);

    // Check for saved theme preference or respect OS preference
    const savedTheme = localStorage.getItem('theme');
    const prefersDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;

    // Apply the right theme
    if (savedTheme === 'dark' || (!savedTheme && prefersDarkMode)) {
        document.body.classList.add('dark-mode');
        toggleButton.innerHTML = '<i class="bi bi-sun-fill" aria-hidden="true"></i>';
        localStorage.setItem('theme', 'dark');
    } else {
        document.body.classList.remove('dark-mode');
        toggleButton.innerHTML = '<i class="bi bi-moon-fill" aria-hidden="true"></i>';
        localStorage.setItem('theme', 'light');
    }

    // Toggle theme on button click
    toggleButton.addEventListener('click', function () {
        if (document.body.classList.contains('dark-mode')) {
            // Switch to light mode
            document.body.classList.remove('dark-mode');
            toggleButton.innerHTML = '<i class="bi bi-moon-fill" aria-hidden="true"></i>';
            localStorage.setItem('theme', 'light');

            // Update toggle button aria-label
            toggleButton.setAttribute('aria-label', 'Toggle dark mode');
        } else {
            // Switch to dark mode
            document.body.classList.add('dark-mode');
            toggleButton.innerHTML = '<i class="bi bi-sun-fill" aria-hidden="true"></i>';
            localStorage.setItem('theme', 'dark');

            // Update toggle button aria-label
            toggleButton.setAttribute('aria-label', 'Toggle light mode');
        }
    });

    // Listen for OS theme changes
    const darkModeMediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    darkModeMediaQuery.addEventListener('change', function (e) {
        // Only apply OS preference if the user hasn't manually set a preference
        if (!localStorage.getItem('theme')) {
            if (e.matches) {
                // Switch to dark mode
                document.body.classList.add('dark-mode');
                toggleButton.innerHTML = '<i class="bi bi-sun-fill" aria-hidden="true"></i>';
            } else {
                // Switch to light mode
                document.body.classList.remove('dark-mode');
                toggleButton.innerHTML = '<i class="bi bi-moon-fill" aria-hidden="true"></i>';
            }
        }
    });

    // Add keyboard shortcut for toggling dark mode (Alt+T)
    document.addEventListener('keydown', function (e) {
        if (e.altKey && e.key === 't') {
            toggleButton.click();
        }
    });
});