using System.Collections.Generic;

namespace HousingOffice.Models;

public static class ResidentFieldLabels
{
    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        ["RowNumber"] = "Հերթական համար",
        ["FullName"] = "Անուն, ազգանուն",
        ["ShareRaw"] = "Բաժնեմաս",
        ["SquareMeters"] = "Ընդհ. քառ. մետր",
        ["DebitDebt"] = "Դեբիտորական պարտք",
        ["CreditDebt"] = "Կրեդիտորական պարտք",
        ["MonthlyCharge"] = "Ամսական հաշվարկ",
        ["DiscountAmount"] = "Զիջվող գումար",
        ["Note"] = "Նշում",
        ["P1"] = "Մուտք 1", ["P2"] = "Մուտք 2", ["P3"] = "Մուտք 3", ["P4"] = "Մուտք 4",
        ["P5"] = "Մուտք 5", ["P6"] = "Մուտք 6", ["P7"] = "Մուտք 7", ["P8"] = "Մուտք 8",
        ["P9"] = "Մուտք 9", ["P10"] = "Մուտք 10", ["P11"] = "Մուտք 11", ["P12"] = "Մուտք 12",
    };

    public static string Label(string key) => Labels.TryGetValue(key, out var v) ? v : key;
}
