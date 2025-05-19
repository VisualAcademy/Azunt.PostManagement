namespace Azunt.PostManagement
{
    public interface IPostStorageService
    {
        Task<string> UploadAsync(Stream stream, string fileName);
        Task<Stream> DownloadAsync(string fileName);
        Task DeleteAsync(string fileName);
    }
}
