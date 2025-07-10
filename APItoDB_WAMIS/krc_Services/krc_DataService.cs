using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using WamisWaterLevelDataApi.Models; // KRC Models
using WamisWaterLevelDataApi.Services; // For KrcReservoirService if needed, or common utilities

namespace WamisWaterLevelDataApi.Services
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
        /// KRC 저수지 코드 정보를 stations 테이블에 Upsert 합니다.
        /// WAMIS의 UpsertStationsAsync를 참고하여 KRC API 응답에 맞게 수정.
        /// KRC API는 fac_code, fac_name, county를 제공. station_type은 'KRC_RESERVOIR'로 지정.
        /// </summary>
        public async Task UpsertKrcReservoirStationsAsync(List<KrcReservoirCodeItem> krcStations)
        {
            if (krcStations == null || !krcStations.Any())
            {
                _logAction("저장할 KRC 저수지 정보가 없습니다.");
                return;
            }

            var stationCodes = new List<string>();
            var stationNames = new List<string>();
            var stationTypes = new List<string>(); // KRC 저수지 타입으로 고정
            // county 정보는 stations 테이블에 직접 저장하지 않음 (필요시 별도 테이블 또는 확장)

            foreach (var station in krcStations)
            {
                if (string.IsNullOrWhiteSpace(station.FacCode)) continue;

                stationCodes.Add(station.FacCode);
                stationNames.Add(station.FacName); // fac_name을 station_name으로 사용
                stationTypes.Add("KRC_RESERVOIR"); // 고정된 타입
            }

            if (!stationCodes.Any()) return;

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
                    cmd.Parameters.AddWithValue("codes", stationCodes);
                    cmd.Parameters.AddWithValue("names", stationNames);
                    cmd.Parameters.AddWithValue("types", stationTypes);
                    var affectedRows = await cmd.ExecuteNonQueryAsync();
                    _logAction($"{affectedRows} (요청된 KRC 저수지 {stationCodes.Count}개 중) KRC 저수지 관측소 정보가 처리/업데이트되었습니다.");
                }
            }
        }

        /// <summary>
        /// KRC 저수지 일별 수위 및 저수율 데이터를 krc_reservoir_daily 테이블에 Upsert 합니다.
        /// </summary>
        public async Task BulkUpsertKrcReservoirDailyDataAsync(List<KrcReservoirLevelItem> levelData)
        {
            if (levelData == null || !levelData.Any())
            {
                _logAction("저장할 KRC 저수지 수위 데이터가 없습니다.");
                return;
            }

            // 중복 제거 및 데이터 변환 (station_code, obs_date 기준)
            var uniqueData = new Dictionary<(string facCode, DateTime obsDate), (double? waterLevel, double? rate)>();

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

                uniqueData[(item.FacCode, obsDate.Date)] = (waterLevel, rate);
            }

            if (!uniqueData.Any())
            {
                _logAction("처리할 유효한 KRC 저수지 수위 데이터가 없습니다.");
                return;
            }

            var stationCodes = uniqueData.Keys.Select(k => k.facCode).ToList();
            var obsDates = uniqueData.Keys.Select(k => k.obsDate).ToList();
            var waterLevels = uniqueData.Values.Select(v => v.waterLevel).ToList();
            var rates = uniqueData.Values.Select(v => v.rate).ToList();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var commandText = @"
                    INSERT INTO krc_reservoir_daily (station_code, obs_date, water_level, rate)
                    SELECT * FROM UNNEST(@station_codes, @obs_dates, @water_levels, @rates)
                    ON CONFLICT (station_code, obs_date) DO UPDATE SET
                        water_level = EXCLUDED.water_level,
                        rate = EXCLUDED.rate;";

                using (var cmd = new NpgsqlCommand(commandText, conn))
                {
                    cmd.Parameters.AddWithValue("station_codes", stationCodes);
                    cmd.Parameters.AddWithValue("obs_dates", obsDates);
                    cmd.Parameters.AddWithValue("water_levels", waterLevels.Select(wl => wl.HasValue ? (object)wl.Value : DBNull.Value).ToList());
                    cmd.Parameters.AddWithValue("rates", rates.Select(r => r.HasValue ? (object)r.Value : DBNull.Value).ToList());

                    var affectedRows = await cmd.ExecuteNonQueryAsync();
                    _logAction($"{affectedRows} (총 {uniqueData.Count}개 항목) KRC 저수지 일별 수위/저수율 데이터가 처리/업데이트되었습니다.");
                }
            }
        }
    }
}
