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
            const password = document.getElementById("EncryptedPassword").value;

            // Here you would typically send this data to a server using AJAX
            // For MVC integration, we'll assume an Account controller endpoint

            fetch('/Account/Login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    username: username,
                    password: password
                })
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        alert("Login successful!");
                        window.location.reload();
                    } else {
                        alert("Login failed: " + data.message);
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    alert("An error occurred during login");
                });

            loginModal.style.display = "none";
        });
    }
});