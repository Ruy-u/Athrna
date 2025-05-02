/**
 * Login Modal JavaScript
 * This script handles the login modal functionality
 */
document.addEventListener('DOMContentLoaded', function () {
    // Get DOM elements
    const loginModal = document.getElementById('loginModal');
    const loginModalForm = document.getElementById('loginModalForm');
    const loginButtons = document.querySelectorAll('.login-btn');
    const modalUsername = document.getElementById('modalUsername');
    const modalPassword = document.getElementById('modalPassword');
    const passwordToggles = document.querySelectorAll('.password-toggle');
    const captchaImage = document.getElementById('captchaImage');
    const refreshCaptchaBtn = document.getElementById('refreshCaptcha');
    const loginErrorMessage = document.getElementById('loginErrorMessage');

    // Bootstrap Modal object
    let modalInstance = null;

    if (loginModal) {
        modalInstance = new bootstrap.Modal(loginModal);
    }

    // Add click event listeners to all login buttons
    if (loginButtons) {
        loginButtons.forEach(button => {
            button.addEventListener('click', function (e) {
                e.preventDefault();
                if (modalInstance) {
                    // Generate CAPTCHA when modal opens
                    generateCaptcha();
                    modalInstance.show();
                } else {
                    // Fallback if modal isn't working
                    window.location.href = '/Account/Login';
                }
            });
        });
    }

    // Handle password visibility toggle
    if (passwordToggles) {
        passwordToggles.forEach(toggle => {
            toggle.addEventListener('click', function () {
                const passwordInput = this.previousElementSibling;
                const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
                passwordInput.setAttribute('type', type);

                // Toggle eye icon
                const icon = this.querySelector('i');
                if (icon) {
                    if (type === 'text') {
                        icon.classList.remove('bi-eye');
                        icon.classList.add('bi-eye-slash');
                    } else {
                        icon.classList.remove('bi-eye-slash');
                        icon.classList.add('bi-eye');
                    }
                }
            });
        });
    }

    // Generate and refresh CAPTCHA
    function generateCaptcha() {
        if (captchaImage) {
            const captchaChars = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ';
            let captcha = '';
            for (let i = 0; i < 6; i++) {
                captcha += captchaChars.charAt(Math.floor(Math.random() * captchaChars.length));
            }
            captchaImage.textContent = captcha;

            // Store captcha in sessionStorage for validation
            sessionStorage.setItem('captcha', captcha);
        }
    }

    // Add event listener for CAPTCHA refresh button
    if (refreshCaptchaBtn) {
        refreshCaptchaBtn.addEventListener('click', function () {
            generateCaptcha();
        });
    }

    // Handle form submission
    if (loginModalForm) {
        loginModalForm.addEventListener('submit', function (e) {
            e.preventDefault();

            // Simple validation
            if (!modalUsername.value || !modalPassword.value) {
                showError('Please enter both username and password.');
                return;
            }

            // Get CSRF token
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

            // Prepare form data
            const formData = new FormData();
            formData.append('Username', modalUsername.value);
            formData.append('Password', modalPassword.value);
            formData.append('RememberMe', document.getElementById('modalRememberMe').checked);
            formData.append('__RequestVerificationToken', token);

            // Send AJAX request
            fetch('/Account/LoginAjax', {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': token
                }
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        // Successful login
                        modalInstance.hide();

                        // Store admin status if applicable
                        if (data.isAdmin) {
                            localStorage.setItem('isAdmin', 'true');
                            localStorage.setItem('adminRoleLevel', data.adminRoleLevel);
                        }

                        // Redirect to appropriate page
                        window.location.href = data.redirectUrl || '/';
                    } else {
                        // Handle errors
                        if (data.requireVerification) {
                            showError('Please verify your email before logging in.');
                        } else if (data.isBanned) {
                            showError('Your account has been suspended. Please contact support.');
                        } else {
                            showError(data.message || 'Invalid username or password');
                        }
                    }
                })
                .catch(error => {
                    console.error('Login error:', error);
                    showError('An error occurred during login. Please try again.');
                });
        });
    }

    // Helper function to show error messages
    function showError(message) {
        if (loginErrorMessage) {
            loginErrorMessage.textContent = message;
            loginErrorMessage.classList.remove('d-none');
        }
    }
});