// Login Modal Functionality
document.addEventListener('DOMContentLoaded', function () {
    const loginButton = document.getElementById("loginButton");
    const loginModal = document.getElementById("loginModal");
    const closeModal = document.getElementById("closeModal");
    const loginForm = document.getElementById("loginForm");

    if (loginButton && loginModal && closeModal && loginForm) {
        loginButton.addEventListener("click", () => {
            loginModal.style.display = "flex";
        });

        closeModal.addEventListener("click", () => {
            loginModal.style.display = "none";
        });

        // Close modal when clicking outside of it
        window.addEventListener("click", (event) => {
            if (event.target === loginModal) {
                loginModal.style.display = "none";
            }
        });

        // Form submission
        loginForm.addEventListener("submit", (event) => {
            event.preventDefault();
            const username = document.getElementById("username").value;
            const password = document.getElementById("password").value;

            // Get the anti-forgery token
            const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
            let token = '';
            if (tokenElement) {
                token = tokenElement.value;
            }

            // Create form data to submit
            const formData = new FormData();
            formData.append('Username', username);
            formData.append('Password', password);

            // Redirect to the login page instead of using fetch
            window.location.href = '/Account/Login';

            loginModal.style.display = "none";
        });
    }
});