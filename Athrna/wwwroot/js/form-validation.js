/**
 * Enhanced client-side form validation for Athrna
 * Provides real-time validation feedback and better user experience
 */
document.addEventListener('DOMContentLoaded', function () {
    // Form validation configuration
    const validationRules = {
        username: {
            minLength: 3,
            maxLength: 50,
            pattern: /^[a-zA-Z0-9_-]+$/,
            messages: {
                required: 'Username is required',
                minLength: 'Username must be at least 3 characters',
                maxLength: 'Username cannot be longer than 50 characters',
                pattern: 'Username can only contain letters, numbers, underscores and hyphens'
            }
        },
        email: {
            pattern: /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/,
            messages: {
                required: 'Email address is required',
                pattern: 'Please enter a valid email address'
            }
        },
        password: {
            minLength: 8,
            pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$/,
            messages: {
                required: 'Password is required',
                minLength: 'Password must be at least 8 characters',
                pattern: 'Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character'
            }
        }
    };

    // Find all forms that need validation
    const forms = document.querySelectorAll('form[data-validate="true"]');

    // Apply validation to each form
    forms.forEach(form => {
        // Get all input fields
        const inputFields = form.querySelectorAll('input, select, textarea');

        // Add validation event listeners to each field
        inputFields.forEach(field => {
            // Skip fields that don't need validation
            if (field.hasAttribute('data-validate-skip')) {
                return;
            }

            // Create feedback element if it doesn't exist
            let feedbackElement = form.querySelector(`#${field.id}-feedback`);
            if (!feedbackElement) {
                feedbackElement = document.createElement('div');
                feedbackElement.id = `${field.id}-feedback`;
                feedbackElement.className = 'invalid-feedback';
                field.parentNode.appendChild(feedbackElement);
            }

            // Add aria attributes for accessibility
            field.setAttribute('aria-describedby', feedbackElement.id);

            // Add event listeners for validation
            field.addEventListener('blur', function () {
                validateField(field, feedbackElement);
            });

            field.addEventListener('input', function () {
                // Clear validation when user is typing
                field.classList.remove('is-invalid');
                field.classList.remove('is-valid');
                feedbackElement.textContent = '';
            });
        });

        // Form submission validation
        form.addEventListener('submit', function (event) {
            let isValid = true;

            // Validate all fields
            inputFields.forEach(field => {
                // Skip fields that don't need validation
                if (field.hasAttribute('data-validate-skip')) {
                    return;
                }

                const feedbackElement = form.querySelector(`#${field.id}-feedback`);
                const fieldValid = validateField(field, feedbackElement);

                if (!fieldValid) {
                    isValid = false;
                }
            });

            // Prevent form submission if validation fails
            if (!isValid) {
                event.preventDefault();

                // Focus the first invalid field
                const firstInvalidField = form.querySelector('.is-invalid');
                if (firstInvalidField) {
                    firstInvalidField.focus();
                }

                // Show validation message
                const formFeedback = form.querySelector('.form-feedback');
                if (formFeedback) {
                    formFeedback.textContent = 'Please fix the errors in the form.';
                    formFeedback.style.display = 'block';
                    formFeedback.setAttribute('role', 'alert');
                }
            }
        });
    });

    /**
     * Validates a field based on its type and attributes
     * @param {HTMLElement} field - The field to validate
     * @param {HTMLElement} feedbackElement - The element to show feedback in
     * @returns {boolean} - Whether the field is valid
     */
    function validateField(field, feedbackElement) {
        // Get field type and value
        const fieldType = field.type;
        const fieldValue = field.value.trim();
        const fieldName = field.name || field.id;

        // Check if the field is required
        const isRequired = field.hasAttribute('required') || field.getAttribute('aria-required') === 'true';

        // Clear previous validation
        field.classList.remove('is-invalid');
        field.classList.remove('is-valid');
        feedbackElement.textContent = '';

        // Required field validation
        if (isRequired && fieldValue === '') {
            field.classList.add('is-invalid');

            // Get custom message or use default
            const fieldRules = validationRules[fieldName] || {};
            const message = fieldRules.messages?.required || 'This field is required';

            feedbackElement.textContent = message;
            return false;
        }

        // Skip further validation if field is empty and not required
        if (fieldValue === '' && !isRequired) {
            return true;
        }

        // Email validation
        if (fieldType === 'email') {
            const emailRules = validationRules.email;
            if (!emailRules.pattern.test(fieldValue)) {
                field.classList.add('is-invalid');
                feedbackElement.textContent = emailRules.messages.pattern;
                return false;
            }
        }

        // Username validation
        if (fieldName === 'username') {
            const usernameRules = validationRules.username;

            if (fieldValue.length < usernameRules.minLength) {
                field.classList.add('is-invalid');
                feedbackElement.textContent = usernameRules.messages.minLength;
                return false;
            }

            if (fieldValue.length > usernameRules.maxLength) {
                field.classList.add('is-invalid');
                feedbackElement.textContent = usernameRules.messages.maxLength;
                return false;
            }

            if (!usernameRules.pattern.test(fieldValue)) {
                field.classList.add('is-invalid');
                feedbackElement.textContent = usernameRules.messages.pattern;
                return false;
            }
        }

        // Password validation
        if (fieldType === 'password' && fieldName === 'password') {
            const passwordRules = validationRules.password;

            if (fieldValue.length < passwordRules.minLength) {
                field.classList.add('is-invalid');
                feedbackElement.textContent = passwordRules.messages.minLength;
                return false;
            }

            if (!passwordRules.pattern.test(fieldValue)) {
                field.classList.add('is-invalid');
                feedbackElement.textContent = passwordRules.messages.pattern;
                return false;
            }
        }

        // Password confirmation validation
        if (fieldName === 'confirmPassword') {
            const passwordField = document.getElementById('password') ||
                document.querySelector('input[name="password"]');

            if (passwordField && fieldValue !== passwordField.value) {
                field.classList.add('is-invalid');
                feedbackElement.textContent = 'Passwords do not match';
                return false;
            }
        }

        // Custom min/max validation for number fields
        if (fieldType === 'number') {
            const min = field.getAttribute('min');
            const max = field.getAttribute('max');

            if (min !== null && parseFloat(fieldValue) < parseFloat(min)) {
                field.classList.add('is-invalid');
                feedbackElement.textContent = `Value must be at least ${min}`;
                return false;
            }

            if (max !== null && parseFloat(fieldValue) > parseFloat(max)) {
                field.classList.add('is-invalid');
                feedbackElement.textContent = `Value must be at most ${max}`;
                return false;
            }
        }

        // Custom pattern validation
        const pattern = field.getAttribute('pattern');
        if (pattern) {
            const regex = new RegExp(pattern);
            if (!regex.test(fieldValue)) {
                field.classList.add('is-invalid');
                feedbackElement.textContent = field.getAttribute('data-pattern-message') || 'Invalid format';
                return false;
            }
        }

        // If we got here, the field is valid
        field.classList.add('is-valid');
        return true;
    }

    /**
     * Creates a password strength meter
     */
    const passwordFields = document.querySelectorAll('input[type="password"][data-show-strength="true"]');

    passwordFields.forEach(field => {
        // Create strength meter container
        const meterContainer = document.createElement('div');
        meterContainer.className = 'password-strength-meter mt-2';

        // Create meter bar
        const meter = document.createElement('div');
        meter.className = 'progress';
        meter.style.height = '5px';

        const meterBar = document.createElement('div');
        meterBar.className = 'progress-bar';
        meterBar.style.width = '0%';
        meterBar.setAttribute('role', 'progressbar');
        meterBar.setAttribute('aria-valuenow', '0');
        meterBar.setAttribute('aria-valuemin', '0');
        meterBar.setAttribute('aria-valuemax', '100');

        meter.appendChild(meterBar);
        meterContainer.appendChild(meter);

        // Create strength text
        const strengthText = document.createElement('small');
        strengthText.className = 'form-text mt-1';
        strengthText.id = `${field.id}-strength`;

        meterContainer.appendChild(strengthText);

        // Insert after the field
        field.parentNode.insertBefore(meterContainer, field.nextSibling);

        // Update meter on input
        field.addEventListener('input', function () {
            updatePasswordStrength(field.value, meterBar, strengthText);
        });
    });

    /**
     * Updates the password strength meter
     * @param {string} password - The password to evaluate
     * @param {HTMLElement} meterBar - The meter bar element
     * @param {HTMLElement} strengthText - The strength text element
     */
    function updatePasswordStrength(password, meterBar, strengthText) {
        // Calculate password strength
        let strength = 0;

        if (password.length >= 8) strength += 20;
        if (password.length >= 12) strength += 10;
        if (/[a-z]/.test(password)) strength += 10;
        if (/[A-Z]/.test(password)) strength += 20;
        if (/\d/.test(password)) strength += 20;
        if (/[^a-zA-Z0-9]/.test(password)) strength += 20;

        // Update meter
        meterBar.style.width = `${strength}%`;

        // Update text and color based on strength
        if (strength < 40) {
            meterBar.className = 'progress-bar bg-danger';
            strengthText.textContent = 'Weak password';
            strengthText.className = 'form-text text-danger mt-1';
        } else if (strength < 70) {
            meterBar.className = 'progress-bar bg-warning';
            strengthText.textContent = 'Moderate password';
            strengthText.className = 'form-text text-warning mt-1';
        } else {
            meterBar.className = 'progress-bar bg-success';
            strengthText.textContent = 'Strong password';
            strengthText.className = 'form-text text-success mt-1';
        }

        // Update ARIA attributes
        meterBar.setAttribute('aria-valuenow', strength);
    }

    /**
     * Adds show/hide password toggle buttons
     */
    const passwordInputs = document.querySelectorAll('input[type="password"][data-toggle-visibility="true"]');

    passwordInputs.forEach(input => {
        // Create toggle button
        const toggleButton = document.createElement('button');
        toggleButton.type = 'button';
        toggleButton.className = 'btn btn-outline-secondary password-toggle';
        toggleButton.innerHTML = '<i class="bi bi-eye" aria-hidden="true"></i>';
        toggleButton.setAttribute('aria-label', 'Show password');

        // Create input group if needed
        let inputGroup = input.parentElement;
        if (!inputGroup.classList.contains('input-group')) {
            const newInputGroup = document.createElement('div');
            newInputGroup.className = 'input-group';
            input.parentNode.insertBefore(newInputGroup, input);
            newInputGroup.appendChild(input);
            inputGroup = newInputGroup;
        }

        // Add button to input group
        inputGroup.appendChild(toggleButton);

        // Toggle password visibility
        toggleButton.addEventListener('click', function () {
            const type = input.getAttribute('type') === 'password' ? 'text' : 'password';
            input.setAttribute('type', type);

            // Update button icon and label
            if (type === 'text') {
                toggleButton.innerHTML = '<i class="bi bi-eye-slash" aria-hidden="true"></i>';
                toggleButton.setAttribute('aria-label', 'Hide password');
            } else {
                toggleButton.innerHTML = '<i class="bi bi-eye" aria-hidden="true"></i>';
                toggleButton.setAttribute('aria-label', 'Show password');
            }
        });
    });
});