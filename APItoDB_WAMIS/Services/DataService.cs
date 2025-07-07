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
        /// <summary>
        /// 테이블 생성 (없을시)
        /// </summary>
        /// <returns></returns>
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
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS wl_daily (station_code TEXT NOT NULL, obs_date DATE NOT NULL, water_level REAL, PRIMARY KEY (station_code, obs_date));", conn).ExecuteNonQueryAsync();
                // 기상
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS weather_hourly (
                    station_code TEXT NOT NULL, obs_time TIMESTAMP NOT NULL, ta REAL, hm REAL, td REAL, ps REAL, ws REAL, wd TEXT, sihr1 REAL, catot REAL, sdtot REAL, sshr1 REAL, PRIMARY KEY (station_code, obs_time) );", conn).ExecuteNonQueryAsync();

                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS weather_daily (
                    station_code TEXT NOT NULL, obs_date DATE NOT NULL, ta_avg REAL, ta_min REAL, ta_max REAL, ws_avg REAL, ws_max REAL, wd_max TEXT, hm_avg REAL, hm_min REAL, evs REAL, evl REAL, catot_avg REAL, ps_avg REAL, ps_max REAL, ps_min REAL, sd_max REAL, td_avg REAL, si_avg REAL, ss_avg REAL, PRIMARY KEY (station_code, obs_date));", conn).ExecuteNonQueryAsync();

                // 유량
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS flow_measurements (station_code TEXT NOT NULL, obs_date DATE NOT NULL, avg_wl REAL, flow REAL, PRIMARY KEY (station_code, obs_date));", conn).ExecuteNonQueryAsync();
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS flow_daily (station_code TEXT NOT NULL, obs_date DATE NOT NULL, flow REAL, PRIMARY KEY (station_code, obs_date));", conn).ExecuteNonQueryAsync();
            
                //댐
                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS dam_hourly (
                    dam_code TEXT NOT NULL, obs_time TIMESTAMP NOT NULL,
                    rwl REAL, ospilwl REAL, rsqty REAL, rsrt REAL, iqty REAL, etqty REAL, 
                    tdqty REAL, edqty REAL, spdqty REAL, otltdqty REAL, itqty REAL, dambsarf REAL,
                    PRIMARY KEY (dam_code, obs_time)
                );", conn).ExecuteNonQueryAsync();

                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS dam_daily (
                    dam_code TEXT NOT NULL, obs_date DATE NOT NULL,
                    rwl REAL, iqty REAL, tdqty REAL, edqty REAL, spdqty REAL, 
                    otltdqty REAL, itqty REAL, rf REAL,
                    PRIMARY KEY (dam_code, obs_date)
                );", conn).ExecuteNonQueryAsync();

                await new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS dam_monthly (
                    dam_code TEXT NOT NULL, obs_month DATE NOT NULL,
                    mnwl REAL, avwl REAL, mxwl REAL, mniqty REAL, aviqty REAL, mxiqty REAL,
                    mntdqty REAL, avtdqty REAL, mxtdqty REAL, mnsqty REAL, avsqty REAL,
                    mxsqty REAL, mnrf REAL, avrf REAL, mxrf REAL,
                    PRIMARY KEY (dam_code, obs_month)
                );", conn).ExecuteNonQueryAsync();

                _logAction("데이터베이스 테이블 준비 완료.");
            }
        }


        /// <summary>
        /// 모든 관측소 정보 불러오기(API)
        /// </summary>
        /// <param name="stationType"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 관측소 정보 업데이트(DB)
        /// </summary>
        /// <param name="stations"></param>
        /// <param name="stationType"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 강수량 데이터 입력(DB)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="stationCode"></param>
        /// <returns></returns>
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

       
        /// <summary>
        /// 수위자료 입력(DB)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="stationCode"></param>
        /// <returns></returns>
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
        public async Task BulkUpsertWaterLevelDailyAsync(List<WaterLevelData> data, string stationCode)
        {
            if (data == null || !data.Any()) return;

            var uniqueData = new Dictionary<DateTime, double?>();
            foreach (var item in data)
            {
                if (DateTime.TryParseExact(item.Ymd, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var obsDate))
                {
                    uniqueData[obsDate.Date] = item.WaterLevel;
                }
            }
            if (!uniqueData.Any()) return;

            var stationCodes = Enumerable.Repeat(stationCode, uniqueData.Count).ToList();
            var obsDates = uniqueData.Keys.ToList();
            var values = uniqueData.Values.ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"INSERT INTO wl_daily (station_code, obs_date, water_level) SELECT * FROM UNNEST(@p1, @p2, @p3) ON CONFLICT (station_code, obs_date) DO UPDATE SET water_level = EXCLUDED.water_level;";
                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("p1", stationCodes);
                    cmd.Parameters.AddWithValue("p2", obsDates);
                    cmd.Parameters.AddWithValue("p3", values);
                    await cmd.ExecuteNonQueryAsync();
                }
                _logAction($"{uniqueData.Count}개의 수위 일자료가 처리되었습니다.");
            }
        }

        /// <summary>
        /// 기상자료 입력(DB)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="stationCode"></param>
        /// <returns></returns>
        public async Task BulkUpsertWeatherHourlyAsync(List<WeatherData> data, string stationCode)
        {
            if (data == null || !data.Any()) return;

            var commandText = @"
                INSERT INTO weather_hourly (
                    station_code, obs_time, ta, hm, td, ps, ws, wd, sihr1, catot, sdtot, sshr1
                ) VALUES (
                    @station_code, @obs_time, @ta, @hm, @td, @ps, @ws, @wd, @sihr1, @catot, @sdtot, @sshr1
                )
                ON CONFLICT (station_code, obs_time) DO UPDATE SET 
                    ta = EXCLUDED.ta, hm = EXCLUDED.hm, td = EXCLUDED.td, ps = EXCLUDED.ps, ws = EXCLUDED.ws,
                    wd = EXCLUDED.wd, sihr1 = EXCLUDED.sihr1, catot = EXCLUDED.catot, sdtot = EXCLUDED.sdtot, sshr1 = EXCLUDED.sshr1;";

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
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

                    using (var cmd = new NpgsqlCommand(commandText, conn))
                    {
                        cmd.Parameters.AddWithValue("station_code", stationCode);
                        cmd.Parameters.AddWithValue("obs_time", obsTime);
                        cmd.Parameters.AddWithValue("ta", (object)item.Ta ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("hm", (object)item.Hm ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("td", (object)item.Td ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ps", (object)item.Ps ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ws", (object)item.Ws ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("wd", (object)item.Wd ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("sihr1", (object)item.Sihr1 ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("catot", (object)item.Catot ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("sdtot", (object)item.Sdtot ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("sshr1", (object)item.Sshr1 ?? DBNull.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                _logAction($"{data.Count}개의 기상 시자료가 처리되었습니다.");
            }
        }

        public async Task BulkUpsertWeatherDailyAsync(List<WeatherData> data, string stationCode)
        {
            if (data == null || !data.Any()) return;

            var commandText = @"
                INSERT INTO weather_daily (
                    station_code, obs_date, ta_avg, ta_min, ta_max, ws_avg, ws_max, wd_max,
                    hm_avg, hm_min, evs, evl, catot_avg, ps_avg, ps_max, ps_min, 
                    sd_max, td_avg, si_avg, ss_avg
                ) VALUES (
                    @station_code, @obs_date, @ta_avg, @ta_min, @ta_max, @ws_avg, @ws_max, @wd_max,
                    @hm_avg, @hm_min, @evs, @evl, @catot_avg, @ps_avg, @ps_max, @ps_min, 
                    @sd_max, @td_avg, @si_avg, @ss_avg
                )
                ON CONFLICT (station_code, obs_date) DO UPDATE SET
                    ta_avg = EXCLUDED.ta_avg, ta_min = EXCLUDED.ta_min, ta_max = EXCLUDED.ta_max,
                    ws_avg = EXCLUDED.ws_avg, ws_max = EXCLUDED.ws_max, wd_max = EXCLUDED.wd_max,
                    hm_avg = EXCLUDED.hm_avg, hm_min = EXCLUDED.hm_min, evs = EXCLUDED.evs, evl = EXCLUDED.evl,
                    catot_avg = EXCLUDED.catot_avg, ps_avg = EXCLUDED.ps_avg, ps_max = EXCLUDED.ps_max,
                    ps_min = EXCLUDED.ps_min, sd_max = EXCLUDED.sd_max, td_avg = EXCLUDED.td_avg,
                    si_avg = EXCLUDED.si_avg, ss_avg = EXCLUDED.ss_avg;";

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                foreach (var item in data)
                {
                    if (!DateTime.TryParseExact(item.Ymd, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var obsDate)) continue;

                    using (var cmd = new NpgsqlCommand(commandText, conn))
                    {
                        cmd.Parameters.AddWithValue("station_code", stationCode);
                        cmd.Parameters.AddWithValue("obs_date", obsDate.Date);
                        cmd.Parameters.AddWithValue("ta_avg", (object)item.Taavg ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ta_min", (object)item.Tamin ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ta_max", (object)item.Tamax ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ws_avg", (object)item.Wsavg ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ws_max", (object)item.Wsmax ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("wd_max", (object)item.Wdmax ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("hm_avg", (object)item.Havg ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("hm_min", (object)item.Hmin ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("evs", (object)item.Evs ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("evl", (object)item.Evl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("catot_avg", (object)item.Catotavg ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ps_avg", (object)item.Psavg ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ps_max", (object)item.Psmax ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ps_min", (object)item.Psmin ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("sd_max", (object)item.Sdmax ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("td_avg", (object)item.Tdavg ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("si_avg", (object)item.Siavg ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ss_avg", (object)item.Ssavg ?? DBNull.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                _logAction($"{data.Count}개의 기상 일자료가 처리되었습니다.");
            }
        }

        /// <summary>
        /// 유량자료 입력(DB)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="stationCode"></param>
        /// <returns></returns>
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

        //유사량 미사용
        //public async Task BulkUpsertFlowMeasurementAsync(List<FlowMeasurementData> data, string stationCode)
        //{
        //    if (data == null || !data.Any()) return;
        //    var uniqueData = new Dictionary<DateTime, (double? wl, double? flow)>();
        //    foreach (var item in data)
        //    {
        //        if (DateTime.TryParseExact(item.ObsYmd, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var obsDate))
        //        {
        //            uniqueData[obsDate.Date] = (item.AvgWl, item.Flow);
        //        }
        //    }
        //    if (!uniqueData.Any()) return;

        //    var stationCodes = Enumerable.Repeat(stationCode, uniqueData.Count).ToList();
        //    var obsDates = uniqueData.Keys.ToList();
        //    var wls = uniqueData.Values.Select(v => v.wl).ToList();
        //    var flows = uniqueData.Values.Select(v => v.flow).ToList();

        //    using (var conn = new NpgsqlConnection(_connectionString))
        //    {
        //        await conn.OpenAsync();
        //        var commandText = @"INSERT INTO flow_measurements (station_code, obs_date, avg_wl, flow) SELECT * FROM UNNEST(@p1, @p2, @p3, @p4) ON CONFLICT (station_code, obs_date) DO UPDATE SET avg_wl = EXCLUDED.avg_wl, flow = EXCLUDED.flow;";
        //        using (var cmd = new NpgsqlCommand(commandText, conn))
        //        {
        //            cmd.Parameters.AddWithValue("p1", stationCodes);
        //            cmd.Parameters.AddWithValue("p2", obsDates);
        //            cmd.Parameters.AddWithValue("p3", wls);
        //            cmd.Parameters.AddWithValue("p4", flows);
        //            await cmd.ExecuteNonQueryAsync();
        //        }
        //        _logAction($"{uniqueData.Count}개의 유량 측정성과가 처리되었습니다.");
        //    }
        //} 




        /// <summary>
        /// 댐자료 입력(DB)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="damCode"></param>
        /// <returns></returns>
        public async Task BulkUpsertDamHourlyAsync(List<DamData> data, string damCode)
        {
            if (data == null || !data.Any()) return;

            var commandText = @"
                INSERT INTO dam_hourly (
                    dam_code, obs_time, rwl, ospilwl, rsqty, rsrt, iqty, etqty, 
                    tdqty, edqty, spdqty, otltdqty, itqty, dambsarf
                ) VALUES (
                    @dam_code, @obs_time, @rwl, @ospilwl, @rsqty, @rsrt, @iqty, @etqty, 
                    @tdqty, @edqty, @spdqty, @otltdqty, @itqty, @dambsarf
                )
                ON CONFLICT (dam_code, obs_time) DO UPDATE SET 
                    rwl = EXCLUDED.rwl, ospilwl = EXCLUDED.ospilwl, rsqty = EXCLUDED.rsqty, rsrt = EXCLUDED.rsrt,
                    iqty = EXCLUDED.iqty, etqty = EXCLUDED.etqty, tdqty = EXCLUDED.tdqty, edqty = EXCLUDED.edqty,
                    spdqty = EXCLUDED.spdqty, otltdqty = EXCLUDED.otltdqty, itqty = EXCLUDED.itqty, dambsarf = EXCLUDED.dambsarf;";

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
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

                    using (var cmd = new NpgsqlCommand(commandText, conn))
                    {
                        cmd.Parameters.AddWithValue("dam_code", damCode);
                        cmd.Parameters.AddWithValue("obs_time", obsTime);
                        cmd.Parameters.AddWithValue("rwl", (object)item.Rwl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ospilwl", (object)item.Ospilwl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("rsqty", (object)item.Rsqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("rsrt", (object)item.Rsrt ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("iqty", (object)item.Iqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("etqty", (object)item.Etqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("tdqty", (object)item.Tdqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("edqty", (object)item.Edqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("spdqty", (object)item.Spdqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("otltdqty", (object)item.Otltdqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("itqty", (object)item.Itqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("dambsarf", (object)item.Dambsarf ?? DBNull.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                _logAction($"{data.Count}개의 댐 시자료가 처리되었습니다.");
            }
        }

        public async Task BulkUpsertDamDailyAsync(List<DamData> data, string damCode)
        {
            if (data == null || !data.Any()) return;

            var commandText = @"
                INSERT INTO dam_daily (
                    dam_code, obs_date, rwl, iqty, tdqty, edqty, spdqty, otltdqty, itqty, rf
                ) VALUES (
                    @dam_code, @obs_date, @rwl, @iqty, @tdqty, @edqty, @spdqty, @otltdqty, @itqty, @rf
                )
                ON CONFLICT (dam_code, obs_date) DO UPDATE SET
                    rwl = EXCLUDED.rwl, iqty = EXCLUDED.iqty, tdqty = EXCLUDED.tdqty, edqty = EXCLUDED.edqty,
                    spdqty = EXCLUDED.spdqty, otltdqty = EXCLUDED.otltdqty, itqty = EXCLUDED.itqty, rf = EXCLUDED.rf;";

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                foreach (var item in data)
                {
                    if (!DateTime.TryParseExact(item.Obsymd, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var obsDate)) continue;

                    using (var cmd = new NpgsqlCommand(commandText, conn))
                    {
                        cmd.Parameters.AddWithValue("dam_code", damCode);
                        cmd.Parameters.AddWithValue("obs_date", obsDate.Date);
                        cmd.Parameters.AddWithValue("rwl", (object)item.Rwl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("iqty", (object)item.Iqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("tdqty", (object)item.Tdqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("edqty", (object)item.Edqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("spdqty", (object)item.Spdqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("otltdqty", (object)item.Otltdqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("itqty", (object)item.Itqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("rf", (object)item.Rf ?? DBNull.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                _logAction($"{data.Count}개의 댐 일자료가 처리되었습니다.");
            }
        }

        public async Task BulkUpsertDamMonthlyAsync(List<DamData> data, string damCode)
        {
            if (data == null || !data.Any()) return;

            var commandText = @"
                INSERT INTO dam_monthly (
                    dam_code, obs_month, mnwl, avwl, mxwl, mniqty, aviqty, mxiqty,
                    mntdqty, avtdqty, mxtdqty, mnsqty, avsqty, mxsqty, mnrf, avrf, mxrf
                ) VALUES (
                    @dam_code, @obs_month, @mnwl, @avwl, @mxwl, @mniqty, @aviqty, @mxiqty,
                    @mntdqty, @avtdqty, @mxtdqty, @mnsqty, @avsqty, @mxsqty, @mnrf, @avrf, @mxrf
                )
                ON CONFLICT (dam_code, obs_month) DO UPDATE SET
                    mnwl = EXCLUDED.mnwl, avwl = EXCLUDED.avwl, mxwl = EXCLUDED.mxwl, mniqty = EXCLUDED.mniqty,
                    aviqty = EXCLUDED.aviqty, mxiqty = EXCLUDED.mxiqty, mntdqty = EXCLUDED.mntdqty, avtdqty = EXCLUDED.avtdqty,
                    mxtdqty = EXCLUDED.mxtdqty, mnsqty = EXCLUDED.mnsqty, avsqty = EXCLUDED.avsqty, mxsqty = EXCLUDED.mxsqty,
                    mnrf = EXCLUDED.mnrf, avrf = EXCLUDED.avrf, mxrf = EXCLUDED.mxrf;";

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                foreach (var item in data)
                {
                    if (!DateTime.TryParseExact(item.Obsymd, "yyyyMM", null, System.Globalization.DateTimeStyles.None, out var obsMonth)) continue;

                    using (var cmd = new NpgsqlCommand(commandText, conn))
                    {
                        cmd.Parameters.AddWithValue("dam_code", damCode);
                        cmd.Parameters.AddWithValue("obs_month", obsMonth.Date);
                        cmd.Parameters.AddWithValue("mnwl", (object)item.Mnwl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("avwl", (object)item.Avwl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("mxwl", (object)item.Mxwl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("mniqty", (object)item.Mniqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("aviqty", (object)item.Aviqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("mxiqty", (object)item.Mxiqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("mntdqty", (object)item.Mntdqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("avtdqty", (object)item.Avtdqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("mxtdqty", (object)item.Mxtdqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("mnsqty", (object)item.Mnsqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("avsqty", (object)item.Avsqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("mxsqty", (object)item.Mxsqty ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("mnrf", (object)item.Mnrf ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("avrf", (object)item.Avrf ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("mxrf", (object)item.Mxrf ?? DBNull.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                _logAction($"{data.Count}개의 댐 월자료가 처리되었습니다.");
            }
        }


        /// <summary>
        /// 마지막 일자 호출
        /// </summary>
        /// <param name="table"></param>
        /// <param name="dateColumn"></param>
        /// <param name="codeColumn"></param>
        /// <param name="code"></param>
        /// <returns></returns>
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
        public async Task<DateTime?> GetLastDailyWaterLevelDateAsync(string stationCode) => await GetLastDateAsync("wl_daily", "obs_date", "station_code", stationCode);
        public async Task<DateTime?> GetLastWeatherHourlyDateAsync(string stationCode) => await GetLastDateAsync("weather_hourly", "obs_time", "station_code", stationCode);
        public async Task<DateTime?> GetLastWeatherDailyDateAsync(string stationCode) => await GetLastDateAsync("weather_daily", "obs_date", "station_code", stationCode);
        public async Task<DateTime?> GetLastFlowMeasurementDateAsync(string stationCode) => await GetLastDateAsync("flow_measurements", "obs_date", "station_code", stationCode);
        public async Task<DateTime?> GetLastFlowDailyDateAsync(string stationCode) => await GetLastDateAsync("flow_daily", "obs_date", "station_code", stationCode);
        public async Task<DateTime?> GetLastDamHourlyDateAsync(string damCode) => await GetLastDateAsync("dam_hourly", "obs_time", "dam_code", damCode);
        public async Task<DateTime?> GetLastDamDailyDateAsync(string damCode) => await GetLastDateAsync("dam_daily", "obs_date", "dam_code", damCode);
        public async Task<DateTime?> GetLastDamMonthlyDateAsync(string damCode) => await GetLastDateAsync("dam_monthly", "obs_month", "dam_code", damCode);
    }
}