using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
using WamisWaterLevelDataApi.Models; // Assuming Models namespace

namespace WamisWaterLevelDataApi.Services
{
    public class KrcReservoirService
    {
        private readonly HttpClient _httpClient;
        private readonly string _serviceKey;
        private const string ReservoirCodeBaseUrl = "http://apis.data.go.kr/B552149/reserviorWaterLevel/reservoircode/";
        private const string ReservoirLevelBaseUrl = "http://apis.data.go.kr/B552149/reserviorWaterLevel/reservoirlevel/";

        public KrcReservoirService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _serviceKey = ConfigurationManager.AppSettings["KrcApiKey"];
            if (string.IsNullOrEmpty(_serviceKey))
            {
                // Fallback for environments where App.config might not be easily available (e.g., testing without full config setup)
                // Or throw an exception if the key is absolutely mandatory.
                Console.WriteLine("Warning: KrcApiKey not found in App.config. Using placeholder. This should be configured for actual use.");
                // In a real application, throw new ConfigurationErrorsException("KrcApiKey is not configured in App.config");
                _serviceKey = "YOUR_KRC_API_KEY_FALLBACK"; // Ensure this key is valid or handle appropriately
            }
        }

        private async Task<T> CallApiAsync<T>(string baseUrl, Dictionary<string, string> queryParams) where T : class
        {
            var requestUrl = baseUrl + "?" + await new FormUrlEncodedContent(queryParams).ReadAsStringAsync();
            Console.WriteLine($"Requesting KRC API: {requestUrl}"); // Logging the request URL

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

                string xmlData = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Try to deserialize as KrcOpenApiErrorResponse first
                    try
                    {
                        KrcOpenApiErrorResponse errorResponse = DeserializeXml<KrcOpenApiErrorResponse>(xmlData);
                        if (errorResponse != null && errorResponse.CmmMsgHeader != null)
                        {
                            throw new HttpRequestException(
                                $"API Error: {errorResponse.CmmMsgHeader.ErrMsg} (Code: {errorResponse.CmmMsgHeader.ReturnReasonCode}, AuthMsg: {errorResponse.CmmMsgHeader.ReturnAuthMsg}). URL: {requestUrl}");
                        }
                    }
                    catch (InvalidOperationException) // Not an OpenAPIErrorResponse, try KrcHeader style
                    {
                        // Try to deserialize as a generic response to get KrcHeader for provider errors
                        var genericResponse = DeserializeXml<KrcReservoirCodeResponse>(xmlData); // Or any other type that has KrcHeader
                        if (genericResponse != null && genericResponse.Header != null)
                        {
                             throw new HttpRequestException(
                                $"API Provider Error: {genericResponse.Header.ReturnAuthMsg} (Code: {genericResponse.Header.ReturnReasonCode}). URL: {requestUrl}");
                        }
                    }
                    // If neither deserialization works, throw a generic error with status code
                    response.EnsureSuccessStatusCode(); // This will throw if not success and not handled above
                }

