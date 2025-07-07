using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WamisDataCollector.Models;
using log4net; 

namespace WamisDataCollector.Services
{
    public class DataSyncService
    {
        private readonly WamisApiClient _apiClient;
        private readonly DataService _dataService;
        private readonly Action<string> _logAction;
        private readonly ILog _log; 

        // 생성자 수정
        public DataSyncService(WamisApiClient apiClient, DataService dataService, Action<string> logAction, ILog log)
        {
            _apiClient = apiClient;
            _dataService = dataService;
            _logAction = logAction;
            _log = log; 
        }
        /// <summary>
        /// 초기 DB입력
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="testMode"></param>
        /// <returns></returns>
        public async Task PerformInitialLoadAsync(DateTime startDate, DateTime endDate, bool testMode = false)
        {
            _logAction($"초기 데이터 로드를 시작합니다... (테스트 모드: {testMode})");
            await _dataService.EnsureTablesExistAsync();
            await FetchAndStoreAllStations(); 

            var allStations = await _dataService.GetAllStationsAsync();
            List<StationInfo> stationsToProcess;

            if (testMode)
            {
                stationsToProcess = allStations
                    .GroupBy(s => s.StationType)
                    .Select(g => g.First()) 
                    .ToList();
                _logAction($"테스트 모드: {stationsToProcess.Count}개의 대표 관측소에 대해서만 데이터 수집을 진행합니다.");
            }
            else
            {
                stationsToProcess = allStations;
            }

            foreach (var station in stationsToProcess) 
            {
                _logAction($"[{station.StationCode}] {station.Name} ({station.StationType}) 데이터 수집 시작...");

                switch (station.StationType)
                {
                    case "RF": await CollectRainfallData(station.StationCode, startDate, endDate); break;
                    case "WL": await CollectWaterLevelData(station.StationCode, startDate, endDate); break;
                    case "WE": await CollectWeatherData(station.StationCode, startDate, endDate); break;
                    case "FLW": await CollectFlowDailyData(station.StationCode, startDate, endDate); break;
                    case "DAM": await CollectDamData(station.StationCode, startDate, endDate); break;
                  //  case "WKW": await CollectFlowMeasurementData(station.StationCode, startDate, endDate); break;
                }
            }
            _logAction("초기 데이터 로드가 완료되었습니다.");
        }

        /// <summary>
        /// 일별 최신화 (DB없을시 3일)
        /// </summary>
        /// <param name="testMode"></param>
        /// <returns></returns>
        public async Task PerformDailyUpdateAsync(bool testMode = false) 
        {
            

            _logAction($"일별 데이터 최신화를 시작합니다... (테스트 모드: {testMode})");
            var allStations = await _dataService.GetAllStationsAsync();
            var endDate = DateTime.Today; // 또는 DateTime.Today.AddDays(-1)
            _log.Info($"===== 일별 데이터 최신화 작업 시작: {endDate:yyyy-MM-dd} =====");

            List<StationInfo> stationsToProcess;

            if (testMode)
            {
                stationsToProcess = allStations
                    .GroupBy(s => s.StationType)
                    .Select(g => g.First())
                    .ToList();
                _logAction($"테스트 모드: {stationsToProcess.Count}개의 대표 관측소에 대해서만 데이터 최신화를 진행합니다.");
            }
            else
            {
                stationsToProcess = allStations;
            }

            foreach (var station in stationsToProcess)
            {
                _logAction($"[{station.StationCode}] {station.Name} ({station.StationType}) 데이터 최신화...");

                switch (station.StationType)
                {
                    case "RF":
                        var lastRfDate = await _dataService.GetLastDailyRainfallDateAsync(station.StationCode);
                        await CollectRainfallData(station.StationCode, lastRfDate?.AddDays(1) ?? endDate.AddDays(-3), endDate);
                        break;
                    case "WL":
                        // 시자료 최신화
                        var lastWlHourlyDate = await _dataService.GetLastWaterLevelHourlyDateAsync(station.StationCode);
                        await CollectWaterLevelData(station.StationCode, lastWlHourlyDate?.AddHours(1) ?? endDate.AddDays(-3), endDate, collectDaily: false); // 일자료는 따로 처리
                        // 일자료 최신화 (신규 추가)
                        var lastWlDailyDate = await _dataService.GetLastDailyWaterLevelDateAsync(station.StationCode);
                        await CollectWaterLevelData(station.StationCode, lastWlDailyDate?.AddDays(1) ?? endDate.AddDays(-3), endDate, collectHourly: false); // 시자료는 위에서 처리
                        break;
                    //var lastWlDate = await _dataService.GetLastWaterLevelHourlyDateAsync(station.StationCode);
                    //await CollectWaterLevelData(station.StationCode, lastWlDate?.AddHours(1) ?? endDate.AddDays(-3), endDate);
                    //break;
                    case "WE":
                        var lastWeDate = await _dataService.GetLastDailyRainfallDateAsync(station.StationCode); 
                        await CollectWeatherData(station.StationCode, lastWeDate?.AddDays(1) ?? endDate.AddDays(-3), endDate);
                        break;
                    case "FLW":
                        var lastFlwDate = await _dataService.GetLastFlowDailyDateAsync(station.StationCode);
                        await CollectFlowDailyData(station.StationCode, lastFlwDate?.AddDays(1) ?? endDate.AddDays(-3), endDate);
                        break;
                    case "DAM":
                        var lastDamDate = await _dataService.GetLastDamDailyDateAsync(station.StationCode);
                        await CollectDamData(station.StationCode, lastDamDate?.AddDays(1) ?? endDate.AddDays(-3), endDate);
                        break;
                    //case "WKW":
                    //    var lastWkwDate = await _dataService.GetLastFlowMeasurementDateAsync(station.StationCode);
                    //    await CollectFlowMeasurementData(station.StationCode, lastWkwDate?.AddDays(1) ?? endDate.AddDays(-365), endDate);
                    //    break;
                }
            }
            _log.Info("일별 데이터 최신화가 성공적으로 완료되었습니다.");
            _logAction("일별 데이터 최신화가 완료되었습니다.");
        }

