(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        // 1. Double-submit protection on all POST forms
        var forms = document.querySelectorAll('form[method="post"], form[method="POST"]');
        forms.forEach(function (form) {
            form.addEventListener("submit", function (e) {
                if (form.dataset.submitting === "true") {
                    e.preventDefault();
                    return;
                }
                if (typeof form.checkValidity === "function" && !form.checkValidity()) {
                    return;
                }
                form.dataset.submitting = "true";
                var submitBtns = form.querySelectorAll('button[type="submit"], input[type="submit"]');
                submitBtns.forEach(function (btn) {
                    btn.disabled = true;
                });
            });
        });

        // 2. Newsletter subscription feedback
        var newsletterForm = document.getElementById("newsletter-form");
        var newsletterStatus = document.getElementById("newsletter-status");
        if (newsletterForm && newsletterStatus) {
            newsletterForm.addEventListener("submit", function (e) {
                e.preventDefault();
                var emailInput = newsletterForm.querySelector('input[type="email"]');
                if (emailInput && emailInput.value.trim() !== "") {
                    newsletterStatus.textContent = "Subscribed. You will receive upcoming editorial previews.";
                    newsletterStatus.classList.remove("d-none");
                    emailInput.value = "";
                }
            });
        }
    });
})();
