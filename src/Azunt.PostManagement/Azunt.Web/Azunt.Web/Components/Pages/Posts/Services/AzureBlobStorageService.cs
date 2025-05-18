using Azunt.PostManagement;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System;

namespace Azunt.Web.Components.Pages.Posts.Services
{
    public class AzureBlobStorageService : IPostStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobStorageService(IConfiguration config)
        {
            var connStr = config["AzureBlobStorage:Default:ConnectionString"];
            var containerName = config["AzureBlobStorage:Default:ContainerName"];

            if (string.IsNullOrWhiteSpace(connStr) || string.IsNullOrWhiteSpace(containerName))
                throw new InvalidOperationException("Azure Blob Storage configuration is missing.");

            _containerClient = new BlobContainerClient(connStr, containerName);
            _containerClient.CreateIfNotExists();
        }

        public async Task<string> UploadAsync(Stream postStream, string postName)
        {
            if (string.IsNullOrWhiteSpace(postName))
                throw new ArgumentException("File name cannot be empty.", nameof(postName));

            // 중복 방지를 위해 고유 파일명 생성
            string safeFileName = await GetUniqueFileNameAsync(postName);

            // URL 인코딩 (업로드 직전에 한 번만)
            string encodedFileName = WebUtility.UrlEncode(safeFileName);

            var blobClient = _containerClient.GetBlobClient(encodedFileName);
            await blobClient.UploadAsync(postStream, overwrite: true);

            return blobClient.Uri.ToString(); // 전체 URL 반환
        }

        private async Task<string> GetUniqueFileNameAsync(string originalName)
        {
            string baseName = Path.GetFileNameWithoutExtension(originalName);
            string extension = Path.GetExtension(originalName);
            string newFileName = originalName;
            int count = 1;

            // 중복 여부는 인코딩되지 않은 파일명으로 검사
            while (await _containerClient.GetBlobClient(WebUtility.UrlEncode(newFileName)).ExistsAsync())
            {
                newFileName = $"{baseName}({count}){extension}";
                count++;
            }

            return newFileName;
        }

        public async Task<Stream> DownloadAsync(string postName)
        {
            if (string.IsNullOrWhiteSpace(postName))
                throw new ArgumentException("File name cannot be empty.", nameof(postName));

            // URL 디코딩
            string decodedFileName = WebUtility.UrlDecode(postName);

            var blobClient = _containerClient.GetBlobClient(decodedFileName);

            if (!await blobClient.ExistsAsync())
                throw new FileNotFoundException($"Post file not found: {postName}");

            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }

        public async Task DeleteAsync(string postName)
        {
            if (string.IsNullOrWhiteSpace(postName))
                throw new ArgumentException("File name cannot be empty.", nameof(postName));

            string decodedFileName = WebUtility.UrlDecode(postName);
            var blobClient = _containerClient.GetBlobClient(decodedFileName);

            await blobClient.DeleteIfExistsAsync();
        }
    }
}
