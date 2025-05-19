using Azunt.PostManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Azunt.Web.Components.Pages.Posts.Services
{
    public class LocalPostStorageService : IPostStorageService
    {
        private readonly string _rootPath;
        private readonly ILogger<LocalPostStorageService> _logger;

        public LocalPostStorageService(IWebHostEnvironment env, ILogger<LocalPostStorageService> logger)
        {
            _logger = logger;
            _rootPath = Path.Combine(env.WebRootPath, "files", "posts");

            if (!Directory.Exists(_rootPath))
            {
                Directory.CreateDirectory(_rootPath);
            }
        }

        public async Task<string> UploadAsync(Stream stream, string fileName)
        {
            string safeFileName = GetUniqueFileName(fileName);
            string fullPath = Path.Combine(_rootPath, safeFileName);

            using (var fileStream = File.Create(fullPath))
            {
                await stream.CopyToAsync(fileStream);
            }

            // 웹 접근 가능한 상대 경로 반환
            return $"/files/posts/{safeFileName}";
        }

        private string GetUniqueFileName(string originalName)
        {
            string baseName = Path.GetFileNameWithoutExtension(originalName);
            string extension = Path.GetExtension(originalName);
            string newFileName = originalName;
            int count = 1;

            while (File.Exists(Path.Combine(_rootPath, newFileName)))
            {
                newFileName = $"{baseName}({count}){extension}";
                count++;
            }

            return newFileName;
        }

        public Task<Stream> DownloadAsync(string fileName)
        {
            string fullPath = Path.Combine(_rootPath, fileName);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Post not found: {fileName}");

            var stream = File.OpenRead(fullPath);
            return Task.FromResult<Stream>(stream);
        }

        public Task DeleteAsync(string fileName)
        {
            string fullPath = Path.Combine(_rootPath, fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }
    }
}
