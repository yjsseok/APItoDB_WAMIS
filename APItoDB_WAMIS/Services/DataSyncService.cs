using System;
using System.Collections.Generic;
using System.Linq; // LINQ 사용을 위해 추가
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

            var allStations = await _dataService.GetAllStationsAsync();

            foreach (var station in allStations)
            {
                _logAction($"[{station.StationCode}] {station.Name} ({station.StationType}) 데이터 수집 시작...");

                switch (station.StationType)
                {
                    case "RF": await CollectRainfallData(station.StationCode, startDate, endDate); break;
                    case "WL": await CollectWaterLevelData(station.StationCode, startDate, endDate); break;
                    case "WE": await CollectWeatherData(station.StationCode, startDate, endDate); break;
                    case "FLW": await CollectFlowDailyData(station.StationCode, startDate, endDate); break;
                    case "WKW": await CollectFlowMeasurementData(station.StationCode, startDate, endDate); break;
                    case "DAM": await CollectDamData(station.StationCode, startDate, endDate); break;
                }
            }
            _logAction("초기 데이터 로드가 완료되었습니다.");
        }

        public async Task PerformDailyUpdateAsync()
        {
            _logAction("일별 데이터 최신화를 시작합니다...");
            var allStations = await _dataService.GetAllStationsAsync();
            var endDate = DateTime.Today;

            foreach (var station in allStations)
            {
                _logAction($"[{station.StationCode}] {station.Name} ({station.StationType}) 데이터 최신화...");

                switch (station.StationType)
                {
                    case "RF":
                        var lastRfDate = await _dataService.GetLastDailyRainfallDateAsync(station.StationCode);
                        await CollectRainfallData(station.StationCode, lastRfDate?.AddDays(1) ?? endDate.AddDays(-3), endDate);
                        break;
                    case "WL":
                        var lastWlDate = await _dataService.GetLastWaterLevelHourlyDateAsync(station.StationCode);
                        await CollectWaterLevelData(station.StationCode, lastWlDate?.AddHours(1) ?? endDate.AddDays(-3), endDate);
                        break;
                    case "WE":
                        var lastWeDate = await _dataService.GetLastDailyRainfallDateAsync(station.StationCode);
                        await CollectWeatherData(station.StationCode, lastWeDate?.AddDays(1) ?? endDate.AddDays(-3), endDate);
                        break;
                    case "FLW":
                        var lastFlwDate = await _dataService.GetLastFlowDailyDateAsync(station.StationCode);
                        await CollectFlowDailyData(station.StationCode, lastFlwDate?.AddDays(1) ?? endDate.AddDays(-3), endDate);
                        break;
                    case "WKW":
                        var lastWkwDate = await _dataService.GetLastFlowMeasurementDateAsync(station.StationCode);
                        await CollectFlowMeasurementData(station.StationCode, lastWkwDate?.AddDays(1) ?? endDate.AddDays(-365), endDate);
                        break;
                    case "DAM":
                        var lastDamDate = await _dataService.GetLastDamDailyDateAsync(station.StationCode);
                        await CollectDamData(station.StationCode, lastDamDate?.AddDays(1) ?? endDate.AddDays(-3), endDate);
                        break;
                }
            }
            _logAction("일별 데이터 최신화가 완료되었습니다.");
        }

        public async Task BackfillMissingDataAsync()
        {
            _logAction("누락 데이터 보충을 시작합니다...");
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today;
            var allStations = await _dataService.GetAllStationsAsync();

            foreach (var station in allStations)
            {
                _logAction($"[{station.StationCode}] {station.Name} ({station.StationType}) 최근 7일 데이터 보충...");
                switch (station.StationType)
                {
                    case "RF": await CollectRainfallData(station.StationCode, startDate, endDate); break;
                    case "WL": await CollectWaterLevelData(station.StationCode, startDate, endDate); break;
                    case "WE": await CollectWeatherData(station.StationCode, startDate, endDate); break;
                    case "FLW": await CollectFlowDailyData(station.StationCode, startDate, endDate); break;
                    case "WKW": await CollectFlowMeasurementData(station.StationCode, startDate, endDate); break;
                    case "DAM": await CollectDamData(station.StationCode, startDate, endDate); break;
                }
            }
            _logAction("누락 데이터 보충이 완료되었습니다.");
        }

        private async Task FetchAndStoreAllStations()
        {
            _logAction("전체 관측소/댐 목록을 수집합니다.");

            var apiEndpoints = new Dictionary<string, string>
            {
                { "RF", "wkw/rf_dubrfobs" },
                { "WL", "wkw/wl_dubwlobs" },
                { "WE", "wkw/we_dwtwtobs" },
                { "WKW", "wkw/wkw_youardata" },
                { "FLW", "wkw/flw_dubobsif" },
                { "DAM", "wkd/mn_dammain" }
            };

            foreach (var endpoint in apiEndpoints)
            {
                var stationResponse = await _apiClient.GetDataAsync<StationResponse>(endpoint.Value, new Dictionary<string, string>());
                if (stationResponse?.List != null && stationResponse.List.Any()) // 리스트가 null이 아니고 비어있지 않은지 확인
                {
                    // StationCode 기준으로 중복 제거 (첫 번째 항목 우선)
                    // StationCode가 null이거나 빈 경우를 필터링하여 GroupBy 전에 문제를 방지
                    var distinctStations = stationResponse.List
                        .Where(s => !string.IsNullOrEmpty(s.StationCode)) // StationCode가 유효한 것만 대상으로 함
                        .GroupBy(s => s.StationCode)
                        .Select(g => g.First())
                        .ToList();

                    if (distinctStations.Any()) // 중복 제거 후에도 데이터가 있다면
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
                await Task.Delay(200); // 기존 지연 시간 유지
            }
        }

        // --- 데이터 타입별 수집 헬퍼 메서드 ---
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

        private async Task CollectWaterLevelData(string stationCode, DateTime startDate, DateTime endDate)
        {
            _logAction($"  수위 시자료 수집...");
            for (var year = startDate.Year; year <= endDate.Year; year++)
            {
                var yearStartDate = (year == startDate.Year) ? startDate : new DateTime(year, 1, 1);
                var yearEndDate = (year == endDate.Year) ? endDate : new DateTime(year, 12, 31);
                var parameters = new Dictionary<string, string> { { "obscd", stationCode }, { "startdt", yearStartDate.ToString("yyyyMMdd") }, { "enddt", yearEndDate.ToString("yyyyMMdd") } };
                var response = await _apiClient.GetDataAsync<WaterLevelResponse>("wkw/wl_hrdata", parameters);
                if (response?.List != null) await _dataService.BulkUpsertWaterLevelHourlyAsync(response.List, stationCode);
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
              //  var parameters = new Dictionary<string, string> { { "obscd", stationCode }, { "year", year.ToString("yyyy") } };
                var parameters = new Dictionary<string, string> { { "obscd", stationCode }, { "year", year.ToString() } };
                var response = await _apiClient.GetDataAsync<FlowDailyResponse>("wkw/flw_dtdata", parameters);
                if (response?.List != null) await _dataService.BulkUpsertFlowDailyAsync(response.List, stationCode);
                
                {
                    int kk = 0;
                }
                await Task.Delay(250);
            }
        }

        private async Task CollectFlowMeasurementData(string stationCode, DateTime startDate, DateTime endDate)
        {
            _logAction($"  유량 측정성과 수집...");
            var parameters = new Dictionary<string, string> { { "obscd", stationCode }, { "startyear", startDate.ToString() }, { "endyear", endDate.ToString() } };
            var response = await _apiClient.GetDataAsync<FlowMeasurementResponse>("wkw/wkw_flwsrrslst", parameters);
            if (response?.List != null) await _dataService.BulkUpsertFlowMeasurementAsync(response.List, stationCode);
            await Task.Delay(250);
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
    }
}