using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using HousingOffice.Models;

namespace HousingOffice.Services;

public sealed class XlsmService
{
    private const string ListSheetName = "Ցուցակ";
    private const string TemplateSheetName = "Շաբլոն";

    public sealed record ImportResult(List<(House House, List<Resident> Residents)> Houses, int CurrentMonth);

    public ImportResult Import(string xlsmPath)
    {
        using var wb = new XLWorkbook(xlsmPath);
        int currentMonth = 12;

        var listSheet = wb.Worksheets.FirstOrDefault(w => w.Name == ListSheetName);
        if (listSheet != null)
        {
            var cell = listSheet.Cell("D2");
            if (cell.TryGetValue<double>(out var d) && d >= 1 && d <= 12) currentMonth = (int)d;
        }

        var houses = new List<(House, List<Resident>)>();
        foreach (var sheet in wb.Worksheets)
        {
            var name = sheet.Name;
            if (name == ListSheetName || name == TemplateSheetName) continue;
            if (sheet.Visibility != XLWorksheetVisibility.Visible) continue;

            var (street, number) = SplitHouseName(name);
            var house = new House { Name = name, Street = street, Number = number };
            var residents = ParseSheet(sheet);
            houses.Add((house, residents));
        }
        return new ImportResult(houses, currentMonth);
    }

    private static List<Resident> ParseSheet(IXLWorksheet sheet)
    {
        var residents = new List<Resident>();
        var used = sheet.RangeUsed();
        if (used == null) return residents;
        int lastRow = used.RangeAddress.LastAddress.RowNumber;

        for (int row = 2; row <= lastRow; row++)
        {
            var numCell = sheet.Cell(row, 1);
            var nameCell = sheet.Cell(row, 2);
            var nameStr = nameCell.GetString().Trim();

            if (string.IsNullOrEmpty(nameStr) && !numCell.TryGetValue<double>(out _)) continue;
            if (string.IsNullOrEmpty(nameStr) && ReadDouble(sheet, row, 4) == 0 && ReadDouble(sheet, row, 5) == 0 &&
                ReadDouble(sheet, row, 7) == 0 && AllPaymentsEmpty(sheet, row)) continue;

            var r = new Resident
            {
                RowNumber = numCell.TryGetValue<double>(out var rn) ? (int)rn : (row - 1),
                FullName = nameStr,
                ShareRaw = sheet.Cell(row, 3).GetString().Trim() is { Length: > 0 } s ? s : null,
                SquareMeters = ReadNullable(sheet, row, 4),
                DebitDebt = ReadDouble(sheet, row, 5),
                CreditDebt = ReadDouble(sheet, row, 6),
                MonthlyCharge = ReadDouble(sheet, row, 7),
                DiscountAmount = ReadDouble(sheet, row, 21),
                Note = sheet.Cell(row, 23).GetString().Trim() is { Length: > 0 } note ? note : null,
            };
            for (int i = 0; i < 12; i++)
                r.Payments[i] = ReadDouble(sheet, row, 8 + i);

            residents.Add(r);
        }
        return residents;
    }

    private static bool AllPaymentsEmpty(IXLWorksheet sheet, int row)
    {
        for (int i = 0; i < 12; i++)
            if (ReadDouble(sheet, row, 8 + i) != 0) return false;
        return true;
    }

    private static double ReadDouble(IXLWorksheet sheet, int row, int col)
    {
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty()) return 0;
        if (cell.TryGetValue<double>(out var d)) return d;
        var s = cell.GetString().Trim();
        return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private static double? ReadNullable(IXLWorksheet sheet, int row, int col)
    {
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<double>(out var d)) return d;
        return null;
    }

    private static (string? street, string? number) SplitHouseName(string name)
    {
        var idx = name.LastIndexOf('_');
        if (idx <= 0 || idx == name.Length - 1) return (name, null);
        return (name[..idx], name[(idx + 1)..]);
    }

    public void Export(string outputPath, IReadOnlyList<(House House, List<Resident> Residents)> houses, int currentMonth, int year)
    {
        using var wb = new XLWorkbook();

        var list = wb.Worksheets.Add(ListSheetName);
        list.Cell("A1").Value = "Ցուցակ";
        list.Cell("C1").Value = "Այս պահին հաշվարկվող ամիս է";
        list.Cell("D2").Value = currentMonth;
        int listRow = 2;
        foreach (var (house, _) in houses)
        {
            list.Cell(listRow, 1).Value = house.Name;
            list.Cell(listRow, 2).SetHyperlink(new XLHyperlink($"'{house.Name}'!A1", house.Name));
            list.Cell(listRow, 2).Value = house.Name;
            listRow++;
        }
        list.Columns().AdjustToContents();

        foreach (var (house, residents) in houses)
        {
            var sheet = wb.Worksheets.Add(house.Name);
            WriteHeader(sheet, year);

            int row = 2;
            foreach (var r in residents)
            {
                sheet.Cell(row, 1).Value = r.RowNumber;
                sheet.Cell(row, 2).Value = r.FullName;
                if (!string.IsNullOrWhiteSpace(r.ShareRaw)) sheet.Cell(row, 3).Value = r.ShareRaw;
                if (r.SquareMeters.HasValue) sheet.Cell(row, 4).Value = r.SquareMeters.Value;
                if (r.DebitDebt != 0) sheet.Cell(row, 5).Value = r.DebitDebt;
                if (r.CreditDebt != 0) sheet.Cell(row, 6).Value = r.CreditDebt;
                if (r.MonthlyCharge != 0) sheet.Cell(row, 7).Value = r.MonthlyCharge;
                for (int i = 0; i < 12; i++)
                    if (r.Payments[i] != 0) sheet.Cell(row, 8 + i).Value = r.Payments[i];
                if (r.DiscountAmount != 0) sheet.Cell(row, 21).Value = r.DiscountAmount;
                if (!string.IsNullOrWhiteSpace(r.Note)) sheet.Cell(row, 23).Value = r.Note;
                sheet.Cell(row, 20).FormulaA1 = $"SUM(H{row}:S{row})";
                sheet.Cell(row, 22).FormulaA1 = $"E{row}-F{row}+(G{row}*'{ListSheetName}'!$D$2)-T{row}-U{row}";
                row++;
            }
            sheet.Row(1).Style.Font.Bold = true;
            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();
        }

        wb.SaveAs(outputPath);
    }

    private static void WriteHeader(IXLWorksheet sheet, int year)
    {
        sheet.Cell("A1").Value = "N";
        sheet.Cell("B1").Value = "Անուն, ազգանուն";
        sheet.Cell("C1").Value = "Մասնա բաժին";
        sheet.Cell("D1").Value = "Ընդ. ք/մ";
        sheet.Cell("E1").Value = "Դեբիտորական պարտք";
        sheet.Cell("F1").Value = $"Դեբիտորական պարտք առ 01.01.{year}";
        sheet.Cell("G1").Value = "Հաշվարկ";
        for (int i = 0; i < 12; i++)
            sheet.Cell(1, 8 + i).Value = $"Մուտք {i + 1}";
        sheet.Cell("T1").Value = "Ընդանուր";
        sheet.Cell("U1").Value = "Զիջվող գումար";
        sheet.Cell("V1").Value = "Վերջնական մնացորդ";
        sheet.Cell("W1").Value = "Նշում";
    }
}
