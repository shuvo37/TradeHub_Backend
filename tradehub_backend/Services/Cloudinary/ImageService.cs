using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace TradeHub.Services;

public class ImageService : IImageService
{

    private readonly Cloudinary _cloudinary;

    public ImageService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;
        _cloudinary = new Cloudinary(
            new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret));
        _cloudinary.Api.Secure = true; // URLs come back as https
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string folder)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = folder,
            UseFilename = false,   // don't trust the client's file name
            UniqueFilename = true, // Cloudinary generates a random public id
            Overwrite = false,
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new InvalidOperationException($"Image upload failed: {result.Error.Message}");

        return result.SecureUrl.ToString();
    }

    
}