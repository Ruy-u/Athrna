// Simple translation script
document.addEventListener('DOMContentLoaded', function () {
    // Check current language
    const lang = getCookie('language') || 'en';

    // If Arabic, set RTL direction
    if (lang === 'ar') {
        document.documentElement.dir = 'rtl';
        document.body.classList.add('rtl');
    } else {
        document.documentElement.dir = 'ltr';
        document.body.classList.remove('rtl');
    }

    // Handle language change
    document.querySelectorAll('.language-dropdown-menu .dropdown-item').forEach(item => {
        item.addEventListener('click', function (e) {
            // Show loading indicator
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
            overlay.innerHTML = '<div style="color:white;">Changing language...</div>';
            document.body.appendChild(overlay);
        });
    });

    // Helper function to get cookie
    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
    }
});