        /// <summary>
        /// 누락된 데이터 채우기 (7일)
        /// </summary>
        /// <param name="testMode"></param>
        /// <returns></returns>
        public async Task BackfillMissingDataAsync(bool testMode = false) 
        {
            _logAction($"누락 데이터 보충을 시작합니다... (테스트 모드: {testMode})");
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today; // 또는 DateTime.Today.AddDays(-1)
            var allStations = await _dataService.GetAllStationsAsync();
            List<StationInfo> stationsToProcess;

            if (testMode)
            {
                stationsToProcess = allStations
                    .GroupBy(s => s.StationType)
                    .Select(g => g.First())
                    .ToList();
                _logAction($"테스트 모드: {stationsToProcess.Count}개의 대표 관측소에 대해서만 누락 데이터 보충을 진행합니다.");
            }
            else
            {
                stationsToProcess = allStations;
            }

            foreach (var station in stationsToProcess)
            {
                _logAction($"[{station.StationCode}] {station.Name} ({station.StationType}) 최근 7일 데이터 보충...");
                switch (station.StationType)
                {
                    case "RF": await CollectRainfallData(station.StationCode, startDate, endDate); break;
                    case "WL": await CollectWaterLevelData(station.StationCode, startDate, endDate); break;
                    case "WE": await CollectWeatherData(station.StationCode, startDate, endDate); break;
                    case "FLW": await CollectFlowDailyData(station.StationCode, startDate, endDate); break;
                    case "DAM": await CollectDamData(station.StationCode, startDate, endDate); break;
                //    case "WKW": await CollectFlowMeasurementData(station.StationCode, startDate, endDate); break;
                }
            }
            _logAction("누락 데이터 보충이 완료되었습니다.");
        }

        /// <summary>
        /// 관측소 데이터 수집
        /// </summary>
        /// <returns></returns>
        private async Task FetchAndStoreAllStations()
        {
            _logAction("전체 관측소/댐 목록을 수집합니다.");

            var apiEndpoints = new Dictionary<string, string>
            {
                { "RF", "wkw/rf_dubrfobs" },
                { "WL", "wkw/wl_dubwlobs" },
                { "WE", "wkw/we_dwtwtobs" },
                { "FLW", "wkw/flw_dubobsif" },
                { "DAM", "wkd/mn_dammain" }
          //      { "WKW", "wkw/wkw_youardata" },
            };

            foreach (var endpoint in apiEndpoints)
            {
                var stationResponse = await _apiClient.GetDataAsync<StationResponse>(endpoint.Value, new Dictionary<string, string>());
                if (stationResponse?.List != null && stationResponse.List.Any()) 
                {
                    var distinctStations = stationResponse.List
                        .Where(s => !string.IsNullOrEmpty(s.StationCode)) 
                        .GroupBy(s => s.StationCode)
                        .Select(g => g.First())
                        .ToList();

                    if (distinctStations.Any()) 
                    {
                        _logAction($"  {endpoint.Key} 유형 관측소 목록 API 응답: {stationResponse.List.Count}개 수신, 중복 제거 후 {distinctStations.Count}개 처리 대상.");
                        await _dataService.UpsertStationsAsync(distinctStations, endpoint.Key);
                    }
                    else
                    {
                        _logAction($"  {endpoint.Key} 유형 관측소 목록 API 응답: {stationResponse.List.Count}개 수신, 유효한 StationCode를 가진 처리 대상 없음.");
                    }
                }
                else
                {
                     _logAction($"  {endpoint.Key} 유형 관측소 목록 API 응답이 없거나 비어있습니다.");
                }
                await Task.Delay(100);
            }
        }


