using OfficeOpenXml;
using System.IO;
using System.Windows.Controls;

namespace Otchet
{
    public static class ExportHelper
    {
        public static void ExportToExcel(DataGrid dataGrid, string filePath)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Документы");

                for (int i = 0; i < dataGrid.Columns.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = dataGrid.Columns[i].Header;
                }

                for (int i = 0; i < dataGrid.Items.Count; i++)
                {
                    var row = dataGrid.Items[i];
                    for (int j = 0; j < dataGrid.Columns.Count; j++)
                    {
                        var cellValue = dataGrid.Columns[j].GetCellContent(row) as TextBlock;
                        worksheet.Cells[i + 2, j + 1].Value = cellValue?.Text;
                    }
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                FileInfo fileInfo = new FileInfo(filePath);
                package.SaveAs(fileInfo);
            }
        }
    }
}