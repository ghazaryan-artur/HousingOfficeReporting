namespace HousingOffice.Models;

public sealed class House
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
}
