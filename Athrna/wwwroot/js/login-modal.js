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

    // Bootstrap Modal instance
    let modalInstance = null;

    if (loginModal) {
        modalInstance = new bootstrap.Modal(loginModal);
    }

    // Add click event listeners to all login buttons
    loginButtons?.forEach(button => {
        button.addEventListener('click', function (e) {
            e.preventDefault();
            if (modalInstance) {
                generateCaptcha();
                modalInstance.show();
            } else {
                window.location.href = '/Account/Login';
            }
        });
    });

    // Handle password visibility toggle
    passwordToggles?.forEach(toggle => {
        toggle.addEventListener('click', function () {
            const passwordInput = this.previousElementSibling;
            const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordInput.setAttribute('type', type);

            const icon = this.querySelector('i');
            if (icon) {
                icon.classList.toggle('bi-eye');
                icon.classList.toggle('bi-eye-slash');
            }
        });
    });

    // Generate and refresh CAPTCHA
    function generateCaptcha() {
        if (captchaImage) {
            const captchaChars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'; // Removed easily confused chars
            let captcha = '';
            captchaImage.innerHTML = ''; // Clear previous CAPTCHA

            // Create the CAPTCHA content
            for (let i = 0; i < 6; i++) {
                const char = captchaChars.charAt(Math.floor(Math.random() * captchaChars.length));
                captcha += char;

                const span = document.createElement('span');
                span.textContent = char;
                span.style.display = 'inline-block';
                span.style.transform = `rotate(${Math.floor(Math.random() * 60 - 30)}deg)`; // Random rotation
                span.style.position = 'relative';
                span.style.top = `${Math.floor(Math.random() * 6 - 3)}px`; // Random Y displacement
                span.style.margin = '0 2px';
                span.style.fontWeight = 'bold';
                span.style.fontSize = '1.6rem';
                span.style.color = '#222'; // Default color for light mode
                span.style.textShadow = `${Math.floor(Math.random() * 3)}px ${Math.floor(Math.random() * 2)}px 1px rgba(0,0,0,0.2)`; // Shadow for depth

                // Add line through letters
                const lineThrough = document.createElement('div');
                lineThrough.style.position = 'absolute';
                lineThrough.style.top = '50%';
                lineThrough.style.left = '0';
                lineThrough.style.width = '100%';
                lineThrough.style.height = '1px';
                lineThrough.style.backgroundColor = 'rgba(0, 0, 0, 0.3)';
                span.appendChild(lineThrough);

                captchaImage.appendChild(span);

                // Adding random blemishes
                if (Math.random() > 0.7) {
                    const blemish = document.createElement('div');
                    blemish.style.position = 'absolute';
                    blemish.style.top = `${Math.random() * 40 - 20}px`;
                    blemish.style.left = `${Math.random() * 40 - 20}px`;
                    blemish.style.width = '4px';
                    blemish.style.height = '4px';
                    blemish.style.backgroundColor = 'rgba(0,0,0,0.1)';
                    blemish.style.borderRadius = '50%';
                    captchaImage.appendChild(blemish);
                }
            }

            sessionStorage.setItem('captcha', captcha);
        }
    }

    // Refresh CAPTCHA on button click
    refreshCaptchaBtn?.addEventListener('click', function () {
        generateCaptcha();
    });

    // Handle form submission
    loginModalForm?.addEventListener('submit', function (e) {
        e.preventDefault();

        if (!modalUsername.value || !modalPassword.value) {
            showError('Please enter both username and password.');
            return;
        }

        // CAPTCHA validation (optional frontend check)
        const captchaInput = document.getElementById('captchaInput');
        const storedCaptcha = sessionStorage.getItem('captcha');
        if (captchaInput && captchaInput.value.trim().toUpperCase() !== storedCaptcha) {
            showError('CAPTCHA does not match.');
            return;
        }

        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        const formData = new FormData();
        formData.append('Username', modalUsername.value);
        formData.append('Password', modalPassword.value);
        formData.append('RememberMe', document.getElementById('modalRememberMe').checked);
        formData.append('__RequestVerificationToken', token);

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
                    modalInstance.hide();

                    if (data.isAdmin) {
                        localStorage.setItem('isAdmin', 'true');
                        localStorage.setItem('adminRoleLevel', data.adminRoleLevel);
                    }

                    if (data.isGuide) {
                        localStorage.setItem('isGuide', 'true');
                    }

                    let redirectUrl = data.redirectUrl || '/';
                    if (data.isAdmin) {
                        redirectUrl = '/Admin';
                    } else if (data.isGuide) {
                        redirectUrl = '/GuideDashboard';
                    }

                    window.location.href = redirectUrl;
                } else {
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

    // Show error message
    function showError(message) {
        if (loginErrorMessage) {
            loginErrorMessage.textContent = message;
            loginErrorMessage.classList.remove('d-none');
        }
    }
});
