/**
 * Enhanced Login Modal Functionality with CAPTCHA
 */
document.addEventListener('DOMContentLoaded', function () {
    // Get elements
    const loginModal = document.getElementById('loginModal');
    const loginForm = document.getElementById('loginModalForm');
    const loginErrorMessage = document.getElementById('loginErrorMessage');
    const captchaDisplay = document.getElementById('captchaImage');
    const captchaInput = document.getElementById('captchaInput');
    const refreshCaptchaButton = document.getElementById('refreshCaptcha');
    const passwordToggle = document.querySelector('.password-toggle');
    const passwordField = document.getElementById('modalPassword');

    // Initialize variables
    let captchaText = '';

    // Handle login button click in the navbar
    const loginButtons = document.querySelectorAll('.login-btn');
    loginButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            e.preventDefault();
            // Bootstrap 5 method to show modal
            const modalElement = new bootstrap.Modal(loginModal);
            modalElement.show();
            // Generate new CAPTCHA
            generateCaptcha();
        });
    });

    // Generate a random CAPTCHA string
    function generateCaptchaText(length = 6) {
        const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789';
        let result = '';
        for (let i = 0; i < length; i++) {
            result += chars.charAt(Math.floor(Math.random() * chars.length));
        }
        return result;
    }

    // Display CAPTCHA in the image element
    function generateCaptcha() {
        if (!captchaDisplay) return;

        captchaText = generateCaptchaText();
        captchaDisplay.textContent = captchaText;

        // Clear any previous CAPTCHA input
        if (captchaInput) {
            captchaInput.value = '';
        }
    }

    // Refresh CAPTCHA when button is clicked
    if (refreshCaptchaButton) {
        refreshCaptchaButton.addEventListener('click', function () {
            generateCaptcha();
        });
    }

    // Toggle password visibility
    if (passwordToggle && passwordField) {
        passwordToggle.addEventListener('click', function () {
            const type = passwordField.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordField.setAttribute('type', type);

            // Update icon
            const icon = this.querySelector('i');
            if (type === 'text') {
                icon.classList.remove('bi-eye');
                icon.classList.add('bi-eye-slash');
            } else {
                icon.classList.remove('bi-eye-slash');
                icon.classList.add('bi-eye');
            }
        });
    }

    // Form submission
    if (loginForm) {
        loginForm.addEventListener('submit', function (e) {
            e.preventDefault();

            // Basic validation
            const username = document.getElementById('modalUsername').value;
            const password = document.getElementById('modalPassword').value;
            const rememberMe = document.getElementById('modalRememberMe').checked;
            const captchaInputValue = captchaInput.value;

            // Reset validation state
            this.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
            loginErrorMessage.classList.add('d-none');

            // Validate username
            if (!username) {
                document.getElementById('modalUsername').classList.add('is-invalid');
                return;
            }

            // Validate password
            if (!password) {
                document.getElementById('modalPassword').classList.add('is-invalid');
                return;
            }

            // Validate CAPTCHA
            if (!captchaInputValue || captchaInputValue.toLowerCase() !== captchaText.toLowerCase()) {
                captchaInput.classList.add('is-invalid');
                captchaInput.nextElementSibling.textContent = 'CAPTCHA does not match. Please try again.';
                generateCaptcha(); // Generate new CAPTCHA after failed attempt
                return;
            }

            // Get CSRF token
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

            // Prepare form data
            const formData = new FormData();
            formData.append('Username', username);
            formData.append('Password', password);
            formData.append('RememberMe', rememberMe);
            formData.append('__RequestVerificationToken', token);

            // Send login request
            fetch('/Account/LoginAjax', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                },
                body: formData
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        // Redirect or refresh page on success
                        window.location.reload();
                    } else {
                        // Show error message
                        loginErrorMessage.textContent = data.message || 'Invalid username or password.';
                        loginErrorMessage.classList.remove('d-none');
                        generateCaptcha(); // Generate new CAPTCHA after failed attempt
                    }
                })
                .catch(error => {
                    console.error('Login error:', error);
                    loginErrorMessage.textContent = 'An error occurred during login. Please try again.';
                    loginErrorMessage.classList.remove('d-none');
                    generateCaptcha(); // Generate new CAPTCHA after error
                });
        });
    }

    // Generate CAPTCHA when modal is shown
    if (loginModal) {
        loginModal.addEventListener('shown.bs.modal', function () {
            generateCaptcha();
            document.getElementById('modalUsername').focus();
        });
    }
});