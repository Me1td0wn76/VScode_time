using System;
using System.Drawing;
using System.Windows.Forms;

namespace VscodeUsageTracker
{
    public partial class MainForm : Form
    {
        private VsCodeMonitor _monitor = null!;
        private NotifyIcon _notifyIcon = null!;
        private System.Windows.Forms.Timer _updateTimer = null!;
        private Label _totalTimeLabel = null!;
        private Label _todayTimeLabel = null!;
        private Label _statusLabel = null!;

        public MainForm()
        {
            InitializeComponent();
            InitializeMonitor();
            InitializeNotifyIcon();
            InitializeUpdateTimer();
            UpdateDisplay();
        }

        private void InitializeComponent()
        {
            this.Text = "VSCode 使用時間トラッカー";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false; // タスクバーに表示しない

            // コントロールの配置
            var titleLabel = new Label
            {
                Text = "VSCode 使用時間",
                Font = new Font("メイリオ", 14, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(200, 30),
                ForeColor = Color.DarkBlue
            };

            _totalTimeLabel = new Label
            {
                Text = "合計時間: 計算中...",
                Font = new Font("メイリオ", 12),
                Location = new Point(20, 60),
                Size = new Size(350, 30),
                ForeColor = Color.DarkGreen
            };

            _todayTimeLabel = new Label
            {
                Text = "今日の使用時間: 計算中...",
                Font = new Font("メイリオ", 12),
                Location = new Point(20, 100),
                Size = new Size(350, 30),
                ForeColor = Color.DarkOrange
            };

            _statusLabel = new Label
            {
                Text = "状態: 監視中",
                Font = new Font("メイリオ", 10),
                Location = new Point(20, 140),
                Size = new Size(350, 25),
                ForeColor = Color.Gray
            };

            var minimizeButton = new Button
            {
                Text = "最小化してトレイに格納",
                Location = new Point(20, 180),
                Size = new Size(180, 30),
                Font = new Font("メイリオ", 9)
            };
            minimizeButton.Click += MinimizeButton_Click;

            var exitButton = new Button
            {
                Text = "終了",
                Location = new Point(220, 180),
                Size = new Size(80, 30),
                Font = new Font("メイリオ", 9)
            };
            exitButton.Click += ExitButton_Click;

            this.Controls.AddRange(new Control[] 
            { 
                titleLabel, 
                _totalTimeLabel, 
                _todayTimeLabel, 
                _statusLabel,
                minimizeButton, 
                exitButton 
            });

            // フォームが閉じられる時の処理
            this.FormClosing += MainForm_FormClosing;
        }

        private void InitializeMonitor()
        {
            _monitor = new VsCodeMonitor();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = SystemIcons.Application;
            _notifyIcon.Text = "VSCode 使用時間トラッカー";
            _notifyIcon.Visible = false;

            // コンテキストメニュー
            var contextMenu = new ContextMenuStrip();
            var showMenuItem = new ToolStripMenuItem("表示", null, ShowForm_Click);
            var exitMenuItem = new ToolStripMenuItem("終了", null, ExitApplication_Click);
            
            contextMenu.Items.AddRange(new ToolStripItem[] { showMenuItem, exitMenuItem });
            _notifyIcon.ContextMenuStrip = contextMenu;

            // ダブルクリックで表示
            _notifyIcon.DoubleClick += ShowForm_Click;
        }

        private void InitializeUpdateTimer()
        {
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 30000; // 30秒ごとに更新
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            try
            {
                var totalTime = _monitor.CalculateTotalUsageTime();
                var todayEvents = _monitor.GetTodayEvents();
                
                // 今日の使用時間を計算
                var todayTime = CalculateTodayTime(todayEvents);

                _totalTimeLabel.Text = $"合計時間: {FormatTimeSpan(totalTime)}";
                _todayTimeLabel.Text = $"今日の使用時間: {FormatTimeSpan(todayTime)}";

                // VSCodeの実行状態を確認
                bool isRunning = System.Diagnostics.Process.GetProcessesByName("Code").Length > 0;
                _statusLabel.Text = $"状態: {(isRunning ? "VSCode実行中" : "VSCode停止中")} - 監視中";

                // トレイアイコンのツールチップも更新
                _notifyIcon.Text = $"VSCode使用時間 - 合計: {FormatTimeSpan(totalTime)}";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"エラー: {ex.Message}";
            }
        }

        private TimeSpan CalculateTodayTime(List<UsageEvent> todayEvents)
        {
            var totalTime = TimeSpan.Zero;
            DateTime? startTime = null;
            
            var sortedEvents = todayEvents.OrderBy(e => e.Timestamp).ToList();
            
            foreach (var evt in sortedEvents)
            {
                if (evt.EventType == "Start")
                {
                    startTime = evt.Timestamp;
                }
                else if (evt.EventType == "End" && startTime.HasValue)
                {
                    totalTime = totalTime.Add(evt.Timestamp - startTime.Value);
                    startTime = null;
                }
            }
            
            // 現在実行中の場合
            if (startTime.HasValue)
            {
                bool isRunning = System.Diagnostics.Process.GetProcessesByName("Code").Length > 0;
                if (isRunning)
                {
                    totalTime = totalTime.Add(DateTime.Now - startTime.Value);
                }
            }
            
            return totalTime;
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{(int)timeSpan.TotalHours}時間{timeSpan.Minutes}分";
        }

        private void MinimizeButton_Click(object? sender, EventArgs e)
        {
            this.Hide();
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(2000, "VSCode使用時間トラッカー", "トレイに最小化されました", ToolTipIcon.Info);
        }

        private void ExitButton_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ShowForm_Click(object? sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            _notifyIcon.Visible = false;
            UpdateDisplay();
        }

        private void ExitApplication_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MinimizeButton_Click(sender, e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _monitor?.Dispose();
                _notifyIcon?.Dispose();
                _updateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
