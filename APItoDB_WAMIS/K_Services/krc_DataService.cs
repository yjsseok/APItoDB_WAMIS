using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using KRC_Services.Models; // KRC Models

namespace KRC_Services.Services
{
    public class KrcDataService
    {
        private readonly string _connectionString;
        private readonly Action<string> _logAction;

        public KrcDataService(string connectionString, Action<string> logAction = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logAction = logAction ?? Console.WriteLine;
        }

        /// <summary>
        /// KRC 저수지 코드 정보를 `krc_reservoircode` 테이블에 Upsert 합니다.
        /// </summary>
        public async Task UpsertKrcReservoirStationsAsync(List<KrcReservoirCodeItem> krcStations)
        {
            if (krcStations == null || !krcStations.Any())
            {
                _logAction("저장할 KRC 저수지 정보가 없습니다.");
                return;
            }

            var facCodes = new List<string>();
            var facNames = new List<string>();
            var counties = new List<string>();

            foreach (var station in krcStations)
            {
                if (string.IsNullOrWhiteSpace(station.FacCode)) continue;

                facCodes.Add(station.FacCode);
                facNames.Add(station.FacName);
                counties.Add(station.County);
            }

            if (!facCodes.Any()) return;

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var upsertCommand = @"
                    INSERT INTO krc_reservoircode (fac_code, fac_name, county)
                    SELECT * FROM UNNEST(@fac_codes, @fac_names, @counties)
                    ON CONFLICT (fac_code) DO UPDATE SET
                        fac_name = EXCLUDED.fac_name,
                        county = EXCLUDED.county;";

                using (var cmd = new NpgsqlCommand(upsertCommand, conn))
                {
                    cmd.Parameters.AddWithValue("fac_codes", facCodes);
                    cmd.Parameters.AddWithValue("fac_names", facNames.Select(n => (object)n ?? DBNull.Value).ToList());
                    cmd.Parameters.AddWithValue("counties", counties.Select(c => (object)c ?? DBNull.Value).ToList());
                    var affectedRows = await cmd.ExecuteNonQueryAsync();
                    _logAction($"{affectedRows} (요청된 KRC 저수지 {facCodes.Count}개 중) KRC 저수지 코드 정보가 `krc_reservoircode` 테이블에 처리/업데이트되었습니다.");
                }
            }
        }

        /// <summary>
        /// KRC 저수지 일별 수위 및 저수율 데이터를 `reservoirlevel` 테이블에 Upsert 합니다.
        /// </summary>
        public async Task BulkUpsertKrcReservoirDailyDataAsync(List<KrcReservoirLevelItem> levelData)
        {
            if (levelData == null || !levelData.Any())
            {
                _logAction("저장할 KRC 저수지 수위 데이터가 없습니다.");
                return;
            }

            var uniqueData = new Dictionary<(string facCode, DateTime obsDate), (string facName, string county, double? waterLevel, double? rate)>();

            foreach (var item in levelData)
            {
                if (string.IsNullOrWhiteSpace(item.FacCode) ||
                    !DateTime.TryParseExact(item.CheckDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var obsDate))
                {
                    _logAction($"잘못된 데이터 형식 건너뜀: FacCode='{item.FacCode}', CheckDate='{item.CheckDate}'");
                    continue;
                }

                double? waterLevel = null;
                if (double.TryParse(item.WaterLevel, NumberStyles.Any, CultureInfo.InvariantCulture, out var wl))
                {
                    waterLevel = wl;
                }

                double? rate = null;
                if (double.TryParse(item.Rate, NumberStyles.Any, CultureInfo.InvariantCulture, out var rt))
                {
                    rate = rt;
                }

                uniqueData[(item.FacCode, obsDate.Date)] = (item.FacName, item.County, waterLevel, rate);
            }

            if (!uniqueData.Any())
            {
                _logAction("처리할 유효한 KRC 저수지 수위 데이터가 없습니다.");
                return;
            }

            var facCodes = uniqueData.Keys.Select(k => k.facCode).ToList();
            var obsDates = uniqueData.Keys.Select(k => k.obsDate).ToList();
            var facNames = uniqueData.Values.Select(v => v.facName).ToList();
            var counties = uniqueData.Values.Select(v => v.county).ToList();
            var waterLevels = uniqueData.Values.Select(v => v.waterLevel).ToList();
            var rates = uniqueData.Values.Select(v => v.rate).ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"
                    INSERT INTO reservoirlevel (fac_code, check_date, fac_name, county, water_level, rate)
                    SELECT * FROM UNNEST(@fac_codes, @check_dates, @fac_names, @counties, @water_levels, @rates)
                    ON CONFLICT (fac_code, check_date) DO UPDATE SET
                        fac_name = EXCLUDED.fac_name,
                        county = EXCLUDED.county,
                        water_level = EXCLUDED.water_level,
                        rate = EXCLUDED.rate;";

                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("fac_codes", facCodes);
                    cmd.Parameters.AddWithValue("check_dates", obsDates);
                    cmd.Parameters.AddWithValue("fac_names", facNames.Select(n => (object)n ?? DBNull.Value).ToList());
                    cmd.Parameters.AddWithValue("counties", counties.Select(c => (object)c ?? DBNull.Value).ToList());
                    cmd.Parameters.AddWithValue("water_levels", waterLevels.Select(wl => wl.HasValue ? (object)wl.Value : DBNull.Value).ToList());
                    cmd.Parameters.AddWithValue("rates", rates.Select(r => r.HasValue ? (object)r.Value : DBNull.Value).ToList());

                    var affectedRows = await cmd.ExecuteNonQueryAsync();
                    _logAction($"{affectedRows} (총 {uniqueData.Count}개 항목) KRC 저수지 일별 수위/저수율 데이터가 `reservoirlevel` 테이블에 처리/업데이트되었습니다.");
                }
            }
        }
    }
}
