using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FazaBoa_API.Services
{
    public class PhotoService
    {
        private readonly ILogger<PhotoService> _logger;

        public PhotoService(ILogger<PhotoService> logger)
        {
            _logger = logger;
        }

        public async Task<string> UploadPhotoAsync(IFormFile photo, string userId)
        {
            if (photo == null || photo.Length == 0)
            {
                throw new ArgumentException("The photo is empty.", nameof(photo));
            }

            // Validar tipo de arquivo
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Invalid file type.");
            }

            // Validar MIME type
            var allowedMimeTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedMimeTypes.Contains(photo.ContentType))
            {
                throw new InvalidOperationException("Invalid MIME type.");
            }

            // Validar tamanho do arquivo (exemplo: 2 MB)
            if (photo.Length > 2 * 1024 * 1024)
            {
                throw new InvalidOperationException("File size exceeds the limit.");
            }

            var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profile-photos");
            Directory.CreateDirectory(uploadsFolderPath);

            var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            return $"/profile-photos/{fileName}";
        }
    }
}
