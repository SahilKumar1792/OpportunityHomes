using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace OpportunityHomes.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _recaptchaSiteKey;

        public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _recaptchaSiteKey = _configuration["Recaptcha:SiteKey"];
        }

        [BindProperty]
        public FormData FormInput { get; set; }


        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || FormInput == null)
            {
                return new JsonResult(new { isSuccess = false, message = "Invalid form data." });
            }

            // Get reCAPTCHA token from the form (either v3 or v2)
            var recaptchaToken = Request.Form["recaptchaResponse"];
            if (string.IsNullOrEmpty(recaptchaToken))
            {
                return new JsonResult(new { isSuccess = false, message = "reCAPTCHA token is missing." });
            }

            bool recaptchaValid = false;

            // Check if it's reCAPTCHA v3 (score-based) or v2 (checkbox-based)
            if (recaptchaToken.ToString().Length > 50) // Typical token length for reCAPTCHA v3
            {
                recaptchaValid = await VerifyRecaptchaV3(recaptchaToken);
            }
            else
            {
                recaptchaValid = await VerifyRecaptchaV2(recaptchaToken);
            }

            // If the reCAPTCHA validation failed
            if (!recaptchaValid)
            {
                return new JsonResult(new { isSuccess = false, message = "reCAPTCHA validation failed." });
            }

            try
            {
                // Save form data (e.g., to CSV)
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "submissions.csv");
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine($"\"{FormInput.FirstName}\",\"{FormInput.LastName}\",\"{FormInput.Email}\",\"{FormInput.CompanyName}\",\"{FormInput.CompanyWebsite}\"");
                }

                return new JsonResult(new { isSuccess = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { isSuccess = false, message = ex.Message });
            }
        }

        // Method to verify reCAPTCHA v2 response
        private async Task<bool> VerifyRecaptchaV2(string recaptchaToken)
        {
            var client = _httpClientFactory.CreateClient();
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _recaptchaSiteKey),
                new KeyValuePair<string, string>("response", recaptchaToken)
            });

            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", requestContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var recaptchaResult = JsonSerializer.Deserialize<RecaptchaResponse>(jsonResponse);

            return recaptchaResult?.Success == true;
        }

        // Method to verify reCAPTCHA v3 response (with score threshold)
        private async Task<bool> VerifyRecaptchaV3(string recaptchaToken)
        {
            var client = _httpClientFactory.CreateClient();
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _recaptchaSiteKey),
                new KeyValuePair<string, string>("response", recaptchaToken)
            });

            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", requestContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var recaptchaResult = JsonSerializer.Deserialize<RecaptchaResponse>(jsonResponse);

            // Check if the response is valid and the score is above threshold (0.5)
            return recaptchaResult?.Success == true && recaptchaResult.Score >= 0.5m;
        }

        // FormData class to bind the form fields
        public class FormData
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string CompanyName { get; set; }
            public string CompanyWebsite { get; set; }
        }

        // RecaptchaResponse class to deserialize the response from Google's reCAPTCHA API
        public class RecaptchaResponse
        {
            public bool Success { get; set; }
            public decimal Score { get; set; }
        }
    }
}
