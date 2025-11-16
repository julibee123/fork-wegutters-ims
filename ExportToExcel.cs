using Microsoft.Win32;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace WEGutters
{
    public static class ExportToExcel
    {
        /// <summary>
        /// Shows a SaveFileDialog and exports the provided items to XLSX if the user confirms.
        /// Returns true if a file was written, false if cancelled or nothing to export.
        /// </summary>
        public static bool ExportWithSaveDialog(Window owner, IEnumerable<InventoryItemDisplay> items)
        {
            if (items == null || !items.Any())
                return false;

            var dlg = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                FileName = "StockReport.xlsx"
            };

            var result = dlg.ShowDialog(owner);
            if (result != true)
                return false;

            Export(items, dlg.FileName);
            return true;
        }

        /// <summary>
        /// Exports the provided items to an Excel file at filePath. Throws on failure.
        /// </summary>
        public static void Export(IEnumerable<InventoryItemDisplay> items, string filePath)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Invalid file path.", nameof(filePath));

            var list = items.ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Stock Report");

            // Header row
            var headers = new[]
            {
                "ID", "Name", "Details", "Category", "SKU",
                "Purchase", "Value", "Sale", "Projected",
                "Qty", "Min", "Unit"
            };

            for (int c = 0; c < headers.Length; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.AshGrey;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            // Data rows
            int row = 2;
            foreach (var item in list)
            {
                ws.Cell(row, 1).Value = item?.itemInstance?.InventoryId ?? 0;
                ws.Cell(row, 2).Value = item?.ItemName ?? string.Empty;
                ws.Cell(row, 3).Value = item?.ItemDetails ?? string.Empty;
                ws.Cell(row, 4).Value = item?.Category ?? string.Empty;
                ws.Cell(row, 5).Value = item?.SKU ?? string.Empty;

                ws.Cell(row, 6).Value = item?.PurchaseCost ?? 0f;
                ws.Cell(row, 6).Style.NumberFormat.Format = "$0.00";

                ws.Cell(row, 7).Value = item?.itemInstance?.calcItemValue() ?? 0f;
                ws.Cell(row, 7).Style.NumberFormat.Format = "$0.00";

                ws.Cell(row, 8).Value = item?.SalePrice ?? 0f;
                ws.Cell(row, 8).Style.NumberFormat.Format = "$0.00";

                ws.Cell(row, 9).Value = item?.itemInstance?.calcProjectedSale() ?? 0f;
                ws.Cell(row, 9).Style.NumberFormat.Format = "$0.00";

                ws.Cell(row, 10).Value = item?.Quantity ?? 0;
                ws.Cell(row, 11).Value = item?.MinCount ?? 0;
                ws.Cell(row, 12).Value = item?.Unit ?? string.Empty;

                // Conditional coloring similar to PDF
                var qty = item?.Quantity ?? 0;
                var min = item?.MinCount ?? 0;
                if (qty < min)
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#ff9696");
                else if (qty == min)
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#ffecad");
                else
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.White;

                row++;
            }

            // Summary rows
            int startSummaryRow = row + 1;
            int totalItems = list.Count;
            int belowMin = list.Count(i => i.Quantity < i.MinCount);
            int atMin = list.Count(i => i.Quantity == i.MinCount);
            float invValue = list.Sum(i => i.itemInstance?.calcItemValue() ?? 0f);
            float projectedSum = list.Sum(i => i.itemInstance?.calcProjectedSale() ?? 0f);

            ws.Cell(startSummaryRow, 1).Value = "Total Item Count:";
            ws.Cell(startSummaryRow, 2).Value = totalItems;

            ws.Cell(startSummaryRow + 1, 1).Value = "Items At Minimum:";
            ws.Cell(startSummaryRow + 1, 2).Value = atMin;

            ws.Cell(startSummaryRow + 2, 1).Value = "Items Below Minimum:";
            ws.Cell(startSummaryRow + 2, 2).Value = belowMin;

            ws.Cell(startSummaryRow + 3, 1).Value = "Inventory Value (Sum of all values):";
            ws.Cell(startSummaryRow + 3, 2).Value = invValue;
            ws.Cell(startSummaryRow + 3, 2).Style.NumberFormat.Format = "$0.00";

            ws.Cell(startSummaryRow + 4, 1).Value = "Projected Sales (Sum of all projected sales):";
            ws.Cell(startSummaryRow + 4, 2).Value = projectedSum;
            ws.Cell(startSummaryRow + 4, 2).Style.NumberFormat.Format = "$0.00";

            // Finish: freeze header and autofit
            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();

            wb.SaveAs(filePath);
        }
    }
}
