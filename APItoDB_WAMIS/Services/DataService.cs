using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using WamisDataCollector.Models;

namespace WamisDataCollector.Services
{
    public class DataService
    {
        private readonly string _connectionString;
        private readonly Action<string> _logAction;

        public DataService(string connectionString, Action<string> logAction = null)
        {
            _connectionString = connectionString;
            _logAction = logAction ?? Console.WriteLine;
        }

        public async Task EnsureTablesExistAsync()
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                _logAction("데이터베이스 테이블 생성/확인 시작...");

                await new NpgsqlCommand(@"
                    CREATE TABLE IF NOT EXISTS stations (
                        station_code TEXT PRIMARY KEY,
                        station_name TEXT,
                        station_type TEXT NOT NULL
                    );", conn).ExecuteNonQueryAsync();

                await new NpgsqlCommand(@"
                    CREATE TABLE IF NOT EXISTS rf_hourly (
                        station_code TEXT NOT NULL,
                        obs_time TIMESTAMP NOT NULL,
                        rainfall REAL,
                        PRIMARY KEY (station_code, obs_time)
                    );", conn).ExecuteNonQueryAsync();

                await new NpgsqlCommand(@"
                    CREATE TABLE IF NOT EXISTS rf_daily (
                        station_code TEXT NOT NULL,
                        obs_date DATE NOT NULL,
                        rainfall REAL,
                        PRIMARY KEY (station_code, obs_date)
                    );", conn).ExecuteNonQueryAsync();

                await new NpgsqlCommand(@"
                    CREATE TABLE IF NOT EXISTS rf_monthly (
                        station_code TEXT NOT NULL,
                        obs_month DATE NOT NULL,
                        rainfall REAL,
                        PRIMARY KEY (station_code, obs_month)
                    );", conn).ExecuteNonQueryAsync();

                _logAction("데이터베이스 테이블 준비 완료.");
            }
        }

        public async Task<DateTime?> GetLastHourlyRainfallDateAsync(string stationCode)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var sql = "SELECT MAX(obs_time) FROM rf_hourly WHERE station_code = @station_code";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("station_code", stationCode);
                    var result = await cmd.ExecuteScalarAsync();
                    return (result != null && result != DBNull.Value) ? (DateTime?)result : null;
                }
            }
        }

