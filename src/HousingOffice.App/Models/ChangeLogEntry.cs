namespace HousingOffice.Models;

public sealed class ChangeLogEntry
{
    public long Id { get; set; }
    public long ResidentId { get; set; }
    public long HouseId { get; set; }
    public string ResidentName { get; set; } = "";
    public string HouseName { get; set; } = "";
    public string ColumnKey { get; set; } = "";
    public string ColumnLabel { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public System.DateTime ChangedAt { get; set; }
    public bool Undone { get; set; }
}
