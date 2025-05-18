using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using Azunt.PostManagement;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;

namespace Azunt.Apis.Posts
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostDownloadController : ControllerBase
    {
        private readonly IPostRepository _postRepository;
        //private readonly IPostStorageService _storageService;

        public PostDownloadController(IPostRepository postRepository 
            /*IPostStorageService storageService*/)
        {
            _postRepository = postRepository;
            //_storageService = storageService;
        }

        /// <summary>
        /// 게시글 목록 엑셀 다운로드
        /// GET /api/PostDownload/ExcelDown
        /// </summary>
        [HttpGet("ExcelDown")]
        public async Task<IActionResult> ExcelDown()
        {
            var items = await _postRepository.GetAllAsync();

            if (!items.Any())
                return NotFound("No post records found.");

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Posts");

            var range = sheet.Cells["B2"].LoadFromCollection(
                items.Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Title,
                    m.Category,
                    Created = m.Created.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    m.CreatedBy
                }),
                PrintHeaders: true
            );

            var header = sheet.Cells["B2:G2"];
            sheet.DefaultColWidth = 20;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.WhiteSmoke);
            range.Style.Border.BorderAround(ExcelBorderStyle.Medium);

            header.Style.Font.Bold = true;
            header.Style.Font.Color.SetColor(Color.White);
            header.Style.Fill.BackgroundColor.SetColor(Color.DarkGreen);

            var content = package.GetAsByteArray();
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{DateTime.Now:yyyyMMddHHmmss}_Posts.xlsx");
        }

        ///// <summary>
        ///// 게시글 첨부파일 다운로드
        ///// GET /api/PostDownload/{fileName}
        ///// </summary>
        //[HttpGet("{fileName}")]
        //public async Task<IActionResult> Download(string fileName)
        //{
        //    try
        //    {
        //        var stream = await _storageService.DownloadAsync(fileName);
        //        return File(stream, "application/octet-stream", fileName);
        //    }
        //    catch (FileNotFoundException)
        //    {
        //        return NotFound($"File not found: {fileName}");
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Download error: {ex.Message}");
        //    }
        //}
    }
}
