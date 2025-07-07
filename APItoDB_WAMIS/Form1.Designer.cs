namespace WamisDataCollector
{
    partial class Form1
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
            this.SuspendLayout();
            // 
            // _dtpStartDate
            // 
            this._dtpStartDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this._dtpStartDate.Location = new System.Drawing.Point(68, 12);
            this._dtpStartDate.Name = "_dtpStartDate";
            this._dtpStartDate.Size = new System.Drawing.Size(200, 21);
            this._dtpStartDate.TabIndex = 0;
            this._dtpStartDate.Value = new System.DateTime(1990, 1, 1, 0, 0, 0, 0);
            // 
            // _dtpEndDate
            // 
            this._dtpEndDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this._dtpEndDate.Location = new System.Drawing.Point(336, 12);
            this._dtpEndDate.Name = "_dtpEndDate";
            this._dtpEndDate.Size = new System.Drawing.Size(200, 21);
            this._dtpEndDate.TabIndex = 1;
            // 
            // _btnInitialLoad
            // 
            this._btnInitialLoad.Location = new System.Drawing.Point(12, 50);
            this._btnInitialLoad.Name = "_btnInitialLoad";
            this._btnInitialLoad.Size = new System.Drawing.Size(150, 30);
            this._btnInitialLoad.TabIndex = 2;
            this._btnInitialLoad.Text = "초기 데이터 로드";
            this._btnInitialLoad.UseVisualStyleBackColor = true;
            this._btnInitialLoad.Click += new System.EventHandler(this.BtnInitialLoad_Click);
            // 
            // _btnDailyUpdate
            // 
            this._btnDailyUpdate.Location = new System.Drawing.Point(170, 50);
            this._btnDailyUpdate.Name = "_btnDailyUpdate";
            this._btnDailyUpdate.Size = new System.Drawing.Size(150, 30);
            this._btnDailyUpdate.TabIndex = 3;
            this._btnDailyUpdate.Text = "일별 최신화";
            this._btnDailyUpdate.UseVisualStyleBackColor = true;
            this._btnDailyUpdate.Click += new System.EventHandler(this.BtnDailyUpdate_Click);
            // 
            // _btnBackfill
            // 
            this._btnBackfill.Location = new System.Drawing.Point(328, 50);
            this._btnBackfill.Name = "_btnBackfill";
            this._btnBackfill.Size = new System.Drawing.Size(150, 30);
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
            this._txtLogs.Location = new System.Drawing.Point(12, 90);
            this._txtLogs.Multiline = true;
            this._txtLogs.Name = "_txtLogs";
            this._txtLogs.ReadOnly = true;
            this._txtLogs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._txtLogs.Size = new System.Drawing.Size(760, 420);
            this._txtLogs.TabIndex = 5;
            // 
            // _progressBar
            // 
            this._progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._progressBar.Location = new System.Drawing.Point(12, 520);
            this._progressBar.MarqueeAnimationSpeed = 0;
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(760, 20);
            this._progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this._progressBar.TabIndex = 6;
            // 
            // lblStartDate
            // 
            this.lblStartDate.AutoSize = true;
            this.lblStartDate.Location = new System.Drawing.Point(12, 15);
            this.lblStartDate.Name = "lblStartDate";
            this.lblStartDate.Size = new System.Drawing.Size(45, 12);
            this.lblStartDate.TabIndex = 7;
            this.lblStartDate.Text = "시작일:";
            // 
            // lblEndDate
            // 
            this.lblEndDate.AutoSize = true;
            this.lblEndDate.Location = new System.Drawing.Point(280, 15);
            this.lblEndDate.Name = "lblEndDate";
            this.lblEndDate.Size = new System.Drawing.Size(45, 12);
            this.lblEndDate.TabIndex = 8;
            this.lblEndDate.Text = "종료일:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.lblEndDate);
            this.Controls.Add(this.lblStartDate);
            this.Controls.Add(this._progressBar);
            this.Controls.Add(this._txtLogs);
            this.Controls.Add(this._btnBackfill);
            this.Controls.Add(this._btnDailyUpdate);
            this.Controls.Add(this._btnInitialLoad);
            this.Controls.Add(this._dtpEndDate);
            this.Controls.Add(this._dtpStartDate);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WAMIS 데이터 수집기";
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
    }
}
