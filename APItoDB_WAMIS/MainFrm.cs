using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;
using WamisDataCollector.Services;
using log4net;
using System.Collections.Generic;
using System.Linq;
using KRC_Services.Services;

namespace WamisDataCollector
{
    public partial class MainFrm : Form
    {
        private readonly Wamis_DataSyncService _syncService;
        private static readonly ILog log = LogManager.GetLogger(typeof(MainFrm));

        // KRC Services
        private KrcReservoirService _krcReservoirService;
        private KrcDataService _krcDataService;
        private Wamis_DataService _wamisDataService; // For KRC station codes and last dates

        public MainFrm()
        {
            InitializeComponent();

            try
            {
                // WAMIS Services
                var wamisApiKey = ConfigurationManager.AppSettings["WamisApiKey"];
                var wamisBaseUrl = ConfigurationManager.AppSettings["WamisBaseUrl"];
                var connectionString = ConfigurationManager.ConnectionStrings["PostgreSqlConnection"].ConnectionString;

                var missingSettings = new List<string>();
                if (string.IsNullOrEmpty(wamisApiKey)) missingSettings.Add("WamisApiKey");
                if (string.IsNullOrEmpty(wamisBaseUrl)) missingSettings.Add("WamisBaseUrl");
                if (string.IsNullOrEmpty(connectionString)) missingSettings.Add("PostgreSqlConnection");

                // KRC Services
                var krcApiKey = ConfigurationManager.AppSettings["KrcApiKey"];
                if (string.IsNullOrEmpty(krcApiKey)) missingSettings.Add("KrcApiKey");


                if (missingSettings.Count > 0)
                {
                    var errorMessage = $"App.config 파일에 다음 설정이 올바르지 않거나 누락되었습니다: {string.Join(", ", missingSettings)}";
                    throw new InvalidOperationException(errorMessage);
                }

                var apiClient = new Wamis_ApiClient(wamisApiKey, wamisBaseUrl, this.Log);
                _wamisDataService = new Wamis_DataService(connectionString, this.Log); // Reused for KRC needs
                _syncService = new Wamis_DataSyncService(apiClient, _wamisDataService, this.Log, log);

                // Initialize KRC Services
                // Assuming KrcReservoirService needs HttpClient. For simplicity, creating a new one.
                // In a more complex app, HttpClient might be managed centrally.
                var httpClientForKrc = new System.Net.Http.HttpClient();
                _krcReservoirService = new KrcReservoirService(httpClientForKrc); // Pass krcApiKey if constructor takes it, or it reads from config
                _krcDataService = new KrcDataService(connectionString, this.Log);

                // Ensure KRC tables exist
             //   Task.Run(async () => await _wamisDataService.EnsureTablesExistAsync()).Wait(); // Ensure krc_reservoir_daily is created
            }
            catch (Exception ex)
            {
                log.Fatal("설정 파일 로드 또는 서비스 초기화 중 심각한 오류 발생", ex);
                MessageBox.Show($"설정 파일(App.config) 로드 또는 서비스 초기화 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        private async void BtnInitialLoad_Click(object sender, EventArgs e)
        {
            var startDate = _dtpStartDate.Value;
            var endDate = _dtpEndDate.Value;
            bool isTestMode = _chkTestMode.Checked; 

            if (MessageBox.Show($"{startDate:yyyy-MM-dd}부터 {endDate:yyyy-MM-dd}까지의 {(isTestMode ? "테스트 모드 " : "")}전체 데이터를 수집합니다. 시간이 오래 걸릴 수 있습니다. 계속하시겠습니까?",
                                $"초기 데이터 로드{(isTestMode ? " (테스트)" : "")}",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await RunTask(async () => await _syncService.PerformInitialLoadAsync(startDate, endDate, isTestMode)); 
            }
        }

        private async void BtnDailyUpdate_Click(object sender, EventArgs e)
        {
            bool isTestMode = _chkTestMode.Checked; 
            if (MessageBox.Show($"일별 데이터 최신화를 {(isTestMode ? "(테스트 모드)" : "")} 시작하시겠습니까?",
                                $"일별 최신화{(isTestMode ? " (테스트)" : "")}",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await RunTask(async () => await _syncService.PerformDailyUpdateAsync(isTestMode)); 
            }
        }

        private async void BtnBackfill_Click(object sender, EventArgs e)
        {
            bool isTestMode = _chkTestMode.Checked; 
            if (MessageBox.Show($"누락 데이터 보충을 {(isTestMode ? "(테스트 모드)" : "")} 시작하시겠습니까? (최근 7일)",
                                $"누락 데이터 보충{(isTestMode ? " (테스트)" : "")}",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await RunTask(async () => await _syncService.BackfillMissingDataAsync(isTestMode)); 
            }
        }

        private async Task RunTask(Func<Task> task)
        {
            try
            {
                SetControlsEnabled(false);
                _txtLogs.Clear();
                await task();
                MessageBox.Show("작업이 완료되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {

                Log($"[오류] {ex.Message}\n{ex.StackTrace}");
                log.Error("작업 중 오류 발생", ex);
                MessageBox.Show("작업 중 오류가 발생했습니다. 로그를 확인하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            _btnInitialLoad.Enabled = enabled;
            _btnDailyUpdate.Enabled = enabled;
            _btnBackfill.Enabled = enabled;
            _dtpStartDate.Enabled = enabled;
            _dtpEndDate.Enabled = enabled;
            _chkTestMode.Enabled = enabled;

            // WAMIS buttons
            _btnInitialLoad.Enabled = enabled;
            _btnDailyUpdate.Enabled = enabled;
            _btnBackfill.Enabled = enabled;

            // KRC buttons
            _btnKrcFetchAllCodes.Enabled = enabled;
            _btnKrcInitialLoad.Enabled = enabled;
            _btnKrcDailyUpdate.Enabled = enabled;
            _btnKrcBackfill.Enabled = enabled;

            _progressBar.Style = enabled ? ProgressBarStyle.Continuous : ProgressBarStyle.Marquee;
            _progressBar.MarqueeAnimationSpeed = enabled ? 0 : 100;
            if (enabled) _progressBar.Value = 0;
        }

        private void UpdateProgressBar(int currentValue, int maxValue)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int, int>(UpdateProgressBar), currentValue, maxValue);
                return;
            }
            if (maxValue > 0)
            {
                _progressBar.Maximum = maxValue;
                _progressBar.Value = Math.Min(currentValue, maxValue);
            }
        }

        private void Log(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(Log), message);
                return;
            }
            _txtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        // KRC Button Handlers
        private async void BtnKrcFetchAllCodes_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("KRC 전체 저수지 코드를 조회하여 DB에 저장/업데이트합니다. 계속하시겠습니까?",
                                "KRC 코드 전체 조회",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await RunTask(async () =>
                {
                    Log("KRC 전체 저수지 코드 조회를 시작합니다...");
                    var allCodes = new List<KRC_Services.Models.KrcReservoirCodeItem>();
                    int pageNo = 1;
                    int totalCount = 0;
                    const int numOfRows = 100; // API가 허용하는 최대치 또는 적절한 값으로 설정

                    do
                    {
                        Log($"KRC 저수지 코드 조회 중 (페이지: {pageNo})...");
                        var response = await _krcReservoirService.GetReservoirCodesAsync(numOfRows: numOfRows, pageNo: pageNo);

                        // --- 추가적인 디버깅 로그 ---
                        if (response == null)
                        {
                            Log($"[DEBUG] 페이지 {pageNo}: API 응답 객체(response)가 null입니다.");
                            break;
                        }
                        if (response.Header != null)
                        {
                            Log($"[DEBUG] 페이지 {pageNo}: 응답 헤더 - Code: {response.Header.ReturnReasonCode}, Msg: {response.Header.ReturnAuthMsg}");
                        }
                        else
                        {
                            Log($"[DEBUG] 페이지 {pageNo}: API 응답 헤더(response.Header)가 null입니다.");
                        }

                        if (response.Body == null)
                        {
                            Log($"[DEBUG] 페이지 {pageNo}: API 응답 바디(response.Body)가 null입니다.");
                            // Body가 null이면 Items, TotalCount 등에 접근 시 NullReferenceException 발생 가능하므로 이후 로직 중단 또는 보호 코드 필요
                            // 여기서는 break를 통해 루프를 중단시킴
                            Log("API 응답의 Body가 null이므로 코드 조회를 중단합니다.");
                            break;
                        }
                        // Body가 null이 아님을 확인했으므로 내부 속성에 접근 가능
                        Log($"[DEBUG] 페이지 {pageNo}: Body.NumOfRows={response.Body.NumOfRows}, Body.PageNo={response.Body.PageNo}, Body.TotalCount={response.Body.TotalCount}");

                        if (response.Body.Items == null)
                        {
                            Log($"[DEBUG] 페이지 {pageNo}: API 응답 아이템 목록(response.Body.Items)이 null입니다.");
                        }
                        else
                        {
                            Log($"[DEBUG] 페이지 {pageNo}: API 응답 아이템 개수: {response.Body.Items.Count}");
                        }
                        // --- 디버깅 로그 끝 ---

                        if (response.Body.Items != null && response.Body.Items.Any()) // response.Body가 null이 아님은 위에서 확인됨
                        {
                            allCodes.AddRange(response.Body.Items);
                            // totalCount는 첫 페이지 응답에서만 설정하거나, 또는 API가 매번 정확한 값을 준다면 매번 업데이트도 가능.
                            // 여기서는 pageNo == 1일 때만 totalCount를 설정하여 이후 페이지에서 totalCount가 0으로 오는 경우를 방지.
                            if (pageNo == 1 && response.Body.TotalCount > 0)
                            {
                                totalCount = response.Body.TotalCount;
                            }
                            Log($"{response.Body.Items.Count}개 코드 수신 (누적: {allCodes.Count} / 전체 예상: {totalCount})");
                        }
                        else
                        {
                            // response.Body.Items가 null이거나 비어있는 경우
                            if (totalCount == 0 && pageNo == 1) // 첫 페이지인데 totalCount도 0이고 아이템도 없으면 정말 데이터가 없는 것
                            {
                                Log("조회된 저수지 코드가 없습니다. (totalCount: 0, 첫 페이지 아이템 없음)");
                            }
                            else if (allCodes.Count >= totalCount && totalCount > 0) // 이미 모든 아이템을 수집한 경우
                            {
                                Log("모든 저수지 코드를 수집한 것으로 보입니다. (누적 아이템 수 >= totalCount)");
                            }
                            else
                            {
                                Log($"페이지 {pageNo}: 추가 조회할 코드가 없거나 API 응답에 아이템이 없습니다. (누적: {allCodes.Count} / 전체 예상: {totalCount})");
                            }
                            // 더 이상 진행할 필요가 없으므로 루프 종료
                            break;
                        }
                        pageNo++;
                    } while (allCodes.Count < totalCount && totalCount > 0); // totalCount가 0이면 루프 한번만 실행되도록 조건 추가

                    Log($"총 {allCodes.Count}개의 KRC 저수지 코드 정보 수집 완료. DB에 저장합니다...");
                    await _krcDataService.UpsertKrcReservoirStationsAsync(allCodes);
                    Log("KRC 저수지 코드 정보 DB 저장 완료.");
                });
            }
        }

        private async void BtnKrcInitialLoad_Click(object sender, EventArgs e)
        {
            await CollectKrcLevelDataAsync("KRC 수위 초기 데이터 로드",
                (facCode, dateS, dateE, county, numOfRows, pageNo, isTestMode)
                => _krcReservoirService.GetReservoirLevelsForInitialSetupAsync(facCode, dateS, dateE, county, numOfRows, pageNo, isTestMode));
        }

        private async void BtnKrcDailyUpdate_Click(object sender, EventArgs e)
        {
            bool isTestMode = _chkTestMode.Checked;
            if (MessageBox.Show($"KRC 저수지 수위 데이터를 최신 정보로 업데이트합니다.{(isTestMode ? " (테스트 모드)" : "")}",
                                $"KRC 수위 일별 최신화{(isTestMode ? " (테스트)" : "")}",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await RunTask(async () =>
                {
                    Log("KRC 저수지 수위 일별 최신화를 시작합니다...");
                    // KRC 저수지 코드 목록을 krc_reservoircode 테이블에서 가져오도록 수정
                    var krcStations = await _wamisDataService.GetKrcReservoirStationInfosAsync();
                    if (krcStations == null || !krcStations.Any())
                    {
                        Log("DB에 KRC 저수지 코드 정보가 없습니다. 먼저 'KRC 전체 코드 조회/저장'을 실행하세요.");
                        return;
                    }

                    Log($"총 {krcStations.Count}개의 KRC 저수지에 대해 일별 최신화를 진행합니다.");

                    for (int i = 0; i < krcStations.Count; i++)
                    {
                        var station = krcStations[i];
                        Log($"({i + 1}/{krcStations.Count}) 저수지 코드: {station.StationCode} ({station.Name}) 최신화 중...");

                        try
                        {
                            // 마지막 날짜 조회 메소드명 변경 반영
                            DateTime? lastDate = await _wamisDataService.GetLastKrcLevelDailyDateAsync(station.StationCode);
                            string startDateForApi;
                            if (lastDate.HasValue)
                            {
                                startDateForApi = lastDate.Value.AddDays(1).ToString("yyyyMMdd");
                            }
                            else // DB에 데이터가 전혀 없는 경우 (초기 로드 안된 상태)
                            {
                                // 기본적으로는 어제부터 오늘까지 (또는 설정된 기본 시작일부터)
                                // 여기서는 메시지를 남기고 건너뛰거나, 특정 기간으로 초기 로드를 유도할 수 있음
                                // 또는, 오늘 하루치만 가져오도록 할 수도 있음.
                                // 우선은 오늘 하루치만 가져오도록 처리
                                Log($"{station.StationCode}: DB에 저장된 데이터가 없어 오늘({DateTime.Now:yyyyMMdd}) 데이터를 수집합니다. 전체 기간 데이터는 '초기 수위 로드'를 이용하세요.");
                                startDateForApi = DateTime.Now.ToString("yyyyMMdd");
                            }

                            string endDateForApi = DateTime.Now.ToString("yyyyMMdd");

                            if (Convert.ToDateTime(startDateForApi) > Convert.ToDateTime(endDateForApi))
                            {
                                Log($"{station.StationCode}: 이미 최신 데이터입니다 (마지막 저장일: {lastDate?.ToString("yyyy-MM-dd")}).");
                                continue;
                            }

                            var levelResponse = await _krcReservoirService.UpdateReservoirLevelsAsync(
                                station.StationCode, lastDate.HasValue ? lastDate.Value.ToString("yyyyMMdd") : DateTime.Now.AddDays(-1).ToString("yyyyMMdd"), // Update uses last saved date string
                                isTestMode: isTestMode);

                            if (levelResponse?.Body?.Items != null && levelResponse.Body.Items.Any())
                            {
                                Log($"{station.StationCode}: {levelResponse.Body.Items.Count}개 업데이트 데이터 수신. DB에 저장합니다.");
                                await _krcDataService.BulkUpsertKrcReservoirDailyDataAsync(levelResponse.Body.Items);
                            }
                            else
                            {
                                Log($"{station.StationCode}: 업데이트할 새로운 수위 데이터가 없습니다.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"[오류] 저수지 코드 {station.StationCode} 최신화 중 오류: {ex.Message}");
                            log.Error($"KRC 수위 일별 최신화 중 오류 (Station: {station.StationCode})", ex);
                        }
                        UpdateProgressBar(i + 1, krcStations.Count);
                    }
                    Log("KRC 저수지 수위 일별 최신화 완료.");
                });
            }
        }

        private async void BtnKrcBackfill_Click(object sender, EventArgs e)
        {
            // KRC 수위 누락 데이터 보충 로직 (BtnKrcInitialLoad_Click과 유사하게 구현됨)
            await CollectKrcLevelDataAsync("KRC 수위 누락 데이터 보충",
                (facCode, dateS, dateE, county, numOfRows, pageNo, isTestMode)
                => _krcReservoirService.GetReservoirLevelsForInitialSetupAsync(facCode, dateS, dateE, county, numOfRows, pageNo, isTestMode));
        }

        /// <summary>
        /// KRC 저수지 수위 데이터를 수집하고 DB에 저장하는 공통 로직. (초기 로드 및 누락 데이터 보충용)
        /// </summary>
        /// <param name="taskTitle">작업 제목 (로그용)</param>
        /// <param name="apiCallFunc">실제 API를 호출하는 함수 (facCode, dateS, dateE, county, numOfRows, pageNo, isTestMode 파라미터를 받고 Task<KrcReservoirLevelResponse> 반환)</param>
        private async Task CollectKrcLevelDataAsync(string taskTitle,
            Func<string, string, string, string, int, int, bool, Task<KRC_Services.Models.KrcReservoirLevelResponse>> apiCallFunc)
        {
            var startDate = _dtpStartDate.Value;
            var endDate = _dtpEndDate.Value;
            bool isTestMode = _chkTestMode.Checked;

            if (MessageBox.Show($"{startDate:yyyy-MM-dd}부터 {endDate:yyyy-MM-dd}까지의 {taskTitle}을(를) {(isTestMode ? "테스트 모드로 " : "")}시작합니다. 계속하시겠습니까?",
                                $"{taskTitle}{(isTestMode ? " (테스트)" : "")}",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await RunTask(async () =>
                {
                    Log($"{taskTitle}을(를) 시작합니다...");
            var allKrcStations = await _wamisDataService.GetKrcReservoirStationInfosAsync();

            if (allKrcStations == null || !allKrcStations.Any())
                    {
                        Log("DB에 KRC 저수지 코드 정보가 없습니다. 먼저 'KRC 전체 코드 조회/저장'을 실행하세요.");
                        return;
                    }

            List<KRC_Services.Models.KrcReservoirStationInfo> stationsToProcess;
            if (isTestMode)
            {
                Log($"[테스트 모드] {taskTitle}을(를) 첫 번째 저수지에 대해서만 진행합니다.");
                stationsToProcess = allKrcStations.Take(1).ToList();
                if (!stationsToProcess.Any())
                {
                    Log("[테스트 모드] 처리할 저수지가 목록에 없습니다.");
                    return;
                }
                Log($"[테스트 모드] 대상 저수지: {stationsToProcess.First().StationCode} ({stationsToProcess.First().Name})");
            }
            else
            {
                stationsToProcess = allKrcStations;
            }

            Log($"총 {stationsToProcess.Count}개의 KRC 저수지에 대해 {taskTitle}을(를) 진행합니다.");

            for (int i = 0; i < stationsToProcess.Count; i++)
                    {
                var station = stationsToProcess[i];
                        DateTime currentStartDate = startDate;

                        while (currentStartDate <= endDate)
                        {
                            DateTime currentEndDate = currentStartDate.AddDays(365 -1); // KRC API 최대 조회 기간 365일
                            if (currentEndDate > endDate)
                            {
                                currentEndDate = endDate;
                            }

                            string dateS = currentStartDate.ToString("yyyyMMdd");
                            string dateE = currentEndDate.ToString("yyyyMMdd");

                            Log($"({i + 1}/{krcStations.Count}) 저수지 코드: {station.StationCode} ({station.Name}) 데이터 수집 중... ({dateS}~{dateE})");

                            try
                            {
                                // KRC API는 최대 365일까지 한번에 조회 가능. numOfRows는 충분히 크게 설정.
                                var levelResponse = await apiCallFunc(
                                    station.StationCode, dateS, dateE, null, 366, 1, isTestMode);

                                if (levelResponse?.Body?.Items != null && levelResponse.Body.Items.Any())
                                {
                                    Log($"{station.StationCode}: {levelResponse.Body.Items.Count}개 데이터 수신. DB에 저장합니다.");
                                    await _krcDataService.BulkUpsertKrcReservoirDailyDataAsync(levelResponse.Body.Items);
                                }
                                else
                                {
                                    Log($"{station.StationCode} ({dateS}~{dateE}): 해당 기간에 조회된 수위 데이터가 없습니다.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log($"[오류] 저수지 코드 {station.StationCode} ({dateS}~{dateE}) 데이터 수집 중 오류: {ex.Message}");
                                log.Error($"{taskTitle} 중 오류 (Station: {station.StationCode}, Period: {dateS}~{dateE})", ex);
                            }
                            currentStartDate = currentEndDate.AddDays(1);
                        }
                        UpdateProgressBar(i + 1, krcStations.Count);
                    }
                    Log($"{taskTitle} 완료.");
                });
            }
        }
    }
}