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

                // 공통
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS stations (station_code TEXT PRIMARY KEY, station_name TEXT, station_type TEXT NOT NULL);", conn).ExecuteNonQueryAsync();
                // 강수량
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS rf_hourly (station_code TEXT NOT NULL, obs_time TIMESTAMP NOT NULL, rainfall REAL, PRIMARY KEY (station_code, obs_time));", conn).ExecuteNonQueryAsync();
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS rf_daily (station_code TEXT NOT NULL, obs_date DATE NOT NULL, rainfall REAL, PRIMARY KEY (station_code, obs_date));", conn).ExecuteNonQueryAsync();
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS rf_monthly (station_code TEXT NOT NULL, obs_month DATE NOT NULL, rainfall REAL, PRIMARY KEY (station_code, obs_month));", conn).ExecuteNonQueryAsync();
                // 수위
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS wl_hourly (station_code TEXT NOT NULL, obs_time TIMESTAMP NOT NULL, water_level REAL, PRIMARY KEY (station_code, obs_time));", conn).ExecuteNonQueryAsync();
                // 기상
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS weather_hourly (station_code TEXT NOT NULL, obs_time TIMESTAMP NOT NULL, temperature REAL, humidity REAL, wind_speed REAL, PRIMARY KEY (station_code, obs_time));", conn).ExecuteNonQueryAsync();
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS weather_daily (station_code TEXT NOT NULL, obs_date DATE NOT NULL, temperature REAL, humidity REAL, wind_speed REAL, PRIMARY KEY (station_code, obs_date));", conn).ExecuteNonQueryAsync();
                // 유량
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS flow_measurements (station_code TEXT NOT NULL, obs_date DATE NOT NULL, avg_wl REAL, flow REAL, PRIMARY KEY (station_code, obs_date));", conn).ExecuteNonQueryAsync();
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS flow_daily (station_code TEXT NOT NULL, obs_date DATE NOT NULL, flow REAL, PRIMARY KEY (station_code, obs_date));", conn).ExecuteNonQueryAsync();
                // 댐
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS dam_hourly (dam_code TEXT NOT NULL, obs_time TIMESTAMP NOT NULL, storage_wl REAL, inflow REAL, total_outflow REAL, PRIMARY KEY (dam_code, obs_time));", conn).ExecuteNonQueryAsync();
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS dam_daily (dam_code TEXT NOT NULL, obs_date DATE NOT NULL, storage_wl REAL, inflow REAL, total_outflow REAL, PRIMARY KEY (dam_code, obs_date));", conn).ExecuteNonQueryAsync();
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS dam_monthly (dam_code TEXT NOT NULL, obs_month DATE NOT NULL, storage_wl REAL, inflow REAL, total_outflow REAL, PRIMARY KEY (dam_code, obs_month));", conn).ExecuteNonQueryAsync();

                _logAction("데이터베이스 테이블 준비 완료.");
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
                                Name = reader.IsDBNull(1) ? null : reader.GetString(1),
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
                var code = station.StationCode;
                var name = station.Name;
                if (string.IsNullOrEmpty(code)) continue;

                codes.Add(code);
                names.Add(name);
                types.Add(stationType);
            }

            if (!codes.Any()) return;

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
                _logAction($"{codes.Count}개의 {stationType} 관측소 정보가 처리되었습니다.");
            }
        }

        // --- 강수량 ---
        public async Task BulkUpsertRainfallHourlyAsync(List<RainfallData> data, string stationCode)
        {
            if (data == null || !data.Any()) return;
            var uniqueData = new Dictionary<DateTime, double?>();
            foreach (var item in data)
            {
                DateTime obsTime;
                if (item.Ymdh != null && item.Ymdh.EndsWith("24"))
                {
                    if (DateTime.TryParseExact(item.Ymdh.Substring(0, 8), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var datePart))
                        obsTime = datePart.AddDays(1);
                    else continue;
                }
                else
                {
                    if (!DateTime.TryParseExact(item.Ymdh, "yyyyMMddHH", null, System.Globalization.DateTimeStyles.None, out obsTime)) continue;
                }
                uniqueData[obsTime] = item.Rainfall;
            }
            if (!uniqueData.Any()) return;

            var stationCodes = Enumerable.Repeat(stationCode, uniqueData.Count).ToList();
            var obsTimes = uniqueData.Keys.ToList();
            var rainfalls = uniqueData.Values.ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"INSERT INTO rf_hourly (station_code, obs_time, rainfall) SELECT * FROM UNNEST(@p1, @p2, @p3) ON CONFLICT (station_code, obs_time) DO UPDATE SET rainfall = EXCLUDED.rainfall;";
                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("p1", stationCodes);
                    cmd.Parameters.AddWithValue("p2", obsTimes);
                    cmd.Parameters.AddWithValue("p3", rainfalls);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 강수량 시자료가 처리되었습니다.");
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

            var stationCodes = Enumerable.Repeat(stationCode, uniqueData.Count).ToList();
            var obsDates = uniqueData.Keys.ToList();
            var rainfalls = uniqueData.Values.ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"INSERT INTO rf_daily (station_code, obs_date, rainfall) SELECT * FROM UNNEST(@p1, @p2, @p3) ON CONFLICT (station_code, obs_date) DO UPDATE SET rainfall = EXCLUDED.rainfall;";
                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("p1", stationCodes);
                    cmd.Parameters.AddWithValue("p2", obsDates);
                    cmd.Parameters.AddWithValue("p3", rainfalls);
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

            var stationCodes = Enumerable.Repeat(stationCode, uniqueData.Count).ToList();
            var obsMonths = uniqueData.Keys.ToList();
            var rainfalls = uniqueData.Values.ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"INSERT INTO rf_monthly (station_code, obs_month, rainfall) SELECT * FROM UNNEST(@p1, @p2, @p3) ON CONFLICT (station_code, obs_month) DO UPDATE SET rainfall = EXCLUDED.rainfall;";
                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("p1", stationCodes);
                    cmd.Parameters.AddWithValue("p2", obsMonths);
                    cmd.Parameters.AddWithValue("p3", rainfalls);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 강수량 월자료가 처리되었습니다.");
            }
        }

        // --- 수위 ---
        public async Task BulkUpsertWaterLevelHourlyAsync(List<WaterLevelData> data, string stationCode)
        {
            if (data == null || !data.Any()) return;
            var uniqueData = new Dictionary<DateTime, double?>();
            foreach (var item in data)
            {
                DateTime obsTime;
                if (item.Ymdh != null && item.Ymdh.EndsWith("24"))
                {
                    if (DateTime.TryParseExact(item.Ymdh.Substring(0, 8), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var datePart))
                        obsTime = datePart.AddDays(1);
                    else continue;
                }
                else
                {
                    if (!DateTime.TryParseExact(item.Ymdh, "yyyyMMddHH", null, System.Globalization.DateTimeStyles.None, out obsTime)) continue;
                }
                uniqueData[obsTime] = item.WaterLevel;
            }
            if (!uniqueData.Any()) return;

            var stationCodes = Enumerable.Repeat(stationCode, uniqueData.Count).ToList();
            var obsTimes = uniqueData.Keys.ToList();
            var values = uniqueData.Values.ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"INSERT INTO wl_hourly (station_code, obs_time, water_level) SELECT * FROM UNNEST(@p1, @p2, @p3) ON CONFLICT (station_code, obs_time) DO UPDATE SET water_level = EXCLUDED.water_level;";
                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("p1", stationCodes);
                    cmd.Parameters.AddWithValue("p2", obsTimes);
                    cmd.Parameters.AddWithValue("p3", values);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 수위 시자료가 처리되었습니다.");
            }
        }

        // --- 기상 ---
        public async Task BulkUpsertWeatherHourlyAsync(List<WeatherData> data, string stationCode)
        {
            if (data == null || !data.Any()) return;
            var uniqueData = new Dictionary<DateTime, (double? temp, double? hum, double? ws)>();
            foreach (var item in data)
            {
                DateTime obsTime;
                if (item.Ymdh != null && item.Ymdh.EndsWith("24"))
                {
                    if (DateTime.TryParseExact(item.Ymdh.Substring(0, 8), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var datePart))
                        obsTime = datePart.AddDays(1);
                    else continue;
                }
                else
                {
                    if (!DateTime.TryParseExact(item.Ymdh, "yyyyMMddHH", null, System.Globalization.DateTimeStyles.None, out obsTime)) continue;
                }
                uniqueData[obsTime] = (item.Temperature, item.Humidity, item.WindSpeed);
            }
            if (!uniqueData.Any()) return;

            var stationCodes = Enumerable.Repeat(stationCode, uniqueData.Count).ToList();
            var obsTimes = uniqueData.Keys.ToList();
            var temps = uniqueData.Values.Select(v => v.temp).ToList();
            var hums = uniqueData.Values.Select(v => v.hum).ToList();
            var wss = uniqueData.Values.Select(v => v.ws).ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"INSERT INTO weather_hourly (station_code, obs_time, temperature, humidity, wind_speed) SELECT * FROM UNNEST(@p1, @p2, @p3, @p4, @p5) ON CONFLICT (station_code, obs_time) DO UPDATE SET temperature = EXCLUDED.temperature, humidity = EXCLUDED.humidity, wind_speed = EXCLUDED.wind_speed;";
                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("p1", stationCodes);
                    cmd.Parameters.AddWithValue("p2", obsTimes);
                    cmd.Parameters.AddWithValue("p3", temps);
                    cmd.Parameters.AddWithValue("p4", hums);
                    cmd.Parameters.AddWithValue("p5", wss);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 기상 시자료가 처리되었습니다.");
            }
        }

        public async Task BulkUpsertWeatherDailyAsync(List<WeatherData> data, string stationCode)
        {
            if (data == null || !data.Any()) return;
            var uniqueData = new Dictionary<DateTime, (double? temp, double? hum, double? ws)>();
            foreach (var item in data)
            {
                if (DateTime.TryParseExact(item.Ymd, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var obsDate))
                {
                    uniqueData[obsDate.Date] = (item.Temperature, item.Humidity, item.WindSpeed);
                }
            }
            if (!uniqueData.Any()) return;

            var stationCodes = Enumerable.Repeat(stationCode, uniqueData.Count).ToList();
            var obsDates = uniqueData.Keys.ToList();
            var temps = uniqueData.Values.Select(v => v.temp).ToList();
            var hums = uniqueData.Values.Select(v => v.hum).ToList();
            var wss = uniqueData.Values.Select(v => v.ws).ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"INSERT INTO weather_daily (station_code, obs_date, temperature, humidity, wind_speed) SELECT * FROM UNNEST(@p1, @p2, @p3, @p4, @p5) ON CONFLICT (station_code, obs_date) DO UPDATE SET temperature = EXCLUDED.temperature, humidity = EXCLUDED.humidity, wind_speed = EXCLUDED.wind_speed;";
                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("p1", stationCodes);
                    cmd.Parameters.AddWithValue("p2", obsDates);
                    cmd.Parameters.AddWithValue("p3", temps);
                    cmd.Parameters.AddWithValue("p4", hums);
                    cmd.Parameters.AddWithValue("p5", wss);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 기상 일자료가 처리되었습니다.");
            }
        }

        // --- 유량 ---
        public async Task BulkUpsertFlowMeasurementAsync(List<FlowMeasurementData> data, string stationCode)
        {
            if (data == null || !data.Any()) return;
            var uniqueData = new Dictionary<DateTime, (double? wl, double? flow)>();
            foreach (var item in data)
            {
                if (DateTime.TryParseExact(item.ObsYmd, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var obsDate))
                {
                    uniqueData[obsDate.Date] = (item.AvgWl, item.Flow);
                }
            }
            if (!uniqueData.Any()) return;

            var stationCodes = Enumerable.Repeat(stationCode, uniqueData.Count).ToList();
            var obsDates = uniqueData.Keys.ToList();
            var wls = uniqueData.Values.Select(v => v.wl).ToList();
            var flows = uniqueData.Values.Select(v => v.flow).ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"INSERT INTO flow_measurements (station_code, obs_date, avg_wl, flow) SELECT * FROM UNNEST(@p1, @p2, @p3, @p4) ON CONFLICT (station_code, obs_date) DO UPDATE SET avg_wl = EXCLUDED.avg_wl, flow = EXCLUDED.flow;";
                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("p1", stationCodes);
                    cmd.Parameters.AddWithValue("p2", obsDates);
                    cmd.Parameters.AddWithValue("p3", wls);
                    cmd.Parameters.AddWithValue("p4", flows);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 유량 측정성과가 처리되었습니다.");
            }
        }

        public async Task BulkUpsertFlowDailyAsync(List<FlowDailyData> data, string stationCode)
        {
            if (data == null || !data.Any()) return;
            var uniqueData = new Dictionary<DateTime, double?>();
            foreach (var item in data)
            {
                if (DateTime.TryParseExact(item.Ymd, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var obsDate))
                {
                    uniqueData[obsDate.Date] = item.Flow;
                }
            }
            if (!uniqueData.Any()) return;

            var stationCodes = Enumerable.Repeat(stationCode, uniqueData.Count).ToList();
            var obsDates = uniqueData.Keys.ToList();
            var flows = uniqueData.Values.ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"INSERT INTO flow_daily (station_code, obs_date, flow) SELECT * FROM UNNEST(@p1, @p2, @p3) ON CONFLICT (station_code, obs_date) DO UPDATE SET flow = EXCLUDED.flow;";
                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("p1", stationCodes);
                    cmd.Parameters.AddWithValue("p2", obsDates);
                    cmd.Parameters.AddWithValue("p3", flows);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 유량 일자료가 처리되었습니다.");
            }
        }

        // --- 댐 ---
        public async Task BulkUpsertDamHourlyAsync(List<DamData> data, string damCode)
        {
            if (data == null || !data.Any()) return;
            var uniqueData = new Dictionary<DateTime, (double? swl, double? inf, double? otf)>();
            foreach (var item in data)
            {
                DateTime obsTime;
                if (item.Obsdh != null && item.Obsdh.EndsWith("24"))
                {
                    if (DateTime.TryParseExact(item.Obsdh.Substring(0, 8), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var datePart))
                        obsTime = datePart.AddDays(1);
                    else continue;
                }
                else
                {
                    if (!DateTime.TryParseExact(item.Obsdh, "yyyyMMddHH", null, System.Globalization.DateTimeStyles.None, out obsTime)) continue;
                }
                uniqueData[obsTime] = (item.StorageWaterLevel, item.Inflow, item.TotalOutflow);
            }
            if (!uniqueData.Any()) return;

            var damCodes = Enumerable.Repeat(damCode, uniqueData.Count).ToList();
            var obsTimes = uniqueData.Keys.ToList();
            var swls = uniqueData.Values.Select(v => v.swl).ToList();
            var infs = uniqueData.Values.Select(v => v.inf).ToList();
            var otfs = uniqueData.Values.Select(v => v.otf).ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"INSERT INTO dam_hourly (dam_code, obs_time, storage_wl, inflow, total_outflow) SELECT * FROM UNNEST(@p1, @p2, @p3, @p4, @p5) ON CONFLICT (dam_code, obs_time) DO UPDATE SET storage_wl = EXCLUDED.storage_wl, inflow = EXCLUDED.inflow, total_outflow = EXCLUDED.total_outflow;";
                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("p1", damCodes);
                    cmd.Parameters.AddWithValue("p2", obsTimes);
                    cmd.Parameters.AddWithValue("p3", swls);
                    cmd.Parameters.AddWithValue("p4", infs);
                    cmd.Parameters.AddWithValue("p5", otfs);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 댐 시자료가 처리되었습니다.");
            }
        }

        public async Task BulkUpsertDamDailyAsync(List<DamData> data, string damCode)
        {
            if (data == null || !data.Any()) return;
            var uniqueData = new Dictionary<DateTime, (double? swl, double? inf, double? otf)>();
            foreach (var item in data)
            {
                if (DateTime.TryParseExact(item.Obsymd, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var obsDate))
                {
                    uniqueData[obsDate.Date] = (item.StorageWaterLevel, item.Inflow, item.TotalOutflow);
                }
            }
            if (!uniqueData.Any()) return;

            var damCodes = Enumerable.Repeat(damCode, uniqueData.Count).ToList();
            var obsDates = uniqueData.Keys.ToList();
            var swls = uniqueData.Values.Select(v => v.swl).ToList();
            var infs = uniqueData.Values.Select(v => v.inf).ToList();
            var otfs = uniqueData.Values.Select(v => v.otf).ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"INSERT INTO dam_daily (dam_code, obs_date, storage_wl, inflow, total_outflow) SELECT * FROM UNNEST(@p1, @p2, @p3, @p4, @p5) ON CONFLICT (dam_code, obs_date) DO UPDATE SET storage_wl = EXCLUDED.storage_wl, inflow = EXCLUDED.inflow, total_outflow = EXCLUDED.total_outflow;";
                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("p1", damCodes);
                    cmd.Parameters.AddWithValue("p2", obsDates);
                    cmd.Parameters.AddWithValue("p3", swls);
                    cmd.Parameters.AddWithValue("p4", infs);
                    cmd.Parameters.AddWithValue("p5", otfs);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 댐 일자료가 처리되었습니다.");
            }
        }

        public async Task BulkUpsertDamMonthlyAsync(List<DamData> data, string damCode)
        {
            if (data == null || !data.Any()) return;
            var uniqueData = new Dictionary<DateTime, (double? swl, double? inf, double? otf)>();
            foreach (var item in data)
            {
                if (DateTime.TryParseExact(item.Obsymd, "yyyyMM", null, System.Globalization.DateTimeStyles.None, out var obsMonth))
                {
                    uniqueData[obsMonth.Date] = (item.StorageWaterLevel, item.Inflow, item.TotalOutflow);
                }
            }
            if (!uniqueData.Any()) return;

            var damCodes = Enumerable.Repeat(damCode, uniqueData.Count).ToList();
            var obsMonths = uniqueData.Keys.ToList();
            var swls = uniqueData.Values.Select(v => v.swl).ToList();
            var infs = uniqueData.Values.Select(v => v.inf).ToList();
            var otfs = uniqueData.Values.Select(v => v.otf).ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"INSERT INTO dam_monthly (dam_code, obs_month, storage_wl, inflow, total_outflow) SELECT * FROM UNNEST(@p1, @p2, @p3, @p4, @p5) ON CONFLICT (dam_code, obs_month) DO UPDATE SET storage_wl = EXCLUDED.storage_wl, inflow = EXCLUDED.inflow, total_outflow = EXCLUDED.total_outflow;";
                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("p1", damCodes);
                    cmd.Parameters.AddWithValue("p2", obsMonths);
                    cmd.Parameters.AddWithValue("p3", swls);
                    cmd.Parameters.AddWithValue("p4", infs);
                    cmd.Parameters.AddWithValue("p5", otfs);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 댐 월자료가 처리되었습니다.");
            }
        }

        private async Task<DateTime?> GetLastDateAsync(string table, string dateColumn, string codeColumn, string code)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var sql = $"SELECT MAX({dateColumn}) FROM {table} WHERE {codeColumn} = @code";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("code", code);
                    var result = await cmd.ExecuteScalarAsync();
                    return (result != null && result != DBNull.Value) ? (DateTime?)result : null;
                }
            }
        }

        public async Task<DateTime?> GetLastHourlyRainfallDateAsync(string stationCode) => await GetLastDateAsync("rf_hourly", "obs_time", "station_code", stationCode);
        public async Task<DateTime?> GetLastDailyRainfallDateAsync(string stationCode) => await GetLastDateAsync("rf_daily", "obs_date", "station_code", stationCode);
        public async Task<DateTime?> GetLastMonthlyRainfallDateAsync(string stationCode) => await GetLastDateAsync("rf_monthly", "obs_month", "station_code", stationCode);
        public async Task<DateTime?> GetLastWaterLevelHourlyDateAsync(string stationCode) => await GetLastDateAsync("wl_hourly", "obs_time", "station_code", stationCode);
        public async Task<DateTime?> GetLastWeatherHourlyDateAsync(string stationCode) => await GetLastDateAsync("weather_hourly", "obs_time", "station_code", stationCode);
        public async Task<DateTime?> GetLastWeatherDailyDateAsync(string stationCode) => await GetLastDateAsync("weather_daily", "obs_date", "station_code", stationCode);
        public async Task<DateTime?> GetLastFlowMeasurementDateAsync(string stationCode) => await GetLastDateAsync("flow_measurements", "obs_date", "station_code", stationCode);
        public async Task<DateTime?> GetLastFlowDailyDateAsync(string stationCode) => await GetLastDateAsync("flow_daily", "obs_date", "station_code", stationCode);
        public async Task<DateTime?> GetLastDamHourlyDateAsync(string damCode) => await GetLastDateAsync("dam_hourly", "obs_time", "dam_code", damCode);
        public async Task<DateTime?> GetLastDamDailyDateAsync(string damCode) => await GetLastDateAsync("dam_daily", "obs_date", "dam_code", damCode);
        public async Task<DateTime?> GetLastDamMonthlyDateAsync(string damCode) => await GetLastDateAsync("dam_monthly", "obs_month", "dam_code", damCode);
    }
}