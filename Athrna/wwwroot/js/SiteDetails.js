// Site Details Page JavaScript
document.addEventListener('DOMContentLoaded', function () {
    // Handle guide contact modal
    const guideContactModal = document.getElementById('guideContactModal');
    if (guideContactModal) {
        guideContactModal.addEventListener('show.bs.modal', function (event) {
            // Button that triggered the modal
            const button = event.relatedTarget;

            // Extract info from data-* attributes
            const guideName = button.getAttribute('data-guide-name');
            const guideEmail = button.getAttribute('data-guide-email');

            // Update the modal's content
            const modalTitle = guideContactModal.querySelector('.modal-title');
            const guideEmailInput = document.getElementById('guideEmail');

            modalTitle.textContent = `Contact ${guideName}`;
            guideEmailInput.value = guideEmail;
        });
    }

    // Handle send message button
    const sendMessageBtn = document.getElementById('sendContactMessage');
    if (sendMessageBtn) {
        sendMessageBtn.addEventListener('click', function () {
            const form = document.getElementById('guideContactForm');

            // Simple validation
            if (form.checkValidity()) {
                // In a real app, you would send the form data to the server
                // For demo, just show success message
                alert('Your message has been sent to the guide. They will contact you shortly.');

                // Close the modal
                const modal = bootstrap.Modal.getInstance(guideContactModal);
                modal.hide();

                // Reset form
                form.reset();
            } else {
                // Trigger browser's native form validation
                form.reportValidity();
            }
        });
    }

    // Initialize date inputs with the current date
    const dateInputs = document.querySelectorAll('input[type="date"]');
    if (dateInputs.length > 0) {
        const today = new Date().toISOString().split('T')[0];
        dateInputs.forEach(input => {
            if (!input.min) {
                input.min = today;
            }
            if (!input.value) {
                input.value = today;
            }
        });
    }

    // Star Rating Interaction
    const ratingStars = document.querySelectorAll('.rating-stars input');
    if (ratingStars.length > 0) {
        ratingStars.forEach(star => {
            star.addEventListener('change', function () {
                document.querySelector('.rating-label').textContent = `You rated: ${this.value} stars`;
            });
        });
    }

    // Toggle service details
    const serviceItems = document.querySelectorAll('.service-item');
    if (serviceItems.length > 0) {
        serviceItems.forEach(item => {
            item.addEventListener('click', function () {
                const details = this.querySelector('.service-details p');
                if (details.style.maxHeight) {
                    details.style.maxHeight = null;
                } else {
                    details.style.maxHeight = details.scrollHeight + 'px';
                }
            });
        });
    }

    // Image Error Handling
    const siteHeroImg = document.querySelector('.site-hero-img');
    if (siteHeroImg) {
        siteHeroImg.addEventListener('error', function () {
            this.src = '/api/placeholder/1200/400';
        });
    }

    const cityImg = document.querySelector('.city-img');
    if (cityImg) {
        cityImg.addEventListener('error', function () {
            this.src = '/api/placeholder/300/150';
        });
    }

    // Show alert messages if present
    const message = document.cookie.replace(/(?:(?:^|.*;\s*)message\s*\=\s*([^;]*).*$)|^.*$/, "$1");
    if (message) {
        alert(decodeURIComponent(message));
        // Clear the message cookie
        document.cookie = "message=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
    }
});