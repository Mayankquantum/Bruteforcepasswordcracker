using System;
using System.IO;

namespace code;

// Logs single-thread vs multi-thread performance to a .txt file and
// returns a readable summary for the GUI (requirement 8).
public class PerformanceLogger
{
    private readonly string _path =
        Path.Combine(Directory.GetCurrentDirectory(), "performance_log.txt");

    public string LogPath => _path;

    public string LogComparison(string targetHash, TimeSpan single, TimeSpan multi, int multiThreads)
    {
        double speedup = multi.TotalSeconds > 0 ? single.TotalSeconds / multi.TotalSeconds : 0;

        string entry =
            $"--- Performance comparison @ {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---{Environment.NewLine}" +
            $"Target hash  : {targetHash}{Environment.NewLine}" +
            $"Single-thread (1 thread)       : {single.TotalSeconds:F2} s{Environment.NewLine}" +
            $"Multi-thread  ({multiThreads} threads)      : {multi.TotalSeconds:F2} s{Environment.NewLine}" +
            $"Speedup (single / multi)       : {speedup:F2}x{Environment.NewLine}{Environment.NewLine}";

        File.AppendAllText(_path, entry);
        return entry;
    }
}
