using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VscodeUsageTracker
{
    public class VsCodeMonitor
    {
        private readonly string _logFilePath;
        private readonly System.Threading.Timer _monitorTimer;
        private bool _wasRunning = false;
        private DateTime? _lastStartTime;
        private const int CheckIntervalMs = 60000; // 60秒ごとにチェック

        public VsCodeMonitor()
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
                Console.WriteLine($"ログディレクトリを作成しました: {logDirectory}");
            }
            
            _logFilePath = Path.Combine(logDirectory, "vscode_usage.json");
            
            // 初回起動時にログファイルが存在しない場合は空の配列で初期化
            if (!File.Exists(_logFilePath))
            {
                File.WriteAllText(_logFilePath, "[]");
                Console.WriteLine($"ログファイルを作成しました: {_logFilePath}");
            }
            
            _monitorTimer = new System.Threading.Timer(CheckVsCodeProcess, null, 0, CheckIntervalMs);
        }

        private void CheckVsCodeProcess(object? state)
        {
            bool isRunning = IsVsCodeRunning();
            
            if (isRunning && !_wasRunning)
            {
                // VSCodeが起動した
                _lastStartTime = DateTime.Now;
                LogEvent(new UsageEvent { EventType = "Start", Timestamp = _lastStartTime.Value });
            }
            else if (!isRunning && _wasRunning && _lastStartTime.HasValue)
            {
                // VSCodeが終了した
                var endTime = DateTime.Now;
                LogEvent(new UsageEvent { EventType = "End", Timestamp = endTime });
            }
            
            _wasRunning = isRunning;
        }

        private bool IsVsCodeRunning()
        {
            try
            {
                var processes = Process.GetProcessesByName("Code");
                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private void LogEvent(UsageEvent usageEvent)
        {
            try
            {
                var events = LoadEvents();
                events.Add(usageEvent);
                
                var json = JsonSerializer.Serialize(events, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(_logFilePath, json);
            }
            catch (Exception ex)
            {
                // ログエラーは無視（必要に応じてエラーハンドリングを追加）
                Debug.WriteLine($"ログエラー: {ex.Message}");
            }
        }

        private List<UsageEvent> LoadEvents()
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    return new List<UsageEvent>();
                }
                
                var json = File.ReadAllText(_logFilePath);
                return JsonSerializer.Deserialize<List<UsageEvent>>(json) ?? new List<UsageEvent>();
            }
            catch
            {
                return new List<UsageEvent>();
            }
        }

        public TimeSpan CalculateTotalUsageTime()
        {
            var events = LoadEvents();
            var totalTime = TimeSpan.Zero;
            
            // イベントを時間順でソート
            events = events.OrderBy(e => e.Timestamp).ToList();
            
            DateTime? startTime = null;
            
            foreach (var evt in events)
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
            
            // 現在実行中の場合は現在時刻まで加算
            if (startTime.HasValue && IsVsCodeRunning())
            {
                totalTime = totalTime.Add(DateTime.Now - startTime.Value);
            }
            
            return totalTime;
        }

        public List<UsageEvent> GetTodayEvents()
        {
            var events = LoadEvents();
            var today = DateTime.Today;
            return events.Where(e => e.Timestamp.Date == today).ToList();
        }

        public Dictionary<DateTime, TimeSpan> GetDailyUsage(int days = 30)
        {
            var events = LoadEvents();
            var result = new Dictionary<DateTime, TimeSpan>();
            var startDate = DateTime.Today.AddDays(-days);
            
            // 過去N日間の各日の使用時間を計算
            for (int i = 0; i <= days; i++)
            {
                var date = startDate.AddDays(i);
                var dayEvents = events.Where(e => e.Timestamp.Date == date).OrderBy(e => e.Timestamp).ToList();
                
                var dailyTime = TimeSpan.Zero;
                DateTime? startTime = null;
                
                foreach (var evt in dayEvents)
                {
                    if (evt.EventType == "Start")
                    {
                        startTime = evt.Timestamp;
                    }
                    else if (evt.EventType == "End" && startTime.HasValue)
                    {
                        dailyTime = dailyTime.Add(evt.Timestamp - startTime.Value);
                        startTime = null;
                    }
                }
                
                // 当日で現在実行中の場合
                if (date == DateTime.Today && startTime.HasValue && IsVsCodeRunning())
                {
                    dailyTime = dailyTime.Add(DateTime.Now - startTime.Value);
                }
                
                result[date] = dailyTime;
            }
            
            return result;
        }

        public Dictionary<int, TimeSpan> GetHourlyUsage()
        {
            var events = LoadEvents();
            var result = new Dictionary<int, TimeSpan>();
            
            // 24時間分を初期化
            for (int i = 0; i < 24; i++)
            {
                result[i] = TimeSpan.Zero;
            }
            
            var sortedEvents = events.OrderBy(e => e.Timestamp).ToList();
            DateTime? startTime = null;
            
            foreach (var evt in sortedEvents)
            {
                if (evt.EventType == "Start")
                {
                    startTime = evt.Timestamp;
                }
                else if (evt.EventType == "End" && startTime.HasValue)
                {
                    var duration = evt.Timestamp - startTime.Value;
                    var hour = startTime.Value.Hour;
                    result[hour] = result[hour].Add(duration);
                    startTime = null;
                }
            }
            
            return result;
        }

        public UsageStatistics GetStatistics()
        {
            var events = LoadEvents();
            var dailyUsage = GetDailyUsage(30);
            
            var stats = new UsageStatistics
            {
                TotalTime = CalculateTotalUsageTime(),
                TodayTime = CalculateTodayTime(),
                WeeklyAverage = CalculateWeeklyAverage(dailyUsage),
                LongestSession = CalculateLongestSession(events),
                TotalSessions = CountSessions(events),
                MostActiveDay = GetMostActiveDay(dailyUsage),
                FirstUseDate = events.Count > 0 ? events.Min(e => e.Timestamp).Date : DateTime.Today
            };
            
            return stats;
        }

        private TimeSpan CalculateTodayTime()
        {
            var todayEvents = GetTodayEvents();
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
            if (startTime.HasValue && IsVsCodeRunning())
            {
                totalTime = totalTime.Add(DateTime.Now - startTime.Value);
            }
            
            return totalTime;
        }

        private TimeSpan CalculateWeeklyAverage(Dictionary<DateTime, TimeSpan> dailyUsage)
        {
            var lastWeek = dailyUsage.Where(d => d.Key >= DateTime.Today.AddDays(-7)).ToList();
            if (lastWeek.Count == 0) return TimeSpan.Zero;
            
            var totalMinutes = lastWeek.Sum(d => d.Value.TotalMinutes);
            return TimeSpan.FromMinutes(totalMinutes / lastWeek.Count);
        }

        private TimeSpan CalculateLongestSession(List<UsageEvent> events)
        {
            var sortedEvents = events.OrderBy(e => e.Timestamp).ToList();
            var longestSession = TimeSpan.Zero;
            DateTime? startTime = null;
            
            foreach (var evt in sortedEvents)
            {
                if (evt.EventType == "Start")
                {
                    startTime = evt.Timestamp;
                }
                else if (evt.EventType == "End" && startTime.HasValue)
                {
                    var sessionTime = evt.Timestamp - startTime.Value;
                    if (sessionTime > longestSession)
                    {
                        longestSession = sessionTime;
                    }
                    startTime = null;
                }
            }
            
            return longestSession;
        }

        private int CountSessions(List<UsageEvent> events)
        {
            return events.Count(e => e.EventType == "Start");
        }

        private DateTime GetMostActiveDay(Dictionary<DateTime, TimeSpan> dailyUsage)
        {
            return dailyUsage.OrderByDescending(d => d.Value).FirstOrDefault().Key;
        }

        public void Dispose()
        {
            _monitorTimer?.Dispose();
        }
    }

    public class UsageEvent
    {
        public string EventType { get; set; } = string.Empty; // "Start" or "End"
        public DateTime Timestamp { get; set; }
    }

    public class UsageStatistics
    {
        public TimeSpan TotalTime { get; set; }
        public TimeSpan TodayTime { get; set; }
        public TimeSpan WeeklyAverage { get; set; }
        public TimeSpan LongestSession { get; set; }
        public int TotalSessions { get; set; }
        public DateTime MostActiveDay { get; set; }
        public DateTime FirstUseDate { get; set; }
    }
}
