namespace SE26Project_18.Backend.Services;

public interface IImageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string folder);
    Task<string> UploadWithNameAsync(Stream fileStream, string objectName, string contentType);
    Task<Stream> GetStreamAsync(string objectName);
    Task DeleteAsync(string objectName);
    Task DeleteByPrefixAsync(string prefix);
    string GetPublicUrl(string objectName);
}