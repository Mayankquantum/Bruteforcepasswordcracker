using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace code;

public partial class MainWindow : Window
{
    private readonly PasswordGenerator _generator = new();
    private readonly PasswordHasher _hasher = new();
    private readonly PerformanceLogger _perfLogger = new();
    private BruteForceEngine? _engine;

    private string _targetHash = "";
    private volatile bool _wasFound;

    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _timer;

    public MainWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += (_, _) =>
            ElapsedText.Text = $"Elapsed: {_stopwatch.Elapsed.TotalSeconds:F1} s";
    }

    // (1) PASSWORD CREATION
    private void OnGenerateClick(object? sender, RoutedEventArgs e)
    {
        string password = _generator.Generate();
        _targetHash = _hasher.Hash(password);

        HashBox.Text = _targetHash;
        ResultBox.Text = "";
        PerfBox.Text = "";
        StatusText.Text = "Password generated. Ready to attack.";
        TriedText.Text = "Candidates tried: 0";
        ElapsedText.Text = "Elapsed: 0.0 s";
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
        PerfButton.IsEnabled = true;
    }

    // (2) START
    private void OnStartClick(object? sender, RoutedEventArgs e)
    {
        if (_targetHash.Length == 0) return;

        _wasFound = false;
        _engine = new BruteForceEngine();
        ThreadsText.Text = $"Threads: {_engine.ThreadCount} (CPU cores - 1)";

        _engine.OnProgress += tried =>
            Dispatcher.UIThread.Post(() => TriedText.Text = $"Candidates tried: {tried:N0}");

        _engine.OnFound += pw =>
        {
            _wasFound = true;
            Dispatcher.UIThread.Post(() => ResultBox.Text = $"FOUND: {pw}");
        };

        _engine.OnCompleted += () =>
            Dispatcher.UIThread.Post(() =>
            {
                _stopwatch.Stop();
                _timer.Stop();
                ElapsedText.Text = $"Elapsed: {_stopwatch.Elapsed.TotalSeconds:F2} s";
                Progress.IsIndeterminate = false;
                GenerateButton.IsEnabled = true;
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                PerfButton.IsEnabled = true;
                StatusText.Text = _wasFound ? "Done." : "Search finished — not found.";
            });

        StatusText.Text = "Attacking...";
        ResultBox.Text = "";
        GenerateButton.IsEnabled = false;
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        PerfButton.IsEnabled = false;
        Progress.IsIndeterminate = true;
        _stopwatch.Restart();
        _timer.Start();

        string target = _targetHash;
        Task.Run(() => _engine.Run(target));
    }

    // (3) STOP
    private void OnStopClick(object? sender, RoutedEventArgs e)
    {
        _engine?.Stop();
        StatusText.Text = "Stopping...";
    }

    // (8) PERFORMANCE TEST: same hash, 1 thread vs (cores-1) threads.
    private void OnPerfClick(object? sender, RoutedEventArgs e)
    {
        if (_targetHash.Length == 0) return;
        string target = _targetHash;

        GenerateButton.IsEnabled = false;
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = false;
        PerfButton.IsEnabled = false;
        StatusText.Text = "Performance test running — single-thread pass first, may take a while.";
        PerfBox.Text = "Running single-thread pass...";

        Task.Run(() =>
        {
            var engine = new BruteForceEngine();
            int multiThreads = engine.ThreadCount;

            TimeSpan single = engine.Run(target, 1);              // forced 1 thread
            Dispatcher.UIThread.Post(() =>
                PerfBox.Text = $"Single-thread: {single.TotalSeconds:F2} s\nRunning multi-thread pass...");

            TimeSpan multi = engine.Run(target);                   // (cores-1) threads
            string summary = _perfLogger.LogComparison(target, single, multi, multiThreads);

            Dispatcher.UIThread.Post(() =>
            {
                PerfBox.Text = summary + $"Log file:\n{_perfLogger.LogPath}";
                StatusText.Text = "Performance test done.";
                GenerateButton.IsEnabled = true;
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                PerfButton.IsEnabled = true;
            });
        });
    }
}
