using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScottPlot;

namespace VscodeUsageTracker
{
    public partial class ModernMainForm : Form
    {
        private VsCodeMonitor _monitor = null!;
        private NotifyIcon _notifyIcon = null!;
        private System.Windows.Forms.Timer _updateTimer = null!;
        
        // UI コントロール
        private Panel _sidePanel = null!;
        private Panel _mainPanel = null!;
        private TabControl _tabControl = null!;
        private Label _totalTimeLabel = null!;
        private Label _todayTimeLabel = null!;
        private Label _statusLabel = null!;
        private Panel _statsPanel = null!;
        private ScottPlot.FormsPlot _dailyChart = null!;
        private ScottPlot.FormsPlot _hourlyChart = null!;

        // カラーテーマ
        private readonly Color _primaryColor = Color.FromArgb(37, 99, 235);      // Blue
        private readonly Color _secondaryColor = Color.FromArgb(99, 102, 241);   // Indigo
        private readonly Color _backgroundColorDark = Color.FromArgb(17, 24, 39); // Dark Gray
        private readonly Color _backgroundColorLight = Color.FromArgb(243, 244, 246); // Light Gray
        private readonly Color _textColorLight = Color.FromArgb(75, 85, 99);     // Gray
        private readonly Color _accentColor = Color.FromArgb(34, 197, 94);       // Green

        public ModernMainForm()
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
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = _backgroundColorLight;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;

            CreateSidePanel();
            CreateMainPanel();
            CreateTabControl();
            
            this.FormClosing += ModernMainForm_FormClosing;
        }

        private void CreateSidePanel()
        {
            _sidePanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = _backgroundColorDark,
                Padding = new Padding(20)
            };

