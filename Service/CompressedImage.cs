using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace SmartRent.Service
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(IFormFile file, string folder);
        void DeleteImage(string imageUrl);
    }

    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _env;

        public ImageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveImageAsync(IFormFile file, string folder)
        {
            var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadPath = Path.Combine(root, "uploads", folder);

            Directory.CreateDirectory(uploadPath);

            var fileName = $"{Guid.NewGuid()}.jpg";
            var fullPath = Path.Combine(uploadPath, fileName);

            using var image = await Image.LoadAsync(file.OpenReadStream());

            if (image.Width > 1200)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(1200, 0)
                }));
            }

            await image.SaveAsync(fullPath, new JpegEncoder { Quality = 75 });

            return $"/uploads/{folder}/{fileName}";
        }

        public void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || imageUrl.Contains("default.png"))
                return;

            var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var relativePath = imageUrl.StartsWith("http")
                ? new Uri(imageUrl).LocalPath
                : imageUrl;

            var fullPath = Path.Combine(root, relativePath.TrimStart('/'));

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