                return DeserializeXml<T>(xmlData);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request Exception: {ex.Message} for URL: {requestUrl}");
                throw; // Re-throw to be handled by the caller
            }
            catch (InvalidOperationException ex) // Catches XML deserialization errors
            {
                Console.WriteLine($"XML Deserialization Exception: {ex.Message} for URL: {requestUrl}");
                // Consider logging the raw XML (xmlData) here for debugging if it's not too large
                throw new InvalidOperationException($"Failed to deserialize XML response from KRC API. URL: {requestUrl}. Error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Generic Exception during API call: {ex.Message} for URL: {requestUrl}");
                throw;
            }
        }


        // --- 저수지 코드 조회 (reservoircode) ---

        /// <summary>
        /// 저수지 코드를 조회합니다. fac_name 또는 county 중 하나는 필수입니다.
        /// </summary>
        /// <param name="facName">저수지 이름</param>
        /// <param name="county">저수지 위치 (시/군)</param>
        /// <param name="numOfRows">한 페이지 결과 수</param>
        /// <param name="pageNo">페이지 번호</param>
        /// <returns>KrcReservoirCodeResponse 객체</returns>
        public async Task<KrcReservoirCodeResponse> GetReservoirCodesAsync(string facName = null, string county = null, int numOfRows = 10, int pageNo = 1)
        {
            if (string.IsNullOrWhiteSpace(facName) && string.IsNullOrWhiteSpace(county))
            {
                throw new ArgumentException("저수지 이름(facName) 또는 저수지 위치(county) 중 하나는 반드시 입력해야 합니다.");
            }

            var queryParams = new Dictionary<string, string>
            {
                { "serviceKey", _serviceKey },
                { "numOfRows", numOfRows.ToString() },
                { "pageNo", pageNo.ToString() }
            };

            if (!string.IsNullOrWhiteSpace(facName))
            {
                queryParams.Add("fac_name", facName);
            }
            if (!string.IsNullOrWhiteSpace(county))
            {
                queryParams.Add("county", county);
            }

            var requestUrl = ReservoirCodeBaseUrl + "?" + await new FormUrlEncodedContent(queryParams).ReadAsStringAsync();

            // 실제 API 호출 및 XML 역직렬화 로직 (다음 단계에서 구체화)
            // HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
            // response.EnsureSuccessStatusCode();
            // string xmlData = await response.Content.ReadAsStringAsync();
            // return DeserializeXml<KrcReservoirCodeResponse>(xmlData);
            Console.WriteLine($"Request URL (Reservoir Codes): {requestUrl}");
            return await Task.FromResult(new KrcReservoirCodeResponse()); // Placeholder
        }


        // --- 저수지 수위 조회 (reservoirlevel) ---

        /// <summary>
        /// 지정된 저수지 코드와 기간으로 수위 정보를 조회합니다. (초기 데이터 구축용)
        /// </summary>
        /// <param name="facCode">저수지 코드</param>
        /// <param name="dateS">조회 시작 날짜 (YYYYMMDD)</param>
        /// <param name="dateE">조회 종료 날짜 (YYYYMMDD)</param>
        /// <param name="county">저수지 위치 (선택 사항, facCode와 함께 사용 가능)</param>
        /// <param name="numOfRows">한 페이지 결과 수</param>
        /// <param name="pageNo">페이지 번호</param>
        /// <param name="isTestMode">테스트 모드 여부</param>
        /// <returns>KrcReservoirLevelResponse 객체</returns>
        public async Task<KrcReservoirLevelResponse> GetReservoirLevelsForInitialSetupAsync(
            string facCode, string dateS, string dateE, string county = null,
            int numOfRows = 30, int pageNo = 1, bool isTestMode = false)
        {
            if (string.IsNullOrWhiteSpace(facCode) && string.IsNullOrWhiteSpace(county))
            {
                 throw new ArgumentException("저수지 코드(facCode) 또는 저수지 위치(county) 중 하나는 반드시 입력해야 합니다.");
            }
            if (isTestMode)
            {
                // 테스트 모드일 경우 목업 데이터 반환 또는 특정 로직 수행
                Console.WriteLine("[TEST MODE] GetReservoirLevelsForInitialSetupAsync called.");
                return new KrcReservoirLevelResponse { Body = new KrcReservoirLevelBody { Items = new List<KrcReservoirLevelItem>() }};
            }

            var queryParams = new Dictionary<string, string>
            {
                { "serviceKey", _serviceKey },
                { "date_s", dateS },
                { "date_e", dateE },
                { "numOfRows", numOfRows.ToString() },
                { "pageNo", pageNo.ToString() }
            };

            if (!string.IsNullOrWhiteSpace(facCode))
            {
                queryParams.Add("fac_code", facCode);
            }
            if (!string.IsNullOrWhiteSpace(county))
            {
                 queryParams.Add("county", county);
            }

            var requestUrl = ReservoirLevelBaseUrl + "?" + await new FormUrlEncodedContent(queryParams).ReadAsStringAsync();

            // 실제 API 호출 및 XML 역직렬화 로직 (다음 단계에서 구체화)
            Console.WriteLine($"Request URL (Initial Setup Reservoir Levels): {requestUrl}");
            return await Task.FromResult(new KrcReservoirLevelResponse()); // Placeholder
        }

        /// <summary>
        /// 최신 저수지 수위 정보를 조회합니다. (실시간 데이터 수집용)
        /// </summary>
        /// <param name="facCode">저수지 코드</param>
        /// <param name="county">저수지 위치 (선택 사항, facCode와 함께 사용 가능)</param>
        /// <param name="isTestMode">테스트 모드 여부</param>
        /// <returns>KrcReservoirLevelResponse 객체</returns>
        public async Task<KrcReservoirLevelResponse> GetRealtimeReservoirLevelsAsync(string facCode, string county = null, bool isTestMode = false)
        {
            // 실시간 데이터는 보통 당일 또는 최근 1일 데이터를 의미하므로, date_s와 date_e를 오늘 날짜로 설정
            string today = DateTime.Now.ToString("yyyyMMdd");
            // For realtime, typically, we want a small number of results, e.g., the latest single reading.
            // The KRC API might not directly support "latest", so getting today's data is a common approach.
            // numOfRows=1 might be appropriate if the API sorts by latest time within the day.
            // If not, we might need to fetch more and pick the latest. For now, using 10 as a small batch.
            return await GetReservoirLevelsForInitialSetupAsync(facCode, today, today, county, 10, 1, isTestMode);
        }

        /// <summary>
        /// DB에 저장된 마지막 데이터 이후부터 현재까지의 저수지 수위 정보를 조회합니다. (데이터 최신화용)
        /// </summary>
        /// <param name="facCode">저수지 코드</param>
        /// <param name="lastSavedDate">DB에 저장된 마지막 데이터의 날짜 (YYYYMMDD)</param>
        /// <param name="county">저수지 위치 (선택 사항, facCode와 함께 사용 가능)</param>
        /// <param name="isTestMode">테스트 모드 여부</param>
        /// <returns>KrcReservoirLevelResponse 객체</returns>
        public async Task<KrcReservoirLevelResponse> UpdateReservoirLevelsAsync(string facCode, string lastSavedDate, string county = null, bool isTestMode = false)
        {
            if (string.IsNullOrEmpty(lastSavedDate) || lastSavedDate.Length != 8)
            {
                throw new ArgumentException("유효한 마지막 저장 날짜(lastSavedDate, YYYYMMDD)를 입력해야 합니다.");
            }

            DateTime startDateFromDb;
            if (!DateTime.TryParseExact(lastSavedDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out startDateFromDb))
            {
                throw new ArgumentException("lastSavedDate 형식이 잘못되었습니다. YYYYMMDD 형식이어야 합니다.");
            }

            DateTime startDateForApi = startDateFromDb.AddDays(1);
            string dateS = startDateForApi.ToString("yyyyMMdd");
            string dateE = DateTime.Now.ToString("yyyyMMdd");

            if (startDateForApi > DateTime.Now.Date) // 이미 최신 데이터 (날짜만 비교)
            {
                Console.WriteLine($"Data for facCode {facCode} is already up to date (Last saved: {lastSavedDate}). No new data to fetch.");
                return new KrcReservoirLevelResponse
                {
                    Header = new KrcHeader { ReturnReasonCode = "00", ReturnAuthMsg = "NO_DATA_TO_UPDATE (ALREADY_CURRENT)"},
                    Body = new KrcReservoirLevelBody { Items = new List<KrcReservoirLevelItem>(), TotalCount = 0 }
                };
            }

            // KRC API는 저수지별 조회 시 최대 365일
            // 기간이 365일을 초과하는 경우, 여러 번 호출해야 할 수 있으나,
            // 이 메소드는 '최신화'이므로, 마지막 저장일로부터 현재까지의 기간이 매우 길지 않다고 가정.
            // 만약 길다면, 호출하는 쪽에서 분할 호출 전략 필요. 여기서는 단일 호출로 가정.
            int maxRowsForUpdate = 365; // API 제약조건에 따라 설정
            return await GetReservoirLevelsForInitialSetupAsync(facCode, dateS, dateE, county, maxRowsForUpdate, 1, isTestMode);
        }

        private T DeserializeXml<T>(string xmlData) where T : class
        {
            if (string.IsNullOrWhiteSpace(xmlData))
            {
                Console.WriteLine("XML data is null or whitespace. Cannot deserialize.");
                return null; // 또는 throw new ArgumentNullException(nameof(xmlData));
            }
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using (var reader = new StringReader(xmlData))
                {
                    return serializer.Deserialize(reader) as T;
                }
            }
            catch (InvalidOperationException ex)
            {
                // Log the XML data that failed to deserialize for easier debugging
                // Be cautious if XML data can be very large or contain sensitive info before logging.
                Console.WriteLine($"Error deserializing XML. Data: \n{xmlData}\nError: {ex.ToString()}");
                throw; // Re-throw the exception to be handled by the caller
            }
        }
    }
}