        /// <summary>
        /// 데이터 타입별 수집 헬퍼 메서드
        /// </summary>
        /// <param name="stationCode"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        private async Task CollectRainfallData(string stationCode, DateTime startDate, DateTime endDate)
        {
            for (var year = startDate.Year; year <= endDate.Year; year++)
            {
                var yearStartDate = (year == startDate.Year) ? startDate : new DateTime(year, 1, 1);
                var yearEndDate = (year == endDate.Year) ? endDate : new DateTime(year, 12, 31);
                _logAction($"  {year}년 강수량 시자료 수집...");
                var hourlyParams = new Dictionary<string, string> { { "obscd", stationCode }, { "startdt", yearStartDate.ToString("yyyyMMdd") }, { "enddt", yearEndDate.ToString("yyyyMMdd") } };
                var hourlyResponse = await _apiClient.GetDataAsync<RainfallResponse>("wkw/rf_hrdata", hourlyParams);
                if (hourlyResponse?.List != null) await _dataService.BulkUpsertRainfallHourlyAsync(hourlyResponse.List, stationCode);
                await Task.Delay(250);
            }
            _logAction($"  강수량 일자료 수집...");
            var dailyParams = new Dictionary<string, string> { { "obscd", stationCode }, { "startdt", startDate.ToString("yyyyMMdd") }, { "enddt", endDate.ToString("yyyyMMdd") } };
            var dailyResponse = await _apiClient.GetDataAsync<RainfallResponse>("wkw/rf_dtdata", dailyParams);
            if (dailyResponse?.List != null) await _dataService.BulkUpsertRainfallDailyAsync(dailyResponse.List, stationCode);
            await Task.Delay(250);
            _logAction($"  강수량 월자료 수집...");
            var monthlyParams = new Dictionary<string, string> { { "obscd", stationCode }, { "startyear", startDate.ToString("yyyy") }, { "endyear", endDate.ToString("yyyy") } };
            var monthlyResponse = await _apiClient.GetDataAsync<RainfallResponse>("wkw/rf_mndata", monthlyParams);
            if (monthlyResponse?.List != null) await _dataService.BulkUpsertRainfallMonthlyAsync(monthlyResponse.List, stationCode);
            await Task.Delay(250);
        }

        private async Task CollectWaterLevelData(string stationCode, DateTime startDate, DateTime endDate, bool collectHourly = true, bool collectDaily = true)
        {
            if (collectHourly)
            {
                _logAction($"  수위 시자료 수집...");
                for (var date = startDate; date <= endDate; date = date.AddMonths(1))
                {
                    var monthStartDate = new DateTime(date.Year, date.Month, 1);
                    var monthEndDate = monthStartDate.AddMonths(1).AddDays(-1);
                    var parameters = new Dictionary<string, string> { { "obscd", stationCode }, { "startdt", monthStartDate.ToString("yyyyMMdd") }, { "enddt", monthEndDate.ToString("yyyyMMdd") } };
                    var response = await _apiClient.GetDataAsync<WaterLevelResponse>("wkw/wl_hrdata", parameters);
                    if (response?.List != null) await _dataService.BulkUpsertWaterLevelHourlyAsync(response.List, stationCode);
                    await Task.Delay(250);
                }
            }

            if (collectDaily)
            {
                _logAction($"  수위 일자료 수집...");
                var dailyParams = new Dictionary<string, string> { { "obscd", stationCode }, { "startdt", startDate.ToString("yyyyMMdd") }, { "enddt", endDate.ToString("yyyyMMdd") } };
                var dailyResponse = await _apiClient.GetDataAsync<WaterLevelResponse>("wkw/wl_dtdata", dailyParams);
                if (dailyResponse?.List != null) await _dataService.BulkUpsertWaterLevelDailyAsync(dailyResponse.List, stationCode);
                await Task.Delay(250);
            }
        }

