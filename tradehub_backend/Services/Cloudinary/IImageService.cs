namespace TradeHub.Services;

public interface IImageService
{
    /// <summary>Uploads an image to Cloudinary and returns its secure URL.</summary>
    Task<string> UploadAsync(Stream stream, string fileName, string folder);
}