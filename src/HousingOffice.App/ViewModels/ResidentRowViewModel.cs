using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HousingOffice.Models;

namespace HousingOffice.ViewModels;

public partial class ResidentRowViewModel : ObservableObject
{
    public Resident Model { get; }
    private readonly Func<int> _currentMonth;
    private readonly Action<ResidentRowViewModel> _onEdited;

    public ResidentRowViewModel(Resident model, Func<int> currentMonth, Action<ResidentRowViewModel> onEdited)
    {
        Model = model;
        _currentMonth = currentMonth;
        _onEdited = onEdited;
    }

    public long Id => Model.Id;
    public int RowNumber { get => Model.RowNumber; set => SetProp(v => Model.RowNumber = v, value); }

    public string FullName
    {
        get => Model.FullName;
        set => SetProp(v => Model.FullName = v ?? "", value ?? "");
    }

    public string? ShareRaw
    {
        get => Model.ShareRaw;
        set => SetProp(v => Model.ShareRaw = v, value);
    }

    public double? SquareMeters { get => Model.SquareMeters; set => SetProp(v => Model.SquareMeters = v, value); }
    public double DebitDebt { get => Model.DebitDebt; set => SetProp(v => Model.DebitDebt = v, value); }
    public double CreditDebt { get => Model.CreditDebt; set => SetProp(v => Model.CreditDebt = v, value); }
    public double MonthlyCharge { get => Model.MonthlyCharge; set { SetProp(v => Model.MonthlyCharge = v, value); OnPropertyChanged(nameof(FinalBalance)); } }
    public double DiscountAmount { get => Model.DiscountAmount; set { SetProp(v => Model.DiscountAmount = v, value); OnPropertyChanged(nameof(FinalBalance)); } }

    public string? Note { get => Model.Note; set => SetProp(v => Model.Note = v, value); }

    public double P1 { get => Model.Payments[0]; set => SetPayment(0, value); }
    public double P2 { get => Model.Payments[1]; set => SetPayment(1, value); }
    public double P3 { get => Model.Payments[2]; set => SetPayment(2, value); }
    public double P4 { get => Model.Payments[3]; set => SetPayment(3, value); }
    public double P5 { get => Model.Payments[4]; set => SetPayment(4, value); }
    public double P6 { get => Model.Payments[5]; set => SetPayment(5, value); }
    public double P7 { get => Model.Payments[6]; set => SetPayment(6, value); }
    public double P8 { get => Model.Payments[7]; set => SetPayment(7, value); }
    public double P9 { get => Model.Payments[8]; set => SetPayment(8, value); }
    public double P10 { get => Model.Payments[9]; set => SetPayment(9, value); }
    public double P11 { get => Model.Payments[10]; set => SetPayment(10, value); }
    public double P12 { get => Model.Payments[11]; set => SetPayment(11, value); }

    public double PaidTotal => Model.PaidTotal;
    public double FinalBalance => Model.FinalBalance(_currentMonth());

    private void SetPayment(int idx, double value)
    {
        if (Math.Abs(Model.Payments[idx] - value) < 1e-9) return;
        Model.Payments[idx] = value;
        OnPropertyChanged($"P{idx + 1}");
        OnPropertyChanged(nameof(PaidTotal));
        OnPropertyChanged(nameof(FinalBalance));
        _onEdited(this);
    }

    private void SetProp<T>(Action<T> setter, T value, [System.Runtime.CompilerServices.CallerMemberName] string? prop = null)
    {
        setter(value);
        OnPropertyChanged(prop);
        _onEdited(this);
    }

    public void RaiseComputed()
    {
        OnPropertyChanged(nameof(PaidTotal));
        OnPropertyChanged(nameof(FinalBalance));
    }
}
