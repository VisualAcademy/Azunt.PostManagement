namespace Azunt.PostManagement
{
    public interface IPostStorageService
    {
        Task<string> UploadAsync(Stream postStream, string postName);
        Task<Stream> DownloadAsync(string postName);
        Task DeleteAsync(string postName);
    }
}
