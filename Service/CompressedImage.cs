using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace SmartRent.Service
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(IFormFile file, string folderName);
        void DeleteImage(string imageUrl);
    }

    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;

        public ImageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveImageAsync(IFormFile file, string folderName)
        {
            string webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadsFolder = Path.Combine(webRootPath, "uploads", folderName);

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = $"{Guid.NewGuid()}.jpg";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var image = await Image.LoadAsync(file.OpenReadStream());

            // Resize Logic
            int maxWidth = 1200;
            if (image.Width > maxWidth)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(maxWidth, 0),
                    Mode = ResizeMode.Max
                }));
            }

            // Compress Logic
            var encoder = new JpegEncoder { Quality = 75 };
            await image.SaveAsync(filePath, encoder);

            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        public void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || imageUrl.Contains("default.png")) return;

            string webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, imageUrl.TrimStart('/'));

            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }
}