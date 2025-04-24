using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Athrna.Services
{
    public class FileService
    {
        private readonly string _basePath;
        private readonly string _baseUrl;

        public FileService(IConfiguration configuration)
        {
            _basePath = configuration["FileStorage:BasePath"];
            _baseUrl = configuration["FileStorage:BaseUrl"];

            // Ensure base directory exists
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subDirectory)
        {
            if (file == null || file.Length == 0)
                return null;

            // Create full directory path
            string fullPath = Path.Combine(_basePath, subDirectory);

            // Create directory if it doesn't exist
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // Generate unique filename
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string uniqueFileName = $"{Guid.NewGuid().ToString("N")}{extension}";
            string filePath = Path.Combine(fullPath, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the URL path (not physical path)
            return $"{_baseUrl}{subDirectory}/{uniqueFileName}";
        }

        public void DeleteFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return;

            // Convert URL to physical path
            if (fileUrl.StartsWith(_baseUrl))
            {
                string relativePath = fileUrl.Substring(_baseUrl.Length);
                string fullPath = Path.Combine(_basePath, relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }
    }
}