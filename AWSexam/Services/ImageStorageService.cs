using Amazon.S3;
using Amazon.S3.Model;

namespace AWSexam.Services
{
    public class ImageStorageService
    {
        private const long MaxSize = 5 * 1024 * 1024; // 5 MB
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucket;

        public ImageStorageService(IAmazonS3 s3Client, IConfiguration config)
        {
            _s3Client = s3Client;
            _bucket = config["S3:BucketName"];
        }
        private readonly Dictionary<string, string> AllowedTypes = new()
        {
            ["image/jpeg"] = "jpg",
            ["image/png"] = "png",
            
        };
        public string? Validate(IFormFile file)
        {
            if(file.Length > MaxSize)
            {
                return "Файл завеликий. максимальний розмір 5 MB";
            }
            if(!AllowedTypes.ContainsKey(file.ContentType))
            {
                return "Неприпустимий тип файлу";
            }
            return null;
        }

        public async Task<string> Upload(IFormFile file)
        {
            var key = $"products/{Guid.NewGuid()}";
            await using var stream = file.OpenReadStream();
            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType,
            });
            return key;
        }
        public string? GetUrl(string? key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            return _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(1)
            });
        }
    }
    }
