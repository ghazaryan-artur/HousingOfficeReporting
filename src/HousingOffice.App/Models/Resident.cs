namespace HousingOffice.Models;

public sealed class Resident
{
    public long Id { get; set; }
    public long HouseId { get; set; }

    public int RowNumber { get; set; }
    public string FullName { get; set; } = "";
    public string? ShareRaw { get; set; }
    public double? SquareMeters { get; set; }
    public double DebitDebt { get; set; }
    public double CreditDebt { get; set; }
    public double MonthlyCharge { get; set; }
    public double DiscountAmount { get; set; }

    public double[] Payments { get; set; } = new double[12];

    public double PaidTotal
    {
        get
        {
            double s = 0;
            for (int i = 0; i < 12; i++) s += Payments[i];
            return s;
        }
    }

    public double FinalBalance(int currentMonthCount)
        => DebitDebt - CreditDebt + MonthlyCharge * currentMonthCount - PaidTotal - DiscountAmount;
}
