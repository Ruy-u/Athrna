// Get elements
const loginModal = document.getElementById('loginModal');
const loginButtons = document.querySelectorAll('.login-btn');
const loginForm = document.getElementById('loginModalForm');
const errorMessage = document.getElementById('loginErrorMessage');
const captchaInput = document.getElementById('captchaInput');
const captchaImage = document.getElementById('captchaImage');
const refreshCaptchaBtn = document.getElementById('refreshCaptcha');
const passwordToggle = document.querySelector('.password-toggle');

// Create Bootstrap modal instance if modal exists
let modal = null;
if (loginModal) {
    modal = new bootstrap.Modal(loginModal);
}

document.addEventListener('DOMContentLoaded', function () {
    // Submit login form via AJAX
    if (loginForm) {
        loginForm.addEventListener('submit', function (e) {
            e.preventDefault();

            // Validate form
            if (!this.checkValidity()) {
                e.stopPropagation();
                this.classList.add('was-validated');
                return;
            }

            const formData = new FormData(this);
            const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]').value;

            fetch('/Account/LoginAjax', {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': antiForgeryToken
                }
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        // Redirect to specified URL
                        window.location.href = data.redirectUrl;
                    } else {
                        // Check if email verification is required
                        if (data.requireVerification) {
                            // Show verification required message with link
                            errorMessage.classList.remove('d-none');
                            errorMessage.classList.remove('alert-danger');
                            errorMessage.classList.add('alert-warning');
                            errorMessage.innerHTML = `Please verify your email address before logging in. <a href="${data.verificationUrl}">Resend verification email</a>`;
                        } else {
                            // Show regular error message
                            errorMessage.classList.remove('d-none');
                            errorMessage.classList.add('alert-danger');
                            errorMessage.classList.remove('alert-warning');
                            errorMessage.textContent = data.message;
                        }

                        // Generate new CAPTCHA
                        if (captchaImage) {
                            generateCaptcha();
                        }
                    }
                })
                .catch(error => {
                    console.error('Error during login:', error);
                    errorMessage.classList.remove('d-none');
                    errorMessage.textContent = 'An error occurred. Please try again.';
                });
        });
    }

    // Generate and refresh CAPTCHA
    function generateCaptcha() {
        if (captchaImage) {
            const chars = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz';
            let captchaText = '';

            // Generate 6 random characters
            for (let i = 0; i < 6; i++) {
                captchaText += chars.charAt(Math.floor(Math.random() * chars.length));
            }

            // Create canvas for CAPTCHA
            const canvas = document.createElement('canvas');
            canvas.width = 150;
            canvas.height = 50;
            const ctx = canvas.getContext('2d');

            // Fill background
            ctx.fillStyle = '#f0f0f0';
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            // Add noise (dots)
            for (let i = 0; i < 100; i++) {
                ctx.fillStyle = `rgba(${Math.random() * 255},${Math.random() * 255},${Math.random() * 255},0.2)`;
                ctx.fillRect(Math.random() * canvas.width, Math.random() * canvas.height, 2, 2);
            }

            // Add lines for noise
            for (let i = 0; i < 4; i++) {
                ctx.strokeStyle = `rgba(${Math.random() * 255},${Math.random() * 255},${Math.random() * 255},0.5)`;
                ctx.beginPath();
                ctx.moveTo(Math.random() * canvas.width, Math.random() * canvas.height);
                ctx.lineTo(Math.random() * canvas.width, Math.random() * canvas.height);
                ctx.stroke();
            }

            // Draw CAPTCHA text
            ctx.fillStyle = '#333';
            ctx.font = 'bold 24px Arial';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';

            // Add each character with slight rotation for security
            for (let i = 0; i < captchaText.length; i++) {
                const x = 20 + i * 20;
                const y = canvas.height / 2 + Math.random() * 8 - 4;
                ctx.save();
                ctx.translate(x, y);
                ctx.rotate((Math.random() * 0.4) - 0.2); // Rotate between -0.2 and 0.2 radians
                ctx.fillText(captchaText[i], 0, 0);
                ctx.restore();
            }

            // Replace content of captchaImage with canvas
            captchaImage.innerHTML = '';
            captchaImage.appendChild(canvas);

            // Store CAPTCHA text in a data attribute (in a real app, store this on the server)
            captchaImage.dataset.captchaText = captchaText;
        }
    }

    // Handle refresh CAPTCHA button
    if (refreshCaptchaBtn) {
        refreshCaptchaBtn.addEventListener('click', function () {
            generateCaptcha();
            if (captchaInput) {
                captchaInput.value = '';
                captchaInput.focus();
            }
        });
    }

    // Initialize CAPTCHA when login modal is shown
    if (loginModal) {
        loginModal.addEventListener('shown.bs.modal', function () {
            if (captchaImage) {
                generateCaptcha();
            }
            // Focus on username field
            document.getElementById('modalUsername').focus();
        });
    }

    // Show login modal when login button is clicked
    if (loginButtons.length > 0 && loginModal) {
        loginButtons.forEach(button => {
            button.addEventListener('click', function (e) {
                e.preventDefault();
                if (modal) {
                    modal.show();

                    // Generate new CAPTCHA
                    if (captchaImage) {
                        generateCaptcha();
                    }
                }
            });
        });
    }

    // Toggle password visibility
    if (passwordToggle) {
        passwordToggle.addEventListener('click', function () {
            const input = document.getElementById('modalPassword');
            const type = input.getAttribute('type') === 'password' ? 'text' : 'password';
            input.setAttribute('type', type);

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
});
