document.addEventListener('DOMContentLoaded', function () {
    // Get all login required buttons
    const loginRequiredButtons = document.querySelectorAll('.login-required-btn');

    // Add click event listeners to all login required buttons
    if (loginRequiredButtons.length > 0) {
        loginRequiredButtons.forEach(button => {
            button.addEventListener('click', function (e) {
                e.preventDefault();

                // Get the action type (book, contact, or bookmark)
                const action = this.getAttribute('data-action');
                let actionText = 'perform this action';

                if (action === 'book') {
                    actionText = 'book a tour';
                } else if (action === 'contact') {
                    actionText = 'contact the guide';
                } else if (action === 'bookmark') {
                    actionText = 'bookmark this site';
                }

                // Show alert with login message
                showLoginAlert(actionText);
            });
        });
    }

    /**
     * Shows a Bootstrap alert with login required message
     * @param {string} action - The action requiring login
     */
    function showLoginAlert(action) {
        // Create the alert element
        const alertDiv = document.createElement('div');
        alertDiv.className = 'alert alert-warning alert-dismissible fade show fixed-top mx-auto mt-3';
        alertDiv.style.maxWidth = '500px';
        alertDiv.style.zIndex = '9999';
        alertDiv.setAttribute('role', 'alert');

        // Create the alert content
        alertDiv.innerHTML = `
            <strong>Login Required!</strong> You need to be logged in to ${action}.
            <a href="#" class="alert-link login-btn ms-2">Login Now</a>
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        `;

        // Add to body
        document.body.appendChild(alertDiv);

        // Auto-dismiss after 5 seconds
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alertDiv);
            bsAlert.close();
        }, 5000);

        // Attach login event to the login link
        const loginLink = alertDiv.querySelector('.login-btn');
        if (loginLink) {
            loginLink.addEventListener('click', function (e) {
                e.preventDefault();

                // Trigger the login modal
                const loginBtns = document.querySelectorAll('.login-btn');
                if (loginBtns.length > 0) {
                    loginBtns[0].click();
                }
            });
        }
    }
});