using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azunt.PostManagement;

namespace Azunt.Apis.Posts
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostExportController : ControllerBase
    {
        private readonly IPostRepository _postRepository;

        public PostExportController(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        /// <summary>
        /// 게시글 목록 엑셀 다운로드
        /// GET /api/PostExport/Excel
        /// </summary>
        [HttpGet("Excel")]
        public async Task<IActionResult> ExportToExcel()
        {
            var items = (await _postRepository.GetAllAsync()).ToList();

            if (!items.Any())
                return NotFound("No post records found.");

            // 데이터 투영 (Title null 처리, Created 문자열로)
            var rows = items.Select(m => new
            {
                m.Id,
                m.Name,
                Title = m.Title ?? string.Empty,
                m.Category,
                Created = FormatLocalTimestamp(m.Created),
                m.CreatedBy
            }).ToList();

            byte[] content;
            using (var ms = new MemoryStream())
            {
                using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true))
                {
                    // 워크북/워크시트 기본 구성
                    var wbPart = doc.AddWorkbookPart();
                    wbPart.Workbook = new Workbook();

                    var wsPart = wbPart.AddNewPart<WorksheetPart>();
                    var sheetData = new SheetData();

                    // 열 너비(B~G = 2~7 열, 20폭)
                    var columns = new Columns(new Column { Min = 2U, Max = 7U, Width = 20D, CustomWidth = true });

                    // 스타일시트 추가 (머리글/본문 스타일)
                    var styles = wbPart.AddNewPart<WorkbookStylesPart>();
                    styles.Stylesheet = BuildStylesheet();
                    styles.Stylesheet.Save();

                    // 워크시트에 Columns + SheetData 추가
                    wsPart.Worksheet = new Worksheet(columns, sheetData);

                    // 시트 등록
                    var sheets = wbPart.Workbook.AppendChild(new Sheets());
                    sheets.Append(new Sheet
                    {
                        Id = wbPart.GetIdOfPart(wsPart),
                        SheetId = 1U,
                        Name = "Posts"
                    });

                    // 컬럼 정의 (B..G)
                    string[] headers = { "Id", "Name", "Title", "Category", "Created", "CreatedBy" };
                    string[] cols = { "B", "C", "D", "E", "F", "G" };

                    // 스타일 인덱스 (BuildStylesheet에서 정의)
                    const uint headerStyle = 1; // Bold+White on DarkGreen, left, medium border
                    const uint bodyStyle = 2;   // WhiteSmoke fill, left, medium border

                    // B2에 머리글
                    uint rowIndex = 2;
                    var headerRow = new Row { RowIndex = rowIndex };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        headerRow.Append(MakeTextCell($"{cols[i]}{rowIndex}", headers[i], headerStyle));
                    }
                    sheetData.Append(headerRow);

                    // 데이터는 B3부터
                    rowIndex = 3;
                    foreach (var r in rows)
                    {
                        var dataRow = new Row { RowIndex = rowIndex };
                        dataRow.Append(MakeNumberCell($"{cols[0]}{rowIndex}", r.Id, bodyStyle));
                        dataRow.Append(MakeTextCell($"{cols[1]}{rowIndex}", r.Name ?? string.Empty, bodyStyle));
                        dataRow.Append(MakeTextCell($"{cols[2]}{rowIndex}", r.Title ?? string.Empty, bodyStyle));
                        dataRow.Append(MakeTextCell($"{cols[3]}{rowIndex}", r.Category ?? string.Empty, bodyStyle));
                        dataRow.Append(MakeTextCell($"{cols[4]}{rowIndex}", r.Created ?? string.Empty, bodyStyle));
                        dataRow.Append(MakeTextCell($"{cols[5]}{rowIndex}", r.CreatedBy ?? string.Empty, bodyStyle));
                        sheetData.Append(dataRow);
                        rowIndex++;
                    }

                    wbPart.Workbook.Save();
                }

                content = ms.ToArray();
            }

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{DateTime.Now:yyyyMMddHHmmss}_Posts.xlsx"
            );
        }

        // === Helpers ===

        private static Stylesheet BuildStylesheet()
        {
            // Fonts
            var fonts = new Fonts() { Count = 2U };
            // 0: 기본
            fonts.Append(new Font(
                new FontSize() { Val = 11D },
                new Color() { Theme = 1 }, // Auto (theme)
                new FontName() { Val = "Calibri" }
            ));
            // 1: 머리글(볼드, 흰색)
            fonts.Append(new Font(
                new Bold(),
                new FontSize() { Val = 11D },
                new Color() { Rgb = "FFFFFFFF" },
                new FontName() { Val = "Calibri" }
            ));

            // Fills (0,1은 반드시 예약: None, Gray125)
            var fills = new Fills() { Count = 4U };
            fills.Append(new Fill(new PatternFill() { PatternType = PatternValues.None }));     // 0
            fills.Append(new Fill(new PatternFill() { PatternType = PatternValues.Gray125 }));  // 1
            // 2: 머리글 배경 (DarkGreen)
            fills.Append(new Fill(new PatternFill(
                new ForegroundColor() { Rgb = "FF006400" }, // DarkGreen
                new BackgroundColor() { Indexed = 64U }
            )
            { PatternType = PatternValues.Solid }));
            // 3: 본문 배경 (WhiteSmoke)
            fills.Append(new Fill(new PatternFill(
                new ForegroundColor() { Rgb = "FFF5F5F5" },
                new BackgroundColor() { Indexed = 64U }
            )
            { PatternType = PatternValues.Solid }));

            // Borders (모든 변 Medium)
            var borders = new Borders() { Count = 2U };
            borders.Append(new Border()); // 0: 기본
            borders.Append(new Border(    // 1: Medium 사방
                new LeftBorder() { Style = BorderStyleValues.Medium },
                new RightBorder() { Style = BorderStyleValues.Medium },
                new TopBorder() { Style = BorderStyleValues.Medium },
                new BottomBorder() { Style = BorderStyleValues.Medium },
                new DiagonalBorder()
            ));

            // CellFormats
            var cellFormats = new CellFormats() { Count = 3U };
            cellFormats.Append(new CellFormat()); // 0: 기본
            // 1: 머리글 (폰트1, 필2, 보더1, 좌정렬)
            cellFormats.Append(new CellFormat
            {
                FontId = 1U,
                FillId = 2U,
                BorderId = 1U,
                ApplyFont = true,
                ApplyFill = true,
                ApplyBorder = true,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Center }
            });
            // 2: 본문 (폰트0, 필3, 보더1, 좌정렬)
            cellFormats.Append(new CellFormat
            {
                FontId = 0U,
                FillId = 3U,
                BorderId = 1U,
                ApplyFont = true,
                ApplyFill = true,
                ApplyBorder = true,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Center }
            });

            return new Stylesheet(fonts, fills, borders, cellFormats);
        }

        private static Cell MakeTextCell(string cellRef, string text, uint styleIndex) =>
            new Cell
            {
                CellReference = cellRef,
                DataType = CellValues.String,          // SharedString 미사용: 직접 문자열
                CellValue = new CellValue(text ?? string.Empty),
                StyleIndex = styleIndex
            };

        private static Cell MakeNumberCell(string cellRef, long number, uint styleIndex) =>
            new Cell
            {
                CellReference = cellRef,
                DataType = CellValues.Number,
                CellValue = new CellValue(number.ToString()),
                StyleIndex = styleIndex
            };

        private static string FormatLocalTimestamp(object value)
        {
            // Created가 DateTimeOffset 또는 DateTime일 수 있으므로 안전하게 처리
            switch (value)
            {
                case DateTimeOffset dto:
                    return dto.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                case DateTime dt:
                    return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                default:
                    return value?.ToString() ?? string.Empty;
            }
        }
    }
}
