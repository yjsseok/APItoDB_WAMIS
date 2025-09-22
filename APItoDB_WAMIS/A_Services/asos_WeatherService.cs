using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Globalization;
using log4net;
using Newtonsoft.Json;
using APItoDB_WAMIS.A_Models;

namespace APItoDB_WAMIS.A_Services
{
    public class asos_WeatherService : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(asos_WeatherService));
        private readonly HttpClient httpClient;
        private readonly string apiKey;
        private readonly string baseUrl = "https://apihub.kma.go.kr/api/typ01/url/kma_sfcdd.php";

        public asos_WeatherService()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            apiKey = ConfigurationManager.AppSettings["ASOSApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("ASOSApiKey가 app.config에 설정되지 않았습니다.");
            }
        }

        public async Task<List<ASOS_WeatherData>> GetDailyWeatherDataAsync(DateTime date)
        {
            try
            {
                string dateStr = date.ToString("yyyyMMdd");
                string url = $"{baseUrl}?tm={dateStr}&stn=0&disp=0&help=0&authKey={apiKey}";
                
                log.Info($"기상청 ASOS API 호출: {date:yyyy-MM-dd}");
                
                using (HttpResponseMessage response = await httpClient.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        return ParseCsvResponse(content, date);
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        log.Error($"API 호출 실패: {response.StatusCode}, {errorContent}");
                        throw new HttpRequestException($"API 호출 실패: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"기상 데이터 조회 중 오류 발생 (날짜: {date:yyyy-MM-dd})", ex);
                throw;
            }
        }

        public async Task<List<ASOS_WeatherData>> GetWeatherDataRangeAsync(DateTime startDate, DateTime endDate, 
            IProgress<int> progress = null)
        {
            var allData = new List<ASOS_WeatherData>();
            var currentDate = startDate;
            int totalDays = (endDate - startDate).Days + 1;
            int processedDays = 0;

            try
            {
                while (currentDate <= endDate)
                {
                    try
                    {
                        var dailyData = await GetDailyWeatherDataAsync(currentDate);
                        if (dailyData != null && dailyData.Count > 0)
                        {
                            allData.AddRange(dailyData);
                            log.Info($"기상 데이터 수집 완료: {currentDate:yyyy-MM-dd}, {dailyData.Count}건");
                        }
                        
                        processedDays++;
                        progress?.Report((processedDays * 100) / totalDays);
                        
                        // API 호출 제한을 고려한 지연
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        log.Warn($"날짜 {currentDate:yyyy-MM-dd} 데이터 수집 실패: {ex.Message}");
                    }
                    
                    currentDate = currentDate.AddDays(1);
                }

                log.Info($"기상 데이터 수집 완료: 총 {allData.Count}건");
                return allData;
            }
            catch (Exception ex)
            {
                log.Error("기간별 기상 데이터 수집 중 오류 발생", ex);
                throw;
            }
        }

        private List<ASOS_WeatherData> ParseCsvResponse(string csvContent, DateTime requestDate)
        {
            var weatherDataList = new List<ASOS_WeatherData>();
            
            if (string.IsNullOrWhiteSpace(csvContent))
            {
                log.Warn($"빈 응답 데이터 (날짜: {requestDate:yyyy-MM-dd})");
                return weatherDataList;
            }

            try
            {
                var lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (lines.Length < 2)
                {
                    log.Warn($"CSV 헤더 또는 데이터가 없습니다 (날짜: {requestDate:yyyy-MM-dd})");
                    return weatherDataList;
                }

                string[] headers = lines[0].Split(',');
                
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        string[] values = lines[i].Split(',');
                        
                        if (values.Length != headers.Length)
                        {
                            log.Warn($"CSV 행 데이터 불일치: 라인 {i + 1}");
                            continue;
                        }

                        var weatherData = ParseWeatherDataFromCsv(headers, values);
                        if (weatherData != null)
                        {
                            weatherDataList.Add(weatherData);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Warn($"CSV 파싱 오류 (라인 {i + 1}): {ex.Message}");
                    }
                }

                log.Info($"CSV 파싱 완료: {weatherDataList.Count}건 (날짜: {requestDate:yyyy-MM-dd})");
            }
            catch (Exception ex)
            {
                log.Error($"CSV 응답 파싱 중 오류 발생 (날짜: {requestDate:yyyy-MM-dd})", ex);
            }

            return weatherDataList;
        }

        private ASOS_WeatherData ParseWeatherDataFromCsv(string[] headers, string[] values)
        {
            var weatherData = new ASOS_WeatherData();

            for (int i = 0; i < headers.Length && i < values.Length; i++)
            {
                string header = headers[i].Trim();
                string value = values[i].Trim();

                if (string.IsNullOrEmpty(value) || value == "-")
                    continue;

                try
                {
                    switch (header.ToUpper())
                    {
                        case "TM": weatherData.TM = value; break;
                        case "WD": weatherData.WD = ParseNullableInt(value); break;
                        case "GST_WD": weatherData.GST_WD = ParseNullableFloat(value); break;
                        case "GST_TM": weatherData.GST_TM = ParseNullableFloat(value); break;
                        case "PS": weatherData.PS = ParseNullableFloat(value); break;
                        case "PR": weatherData.PR = ParseNullableFloat(value); break;
                        case "TD": weatherData.TD = ParseNullableFloat(value); break;
                        case "PV": weatherData.PV = ParseNullableFloat(value); break;
                        case "RN_DAY": weatherData.RN_DAY = ParseNullableFloat(value); break;
                        case "SD_HR3": weatherData.SD_HR3 = ParseNullableFloat(value); break;
                        case "SD_TOT": weatherData.SD_TOT = ParseNullableFloat(value); break;
                        case "WP": weatherData.WP = ParseNullableFloat(value); break;
                        case "CA_TOT": weatherData.CA_TOT = ParseNullableFloat(value); break;
                        case "CH_MIN": weatherData.CH_MIN = ParseNullableFloat(value); break;
                        case "CT_TOP": weatherData.CT_TOP = ParseNullableFloat(value); break;
                        case "CT_LOW": weatherData.CT_LOW = ParseNullableFloat(value); break;
                        case "SI": weatherData.SI = ParseNullableFloat(value); break;
                        case "TS": weatherData.TS = ParseNullableFloat(value); break;
                        case "TE_01": weatherData.TE_01 = ParseNullableFloat(value); break;
                        case "TE_03": weatherData.TE_03 = ParseNullableFloat(value); break;
                        case "WH": weatherData.WH = ParseNullableFloat(value); break;
                        case "IR": weatherData.IR = ParseNullableFloat(value); break;
                        case "RN_JUN": weatherData.RN_JUN = ParseNullableFloat(value); break;
                        case "WR_DAY": weatherData.WR_DAY = ParseNullableFloat(value); break;
                        case "WS_MAX": weatherData.WS_MAX = ParseNullableFloat(value); break;
                        case "WD_INS": weatherData.WD_INS = ParseNullableFloat(value); break;
                        case "WS_INS_TM": weatherData.WS_INS_TM = ParseNullableFloat(value); break;
                        case "TA_MAX": weatherData.TA_MAX = ParseNullableFloat(value); break;
                        case "TA_MIN": weatherData.TA_MIN = ParseNullableFloat(value); break;
                        case "TD_AVG": weatherData.TD_AVG = ParseNullableFloat(value); break;
                        case "TG_MIN": weatherData.TG_MIN = ParseNullableFloat(value); break;
                        case "HM_MIN": weatherData.HM_MIN = ParseNullableFloat(value); break;
                        case "PV_AVG": weatherData.PV_AVG = ParseNullableFloat(value); break;
                        case "EV_L": weatherData.EV_L = ParseNullableFloat(value); break;
                        case "PA_AVG": weatherData.PA_AVG = ParseNullableFloat(value); break;
                        case "PS_MAX": weatherData.PS_MAX = ParseNullableFloat(value); break;
                        case "PS_MIN": weatherData.PS_MIN = ParseNullableFloat(value); break;
                        case "SS_DAY": weatherData.SS_DAY = ParseNullableFloat(value); break;
                        case "SS_CMB": weatherData.SS_CMB = ParseNullableFloat(value); break;
                        case "RN_D99": weatherData.RN_D99 = ParseNullableFloat(value); break;
                        case "SD_NEW": weatherData.SD_NEW = ParseNullableFloat(value); break;
                        case "SD_MAX": weatherData.SD_MAX = ParseNullableFloat(value); break;
                        case "TE_05": weatherData.TE_05 = ParseNullableFloat(value); break;
                        case "TE_15": weatherData.TE_15 = ParseNullableFloat(value); break;
                        case "TE_50": weatherData.TE_50 = ParseNullableFloat(value); break;
                        case "TMST": weatherData.TMST = ParseNullableFloat(value); break;
                        case "DTM": weatherData.DTM = ParseNullableFloat(value); break;
                        case "DIR": weatherData.DIR = ParseNullableFloat(value); break;
                        case "LAT": weatherData.LAT = ParseNullableFloat(value); break;
                        case "VAL": weatherData.VAL = ParseNullableFloat(value); break;
                        case "STN": weatherData.STN = ParseNullableFloat(value); break;
                        case "WS": weatherData.WS = ParseNullableFloat(value); break;
                        case "GST_WS": weatherData.GST_WS = ParseNullableFloat(value); break;
                        case "PA": weatherData.PA = ParseNullableFloat(value); break;
                        case "PT": weatherData.PT = ParseNullableFloat(value); break;
                        case "TA": weatherData.TA = ParseNullableFloat(value); break;
                        case "HM": weatherData.HM = ParseNullableFloat(value); break;
                        case "RN": weatherData.RN = ParseNullableFloat(value); break;
                        case "RN_INT": weatherData.RN_INT = ParseNullableFloat(value); break;
                        case "SD_DAY": weatherData.SD_DAY = ParseNullableFloat(value); break;
                        case "WC": weatherData.WC = ParseNullableFloat(value); break;
                        case "WW": weatherData.WW = ParseNullableFloat(value); break;
                        case "CA_MID": weatherData.CA_MID = ParseNullableFloat(value); break;
                        case "CT": weatherData.CT = ParseNullableFloat(value); break;
                        case "CT_MID": weatherData.CT_MID = ParseNullableFloat(value); break;
                        case "SS": weatherData.SS = ParseNullableFloat(value); break;
                        case "ST_GD": weatherData.ST_GD = ParseNullableFloat(value); break;
                        case "TE_005": weatherData.TE_005 = ParseNullableFloat(value); break;
                        case "TE_02": weatherData.TE_02 = ParseNullableFloat(value); break;
                        case "ST_SEA": weatherData.ST_SEA = ParseNullableFloat(value); break;
                        case "BF": weatherData.BF = ParseNullableFloat(value); break;
                        case "IX": weatherData.IX = ParseNullableFloat(value); break;
                        case "WS_AVG": weatherData.WS_AVG = ParseNullableFloat(value); break;
                        case "WD_MAX": weatherData.WD_MAX = ParseNullableFloat(value); break;
                        case "WS_MAX_TM": weatherData.WS_MAX_TM = ParseNullableFloat(value); break;
                        case "WS_INS": weatherData.WS_INS = ParseNullableFloat(value); break;
                        case "TA_AVG": weatherData.TA_AVG = ParseNullableFloat(value); break;
                        case "TA_MAX_TM": weatherData.TA_MAX_TM = ParseNullableFloat(value); break;
                        case "TA_MIN_TM": weatherData.TA_MIN_TM = ParseNullableFloat(value); break;
                        case "TS_AVG": weatherData.TS_AVG = ParseNullableFloat(value); break;
                        case "HM_AVG": weatherData.HM_AVG = ParseNullableFloat(value); break;
                        case "HM_MIN_TM": weatherData.HM_MIN_TM = ParseNullableFloat(value); break;
                        case "EV_S": weatherData.EV_S = ParseNullableFloat(value); break;
                        case "FG_DUR": weatherData.FG_DUR = ParseNullableFloat(value); break;
                        case "PS_AVG": weatherData.PS_AVG = ParseNullableFloat(value); break;
                        case "PS_MAX_TM": weatherData.PS_MAX_TM = ParseNullableFloat(value); break;
                        case "PS_MIN_TM": weatherData.PS_MIN_TM = ParseNullableFloat(value); break;
                        case "SS_DUR": weatherData.SS_DUR = ParseNullableFloat(value); break;
                        case "SI_DAY": weatherData.SI_DAY = ParseNullableFloat(value); break;
                        case "RN_DUR": weatherData.RN_DUR = ParseNullableFloat(value); break;
                        case "SD_NEW_TM": weatherData.SD_NEW_TM = ParseNullableFloat(value); break;
                        case "SD_MAX_TM": weatherData.SD_MAX_TM = ParseNullableFloat(value); break;
                        case "TE_10": weatherData.TE_10 = ParseNullableFloat(value); break;
                        case "TE_30": weatherData.TE_30 = ParseNullableFloat(value); break;
                        case "S": weatherData.S = ParseNullableFloat(value); break;
                        case "TMED": weatherData.TMED = ParseNullableFloat(value); break;
                        case "ST": weatherData.ST = ParseNullableFloat(value); break;
                        case "LON": weatherData.LON = ParseNullableFloat(value); break;
                        case "HT": weatherData.HT = ParseNullableFloat(value); break;
                    }
                }
                catch (Exception ex)
                {
                    log.Warn($"필드 파싱 오류 ({header}={value}): {ex.Message}");
                }
            }

            return weatherData;
        }

        private int? ParseNullableInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-")
                return null;
            
            if (int.TryParse(value, out int result))
                return result;
                
            return null;
        }

        private float? ParseNullableFloat(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-")
                return null;
            
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;
                
            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                httpClient?.Dispose();
            }
        }
    }
}