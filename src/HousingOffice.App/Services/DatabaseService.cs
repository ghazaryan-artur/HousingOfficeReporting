using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HousingOffice.Models;
using Microsoft.Data.Sqlite;

namespace HousingOffice.Services;

public sealed class DatabaseService
{
    public string DbPath { get; }
    public int Year { get; }
    private readonly string _connString;

    public DatabaseService(int year, string dbPath)
    {
        Year = year;
        DbPath = dbPath;
        _connString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        InitSchema();
    }

    private SqliteConnection Open()
    {
        var c = new SqliteConnection(_connString);
        c.Open();
        using var pragma = c.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON;";
        pragma.ExecuteNonQuery();
        return c;
    }

    private void InitSchema()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS settings (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS houses (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    street TEXT,
    number TEXT
);
CREATE TABLE IF NOT EXISTS residents (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    house_id INTEGER NOT NULL REFERENCES houses(id) ON DELETE CASCADE,
    row_number INTEGER NOT NULL,
    full_name TEXT NOT NULL DEFAULT '',
    share_raw TEXT,
    square_meters REAL,
    debit_debt REAL NOT NULL DEFAULT 0,
    credit_debt REAL NOT NULL DEFAULT 0,
    monthly_charge REAL NOT NULL DEFAULT 0,
    discount_amount REAL NOT NULL DEFAULT 0,
    p1 REAL NOT NULL DEFAULT 0,
    p2 REAL NOT NULL DEFAULT 0,
    p3 REAL NOT NULL DEFAULT 0,
    p4 REAL NOT NULL DEFAULT 0,
    p5 REAL NOT NULL DEFAULT 0,
    p6 REAL NOT NULL DEFAULT 0,
    p7 REAL NOT NULL DEFAULT 0,
    p8 REAL NOT NULL DEFAULT 0,
    p9 REAL NOT NULL DEFAULT 0,
    p10 REAL NOT NULL DEFAULT 0,
    p11 REAL NOT NULL DEFAULT 0,
    p12 REAL NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS ix_residents_house ON residents(house_id, row_number);
";
        cmd.ExecuteNonQuery();
    }

    public YearSettings LoadSettings()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT key, value FROM settings";
        var settings = new YearSettings { Year = Year };
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var key = r.GetString(0);
            var val = r.GetString(1);
            if (key == "current_month" && int.TryParse(val, out var m)) settings.CurrentMonth = m;
        }
        return settings;
    }

    public void SaveCurrentMonth(int month)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO settings(key,value) VALUES('current_month',$v) " +
                          "ON CONFLICT(key) DO UPDATE SET value=$v";
        cmd.Parameters.AddWithValue("$v", month.ToString(CultureInfo.InvariantCulture));
        cmd.ExecuteNonQuery();
    }

    public List<House> ListHouses()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT id,name,sort_order,street,number FROM houses ORDER BY sort_order, name";
        var result = new List<House>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            result.Add(new House
            {
                Id = r.GetInt64(0),
                Name = r.GetString(1),
                SortOrder = r.GetInt32(2),
                Street = r.IsDBNull(3) ? null : r.GetString(3),
                Number = r.IsDBNull(4) ? null : r.GetString(4),
            });
        }
        return result;
    }

    public long AddHouse(string name, string? street, string? number)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"INSERT INTO houses(name,sort_order,street,number)
                            VALUES($n, COALESCE((SELECT MAX(sort_order)+1 FROM houses),0), $s, $num);
                            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$n", name);
        cmd.Parameters.AddWithValue("$s", (object?)street ?? System.DBNull.Value);
        cmd.Parameters.AddWithValue("$num", (object?)number ?? System.DBNull.Value);
        return (long)cmd.ExecuteScalar()!;
    }

    public void DeleteHouse(long houseId)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM houses WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", houseId);
        cmd.ExecuteNonQuery();
    }

    public List<Resident> ListResidents(long houseId)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT id, house_id, row_number, full_name, share_raw, square_meters,
                                    debit_debt, credit_debt, monthly_charge, discount_amount,
                                    p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12
                             FROM residents WHERE house_id=$h ORDER BY row_number";
        cmd.Parameters.AddWithValue("$h", houseId);
        var list = new List<Resident>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var resident = new Resident
            {
                Id = r.GetInt64(0),
                HouseId = r.GetInt64(1),
                RowNumber = r.GetInt32(2),
                FullName = r.GetString(3),
                ShareRaw = r.IsDBNull(4) ? null : r.GetString(4),
                SquareMeters = r.IsDBNull(5) ? null : r.GetDouble(5),
                DebitDebt = r.GetDouble(6),
                CreditDebt = r.GetDouble(7),
                MonthlyCharge = r.GetDouble(8),
                DiscountAmount = r.GetDouble(9),
            };
            for (int i = 0; i < 12; i++) resident.Payments[i] = r.GetDouble(10 + i);
            list.Add(resident);
        }
        return list;
    }

    public long AddResident(long houseId, string fullName, double? squareMeters, double debitDebt, double monthlyCharge)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"INSERT INTO residents(house_id,row_number,full_name,square_meters,debit_debt,monthly_charge)
                             VALUES($h, COALESCE((SELECT MAX(row_number)+1 FROM residents WHERE house_id=$h),1), $n, $sq, $d, $m);
                             SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$h", houseId);
        cmd.Parameters.AddWithValue("$n", fullName);
        cmd.Parameters.AddWithValue("$sq", (object?)squareMeters ?? System.DBNull.Value);
        cmd.Parameters.AddWithValue("$d", debitDebt);
        cmd.Parameters.AddWithValue("$m", monthlyCharge);
        return (long)cmd.ExecuteScalar()!;
    }

    public void DeleteResident(long residentId)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "DELETE FROM residents WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", residentId);
        cmd.ExecuteNonQuery();
    }

    public void UpdateResident(Resident r)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"UPDATE residents SET
                                full_name=$name, share_raw=$share, square_meters=$sq,
                                debit_debt=$d, credit_debt=$cr, monthly_charge=$m, discount_amount=$disc,
                                p1=$p1,p2=$p2,p3=$p3,p4=$p4,p5=$p5,p6=$p6,
                                p7=$p7,p8=$p8,p9=$p9,p10=$p10,p11=$p11,p12=$p12
                            WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", r.Id);
        cmd.Parameters.AddWithValue("$name", r.FullName);
        cmd.Parameters.AddWithValue("$share", (object?)r.ShareRaw ?? System.DBNull.Value);
        cmd.Parameters.AddWithValue("$sq", (object?)r.SquareMeters ?? System.DBNull.Value);
        cmd.Parameters.AddWithValue("$d", r.DebitDebt);
        cmd.Parameters.AddWithValue("$cr", r.CreditDebt);
        cmd.Parameters.AddWithValue("$m", r.MonthlyCharge);
        cmd.Parameters.AddWithValue("$disc", r.DiscountAmount);
        for (int i = 0; i < 12; i++)
            cmd.Parameters.AddWithValue($"$p{i + 1}", r.Payments[i]);
        cmd.ExecuteNonQuery();
    }

    public void BulkImport(IEnumerable<(House house, List<Resident> residents)> data, int currentMonth)
    {
        using var c = Open();
        using var tx = c.BeginTransaction();
        using (var wipe = c.CreateCommand())
        {
            wipe.Transaction = tx;
            wipe.CommandText = "DELETE FROM residents; DELETE FROM houses;";
            wipe.ExecuteNonQuery();
        }

        int sort = 0;
        foreach (var (h, residents) in data)
        {
            long houseId;
            using (var ih = c.CreateCommand())
            {
                ih.Transaction = tx;
                ih.CommandText = "INSERT INTO houses(name,sort_order,street,number) VALUES($n,$so,$s,$num); SELECT last_insert_rowid();";
                ih.Parameters.AddWithValue("$n", h.Name);
                ih.Parameters.AddWithValue("$so", sort++);
                ih.Parameters.AddWithValue("$s", (object?)h.Street ?? System.DBNull.Value);
                ih.Parameters.AddWithValue("$num", (object?)h.Number ?? System.DBNull.Value);
                houseId = (long)ih.ExecuteScalar()!;
            }

            foreach (var r in residents)
            {
                using var ir = c.CreateCommand();
                ir.Transaction = tx;
                ir.CommandText = @"INSERT INTO residents(house_id,row_number,full_name,share_raw,square_meters,
                                        debit_debt,credit_debt,monthly_charge,discount_amount,
                                        p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12)
                                   VALUES($h,$rn,$fn,$sh,$sq,$d,$cr,$m,$disc,
                                          $p1,$p2,$p3,$p4,$p5,$p6,$p7,$p8,$p9,$p10,$p11,$p12)";
                ir.Parameters.AddWithValue("$h", houseId);
                ir.Parameters.AddWithValue("$rn", r.RowNumber);
                ir.Parameters.AddWithValue("$fn", r.FullName);
                ir.Parameters.AddWithValue("$sh", (object?)r.ShareRaw ?? System.DBNull.Value);
                ir.Parameters.AddWithValue("$sq", (object?)r.SquareMeters ?? System.DBNull.Value);
                ir.Parameters.AddWithValue("$d", r.DebitDebt);
                ir.Parameters.AddWithValue("$cr", r.CreditDebt);
                ir.Parameters.AddWithValue("$m", r.MonthlyCharge);
                ir.Parameters.AddWithValue("$disc", r.DiscountAmount);
                for (int i = 0; i < 12; i++)
                    ir.Parameters.AddWithValue($"$p{i + 1}", r.Payments[i]);
                ir.ExecuteNonQuery();
            }
        }

        using (var s = c.CreateCommand())
        {
            s.Transaction = tx;
            s.CommandText = "INSERT INTO settings(key,value) VALUES('current_month',$v) ON CONFLICT(key) DO UPDATE SET value=$v";
            s.Parameters.AddWithValue("$v", currentMonth.ToString(CultureInfo.InvariantCulture));
            s.ExecuteNonQuery();
        }

        tx.Commit();
    }
}
