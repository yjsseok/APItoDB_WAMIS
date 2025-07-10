namespace WamisDataCollector
{
    partial class MainFrm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this._dtpStartDate = new System.Windows.Forms.DateTimePicker();
            this._dtpEndDate = new System.Windows.Forms.DateTimePicker();
            this._btnInitialLoad = new System.Windows.Forms.Button();
            this._btnDailyUpdate = new System.Windows.Forms.Button();
            this._btnBackfill = new System.Windows.Forms.Button();
            this._txtLogs = new System.Windows.Forms.TextBox();
            this._progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStartDate = new System.Windows.Forms.Label();
            this.lblEndDate = new System.Windows.Forms.Label();
            this._chkTestMode = new System.Windows.Forms.CheckBox();
            this.groupBoxKRC = new System.Windows.Forms.GroupBox();
            this._btnKrcBackfill = new System.Windows.Forms.Button();
            this._btnKrcDailyUpdate = new System.Windows.Forms.Button();
            this._btnKrcInitialLoad = new System.Windows.Forms.Button();
            this._btnKrcFetchAllCodes = new System.Windows.Forms.Button();
            this.groupBoxWamis = new System.Windows.Forms.GroupBox();
            this.groupBoxKRC.SuspendLayout();
            this.groupBoxWamis.SuspendLayout();
            this.SuspendLayout();
            // 
            // _dtpStartDate
            // 
            this._dtpStartDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this._dtpStartDate.Location = new System.Drawing.Point(78, 15);
            this._dtpStartDate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._dtpStartDate.Name = "_dtpStartDate";
            this._dtpStartDate.Size = new System.Drawing.Size(190, 25);
            this._dtpStartDate.TabIndex = 0;
            this._dtpStartDate.Value = new System.DateTime(1990, 1, 1, 0, 0, 0, 0);
            // 
            // _dtpEndDate
            // 
            this._dtpEndDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this._dtpEndDate.Location = new System.Drawing.Point(346, 15);
            this._dtpEndDate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._dtpEndDate.Name = "_dtpEndDate";
            this._dtpEndDate.Size = new System.Drawing.Size(190, 25);
            this._dtpEndDate.TabIndex = 1;
            // 
            // _btnInitialLoad
            // 
            this._btnInitialLoad.Location = new System.Drawing.Point(10, 25);
            this._btnInitialLoad.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._btnInitialLoad.Name = "_btnInitialLoad";
            this._btnInitialLoad.Size = new System.Drawing.Size(140, 38);
            this._btnInitialLoad.TabIndex = 2;
            this._btnInitialLoad.Text = "초기 데이터 로드";
            this._btnInitialLoad.UseVisualStyleBackColor = true;
            this._btnInitialLoad.Click += new System.EventHandler(this.BtnInitialLoad_Click);
            // 
            // _btnDailyUpdate
            // 
            this._btnDailyUpdate.Location = new System.Drawing.Point(156, 25);
            this._btnDailyUpdate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._btnDailyUpdate.Name = "_btnDailyUpdate";
            this._btnDailyUpdate.Size = new System.Drawing.Size(140, 38);
            this._btnDailyUpdate.TabIndex = 3;
            this._btnDailyUpdate.Text = "일별 최신화";
            this._btnDailyUpdate.UseVisualStyleBackColor = true;
            this._btnDailyUpdate.Click += new System.EventHandler(this.BtnDailyUpdate_Click);
            // 
            // _btnBackfill
            // 
            this._btnBackfill.Location = new System.Drawing.Point(302, 25);
            this._btnBackfill.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._btnBackfill.Name = "_btnBackfill";
            this._btnBackfill.Size = new System.Drawing.Size(140, 38);
            this._btnBackfill.TabIndex = 4;
            this._btnBackfill.Text = "누락 데이터 보충";
            this._btnBackfill.UseVisualStyleBackColor = true;
            this._btnBackfill.Click += new System.EventHandler(this.BtnBackfill_Click);
            // 
            // _txtLogs
            // 
            this._txtLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._txtLogs.Location = new System.Drawing.Point(14, 182);
            this._txtLogs.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._txtLogs.Multiline = true;
            this._txtLogs.Name = "_txtLogs";
            this._txtLogs.ReadOnly = true;
            this._txtLogs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._txtLogs.Size = new System.Drawing.Size(868, 601);
            this._txtLogs.TabIndex = 5;
            // 
            // _progressBar
            // 
            this._progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._progressBar.Location = new System.Drawing.Point(14, 797);
            this._progressBar.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._progressBar.MarqueeAnimationSpeed = 0;
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(869, 25);
            this._progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this._progressBar.TabIndex = 6;
            // 
            // lblStartDate
            // 
            this.lblStartDate.AutoSize = true;
            this.lblStartDate.Location = new System.Drawing.Point(14, 19);
            this.lblStartDate.Name = "lblStartDate";
            this.lblStartDate.Size = new System.Drawing.Size(57, 15);
            this.lblStartDate.TabIndex = 7;
            this.lblStartDate.Text = "시작일:";
            // 
            // lblEndDate
            // 
            this.lblEndDate.AutoSize = true;
            this.lblEndDate.Location = new System.Drawing.Point(282, 19);
            this.lblEndDate.Name = "lblEndDate";
            this.lblEndDate.Size = new System.Drawing.Size(57, 15);
            this.lblEndDate.TabIndex = 8;
            this.lblEndDate.Text = "종료일:";
            // 
            // _chkTestMode
            // 
            this._chkTestMode.AutoSize = true;
            this._chkTestMode.Location = new System.Drawing.Point(779, 21);
            this._chkTestMode.Name = "_chkTestMode";
            this._chkTestMode.Size = new System.Drawing.Size(109, 19);
            this._chkTestMode.TabIndex = 10;
            this._chkTestMode.Text = "테스트 모드";
            this._chkTestMode.UseVisualStyleBackColor = true;
            // 
            // groupBoxKRC
            // 
            this.groupBoxKRC.Controls.Add(this._btnKrcBackfill);
            this.groupBoxKRC.Controls.Add(this._btnKrcDailyUpdate);
            this.groupBoxKRC.Controls.Add(this._btnKrcInitialLoad);
            this.groupBoxKRC.Controls.Add(this._btnKrcFetchAllCodes);
            this.groupBoxKRC.Location = new System.Drawing.Point(14, 115);
            this.groupBoxKRC.Name = "groupBoxKRC";
            this.groupBoxKRC.Size = new System.Drawing.Size(868, 60);
            this.groupBoxKRC.TabIndex = 11;
            this.groupBoxKRC.TabStop = false;
            this.groupBoxKRC.Text = "KRC 농업용 저수지";
            // 
            // _btnKrcBackfill
            // 
            this._btnKrcBackfill.Location = new System.Drawing.Point(448, 20);
            this._btnKrcBackfill.Name = "_btnKrcBackfill";
            this._btnKrcBackfill.Size = new System.Drawing.Size(140, 34);
            this._btnKrcBackfill.TabIndex = 3;
            this._btnKrcBackfill.Text = "수위 누락 보충";
            this._btnKrcBackfill.UseVisualStyleBackColor = true;
            this._btnKrcBackfill.Click += new System.EventHandler(this.BtnKrcBackfill_Click);
            // 
            // _btnKrcDailyUpdate
            // 
            this._btnKrcDailyUpdate.Location = new System.Drawing.Point(302, 20);
            this._btnKrcDailyUpdate.Name = "_btnKrcDailyUpdate";
            this._btnKrcDailyUpdate.Size = new System.Drawing.Size(140, 34);
            this._btnKrcDailyUpdate.TabIndex = 2;
            this._btnKrcDailyUpdate.Text = "수위 일별 최신화";
            this._btnKrcDailyUpdate.UseVisualStyleBackColor = true;
            this._btnKrcDailyUpdate.Click += new System.EventHandler(this.BtnKrcDailyUpdate_Click);
            // 
            // _btnKrcInitialLoad
            // 
            this._btnKrcInitialLoad.Location = new System.Drawing.Point(156, 20);
            this._btnKrcInitialLoad.Name = "_btnKrcInitialLoad";
            this._btnKrcInitialLoad.Size = new System.Drawing.Size(140, 34);
            this._btnKrcInitialLoad.TabIndex = 1;
            this._btnKrcInitialLoad.Text = "초기 수위 로드";
            this._btnKrcInitialLoad.UseVisualStyleBackColor = true;
            this._btnKrcInitialLoad.Click += new System.EventHandler(this.BtnKrcInitialLoad_Click);
            // 
            // _btnKrcFetchAllCodes
            // 
            this._btnKrcFetchAllCodes.Location = new System.Drawing.Point(10, 20);
            this._btnKrcFetchAllCodes.Name = "_btnKrcFetchAllCodes";
            this._btnKrcFetchAllCodes.Size = new System.Drawing.Size(140, 34);
            this._btnKrcFetchAllCodes.TabIndex = 0;
            this._btnKrcFetchAllCodes.Text = "코드 조회/저장";
            this._btnKrcFetchAllCodes.UseVisualStyleBackColor = true;
            this._btnKrcFetchAllCodes.Click += new System.EventHandler(this.BtnKrcFetchAllCodes_Click);
            // 
            // groupBoxWamis
            // 
            this.groupBoxWamis.Controls.Add(this._btnInitialLoad);
            this.groupBoxWamis.Controls.Add(this._btnDailyUpdate);
            this.groupBoxWamis.Controls.Add(this._btnBackfill);
            this.groupBoxWamis.Location = new System.Drawing.Point(14, 45);
            this.groupBoxWamis.Name = "groupBoxWamis";
            this.groupBoxWamis.Size = new System.Drawing.Size(868, 64);
            this.groupBoxWamis.TabIndex = 12;
            this.groupBoxWamis.TabStop = false;
            this.groupBoxWamis.Text = "WAMIS 수자원";
            // 
            // MainFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(896, 848);
            this.Controls.Add(this.groupBoxWamis);
            this.Controls.Add(this.groupBoxKRC);
            this.Controls.Add(this._chkTestMode);
            this.Controls.Add(this.lblEndDate);
            this.Controls.Add(this.lblStartDate);
            this.Controls.Add(this._progressBar);
            this.Controls.Add(this._txtLogs);
            this.Controls.Add(this._dtpEndDate);
            this.Controls.Add(this._dtpStartDate);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainFrm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "데이터 수집기 (WAMIS & KRC)";
            this.groupBoxKRC.ResumeLayout(false);
            this.groupBoxWamis.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker _dtpStartDate;
        private System.Windows.Forms.DateTimePicker _dtpEndDate;
        private System.Windows.Forms.Button _btnInitialLoad;
        private System.Windows.Forms.Button _btnDailyUpdate;
        private System.Windows.Forms.Button _btnBackfill;
        private System.Windows.Forms.TextBox _txtLogs;
        private System.Windows.Forms.ProgressBar _progressBar;
        private System.Windows.Forms.Label lblStartDate;
        private System.Windows.Forms.Label lblEndDate;
        private System.Windows.Forms.CheckBox _chkTestMode;
        private System.Windows.Forms.GroupBox groupBoxKRC;
        private System.Windows.Forms.Button _btnKrcBackfill;
        private System.Windows.Forms.Button _btnKrcDailyUpdate;
        private System.Windows.Forms.Button _btnKrcInitialLoad;
        private System.Windows.Forms.Button _btnKrcFetchAllCodes;
        private System.Windows.Forms.GroupBox groupBoxWamis;
    }
}
