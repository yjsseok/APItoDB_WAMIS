using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;
using WamisDataCollector.Services;
using log4net;

namespace WamisDataCollector
{
    public partial class Form1 : Form
    {
        private readonly DataSyncService _syncService;
        private static readonly ILog log = LogManager.GetLogger(typeof(Form1)); 

        public Form1()
        {
            // 디자이너 파일에 정의된 UI 컨트롤들을 초기화합니다.
            InitializeComponent();

            // 서비스 초기화
            try
            {
                // App.config에서 설정 읽기
                var apiKey = ConfigurationManager.AppSettings["WamisApiKey"];
                var baseUrl = ConfigurationManager.AppSettings["WamisBaseUrl"];
                var connectionString = ConfigurationManager.ConnectionStrings["PostgreSqlConnection"].ConnectionString;

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("App.config 파일에 API 또는 데이터베이스 연결 설정이 올바르지 않습니다.");
                }

                var apiClient = new WamisApiClient(apiKey, baseUrl, this.Log);
                var dataService = new DataService(connectionString, this.Log);
                _syncService = new DataSyncService(apiClient, dataService, this.Log, log);
            }
            catch (Exception ex)
            {
                log.Fatal("설정 파일 로드 중 심각한 오류 발생", ex); 
                MessageBox.Show($"설정 파일(App.config) 로드 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        private async void BtnInitialLoad_Click(object sender, EventArgs e)
        {
            var startDate = _dtpStartDate.Value;
            var endDate = _dtpEndDate.Value;
            bool isTestMode = _chkTestMode.Checked; // 테스트 모드 상태 확인

            if (MessageBox.Show($"{startDate:yyyy-MM-dd}부터 {endDate:yyyy-MM-dd}까지의 {(isTestMode ? "테스트 모드 " : "")}전체 데이터를 수집합니다. 시간이 오래 걸릴 수 있습니다. 계속하시겠습니까?",
                                $"초기 데이터 로드{(isTestMode ? " (테스트)" : "")}",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await RunTask(async () => await _syncService.PerformInitialLoadAsync(startDate, endDate, isTestMode)); // testMode 전달
            }
        }

        private async void BtnDailyUpdate_Click(object sender, EventArgs e)
        {
            bool isTestMode = _chkTestMode.Checked; // 테스트 모드 상태 확인
            if (MessageBox.Show($"일별 데이터 최신화를 {(isTestMode ? "(테스트 모드)" : "")} 시작하시겠습니까?",
                                $"일별 최신화{(isTestMode ? " (테스트)" : "")}",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await RunTask(async () => await _syncService.PerformDailyUpdateAsync(isTestMode)); // testMode 전달
            }
        }

        private async void BtnBackfill_Click(object sender, EventArgs e)
        {
            bool isTestMode = _chkTestMode.Checked; // 테스트 모드 상태 확인
            if (MessageBox.Show($"누락 데이터 보충을 {(isTestMode ? "(테스트 모드)" : "")} 시작하시겠습니까? (최근 7일)",
                                $"누락 데이터 보충{(isTestMode ? " (테스트)" : "")}",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await RunTask(async () => await _syncService.BackfillMissingDataAsync(isTestMode)); // testMode 전달
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
            _progressBar.MarqueeAnimationSpeed = enabled ? 0 : 100;
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
    }
}