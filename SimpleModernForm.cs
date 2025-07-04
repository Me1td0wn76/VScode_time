using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace VscodeUsageTracker
{
    public partial class SimpleModernForm : Form
    {
        private VsCodeMonitor _monitor = null!;
        private NotifyIcon _notifyIcon = null!;
        private System.Windows.Forms.Timer _updateTimer = null!;
        
        // UI コントロール
        private Panel _headerPanel = null!;
        private Panel _statsPanel = null!;
        private Panel _buttonPanel = null!;
        private Label _titleLabel = null!;
        private Label _statusLabel = null!;
        private Label _totalTimeLabel = null!;
        private Label _todayTimeLabel = null!;
        private Label _weeklyAvgLabel = null!;
        private Label _sessionsLabel = null!;

        // カラーテーマ
        private readonly Color _primaryColor = Color.FromArgb(59, 130, 246);    // Blue
        private readonly Color _successColor = Color.FromArgb(34, 197, 94);     // Green
        private readonly Color _warningColor = Color.FromArgb(245, 158, 11);    // Yellow
        private readonly Color _dangerColor = Color.FromArgb(239, 68, 68);      // Red
        private readonly Color _backgroundColor = Color.FromArgb(248, 250, 252); // Light Gray
        private readonly Color _cardColor = Color.White;
        private readonly Color _textDark = Color.FromArgb(31, 41, 55);
        private readonly Color _textLight = Color.FromArgb(107, 114, 128);

        public SimpleModernForm()
        {
            InitializeComponent();
            InitializeMonitor();
            InitializeNotifyIcon();
            InitializeUpdateTimer();
            UpdateDisplay();
        }

        private void InitializeComponent()
        {
            this.Text = "VSCode 使用時間トラッカー v2.0";
            this.Size = new Size(500, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = _backgroundColor;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.Font = new Font("Segoe UI", 9);

            CreateHeaderPanel();
            CreateStatsPanel();
            CreateButtonPanel();
            
            this.FormClosing += SimpleModernForm_FormClosing;
        }

        private void CreateHeaderPanel()
        {
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = _primaryColor,
                Padding = new Padding(20)
            };

            _titleLabel = new Label
            {
                Text = "VSCode 使用時間トラッカー",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                Size = new Size(400, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _statusLabel = new Label
            {
                Text = "監視中...",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(219, 234, 254),
                Location = new Point(20, 45),
                Size = new Size(400, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _headerPanel.Controls.AddRange(new Control[] { _titleLabel, _statusLabel });
            this.Controls.Add(_headerPanel);
        }

        private void CreateStatsPanel()
        {
            _statsPanel = new Panel
            {
                Location = new Point(0, 80), // ヘッダーの高さ分だけ下にずらす
                Size = new Size(500, 490),   // 明示的にサイズを指定
                BackColor = _backgroundColor,
                Padding = new Padding(20),
                AutoScroll = true
            };

            // 統計カードを作成
            _totalTimeLabel = CreateStatCard(
                "📊 合計使用時間", 
                "計算中...", 
                new Point(20, 20), 
                _primaryColor
            );

            _todayTimeLabel = CreateStatCard(
                "📅 今日の使用時間", 
                "計算中...", 
                new Point(20, 130), 
                _successColor
            );

            _weeklyAvgLabel = CreateStatCard(
                "📈 週平均使用時間", 
                "計算中...", 
                new Point(20, 240), 
                _warningColor
            );

            _sessionsLabel = CreateStatCard(
                "🔢 総セッション数", 
                "計算中...", 
                new Point(20, 350), 
                _dangerColor
            );

            this.Controls.Add(_statsPanel);
        }

        private Label CreateStatCard(string title, string value, Point location, Color accentColor)
        {
            var cardPanel = new Panel
            {
                Location = location,
                Size = new Size(440, 90),
                BackColor = _cardColor,
                BorderStyle = BorderStyle.None
            };

            // カード外枠の影効果
            cardPanel.Paint += (s, e) =>
            {
                var rect = cardPanel.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                using (var pen = new Pen(Color.FromArgb(229, 231, 235)))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            var accentBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(4, 90),
                BackColor = accentColor
            };

            var titleLabelControl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = _textDark,
                Location = new Point(20, 15),
                Size = new Size(400, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(20, 35),
                Size = new Size(400, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };

            cardPanel.Controls.AddRange(new Control[] { accentBar, titleLabelControl, valueLabel });
            _statsPanel.Controls.Add(cardPanel);

            return valueLabel;
        }

        private void CreateButtonPanel()
        {
            _buttonPanel = new Panel
            {
                Location = new Point(0, 570), // フォームの下部に固定配置
                Size = new Size(500, 80),
                BackColor = _backgroundColor,
                Padding = new Padding(20)
            };

            var minimizeButton = new Button
            {
                Text = "トレイに最小化",
                Location = new Point(20, 20),
                Size = new Size(200, 40),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                BackColor = _primaryColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            minimizeButton.FlatAppearance.BorderSize = 0;
            minimizeButton.Click += MinimizeButton_Click;

            var exitButton = new Button
            {
                Text = "終了",
                Location = new Point(240, 20),
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                BackColor = _dangerColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            exitButton.FlatAppearance.BorderSize = 0;
            exitButton.Click += ExitButton_Click;

            var refreshButton = new Button
            {
                Text = "更新",
                Location = new Point(360, 20),
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                BackColor = _successColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.Click += (s, e) => UpdateDisplay();

            _buttonPanel.Controls.AddRange(new Control[] { minimizeButton, exitButton, refreshButton });
            this.Controls.Add(_buttonPanel);
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

            var contextMenu = new ContextMenuStrip();
            var showMenuItem = new ToolStripMenuItem("表示", null, ShowForm_Click);
            var exitMenuItem = new ToolStripMenuItem("終了", null, ExitApplication_Click);
            
            contextMenu.Items.AddRange(new ToolStripItem[] { showMenuItem, exitMenuItem });
            _notifyIcon.ContextMenuStrip = contextMenu;
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
                var stats = _monitor.GetStatistics();
                
                // 統計更新
                _totalTimeLabel.Text = FormatTimeSpan(stats.TotalTime);
                _todayTimeLabel.Text = FormatTimeSpan(stats.TodayTime);
                _weeklyAvgLabel.Text = FormatTimeSpan(stats.WeeklyAverage);
                _sessionsLabel.Text = $"{stats.TotalSessions} 回";

                // ステータス更新
                bool isRunning = System.Diagnostics.Process.GetProcessesByName("Code").Length > 0;
                _statusLabel.Text = isRunning ? "● VSCode実行中 - 監視中" : "○ VSCode停止中 - 監視中";

                // トレイアイコンのツールチップ更新
                _notifyIcon.Text = $"VSCode使用時間 - 合計: {FormatTimeSpan(stats.TotalTime)}";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"エラー: {ex.Message}";
                _statusLabel.ForeColor = _dangerColor;
            }
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalMinutes < 1)
            {
                return "0分";
            }
            else if (timeSpan.TotalHours < 1)
            {
                return $"{timeSpan.Minutes}分";
            }
            else if (timeSpan.TotalDays >= 1)
            {
                return $"{(int)timeSpan.TotalDays}日 {timeSpan.Hours}時間 {timeSpan.Minutes}分";
            }
            else
            {
                return $"{(int)timeSpan.TotalHours}時間 {timeSpan.Minutes}分";
            }
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

        private void SimpleModernForm_FormClosing(object? sender, FormClosingEventArgs e)
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
