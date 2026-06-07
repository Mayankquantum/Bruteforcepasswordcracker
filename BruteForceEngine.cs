using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace code;

// ORCHESTRATOR. Loops length 1..6, splits work across (cores-1) threads,
// stops everything the instant one thread finds the match.
public class BruteForceEngine
{
    private readonly PasswordValidator _validator = new();
    private const int MaxLength = 6;

    // Events the GUI subscribes to. The engine drives the GUI, not the reverse.
    public event Action<long> OnProgress = delegate { };   // candidates tried so far
    public event Action<string> OnFound = delegate { };    // the password, once found
    public event Action OnCompleted = delegate { };        // search ended

    private long _tried;
    private volatile bool _running;
    private volatile string _found = "";   // "" means not found yet

    // Max of (CPU cores - 1), never below 1.
    public int ThreadCount => Math.Max(1, Environment.ProcessorCount - 1);

    // Runs the attack, returns how long it took. threadOverride forces a
    // thread count (Stage 6 uses 1 to time a single-thread pass).
    public TimeSpan Run(string targetHash, int? threadOverride = null)
    {
        int threads = threadOverride ?? ThreadCount;
        _running = true;
        _tried = 0;
        _found = "";

        var sw = Stopwatch.StartNew();

        // Start at length 1 and climb. Engine never knows the real length.
        for (int length = 1; length <= MaxLength && _running && _found.Length == 0; length++)
        {
            int len = length;

            // Split by FIRST character: each worker owns starting letters
            // workerId, workerId+threads, workerId+2*threads, ...
            // so no two workers ever test the same candidate.
            Parallel.For(0, threads,
                new ParallelOptions { MaxDegreeOfParallelism = threads },
                (workerId, loopState) =>
                {
                    var generator = new CombinationGenerator();

                    for (int first = workerId; first < Charset.Length; first += threads)
                    {
                        if (_found.Length > 0 || !_running) { loopState.Stop(); return; }

                        char firstChar = Charset.Characters[first];

                        if (len == 1)
                        {
                            TryCandidate(firstChar.ToString(), targetHash, loopState);
                        }
                        else
                        {
                            foreach (string tail in generator.Generate(len - 1))
                            {
                                if (_found.Length > 0 || !_running) { loopState.Stop(); return; }
                                TryCandidate(firstChar + tail, targetHash, loopState);
                            }
                        }
                    }
                });
        }

        sw.Stop();
        _running = false;
        if (_found.Length > 0) OnFound.Invoke(_found);
        OnCompleted.Invoke();
        return sw.Elapsed;
    }

    private void TryCandidate(string candidate, string targetHash, ParallelLoopState loopState)
    {
        long count = Interlocked.Increment(ref _tried);
        if (count % 100000 == 0) OnProgress.Invoke(count);

        if (_validator.IsMatch(candidate, targetHash))
        {
            _found = candidate;
            loopState.Stop();
        }
    }

    public void Stop() => _running = false;
}
