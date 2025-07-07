using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;
using WamisDataCollector.Services;

namespace WamisDataCollector
{
    public partial class Form1 : Form
    {
        private readonly DataSyncService _syncService;

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
                _syncService = new DataSyncService(apiClient, dataService, this.Log);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 파일(App.config) 로드 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        private async void BtnInitialLoad_Click(object sender, EventArgs e)
        {
            var startDate = _dtpStartDate.Value;
            var endDate = _dtpEndDate.Value;

            if (MessageBox.Show($"{startDate:yyyy-MM-dd}부터 {endDate:yyyy-MM-dd}까지의 전체 데이터를 수집합니다. 시간이 오래 걸릴 수 있습니다. 계속하시겠습니까?", "초기 데이터 로드", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await RunTask(async () => await _syncService.PerformInitialLoadAsync(startDate, endDate));
            }
        }

        private async void BtnDailyUpdate_Click(object sender, EventArgs e)
        {
            await RunTask(async () => await _syncService.PerformDailyUpdateAsync());
        }

        private async void BtnBackfill_Click(object sender, EventArgs e)
        {
            await RunTask(async () => await _syncService.BackfillMissingDataAsync());
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