        private async Task CollectWeatherData(string stationCode, DateTime startDate, DateTime endDate)
        {
            _logAction($"  기상 시자료 수집...");
            for (var year = startDate.Year; year <= endDate.Year; year++)
            {
                var yearStartDate = (year == startDate.Year) ? startDate : new DateTime(year, 1, 1);
                var yearEndDate = (year == endDate.Year) ? endDate : new DateTime(year, 12, 31);
                var parameters = new Dictionary<string, string> { { "obscd", stationCode }, { "startdt", yearStartDate.ToString("yyyyMMdd") }, { "enddt", yearEndDate.ToString("yyyyMMdd") } };
                var response = await _apiClient.GetDataAsync<WeatherResponse>("wkw/we_hrdata", parameters);
                if (response?.List != null) await _dataService.BulkUpsertWeatherHourlyAsync(response.List, stationCode);
                await Task.Delay(250);
            }
            _logAction($"  기상 일자료 수집...");
            var dailyParams = new Dictionary<string, string> { { "obscd", stationCode }, { "startdt", startDate.ToString("yyyyMMdd") }, { "enddt", endDate.ToString("yyyyMMdd") } };
            var dailyResponse = await _apiClient.GetDataAsync<WeatherResponse>("wkw/we_dtdata", dailyParams);
            if (dailyResponse?.List != null) await _dataService.BulkUpsertWeatherDailyAsync(dailyResponse.List, stationCode);
            await Task.Delay(250);
        }

        private async Task CollectFlowDailyData(string stationCode, DateTime startDate, DateTime endDate)
        {
            _logAction($"  유량 일자료 수집...");
            for (var year = startDate.Year; year <= endDate.Year; year++)
            {
                var parameters = new Dictionary<string, string> { { "obscd", stationCode }, { "year", year.ToString() } };
                var response = await _apiClient.GetDataAsync<FlowDailyResponse>("wkw/flw_dtdata", parameters);
                if (response?.List != null) await _dataService.BulkUpsertFlowDailyAsync(response.List, stationCode);
                await Task.Delay(250);
            }
        }

        private async Task CollectDamData(string damCode, DateTime startDate, DateTime endDate)
        {
            _logAction($"  댐 시자료 수집...");
            for (var year = startDate.Year; year <= endDate.Year; year++)
            {
                var yearStartDate = (year == startDate.Year) ? startDate : new DateTime(year, 1, 1);
                var yearEndDate = (year == endDate.Year) ? endDate : new DateTime(year, 12, 31);
                var parameters = new Dictionary<string, string> { { "damcd", damCode }, { "startdt", yearStartDate.ToString("yyyyMMdd") }, { "enddt", yearEndDate.ToString("yyyyMMdd") } };
                var response = await _apiClient.GetDataAsync<DamResponse>("wkd/mn_hrdata", parameters);
                if (response?.List != null) await _dataService.BulkUpsertDamHourlyAsync(response.List, damCode);
                await Task.Delay(250);
            }
            _logAction($"  댐 일자료 수집...");
            var dailyParams = new Dictionary<string, string> { { "damcd", damCode }, { "startdt", startDate.ToString("yyyyMMdd") }, { "enddt", endDate.ToString("yyyyMMdd") } };
            var dailyResponse = await _apiClient.GetDataAsync<DamResponse>("wkd/mn_dtdata", dailyParams);
            if (dailyResponse?.List != null) await _dataService.BulkUpsertDamDailyAsync(dailyResponse.List, damCode);
            await Task.Delay(250);
            _logAction($"  댐 월자료 수집...");
            var monthlyParams = new Dictionary<string, string> { { "damcd", damCode }, { "startyear", startDate.ToString("yyyy") }, { "endyear", endDate.ToString("yyyy") } };
            var monthlyResponse = await _apiClient.GetDataAsync<DamResponse>("wkd/mn_mndata", monthlyParams);
            if (monthlyResponse?.List != null) await _dataService.BulkUpsertDamMonthlyAsync(monthlyResponse.List, damCode);
            await Task.Delay(250);
        }

        //private async Task CollectFlowMeasurementData(string stationCode, DateTime startDate, DateTime endDate)
        //{
        //    _logAction($"  유량 측정성과 수집...");
        //    var parameters = new Dictionary<string, string> { { "obscd", stationCode }, { "startyear", startDate.ToString("yyyy") }, { "endyear", endDate.ToString("yyyy") } };
        //    { 
        //        int fff = 0; 
        //    }
            

        //    var response = await _apiClient.GetDataAsync<FlowMeasurementResponse>("wkw/wkw_flwsrrslst", parameters);
        //    if (response?.List != null) await _dataService.BulkUpsertFlowMeasurementAsync(response.List, stationCode);
        //    await Task.Delay(250);
        //}
    }
}