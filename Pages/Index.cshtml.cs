using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using ClosedXML.Excel;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpportunityHomes.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public readonly string _recaptchaSiteKey;
        private readonly string _recaptchaSecretKey;

        public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _recaptchaSiteKey = _configuration["Recaptcha:SiteKey"];
            _recaptchaSecretKey = _configuration["Recaptcha:SecretKey"];
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
            recaptchaValid = await VerifyRecaptchaV3(recaptchaToken);
            // If the reCAPTCHA validation failed
            if (!recaptchaValid)
            {
                return new JsonResult(new { isSuccess = false, message = "reCAPTCHA validation failed." });
            }

            try
            {
                SaveSubmissionToCsv();
                //SaveDataInTxtFormat();
                //SaveSubmissionToExcel();
                return new JsonResult(new { isSuccess = true });
            }
            catch (Exception ex)
            {
                LogError(ex);
                return new JsonResult(new { isSuccess = false, message = ex.Message });
            }
        }

        // Method to verify reCAPTCHA v2 response
        private async Task<bool> VerifyRecaptchaV2(string recaptchaToken)
        {
            var client = _httpClientFactory.CreateClient();
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _recaptchaSecretKey),
                new KeyValuePair<string, string>("response", recaptchaToken)
            });

            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", requestContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var recaptchaResult = JsonConvert.DeserializeObject<RecaptchaResponse>(jsonResponse);

            return recaptchaResult?.Success == true;
        }

        // Method to verify reCAPTCHA v3 response (with score threshold)
        private async Task<bool> VerifyRecaptchaV3(string recaptchaToken)
        {
            var client = _httpClientFactory.CreateClient();

            // Prepare request with the secret key and token
            var requestContent = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("secret", _recaptchaSecretKey),
        new KeyValuePair<string, string>("response", recaptchaToken)
    });

            // Call the reCAPTCHA API endpoint
            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", requestContent);

            if (!response.IsSuccessStatusCode)
            {
                // Log if the request itself fails (e.g., non-2xx status)
                LogError(new Exception("reCAPTCHA verification request failed"));
                return false;
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();

            // Deserialize the JSON response using Newtonsoft.Json (version 13.0.3)
            var recaptchaResult = JsonConvert.DeserializeObject<RecaptchaResponse>(jsonResponse);

            // Log the response for debugging purposes
            LogRecaptchaResponse(recaptchaResult);

            // Check for success and ensure the score is above a threshold (0.5 for v3)
            if (recaptchaResult?.Success == true && recaptchaResult.Score >= 0.5m)
            {
                return true;
            }

            return false;
        }

        private void SaveSubmissionToCsv()
        {
            try
            {
                // Define the file path
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "submissions.csv");

                // Ensure the directory exists
                var directoryPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                // Check if the file exists
                if (!System.IO.File.Exists(filePath))
                {
                    // Write the header to the file if it doesn't exist
                    System.IO.File.AppendAllText(filePath, "\"TimesStamp\",\"FirstName\",\"LastName\",\"Email\",\"CompanyName\",\"CompanyWebsite\"" + Environment.NewLine);
                }
                string timestamp = DateTime.UtcNow.ToString("MM/dd/yyyy hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
                // Write the data to the file (each new submission is appended)
                System.IO.File.AppendAllText(filePath, $"\"{timestamp}\",\"{FormInput.FirstName}\",\"{FormInput.LastName}\",\"{FormInput.Email}\",\"{FormInput.CompanyName}\",\"{FormInput.CompanyWebsite}\"" + Environment.NewLine);
            }
            catch (Exception)
            {
                throw;
            }
        }
        // Method to save the submission to a TXT file
        private void SaveDataInTxtFormat()
        {
            try
            {


                int firstNameColumnWidth = 15;
                int lastNameColumnWidth = 15;
                int otherColumnWidth = 25;

                // Construct the header
                string header = $"{PadRight("First Name", firstNameColumnWidth)}" +
                                $"{PadRight("Last Name", lastNameColumnWidth)}" +
                                $"{PadRight("Email", otherColumnWidth)}" +
                                $"{PadRight("Company Name", otherColumnWidth)}" +
                                $"{PadRight("Company Website", otherColumnWidth)}";

                // Construct the data
                string data = $"{PadRight(FormInput.FirstName, firstNameColumnWidth)}" +
                              $"{PadRight(FormInput.LastName, lastNameColumnWidth)}" +
                              $"{PadRight(FormInput.Email, otherColumnWidth)}" +
                              $"{PadRight(FormInput.CompanyName, otherColumnWidth)}" +
                              $"{PadRight(FormInput.CompanyWebsite = string.IsNullOrEmpty(FormInput.CompanyWebsite) ? "" : FormInput.CompanyWebsite, otherColumnWidth)}";

                // Define the file path
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "submissions.txt");
                
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
               
                // Check if the file exists
                if (!System.IO.File.Exists(filePath))
                {
                    // Write headers only once if the file doesn't exist
                    System.IO.File.AppendAllText(filePath, header + Environment.NewLine);
                }

                // Append the data below the headers
                System.IO.File.AppendAllText(filePath, data + Environment.NewLine);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static string PadRight(string text, int length)
        {
            return text.Length < length ? text.PadRight(length) : text.Substring(0, length);
        }
        private void SaveSubmissionToExcel()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "submission.xlsx");

            // Create the file if it doesn't exist
            if (!System.IO.File.Exists(filePath))
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.AddWorksheet("Submissions");

                    // Manually set headers
                    worksheet.Cell(1, 1).Value = "First Name";
                    worksheet.Cell(1, 2).Value = "Last Name";
                    worksheet.Cell(1, 3).Value = "Email";
                    worksheet.Cell(1, 4).Value = "Company Name";
                    worksheet.Cell(1, 5).Value = "Company Website";

                    // Apply formatting for headers
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;  // Bold headers
                    headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;  // Center align headers
                    headerRow.Style.Fill.SetBackgroundColor(XLColor.LightGray);  // Set header background color

                    // Set column widths to fit the data
                    worksheet.Column(1).Width = 20;
                    worksheet.Column(2).Width = 20;
                    worksheet.Column(3).Width = 30;
                    worksheet.Column(4).Width = 30;
                    worksheet.Column(5).Width = 40;

                    // Save the workbook
                    workbook.SaveAs(filePath);
                }
            }

            // Append the data
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);

                // Find the next empty row
                int row = worksheet.LastRowUsed().RowNumber() + 1;

                // Manually insert the data in columns, ensuring that the values are not split
                var dataValues = new[] {
      FormInput.FirstName,
      FormInput.LastName,
      FormInput.Email,
      FormInput.CompanyName,
      FormInput.CompanyWebsite
  };

                for (int i = 0; i < dataValues.Length; i++)
                {
                    worksheet.Cell(row, i + 1).Value = dataValues[i];
                }

                // Apply formatting to the newly added data row
                var dataRow = worksheet.Row(row);
                dataRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;  // Center align data

                workbook.SaveAs(filePath);
            }
        }
        // FormData class to bind the form fields
        public class FormData
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string CompanyName { get; set; }
            public string? CompanyWebsite { get; set; }
        }

        // RecaptchaResponse class to deserialize the response from Google's reCAPTCHA API
        public class RecaptchaResponse
        {
            public bool Success { get; set; }
            public decimal Score { get; set; }
            public string Action { get; set; }
            public string ChallengeTs { get; set; }
            public string Hostname { get; set; }
        }
        private static void LogError(Exception ex)
        {
            string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ErrorLogs.txt");
            using var writer = new StreamWriter(logFilePath, true);
            writer.WriteLine("********* ERROR OCCURRED ********* {0}", DateTime.Now);
            writer.WriteLine(ex.ToString());
            writer.WriteLine();
        }
        private static void LogRecaptchaResponse(RecaptchaResponse res)
        {
            string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "LogRecaptchaResponse.txt");
            using var writer = new StreamWriter(logFilePath, true);
            writer.WriteLine("********* Recaptcha Response ********* {0}", DateTime.Now);
            writer.WriteLine(string.Format("IsSuccess: {0}, Score: {1}", res.Success.ToString(), res.Score.ToString()));
            writer.WriteLine();
        }
    }
}
