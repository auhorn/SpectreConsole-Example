using Spectre.Console;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace spectre_example;

internal class Program
{
    static async Task Main(string[] args)
    {
        AnsiConsole.MarkupLine("[green]Starting[/]");

        // creating some data
        var allActions = new List<Tuple<string, int>>
        {
            new Tuple<string, int>("Action 1", 10),
            new Tuple<string, int>("Action 2", 10),
            new Tuple<string, int>("Action 3", 5),
            new Tuple<string, int>("Action 4", 7),
            new Tuple<string, int>("Action 5", 2),
            new Tuple<string, int>("Action 6", 10),
            new Tuple<string, int>("Action 7", 3),
            new Tuple<string, int>("Action 8", 8),
            new Tuple<string, int>("Action 9", 2),
            new Tuple<string, int>("Action 10", 3)
        };

        
        Stopwatch stopWatch = new();

        // collect exceptions that may occur during processing
        var exceptions = new ConcurrentQueue<Exception>();

        stopWatch.Start();
        await AnsiConsole.Progress()
            .AutoRefresh(true)
            .AutoClear(true)
            .HideCompleted(true) // in case we have a lot we only want to see the ones that are still running
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn()
            })
            .StartAsync(async ctx =>
            {
                // how many parallel tasks to run
                ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = 3 };

                // one task for the overall progress
                var overallTask = ctx.AddTask("[green]Overall Progress[/]");
                overallTask.MaxValue(allActions.Count);

                // run all tasks in parallel
                await Parallel.ForEachAsync(allActions, parallelOptions,
                    async (oneAction, cancellationToken) =>
                    {
                        var task = ctx.AddTask(oneAction.Item1);
                        try
                        {
                            await DoWorkAsync(task, oneAction);
                        }
                        catch (Exception ex)
                        {
                            // continue with the next task and enqueue the exception for later processing
                            exceptions.Enqueue(ex);
                        }
                        overallTask.Increment(1);
                    }
                );
            });

        // if we have exceptions we can throw them here or do something else
        if (exceptions.Count > 0)
        {
            foreach (var ex in exceptions)
            {
                AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            }
            //throw new AggregateException(exceptions);
        }

        stopWatch.Stop();
        AnsiConsole.MarkupLine($"[green]Done[/]. Completing [blue]{allActions.Count}[/] items in [blue]{stopWatch.Elapsed.Seconds}[/] sec. ");
    }

    /// <summary>
    /// Do the actual work...
    /// </summary>
    /// <param name="task">reference to the task to update the UI</param>
    /// <param name="action">parameter for the task to do</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">in case something is wrong...</exception>
    static async Task DoWorkAsync(ProgressTask task, Tuple<string, int> action)
    {
        if (action.Item2 % 3 == 0)
        {
            throw new ArgumentException($"Invalid {action.Item1}");
        }

        task.MaxValue(action.Item2);
        for (int i = 0; i < action.Item2; i++)
        {
            // not doing something useful here
            await Task.Delay(250);

            // don't forget to update progress
            task.Increment(1);
        }

    }

}