        public async Task<DateTime?> GetLastDailyRainfallDateAsync(string stationCode)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var sql = "SELECT MAX(obs_date) FROM rf_daily WHERE station_code = @station_code";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("station_code", stationCode);
                    var result = await cmd.ExecuteScalarAsync();
                    return (result != null && result != DBNull.Value) ? (DateTime?)result : null;
                }
            }
        }

        public async Task<DateTime?> GetLastMonthlyRainfallDateAsync(string stationCode)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var sql = "SELECT MAX(obs_month) FROM rf_monthly WHERE station_code = @station_code";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("station_code", stationCode);
                    var result = await cmd.ExecuteScalarAsync();
                    return (result != null && result != DBNull.Value) ? (DateTime?)result : null;
                }
            }
        }

        public async Task<List<StationInfo>> GetAllStationsAsync(string stationType = null)
        {
            var stations = new List<StationInfo>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var sql = "SELECT station_code, station_name, station_type FROM stations";
                if (!string.IsNullOrEmpty(stationType))
                {
                    sql += " WHERE station_type = @station_type";
                }

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(stationType))
                    {
                        cmd.Parameters.AddWithValue("station_type", stationType);
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            stations.Add(new StationInfo
                            {
                                StationCode = reader.GetString(0),
                                DamCode = reader.GetString(0),
                                StationName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                DamName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                StationType = reader.GetString(2)
                            });
                        }
                    }
                }
            }
            return stations;
        }

        public async Task UpsertStationsAsync(List<StationInfo> stations, string stationType)
        {
            if (stations == null || !stations.Any()) return;

            var codes = new List<string>();
            var names = new List<string>();
            var types = new List<string>();

            foreach (var station in stations)
            {
                var code = stationType == "DAM" ? station.DamCode : station.StationCode;
                var name = stationType == "DAM" ? station.DamName : station.StationName;
                if (string.IsNullOrEmpty(code)) continue;

                codes.Add(code);
                names.Add(name);
                types.Add(stationType);
            }

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var upsertCommand = @"
                    INSERT INTO stations (station_code, station_name, station_type)
                    SELECT * FROM UNNEST(@codes, @names, @types)
                    ON CONFLICT (station_code) DO UPDATE SET
                        station_name = EXCLUDED.station_name,
                        station_type = EXCLUDED.station_type;";

                using (var cmd = new NpgsqlCommand(upsertCommand, conn))
                {
                    cmd.Parameters.AddWithValue("codes", codes);
                    cmd.Parameters.AddWithValue("names", names);
                    cmd.Parameters.AddWithValue("types", types);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{stations.Count}개의 {stationType} 관측소 정보가 처리되었습니다.");
            }
        }

        public async Task BulkUpsertRainfallHourlyAsync(List<RainfallData> data, string stationCode)
        {
            if (data == null || !data.Any()) return;

            var uniqueData = new Dictionary<DateTime, double?>();
            foreach (var item in data)
            {
                DateTime obsTime;
                // '24시' 데이터 처리 로직 추가
                if (item.Ymdh != null && item.Ymdh.EndsWith("24"))
                {
                    if (DateTime.TryParseExact(item.Ymdh.Substring(0, 8), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var datePart))
                    {
                        obsTime = datePart.AddDays(1); // 다음 날 00시로 변환
                    }
                    else
                    {
                        continue; // 날짜 파싱 실패 시 건너뛰기
                    }
                }
                else
                {
                    if (!DateTime.TryParseExact(item.Ymdh, "yyyyMMddHH", null, System.Globalization.DateTimeStyles.None, out obsTime))
                    {
                        continue; // 일반 시간 파싱 실패 시 건너뛰기
                    }
                }
                uniqueData[obsTime] = item.Rainfall;
            }

            if (!uniqueData.Any()) return;

            var obsTimes = uniqueData.Keys.ToList();
            var rainfalls = uniqueData.Values.ToList();
            var stationCodes = Enumerable.Repeat(stationCode, obsTimes.Count).ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"
                    INSERT INTO rf_hourly (station_code, obs_time, rainfall)
                    SELECT * FROM UNNEST(@station_codes, @obs_times, @rainfalls)
                    ON CONFLICT (station_code, obs_time) DO UPDATE SET
                        rainfall = EXCLUDED.rainfall;";

                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("station_codes", stationCodes);
                    cmd.Parameters.AddWithValue("obs_times", obsTimes);
                    cmd.Parameters.AddWithValue("rainfalls", rainfalls);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 강수량 시자료가 처리되었습니다. (중복 {data.Count - uniqueData.Count}건 제거)");
            }
        }
        public async Task BulkUpsertRainfallDailyAsync(List<RainfallData> data, string stationCode)
        {
            if (data == null || !data.Any()) return;

            var uniqueData = new Dictionary<DateTime, double?>();
            foreach (var item in data)
            {
                if (DateTime.TryParseExact(item.Ymd, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var obsDate))
                {
                    uniqueData[obsDate.Date] = item.Rainfall;
                }
            }

            if (!uniqueData.Any()) return;

            var obsDates = uniqueData.Keys.ToList();
            var rainfalls = uniqueData.Values.ToList();
            var stationCodes = Enumerable.Repeat(stationCode, obsDates.Count).ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"
                    INSERT INTO rf_daily (station_code, obs_date, rainfall)
                    SELECT * FROM UNNEST(@station_codes, @obs_dates, @rainfalls)
                    ON CONFLICT (station_code, obs_date) DO UPDATE SET
                        rainfall = EXCLUDED.rainfall;";

                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("station_codes", stationCodes);
                    cmd.Parameters.AddWithValue("obs_dates", obsDates);
                    cmd.Parameters.AddWithValue("rainfalls", rainfalls);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 강수량 일자료가 처리되었습니다.");
            }
        }
        public async Task BulkUpsertRainfallMonthlyAsync(List<RainfallData> data, string stationCode)
        {
            if (data == null || !data.Any()) return;

            var uniqueData = new Dictionary<DateTime, double?>();
            foreach (var item in data)
            {
                if (DateTime.TryParseExact(item.Ym, "yyyyMM", null, System.Globalization.DateTimeStyles.None, out var obsMonth))
                {
                    uniqueData[obsMonth.Date] = item.Rainfall;
                }
            }

            if (!uniqueData.Any()) return;

            var obsMonths = uniqueData.Keys.ToList();
            var rainfalls = uniqueData.Values.ToList();
            var stationCodes = Enumerable.Repeat(stationCode, obsMonths.Count).ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"
                    INSERT INTO rf_monthly (station_code, obs_month, rainfall)
                    SELECT * FROM UNNEST(@station_codes, @obs_months, @rainfalls)
                    ON CONFLICT (station_code, obs_month) DO UPDATE SET
                        rainfall = EXCLUDED.rainfall;";

                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("station_codes", stationCodes);
                    cmd.Parameters.AddWithValue("obs_months", obsMonths);
                    cmd.Parameters.AddWithValue("rainfalls", rainfalls);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 강수량 월자료가 처리되었습니다.");
            }
        }
    }
}



