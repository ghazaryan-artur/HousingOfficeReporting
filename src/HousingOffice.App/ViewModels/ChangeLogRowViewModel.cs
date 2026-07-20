using System;
using System.Globalization;
using HousingOffice.Models;

namespace HousingOffice.ViewModels;

public sealed class ChangeLogRowViewModel
{
    public ChangeLogEntry Entry { get; }

    public ChangeLogRowViewModel(ChangeLogEntry entry) => Entry = entry;

    public long Id => Entry.Id;
    public string HouseName => Entry.HouseName;
    public string ResidentName => Entry.ResidentName;
    public string ColumnLabel => Entry.ColumnLabel;
    public string OldValueDisplay => Format(Entry.OldValue);
    public string NewValueDisplay => Format(Entry.NewValue);
    public string ChangedAtDisplay => Entry.ChangedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    public bool Undone => Entry.Undone;
    public string StatusText => Undone ? "Հետարկված" : "Ակտիվ";

    private static string Format(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return "—";
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return d == Math.Truncate(d)
                ? d.ToString("N0", CultureInfo.InvariantCulture)
                : d.ToString("N2", CultureInfo.InvariantCulture);
        return raw;
    }
}
