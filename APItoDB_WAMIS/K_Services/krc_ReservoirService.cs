using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
using KRC_Services.Models;

namespace KRC_Services.Services
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
        }

        private async Task<T> CallApiAsync<T>(string baseUrl, Dictionary<string, string> queryParams) where T : class
        {
            var paramList = new List<string>();

            foreach (var kv in queryParams)
            {
                if (kv.Key == "serviceKey")
                {
                    paramList.Add($"{kv.Key}={kv.Value}");
                }
                // county 파라미터가 공백(" ")일 경우에도 Uri.EscapeDataString을 적용 (원복)
                // else if (kv.Key == "county" && kv.Value == " ")
                // {
                //    paramList.Add($"{kv.Key}= "); // 공백을 그대로 전달
                // }
                else
                {
                    paramList.Add($"{kv.Key}={Uri.EscapeDataString(kv.Value ?? "")}");
                }
            }
            var requestUrl = baseUrl + "?" + string.Join("&", paramList);

            Console.WriteLine($"Requesting KRC API: [{requestUrl}]"); // 대괄호로 공백 확인


            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

                // 강제로 UTF-8로 읽기
                string xmlData;
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true))
                {
                    xmlData = await reader.ReadToEndAsync();
                }

                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        KrcOpenApiErrorResponse errorResponse = DeserializeXml<KrcOpenApiErrorResponse>(xmlData);
                        if (errorResponse != null && errorResponse.CmmMsgHeader != null)
                        {
                            throw new HttpRequestException(
                                $"API Error: {errorResponse.CmmMsgHeader.ErrMsg} (Code: {errorResponse.CmmMsgHeader.ReturnReasonCode}, AuthMsg: {errorResponse.CmmMsgHeader.ReturnAuthMsg}). URL: {requestUrl}");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        var genericResponse = DeserializeXml<KrcReservoirCodeResponse>(xmlData);
                        if (genericResponse != null && genericResponse.Header != null)
                        {
                            throw new HttpRequestException(
                               $"API Provider Error: {genericResponse.Header.ReturnAuthMsg} (Code: {genericResponse.Header.ReturnReasonCode}). URL: {requestUrl}");
                        }
                    }
                    response.EnsureSuccessStatusCode();
                }

                Console.WriteLine($"[DEBUG] KRC API Response XML for {typeof(T).Name}:\n{xmlData}"); // XML 데이터 로깅
                return DeserializeXml<T>(xmlData);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request Exception: {ex.Message} for URL: {requestUrl}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"XML Deserialization Exception: {ex.Message} for URL: {requestUrl}");
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
            var queryParams = new Dictionary<string, string>
            {
                { "serviceKey", _serviceKey },
                { "pageNo", pageNo.ToString() },
                { "numOfRows", numOfRows.ToString() }
            };

            if (!string.IsNullOrWhiteSpace(facName))
            {
                queryParams.Add("fac_name", facName);
            }

            // API 요구사항: fac_name과 county 중 하나는 필수일 수 있으나, API 가이드상 명확하지 않음.
            // county가 null이 아니고 빈 문자열도 아닐 때만 추가. 또는 county가 제공되면 항상 추가.
            // 기존 로직: county가 null이 아니거나, facName도 없으면 "county= "를 보냄.
            // 수정 로직: county가 유의미한 값을 가질 때만 추가. facName과 county 둘 다 없으면 API가 오류를 반환할 것으로 예상.
            // 또는, API가 county="" 를 허용하고 facName이 없을 때 필수 파라미터로 간주한다면 기존 로직이 맞을 수 있음.
            // 여기서는 county가 제공될 때만 추가하는 것으로 단순화. 만약 fac_name 없이 county만으로 조회해야하고 빈 county가 유효하다면,
            // 호출하는 쪽(MainFrm)에서 GetReservoirCodesAsync(county: " ") 와 같이 명시적으로 호출하거나,
            // 이 메소드 내에서 county가 null이고 facName도 null일 때 county = " " 로 설정하는 로직 추가 필요.
            if (string.IsNullOrWhiteSpace(facName) && string.IsNullOrWhiteSpace(county))
            {
                // facName과 county가 모두 제공되지 않은 경우, 전체 지역 조회를 위해 county를 공백으로 설정
                queryParams.Add("county", " ");
            }
            else if (!string.IsNullOrWhiteSpace(county))
            {
                queryParams.Add("county", county);
            }
            // facName만 제공된 경우는 county를 추가하지 않음


            // Console.WriteLine($"Request URL (Reservoir Codes): {requestUrl}"); // CallApiAsync 내부에서 로깅

            // 실제 API 호출 및 역직렬화
            return await CallApiAsync<KrcReservoirCodeResponse>(ReservoirCodeBaseUrl, queryParams);
        }


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
                Console.WriteLine("[TEST MODE] GetReservoirLevelsForInitialSetupAsync called.");
                return new KrcReservoirLevelResponse { Body = new KrcReservoirLevelBody { Items = new List<KrcReservoirLevelItem>() } };
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

            // var requestUrl = ReservoirLevelBaseUrl + "?" + await new FormUrlEncodedContent(queryParams).ReadAsStringAsync(); // CallApiAsync 내부에서 처리
            // Console.WriteLine($"Request URL (Initial Setup Reservoir Levels): {requestUrl}"); // CallApiAsync 내부에서 로깅

            // 실제 API 호출 및 XML 역직렬화
            return await CallApiAsync<KrcReservoirLevelResponse>(ReservoirLevelBaseUrl, queryParams);
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
                    Header = new KrcHeader { ReturnReasonCode = "00", ReturnAuthMsg = "NO_DATA_TO_UPDATE (ALREADY_CURRENT)" },
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