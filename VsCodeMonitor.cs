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
            string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "log");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            _logFilePath = Path.Combine(logDirectory, "vscode_usage.json");
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
}
