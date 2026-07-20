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
CREATE TABLE IF NOT EXISTS change_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    resident_id INTEGER NOT NULL,
    house_id INTEGER NOT NULL,
    resident_name TEXT NOT NULL,
    house_name TEXT NOT NULL,
    column_key TEXT NOT NULL,
    column_label TEXT NOT NULL,
    old_value TEXT,
    new_value TEXT,
    changed_at TEXT NOT NULL,
    undone INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS ix_change_log_time ON change_log(id DESC);
";
        cmd.ExecuteNonQuery();

        using var pragma = c.CreateCommand();
        pragma.CommandText = "SELECT COUNT(*) FROM pragma_table_info('residents') WHERE name='note'";
        var hasNote = (long)pragma.ExecuteScalar()! > 0;
        if (!hasNote)
        {
            using var alter = c.CreateCommand();
            alter.CommandText = "ALTER TABLE residents ADD COLUMN note TEXT";
            alter.ExecuteNonQuery();
        }
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
                                    p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12, note
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
            resident.Note = r.IsDBNull(22) ? null : r.GetString(22);
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
                                p7=$p7,p8=$p8,p9=$p9,p10=$p10,p11=$p11,p12=$p12,
                                note=$note
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
        cmd.Parameters.AddWithValue("$note", (object?)r.Note ?? System.DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void LogChange(long residentId, long houseId, string residentName, string houseName,
        string columnKey, string columnLabel, string? oldValue, string? newValue)
    {
        using var c = Open();
        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = @"INSERT INTO change_log
                                    (resident_id, house_id, resident_name, house_name, column_key, column_label, old_value, new_value, changed_at, undone)
                                VALUES
                                    ($rid, $hid, $rn, $hn, $ck, $cl, $ov, $nv, $ts, 0)";
            cmd.Parameters.AddWithValue("$rid", residentId);
            cmd.Parameters.AddWithValue("$hid", houseId);
            cmd.Parameters.AddWithValue("$rn", residentName);
            cmd.Parameters.AddWithValue("$hn", houseName);
            cmd.Parameters.AddWithValue("$ck", columnKey);
            cmd.Parameters.AddWithValue("$cl", columnLabel);
            cmd.Parameters.AddWithValue("$ov", (object?)oldValue ?? System.DBNull.Value);
            cmd.Parameters.AddWithValue("$nv", (object?)newValue ?? System.DBNull.Value);
            cmd.Parameters.AddWithValue("$ts", System.DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
            cmd.ExecuteNonQuery();
        }

        using (var prune = c.CreateCommand())
        {
            prune.CommandText = "DELETE FROM change_log WHERE id NOT IN (SELECT id FROM change_log ORDER BY id DESC LIMIT 500)";
            prune.ExecuteNonQuery();
        }
    }

    public List<ChangeLogEntry> ListRecentChanges(int limit = 100)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT id, resident_id, house_id, resident_name, house_name, column_key, column_label, old_value, new_value, changed_at, undone
                             FROM change_log ORDER BY id DESC LIMIT $lim";
        cmd.Parameters.AddWithValue("$lim", limit);
        var list = new List<ChangeLogEntry>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(ReadChangeLogEntry(r));
        return list;
    }

    public ChangeLogEntry? UndoChange(long changeLogId)
    {
        using var c = Open();
        ChangeLogEntry? entry = null;
        using (var sel = c.CreateCommand())
        {
            sel.CommandText = @"SELECT id, resident_id, house_id, resident_name, house_name, column_key, column_label, old_value, new_value, changed_at, undone
                                 FROM change_log WHERE id=$id";
            sel.Parameters.AddWithValue("$id", changeLogId);
            using var r = sel.ExecuteReader();
            if (r.Read()) entry = ReadChangeLogEntry(r);
        }
        if (entry == null || entry.Undone) return null;

        var column = ResidentColumnSql(entry.ColumnKey);
        if (column == null) return null;

        using (var upd = c.CreateCommand())
        {
            upd.CommandText = $"UPDATE residents SET {column}=$v WHERE id=$id";
            upd.Parameters.AddWithValue("$v", (object?)entry.OldValue ?? System.DBNull.Value);
            upd.Parameters.AddWithValue("$id", entry.ResidentId);
            if (upd.ExecuteNonQuery() == 0) return null;
        }
        using (var mark = c.CreateCommand())
        {
            mark.CommandText = "UPDATE change_log SET undone=1 WHERE id=$id";
            mark.Parameters.AddWithValue("$id", changeLogId);
            mark.ExecuteNonQuery();
        }
        entry.Undone = true;
        return entry;
    }

    private static string? ResidentColumnSql(string columnKey) => columnKey switch
    {
        "RowNumber" => "row_number",
        "FullName" => "full_name",
        "ShareRaw" => "share_raw",
        "SquareMeters" => "square_meters",
        "DebitDebt" => "debit_debt",
        "CreditDebt" => "credit_debt",
        "MonthlyCharge" => "monthly_charge",
        "DiscountAmount" => "discount_amount",
        "Note" => "note",
        "P1" => "p1", "P2" => "p2", "P3" => "p3", "P4" => "p4",
        "P5" => "p5", "P6" => "p6", "P7" => "p7", "P8" => "p8",
        "P9" => "p9", "P10" => "p10", "P11" => "p11", "P12" => "p12",
        _ => null,
    };

    private static ChangeLogEntry ReadChangeLogEntry(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(0),
        ResidentId = r.GetInt64(1),
        HouseId = r.GetInt64(2),
        ResidentName = r.GetString(3),
        HouseName = r.GetString(4),
        ColumnKey = r.GetString(5),
        ColumnLabel = r.GetString(6),
        OldValue = r.IsDBNull(7) ? null : r.GetString(7),
        NewValue = r.IsDBNull(8) ? null : r.GetString(8),
        ChangedAt = System.DateTime.Parse(r.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        Undone = r.GetInt64(10) != 0,
    };

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
                                        p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,note)
                                   VALUES($h,$rn,$fn,$sh,$sq,$d,$cr,$m,$disc,
                                          $p1,$p2,$p3,$p4,$p5,$p6,$p7,$p8,$p9,$p10,$p11,$p12,$note)";
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
                ir.Parameters.AddWithValue("$note", (object?)r.Note ?? System.DBNull.Value);
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
