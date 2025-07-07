using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WamisDataCollector.Models;

namespace WamisDataCollector.Services
{
    public class DataSyncService
    {
        private readonly WamisApiClient _apiClient;
        private readonly DataService _dataService;
        private readonly Action<string> _logAction;
        public DataSyncService(WamisApiClient apiClient, DataService dataService, Action<string> logAction)
        {
            _apiClient = apiClient;
            _dataService = dataService;
            _logAction = logAction;
        }

        public async Task PerformInitialLoadAsync(DateTime startDate, DateTime endDate)
        {
            _logAction("초기 데이터 로드를 시작합니다...");
            await _dataService.EnsureTablesExistAsync();
            await FetchAndStoreAllStations();

            //var allStations = await _dataService.GetAllStationsAsync();
            //var rfStations = allStations.Where(s => s.StationType == "RF").ToList();


            //테스트를 위한 단일 관측소 목록 직접 생성######################################################################################################################################
            _logAction("[테스트 모드] 단일 관측소에 대해서만 데이터를 수집합니다.");
            var rfStations = new List<WamisDataCollector.Models.StationInfo>
                {
                    // 아래에 테스트하고 싶은 관측소 코드를 입력하세요.
                  new WamisDataCollector.Models.StationInfo { StationCode = "50024051", StationType = "RF" }
                };

            foreach (var station in rfStations)
            {
                _logAction($"[{station.StationCode}] 관측소 데이터 수집 시작 ({startDate:yyyy} ~ {endDate:yyyy})...");

                // 1. 시자료 수집
                for (var year = startDate.Year; year <= endDate.Year; year++)
                {
                    var yearStartDate = (year == startDate.Year) ? startDate : new DateTime(year, 1, 1);
                    var yearEndDate = (year == endDate.Year) ? endDate : new DateTime(year, 12, 31);

                    _logAction($"  {year}년도 강수량 시자료 수집 중...");
                    var parameters = new Dictionary<string, string>
                    {
                        { "obscd", station.StationCode },
                        { "startdt", yearStartDate.ToString("yyyyMMdd") },
                        { "enddt", yearEndDate.ToString("yyyyMMdd") }
                    };
                    var response = await _apiClient.GetDataAsync<RainfallResponse>("wkw/rf_hrdata", parameters);
                    if (response?.List != null)
                    {
                        await _dataService.BulkUpsertRainfallHourlyAsync(response.List, station.StationCode);
                    }
                    await Task.Delay(250);
                }

                // 2. 일자료 수집
                _logAction($"  전체 기간 강수량 일자료 수집 중...");
                var dailyParams = new Dictionary<string, string>
                {
                    { "obscd", station.StationCode },
                    { "startdt", startDate.ToString("yyyyMMdd") },
                    { "enddt", endDate.ToString("yyyyMMdd") }
                };
                var dailyResponse = await _apiClient.GetDataAsync<RainfallResponse>("wkw/rf_dtdata", dailyParams);
                if (dailyResponse?.List != null)
                {
                    await _dataService.BulkUpsertRainfallDailyAsync(dailyResponse.List, station.StationCode);
                }
                await Task.Delay(250);

                // 3. 월자료 수집
                _logAction($"  전체 기간 강수량 월자료 수집 중...");
                var monthlyParams = new Dictionary<string, string>
                {
                    { "obscd", station.StationCode },
                    { "startyear", startDate.ToString("yyyy") },
                    { "endyear", endDate.ToString("yyyy") }
                };
                var monthlyResponse = await _apiClient.GetDataAsync<RainfallResponse>("wkw/rf_mndata", monthlyParams);
                if (monthlyResponse?.List != null)
                {
                    await _dataService.BulkUpsertRainfallMonthlyAsync(monthlyResponse.List, station.StationCode);
                }
                await Task.Delay(250);
            }
            _logAction("초기 데이터 로드가 완료되었습니다.");
        }
        public async Task PerformDailyUpdateAsync()
        {
            _logAction("일별 데이터 최신화를 시작합니다...");
            var endDate = DateTime.Today;

            // var allStations = await _dataService.GetAllStationsAsync();
            // 2. 테스트를 위한 단일 관측소 목록 직접 생성
            _logAction("[테스트 모드] 단일 관측소에 대해서만 일별 최신화를 수행합니다.");
            var allStations = new List<WamisDataCollector.Models.StationInfo>
                   {
                             // 아래에 테스트하고 싶은 관측소 코드를 입력하세요.
                       new WamisDataCollector.Models.StationInfo { StationCode = "50024051", StationType = "RF" }
                   };      

            foreach (var station in allStations)
            {
                if (station.StationType == "RF")
                {
                    _logAction($"[{station.StationCode}] 강수량 데이터 최신화...");

                    // 1. 시자료 최신화
                    var lastHourlyDate = await _dataService.GetLastHourlyRainfallDateAsync(station.StationCode);
                    var startHourlyDate = lastHourlyDate?.AddHours(1) ?? DateTime.Today.AddDays(-3);
                    _logAction($"  시자료 최신화 중 (시작일: {startHourlyDate:yyyy-MM-dd HH:mm})...");
                    var hourlyParams = new Dictionary<string, string>
                    {
                        { "obscd", station.StationCode },
                        { "startdt", startHourlyDate.ToString("yyyyMMdd") },
                        { "enddt", endDate.ToString("yyyyMMdd") }
                    };
                    var hourlyResponse = await _apiClient.GetDataAsync<RainfallResponse>("wkw/rf_hrdata", hourlyParams);
                    if (hourlyResponse?.List != null)
                    {
                        await _dataService.BulkUpsertRainfallHourlyAsync(hourlyResponse.List, station.StationCode);
                    }
                    await Task.Delay(250);

                    // 2. 일자료 최신화
                    var lastDailyDate = await _dataService.GetLastDailyRainfallDateAsync(station.StationCode);
                    var startDailyDate = lastDailyDate?.AddDays(1) ?? DateTime.Today.AddDays(-3);
                    _logAction($"  일자료 최신화 중 (시작일: {startDailyDate:yyyy-MM-dd})...");
                    var dailyParams = new Dictionary<string, string>
                    {
                        { "obscd", station.StationCode },
                        { "startdt", startDailyDate.ToString("yyyyMMdd") },
                        { "enddt", endDate.ToString("yyyyMMdd") }
                    };
                    var dailyResponse = await _apiClient.GetDataAsync<RainfallResponse>("wkw/rf_dtdata", dailyParams);
                    if (dailyResponse?.List != null)
                    {
                        await _dataService.BulkUpsertRainfallDailyAsync(dailyResponse.List, station.StationCode);
                    }
                    await Task.Delay(250);

                    // 3. 월자료 최신화
                    var lastMonthlyDate = await _dataService.GetLastMonthlyRainfallDateAsync(station.StationCode);
                    var startMonthlyYear = lastMonthlyDate?.Year ?? endDate.Year;
                    _logAction($"  월자료 최신화 중 (시작년도: {startMonthlyYear})...");
                    var monthlyParams = new Dictionary<string, string>
                    {
                        { "obscd", station.StationCode },
                        { "startyear", startMonthlyYear.ToString("yyyy") },
                        { "endyear", endDate.ToString("yyyy") }
                    };
                    var monthlyResponse = await _apiClient.GetDataAsync<RainfallResponse>("wkw/rf_mndata", monthlyParams);
                    if (monthlyResponse?.List != null)
                    {
                        await _dataService.BulkUpsertRainfallMonthlyAsync(monthlyResponse.List, station.StationCode);
                    }
                    await Task.Delay(250);
                }
            }

            _logAction("일별 데이터 최신화가 완료되었습니다.");
        }
        public async Task BackfillMissingDataAsync()
        {
            _logAction("누락 데이터 보충을 시작합니다...");
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today;

            // 1. 기존 코드 주석 처리
            /*
            var allStations = await _dataService.GetAllStationsAsync();
            */

            // 2. 테스트를 위한 단일 관측소 목록 직접 생성
            _logAction("[테스트 모드] 단일 관측소에 대해서만 일별 최신화를 수행합니다.");
            var allStations = new List<WamisDataCollector.Models.StationInfo>
    {
        // 아래에 테스트하고 싶은 관측소 코드를 입력하세요.
        new WamisDataCollector.Models.StationInfo { StationCode = "50024051", StationType = "RF" }
    };


            foreach (var station in allStations)
            {
                if (station.StationType == "RF")
                {
                    _logAction($"[{station.StationCode}] 관측소의 최근 7일 데이터 누락분 확인...");

                    // 1. 시자료 보충
                    _logAction("  시자료 보충 중...");
                    var hourlyParams = new Dictionary<string, string>
                    {
                        { "obscd", station.StationCode },
                        { "startdt", startDate.ToString("yyyyMMdd") },
                        { "enddt", endDate.ToString("yyyyMMdd") }
                    };
                    var hourlyResponse = await _apiClient.GetDataAsync<RainfallResponse>("wkw/rf_hrdata", hourlyParams);
                    if (hourlyResponse?.List != null)
                    {
                        await _dataService.BulkUpsertRainfallHourlyAsync(hourlyResponse.List, station.StationCode);
                    }
                    await Task.Delay(250);

                    // 2. 일자료 보충
                    _logAction("  일자료 보충 중...");
                    var dailyParams = new Dictionary<string, string>
                    {
                        { "obscd", station.StationCode },
                        { "startdt", startDate.ToString("yyyyMMdd") },
                        { "enddt", endDate.ToString("yyyyMMdd") }
                    };
                    var dailyResponse = await _apiClient.GetDataAsync<RainfallResponse>("wkw/rf_dtdata", dailyParams);
                    if (dailyResponse?.List != null)
                    {
                        await _dataService.BulkUpsertRainfallDailyAsync(dailyResponse.List, station.StationCode);
                    }
                    await Task.Delay(250);

                    // 3. 월자료 보충
                    _logAction("  월자료 보충 중...");
                    var monthlyParams = new Dictionary<string, string>
                    {
                        { "obscd", station.StationCode },
                        { "startyear", startDate.ToString("yyyy") },
                        { "endyear", endDate.ToString("yyyy") }
                    };
                    var monthlyResponse = await _apiClient.GetDataAsync<RainfallResponse>("wkw/rf_mndata", monthlyParams);
                    if (monthlyResponse?.List != null)
                    {
                        await _dataService.BulkUpsertRainfallMonthlyAsync(monthlyResponse.List, station.StationCode);
                    }
                    await Task.Delay(250);
                }
            }
            _logAction("누락 데이터 보충이 완료되었습니다.");
        }
        private async Task FetchAndStoreAllStations()
        {
            _logAction("전체 관측소/댐 목록을 수집합니다.");

            var rfStations = await _apiClient.GetDataAsync<StationResponse>("wkw/rf_dubrfobs", new Dictionary<string, string>());
            await _dataService.UpsertStationsAsync(rfStations?.List, "RF");
            await Task.Delay(200);

            var wlStations = await _apiClient.GetDataAsync<StationResponse>("wkw/wl_dubwlobs", new Dictionary<string, string>());
            await _dataService.UpsertStationsAsync(wlStations?.List, "WL");
            await Task.Delay(200);

            var damStations = await _apiClient.GetDataAsync<StationResponse>("wkw/mn_damdub", new Dictionary<string, string>());
            await _dataService.UpsertStationsAsync(damStations?.List, "DAM");
            await Task.Delay(200);
        }
    }
}
