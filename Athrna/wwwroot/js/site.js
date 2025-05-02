/**
 * Main Site JavaScript
 * This script handles global site functionality
 */
document.addEventListener('DOMContentLoaded', function () {
    // Mobile menu toggle
    const mobileMenuToggle = document.querySelector('.mobile-menu-toggle');
    const headerActions = document.querySelector('.header-actions');

    if (mobileMenuToggle && headerActions) {
        mobileMenuToggle.addEventListener('click', function () {
            headerActions.classList.toggle('show');

            // Update aria-expanded attribute
            const isExpanded = headerActions.classList.contains('show');
            this.setAttribute('aria-expanded', isExpanded);
        });
    }

    // Mobile search toggle
    const searchToggle = document.querySelector('.search-toggle');
    const searchBar = document.querySelector('.search-bar');

    if (searchToggle && searchBar) {
        searchToggle.addEventListener('click', function () {
            searchBar.classList.toggle('show');

            // Update aria-expanded attribute
            const isExpanded = searchBar.classList.contains('show');
            this.setAttribute('aria-expanded', isExpanded);
        });
    }

    // Handle success messages
    if (document.querySelector('.alert-success')) {
        setTimeout(function () {
            const alerts = document.querySelectorAll('.alert-success');
            alerts.forEach(alert => {
                // Create bootstrap alert instance and close it
                const bsAlert = new bootstrap.Alert(alert);
                bsAlert.close();
            });
        }, 5000); // Auto-close after 5 seconds
    }

    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Form validation
    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach(form => {
        form.addEventListener('submit', event => {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });

    // Add animation class to newly inserted elements
    const animatedElements = document.querySelectorAll('.fade-in');
    animatedElements.forEach(element => {
        // Use Intersection Observer to trigger animation when element is in viewport
        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('visible');
                    observer.unobserve(entry.target);
                }
            });
        });
        observer.observe(element);
    });

    // Handle back-to-top button
    const backToTopButton = document.querySelector('.back-to-top');
    if (backToTopButton) {
        window.addEventListener('scroll', function () {
            if (window.pageYOffset > 300) {
                backToTopButton.classList.add('show');
            } else {
                backToTopButton.classList.remove('show');
            }
        });

        backToTopButton.addEventListener('click', function (e) {
            e.preventDefault();
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        });
    }
});