            // ロゴ・タイトル
            var titleLabel = new Label
            {
                Text = "VSCode Tracker",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                Size = new Size(240, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ステータス
            _statusLabel = new Label
            {
                Text = "監視中...",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.LightGray,
                Location = new Point(20, 70),
                Size = new Size(240, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // メイン統計カード
            CreateStatsCards();

            // ボタン
            var minimizeButton = CreateStyledButton("トレイに最小化", new Point(20, 550), MinimizeButton_Click);
            var exitButton = CreateStyledButton("終了", new Point(20, 590), ExitButton_Click);
            exitButton.BackColor = Color.FromArgb(239, 68, 68); // Red

            _sidePanel.Controls.AddRange(new Control[] 
            { 
                titleLabel, 
                _statusLabel,
                _statsPanel,
                minimizeButton, 
                exitButton 
            });

            this.Controls.Add(_sidePanel);
        }

        private void CreateStatsCards()
        {
            _statsPanel = new Panel
            {
                Location = new Point(20, 120),
                Size = new Size(240, 400),
                BackColor = Color.Transparent
            };

            // 統計カードを作成
            CreateStatCard("合計時間", "計算中...", new Point(0, 0), _primaryColor);
            CreateStatCard("今日の時間", "計算中...", new Point(0, 100), _secondaryColor);
        }

        private void CreateStatCard(string title, string value, Point location, Color accentColor)
        {
            var cardPanel = new Panel
            {
                Location = location,
                Size = new Size(240, 80),
                BackColor = Color.FromArgb(31, 41, 55),
                Margin = new Padding(0, 0, 0, 10)
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.LightGray,
                Location = new Point(15, 10),
                Size = new Size(210, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 35),
                Size = new Size(210, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var accentBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(4, 80),
                BackColor = accentColor
            };

            cardPanel.Controls.AddRange(new Control[] { accentBar, titleLabel, valueLabel });
            _statsPanel.Controls.Add(cardPanel);

            // ラベルを後で更新できるように保存
            if (title == "合計時間")
            {
                _totalTimeLabel = valueLabel;
            }
            else if (title == "今日の時間")
            {
                _todayTimeLabel = valueLabel;
            }
        }

        private Button CreateStyledButton(string text, Point location, EventHandler clickHandler)
        {
            var button = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(240, 35),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                BackColor = _primaryColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            
            button.FlatAppearance.BorderSize = 0;
            button.Click += clickHandler;
            
            return button;
        }

        private void CreateMainPanel()
        {
            _mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _backgroundColorLight,
                Padding = new Padding(20)
            };

            this.Controls.Add(_mainPanel);
        }

        private void CreateTabControl()
        {
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                Padding = new Point(20, 10)
            };

            // ダッシュボードタブ
            var dashboardTab = new TabPage("ダッシュボード");
            CreateDashboardTab(dashboardTab);

            // 日別グラフタブ
            var dailyTab = new TabPage("日別使用時間");
            CreateDailyChartTab(dailyTab);

            // 時間別グラフタブ
            var hourlyTab = new TabPage("時間別パターン");
            CreateHourlyChartTab(hourlyTab);

            _tabControl.TabPages.AddRange(new TabPage[] { dashboardTab, dailyTab, hourlyTab });
            _mainPanel.Controls.Add(_tabControl);
        }

        private void CreateDashboardTab(TabPage tab)
        {
            tab.BackColor = _backgroundColorLight;
            
            var headerLabel = new Label
            {
                Text = "使用状況ダッシュボード",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = _textColorLight,
                Location = new Point(20, 20),
                Size = new Size(300, 30)
            };

            // 詳細統計パネル
            var detailStatsPanel = new Panel
            {
                Location = new Point(20, 70),
                Size = new Size(640, 400),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 統計ラベルを後で追加
            var statsLabels = new List<Label>();
            for (int i = 0; i < 6; i++)
            {
                var label = new Label
                {
                    Font = new Font("Segoe UI", 12),
                    ForeColor = _textColorLight,
                    Location = new Point(30, 30 + i * 50),
                    Size = new Size(580, 30),
                    Text = "読み込み中..."
                };
                statsLabels.Add(label);
                detailStatsPanel.Controls.Add(label);
            }

            tab.Controls.AddRange(new Control[] { headerLabel, detailStatsPanel });
        }

        private void CreateDailyChartTab(TabPage tab)
        {
            tab.BackColor = _backgroundColorLight;
            
            _dailyChart = new ScottPlot.FormsPlot
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            _dailyChart.Plot.Style(Style.Gray1);
            _dailyChart.Plot.Title("日別使用時間（過去30日）");
            _dailyChart.Plot.XLabel("日付");
            _dailyChart.Plot.YLabel("使用時間（時間）");

            tab.Controls.Add(_dailyChart);
        }

        private void CreateHourlyChartTab(TabPage tab)
        {
            tab.BackColor = _backgroundColorLight;
            
            _hourlyChart = new ScottPlot.FormsPlot
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            _hourlyChart.Plot.Style(Style.Gray1);
            _hourlyChart.Plot.Title("時間別使用パターン");
            _hourlyChart.Plot.XLabel("時間");
            _hourlyChart.Plot.YLabel("使用時間（分）");

            tab.Controls.Add(_hourlyChart);
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
                
                // サイドパネルの統計更新
                _totalTimeLabel.Text = FormatTimeSpan(stats.TotalTime);
                _todayTimeLabel.Text = FormatTimeSpan(stats.TodayTime);

                // ステータス更新
                bool isRunning = System.Diagnostics.Process.GetProcessesByName("Code").Length > 0;
                _statusLabel.Text = isRunning ? "● VSCode実行中" : "○ VSCode停止中";
                _statusLabel.ForeColor = isRunning ? _accentColor : Color.LightGray;

                // ダッシュボードの詳細統計更新
                UpdateDashboardStats(stats);

                // グラフ更新
                UpdateCharts();

                // トレイアイコンのツールチップ更新
                _notifyIcon.Text = $"VSCode使用時間 - 合計: {FormatTimeSpan(stats.TotalTime)}";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"エラー: {ex.Message}";
                _statusLabel.ForeColor = Color.Red;
            }
        }

        private void UpdateDashboardStats(UsageStatistics stats)
        {
            if (_tabControl.TabPages[0].Controls.Count > 1)
            {
                var detailPanel = _tabControl.TabPages[0].Controls[1] as Panel;
                if (detailPanel != null && detailPanel.Controls.Count >= 6)
                {
                    var labels = detailPanel.Controls.OfType<Label>().ToArray();
                    labels[0].Text = $"📊 合計使用時間: {FormatTimeSpan(stats.TotalTime)}";
                    labels[1].Text = $"📅 今日の使用時間: {FormatTimeSpan(stats.TodayTime)}";
                    labels[2].Text = $"📈 週平均: {FormatTimeSpan(stats.WeeklyAverage)}";
                    labels[3].Text = $"⏰ 最長セッション: {FormatTimeSpan(stats.LongestSession)}";
                    labels[4].Text = $"🔢 総セッション数: {stats.TotalSessions}回";
                    labels[5].Text = $"🏆 最もアクティブな日: {stats.MostActiveDay:yyyy/MM/dd}";
                }
            }
        }

        private void UpdateCharts()
        {
            try
            {
                // 日別グラフの更新
                var dailyUsage = _monitor.GetDailyUsage(30);
                if (dailyUsage != null && dailyUsage.Count > 0)
                {
                    var dates = dailyUsage.Keys.OrderBy(d => d).ToArray();
                    var hours = dailyUsage.Values.Select(t => t.TotalHours).ToArray();

                    _dailyChart.Plot.Clear();
                    if (hours.Length > 0)
                    {
                        var dailyBar = _dailyChart.Plot.AddBar(hours);
                        dailyBar.FillColor = Color.FromArgb(37, 99, 235);
                        
                        _dailyChart.Plot.XTicks(Enumerable.Range(0, dates.Length).Select(i => (double)i).ToArray(),
                                               dates.Select(d => d.ToString("M/d")).ToArray());
                    }
                    _dailyChart.Refresh();
                }

                // 時間別グラフの更新
                var hourlyUsage = _monitor.GetHourlyUsage();
                if (hourlyUsage != null && hourlyUsage.Count > 0)
                {
                    var hourLabels = Enumerable.Range(0, 24).ToArray();
                    var hourMinutes = hourlyUsage.Values.Select(t => t.TotalMinutes).ToArray();

                    _hourlyChart.Plot.Clear();
                    if (hourMinutes.Length > 0)
                    {
                        var hourlyBar = _hourlyChart.Plot.AddBar(hourMinutes);
                        hourlyBar.FillColor = Color.FromArgb(99, 102, 241);
                        
                        _hourlyChart.Plot.XTicks(Enumerable.Range(0, 24).Select(i => (double)i).ToArray(),
                                                hourLabels.Select(h => h.ToString("00")).ToArray());
                    }
                    _hourlyChart.Refresh();
                }
            }
            catch (Exception ex)
            {
                // グラフ更新エラーは無視（デバッグ時のみ表示）
                System.Diagnostics.Debug.WriteLine($"グラフ更新エラー: {ex.Message}");
            }
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                return $"{(int)timeSpan.TotalDays}日{timeSpan.Hours}時間{timeSpan.Minutes}分";
            }
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

        private void ModernMainForm_FormClosing(object? sender, FormClosingEventArgs e)
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
