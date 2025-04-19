namespace Athrna.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public int StatusCode { get; set; } = 500;
        public string ErrorMessage { get; set; } = "An unexpected error occurred.";
        public string? ExceptionType { get; set; }
        public string? ExceptionPath { get; set; }
        public bool ShowDetails { get; set; } = false;
    }
}
