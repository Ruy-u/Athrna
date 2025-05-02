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
document.addEventListener('DOMContentLoaded', function () {
    // Handle guide availability toggles
    document.querySelectorAll('[data-bs-toggle="collapse"][data-bs-target^="#guideAvailability-"]').forEach(button => {
        button.addEventListener('shown.bs.collapse', function () {
            const guideId = this.getAttribute('data-bs-target').split('-')[1];
            loadGuideAvailability(guideId);
        });
    });

    // Function to load guide availability
    function loadGuideAvailability(guideId) {
        const availabilityContainer = document.getElementById(`availabilityInfo-${guideId}`);

        if (!availabilityContainer) return;

        // Make AJAX request to get guide availability
        fetch(`/Booking/GetGuideAvailability?guideId=${guideId}`)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Failed to load availability');
                }
                return response.json();
            })
            .then(data => {
                // Clear loading indicator
                availabilityContainer.innerHTML = '';

                if (!data || data.length === 0) {
                    availabilityContainer.innerHTML = '<div class="text-center">No availability information available.</div>';
                    return;
                }

                // Create availability table
                const table = document.createElement('table');
                table.className = 'table table-sm table-bordered m-0';

                // Create table header
                const thead = document.createElement('thead');
                const headerRow = document.createElement('tr');

                const dayHeader = document.createElement('th');
                dayHeader.textContent = 'Day';

                const hoursHeader = document.createElement('th');
                hoursHeader.textContent = 'Available Hours';

                headerRow.appendChild(dayHeader);
                headerRow.appendChild(hoursHeader);
                thead.appendChild(headerRow);
                table.appendChild(thead);

                // Create table body
                const tbody = document.createElement('tbody');

                // Process each day's availability
                data.forEach(item => {
                    if (item.isAvailable) {
                        const row = document.createElement('tr');

                        const dayCell = document.createElement('td');
                        dayCell.textContent = item.dayOfWeek;

                        const hoursCell = document.createElement('td');
                        hoursCell.textContent = `${formatTime(item.startTime)} - ${formatTime(item.endTime)}`;

                        row.appendChild(dayCell);
                        row.appendChild(hoursCell);
                        tbody.appendChild(row);
                    }
                });

                table.appendChild(tbody);
                availabilityContainer.appendChild(table);
            })
            .catch(error => {
                console.error('Error loading guide availability:', error);
                availabilityContainer.innerHTML = '<div class="text-center text-danger">Failed to load availability information.</div>';
            });
    }

    // Helper function to format time
    function formatTime(timeString) {
        const [hours, minutes] = timeString.split(':');
        const hour = parseInt(hours);
        const ampm = hour >= 12 ? 'PM' : 'AM';
        const hour12 = hour % 12 || 12;
        return `${hour12}:${minutes} ${ampm}`;
    }
});