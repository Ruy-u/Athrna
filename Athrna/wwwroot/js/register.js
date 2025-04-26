document.addEventListener('DOMContentLoaded', function () {
    // Handle successful registration
    const successMessage = document.querySelector('.alert-success');
    if (successMessage) {
        // Add highlight class for animation
        successMessage.classList.add('highlight');

        // Scroll to the success message
        successMessage.scrollIntoView({ behavior: 'smooth', block: 'center' });

        // Reset form on successful registration
        const form = document.querySelector('form');
        form.reset();

        // Show additional information based on registration type
        const isGuideRegistration = successMessage.textContent.includes('guide application');

        // Create "What's Next" section
        const whatNextSection = document.createElement('div');
        whatNextSection.className = 'registration-success-section';

        const successIcon = document.createElement('div');
        successIcon.className = 'success-icon';
        successIcon.innerHTML = '✓';

        const whatNextTitle = document.createElement('h4');
        whatNextTitle.textContent = "What's Next?";

        const whatNextList = document.createElement('ul');
        whatNextList.className = 'what-next-list';

        // Common steps
        const emailField = document.querySelector('#email');
        const emailValue = emailField ? emailField.value : 'your email';

        whatNextList.innerHTML = `
            <li>Check your email (${emailValue}) for a verification link</li>
            <li>Click the verification link to activate your account</li>
            <li>Once verified, you can log in to your account</li>
        `;

        // Add guide-specific steps
        if (isGuideRegistration) {
            whatNextList.innerHTML += `
                <li>Your guide application will be reviewed by our administrators</li>
                <li>You will receive an email notification regarding your application status</li>
                <li>Once approved, you'll be able to access guide-specific features</li>
            `;
        }

        // Assemble and insert the section
        whatNextSection.appendChild(successIcon);
        whatNextSection.appendChild(whatNextTitle);
        whatNextSection.appendChild(whatNextList);

        // Insert after success message
        successMessage.after(whatNextSection);

        // Reset guide form section
        const guideCheckbox = document.getElementById('registerAsGuide');
        const guideDetailsSection = document.getElementById('guideDetailsSection');

        if (guideCheckbox && guideDetailsSection) {
            guideCheckbox.checked = false;
            guideDetailsSection.style.display = 'none';
        }
    }

    // Toggle guide details section
    const guideCheckbox = document.getElementById('registerAsGuide');
    const guideDetailsSection = document.getElementById('guideDetailsSection');
    const guideFields = document.querySelectorAll('.guide-field');

    if (guideCheckbox && guideDetailsSection) {
        guideCheckbox.addEventListener('change', function () {
            guideDetailsSection.style.display = this.checked ? 'block' : 'none';

            // Make guide fields required when checkbox is checked
            guideFields.forEach(field => {
                if (this.checked) {
                    field.setAttribute('required', 'required');
                } else {
                    field.removeAttribute('required');
                    // Clear validation errors when unchecked
                    field.classList.remove('is-invalid');
                    const errorSpan = field.nextElementSibling;
                    if (errorSpan && errorSpan.classList.contains('text-danger')) {
                        errorSpan.textContent = '';
                    }
                }
            });
        });
    }

    // Password visibility toggles
    document.querySelectorAll('.password-toggle').forEach(button => {
        button.addEventListener('click', function () {
            const input = this.previousElementSibling;
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
    });

    // Password strength meter
    const passwordField = document.getElementById('password');
    const strengthMeter = document.querySelector('.password-strength-meter');
    const progressBar = document.querySelector('.progress-bar');
    const strengthText = document.querySelector('.strength-text');

    if (passwordField && strengthMeter && progressBar && strengthText) {
        // Show strength meter when password field is focused
        passwordField.addEventListener('focus', function () {
            strengthMeter.style.display = 'block';
        });

        // Update strength meter on input
        passwordField.addEventListener('input', function () {
            const password = this.value;
            let strength = 0;

            if (password.length >= 8) strength += 20;
            if (password.length >= 12) strength += 10;
            if (/[a-z]/.test(password)) strength += 10;
            if (/[A-Z]/.test(password)) strength += 20;
            if (/\d/.test(password)) strength += 20;
            if (/[^a-zA-Z0-9]/.test(password)) strength += 20;

            // Update progress bar
            progressBar.style.width = `${strength}%`;
            progressBar.setAttribute('aria-valuenow', strength);

            // Update text and color
            if (strength < 40) {
                progressBar.className = 'progress-bar bg-danger';
                strengthText.textContent = 'Weak';
                strengthText.className = 'text-danger';
            } else if (strength < 70) {
                progressBar.className = 'progress-bar bg-warning';
                strengthText.textContent = 'Moderate';
                strengthText.className = 'text-warning';
            } else {
                progressBar.className = 'progress-bar bg-success';
                strengthText.textContent = 'Strong';
                strengthText.className = 'text-success';
            }
        });
    }

    // Form validation
    const form = document.querySelector('form');
    if (form) {
        form.addEventListener('submit', function (e) {
            // Clear guide fields if not registering as guide
            if (!guideCheckbox.checked) {
                guideFields.forEach(field => {
                    field.value = '';
                });
            }
        });
    }
});