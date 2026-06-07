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
    private BruteForceEngine? _engine;

    private string _targetHash = "";
    private volatile bool _wasFound;

    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _timer;

    public MainWindow()
    {
        InitializeComponent();

        // Ticks 10x/sec to refresh the elapsed-time label while running.
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += (_, _) =>
            ElapsedText.Text = $"Elapsed: {_stopwatch.Elapsed.TotalSeconds:F1} s";
    }

    // (1) PASSWORD CREATION: random password, hash it, show only the hash.
    private void OnGenerateClick(object? sender, RoutedEventArgs e)
    {
        string password = _generator.Generate();   // 4 or 5 chars, never displayed
        _targetHash = _hasher.Hash(password);

        HashBox.Text = _targetHash;
        ResultBox.Text = "";
        StatusText.Text = "Password generated. Ready to attack.";
        TriedText.Text = "Candidates tried: 0";
        ElapsedText.Text = "Elapsed: 0.0 s";
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
    }

    // (2) START: run the multi-threaded attack OFF the UI thread.
    private void OnStartClick(object? sender, RoutedEventArgs e)
    {
        if (_targetHash.Length == 0) return;

        _wasFound = false;
        _engine = new BruteForceEngine();
        ThreadsText.Text = $"Threads: {_engine.ThreadCount} (CPU cores - 1)";

        // Engine events fire from WORKER threads -> marshal to UI thread.
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
                StatusText.Text = _wasFound ? "Done." : "Search finished — not found.";
            });

        StatusText.Text = "Attacking...";
        ResultBox.Text = "";
        GenerateButton.IsEnabled = false;
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        Progress.IsIndeterminate = true;
        _stopwatch.Restart();
        _timer.Start();

        string target = _targetHash;
        Task.Run(() => _engine.Run(target));   // background, so the window stays responsive
    }

    // (3) STOP: halt all threads.
    private void OnStopClick(object? sender, RoutedEventArgs e)
    {
        _engine?.Stop();
        StatusText.Text = "Stopping...";
    }
}
