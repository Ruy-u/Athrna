using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Athrna.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TextToSpeechController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TextToSpeechController> _logger;

        public TextToSpeechController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<TextToSpeechController> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SynthesizeSpeech([FromBody] TextToSpeechRequest request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrEmpty(request.Text))
                {
                    return BadRequest("Text is required");
                }

                // Get API key from configuration
                var apiKey = _configuration["GoogleCloud:TextToSpeechApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("Google Cloud Text-to-Speech API key not configured");
                    return StatusCode(500, "API configuration error");
                }

                // Prepare request to Google Cloud Text-to-Speech API
                var client = _httpClientFactory.CreateClient();
                var requestUrl = "https://texttospeech.googleapis.com/v1/text:synthesize";

                // Set language code appropriately based on request
                var languageCode = request.LanguageCode ?? "en-US";
                switch (request.LanguageCode)
                {
                    case "ar":
                        languageCode = "ar-XA";
                        break;
                    case "fr":
                        languageCode = "fr-FR";
                        break;
                    case "de":
                        languageCode = "de-DE";
                        break;
                    case "es":
                        languageCode = "es-ES";
                        break;
                    default:
                        languageCode = "en-US";
                        break;
                }

                // Build the request payload
                var googleRequest = new
                {
                    input = new
                    {
                        text = request.Text
                    },
                    voice = new
                    {
                        languageCode = languageCode,
                        name = request.VoiceName,
                        ssmlGender = request.SsmlGender
                    },
                    audioConfig = new
                    {
                        audioEncoding = "MP3"
                    }
                };

                // Log API request (without sensitive data)
                _logger.LogInformation("Calling Google Cloud Text-to-Speech API for language: {LanguageCode}", languageCode);

                // Send request to Google API
                var content = new StringContent(JsonSerializer.Serialize(googleRequest), Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("X-Goog-Api-Key", apiKey);

                var response = await client.PostAsync(requestUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Google API returned error: {ErrorContent}", errorContent);
                    return StatusCode((int)response.StatusCode, "Google API returned an error");
                }

                // Process successful response
                var responseContent = await response.Content.ReadAsStringAsync();
                return Ok(JsonSerializer.Deserialize<JsonElement>(responseContent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing text-to-speech request");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }
    }

    public class TextToSpeechRequest
    {
        public string Text { get; set; }
        public string LanguageCode { get; set; }
        public string VoiceName { get; set; }
        public string SsmlGender { get; set; }
    }
}