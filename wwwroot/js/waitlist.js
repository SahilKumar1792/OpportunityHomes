$(function () {
    // Submit the form using AJAX
    $('#waitlistForm').on('submit', function (e) {
        e.preventDefault(); // Prevent the form from submitting normally

        if (validateForm()) {
            const submitButton = document.getElementById('submitButton');
            const thankYouMessage = document.getElementById('thankYouMessage');
            const recaptchaErrorMessage = document.getElementById('recaptchaErrorMessage'); // Element to show reCAPTCHA error

            // Clear previous errors
            submitButton.disabled = true;
            submitButton.textContent = "Please wait...";
            recaptchaErrorMessage.classList.add('d-none'); // Hide the error message initially

            // Execute reCAPTCHA to get a score
            grecaptcha.execute(_recaptchaSiteKey, { action: 'submit' }).then(function (token) {
                // Send the token to the server
                $('#recaptchaResponse').val(token);

                // Make AJAX request to submit the form
                var formData = $('#waitlistForm').serialize(); // Serialize form data
                $.ajax({
                    type: "POST",
                    url: "/Index",
                    data: formData,
                    success: function (response) {
                        if (response.isSuccess) {
                            submitButton.disabled = true;
                            submitButton.textContent = "You are on the Waitlist!";
                            thankYouMessage.classList.remove('d-none');
                        } else {
                            if (response.message && response.message.includes("reCAPTCHA")) {
                                // Show the reCAPTCHA v2 if the score was too low
                                recaptchaErrorMessage.classList.remove('d-none'); // Show the reCAPTCHA error message
                            } else {
                                console.log('An error occurred: ' + response.message);
                            }
                        }
                    },
                    error: function (error) {
                        alert('An error occurred: ' + error);
                    }
                });
            });
        }
    });
});
function validateField(input) {
    const errorMessage = input.nextElementSibling; // For the required error message
    const validEmailMessage = errorMessage.nextElementSibling; // For the valid email error message
    const label = input.previousElementSibling; // For the label element
    input.value = input.value.replace(/['\"]/g, '');
    // Check if the field is required and empty
    if (input.value.trim() === "") {
        // Add class only if not already present
        if (!label.classList.contains('text-red')) {
            label.classList.add('text-red'); // Highlight the label with red text
        }

        if (errorMessage.classList.contains('d-none')) {
            errorMessage.classList.remove('d-none'); // Show the required error message
        }

        if (!validEmailMessage.classList.contains('d-none')) {
            validEmailMessage.classList.add('d-none'); // Hide the valid email error message
        }
    } else {
        // Remove class only if it exists
        if (label.classList.contains('text-red')) {
            label.classList.remove('text-red'); // Remove red text from the label
        }

        if (!errorMessage.classList.contains('d-none')) {
            errorMessage.classList.add('d-none'); // Hide the required error message
        }

        // If the field is an email input, validate the email format
        if (input.name === "email") {


            if (!isValidEmailAddress(input.value)) {
                if (validEmailMessage.classList.contains('d-none')) {
                    validEmailMessage.classList.remove('d-none'); // Show invalid email error message
                }
            }
            else {
                if (!validEmailMessage.classList.contains('d-none')) {
                    validEmailMessage.classList.add('d-none'); // Hide the invalid email error message if valid
                }
            }
        }
    }
}


function validateForm() {
    let isValid = true; // Assume the form is valid to start
    const form = document.getElementById('waitlistForm');
    const inputs = form.querySelectorAll('input.required'); // Get all required inputs

    inputs.forEach((input) => {
        const errorMessage = input.nextElementSibling; // Get the <p> error message element
        const label = input.previousElementSibling; // Get the <label> element

        if (input.value.trim() === "") {
            // Field is invalid


            if (!label.classList.contains('text-red')) {
                label.classList.add('text-red'); // Highlight the label with red text
            }

            if (errorMessage.classList.contains('d-none')) {
                errorMessage.classList.remove('d-none'); // Show the error message
            }

            isValid = false; // Mark form as invalid
        }
        else if (input.name === "email") {
            let validEmailMessage = input.nextElementSibling.nextElementSibling;
            if (!isValidEmailAddress(input.value)) {
                if (validEmailMessage.classList.contains('d-none')) {
                    validEmailMessage.classList.remove('d-none'); // Show invalid email error message
                }
                isValid = false;
            }
            else {
                if (!validEmailMessage.classList.contains('d-none')) {
                    validEmailMessage.classList.add('d-none'); // Hide the invalid email error message if valid
                }
            }
        }
        else {
            // Field is valid


            if (label.classList.contains('text-red')) {
                label.classList.remove('text-red'); // Remove red text from the label
            }

            if (!errorMessage.classList.contains('d-none')) {
                errorMessage.classList.add('d-none'); // Hide the error message
            }
        }
    });

    return isValid;
}

function isValidEmailAddress(Email) {
    Email = Email.trim();
    var regex = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
    return regex.test(Email);
}