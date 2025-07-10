using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;
using WamisDataCollector.Services;
using log4net;
using System.Collections.Generic;

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
                Task.Run(async () => await _wamisDataService.EnsureTablesExistAsync()).Wait(); // Ensure krc_reservoir_daily is created
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
                    var allCodes = new List<WamisWaterLevelDataApi.Models.KrcReservoirCodeItem>();
                    int pageNo = 1;
                    int totalCount = 0;
                    const int numOfRows = 100; // API가 허용하는 최대치 또는 적절한 값으로 설정

                    do
                    {
                        Log($"KRC 저수지 코드 조회 중 (페이지: {pageNo})...");
                        var response = await _krcReservoirService.GetReservoirCodesAsync(numOfRows: numOfRows, pageNo: pageNo);
                        if (response?.Body?.Items != null && response.Body.Items.Any())
                        {
                            allCodes.AddRange(response.Body.Items);
                            totalCount = response.Body.TotalCount; // 첫 페이지에서 전체 개수 확인
                            Log($"{response.Body.Items.Count}개 코드 수신 (누적: {allCodes.Count} / 전체 예상: {totalCount})");
                        }
                        else
                        {
                            Log("더 이상 조회할 코드가 없거나 API 응답에 오류가 있습니다.");
                            break;
                        }
                        pageNo++;
                    } while (allCodes.Count < totalCount);

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
                    var krcStations = await _wamisDataService.GetAllStationsAsync("KRC_RESERVOIR");
                    if (krcStations == null || !krcStations.Any())
                    {
                        Log("DB에 KRC 저수지 정보가 없습니다. 먼저 'KRC 전체 코드 조회/저장'을 실행하세요.");
                        return;
                    }

                    Log($"총 {krcStations.Count}개의 KRC 저수지에 대해 일별 최신화를 진행합니다.");

                    for (int i = 0; i < krcStations.Count; i++)
                    {
                        var station = krcStations[i];
                        Log($"({i + 1}/{krcStations.Count}) 저수지 코드: {station.StationCode} ({station.Name}) 최신화 중...");

                        try
                        {
                            DateTime? lastDate = await _wamisDataService.GetLastKrcReservoirDailyDateAsync(station.StationCode);
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
            Func<string, string, string, string, int, int, bool, Task<WamisWaterLevelDataApi.Models.KrcReservoirLevelResponse>> apiCallFunc)
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
                    var krcStations = await _wamisDataService.GetAllStationsAsync("KRC_RESERVOIR");
                    if (krcStations == null || !krcStations.Any())
                    {
                        Log("DB에 KRC 저수지 정보가 없습니다. 먼저 'KRC 전체 코드 조회/저장'을 실행하세요.");
                        return;
                    }

                    Log($"총 {krcStations.Count}개의 KRC 저수지에 대해 {taskTitle}을(를) 진행합니다.");

                    for (int i = 0; i < krcStations.Count; i++)
                    {
                        var station = krcStations[i];
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