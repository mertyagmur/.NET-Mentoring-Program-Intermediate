/*
*  Create a Task and attach continuations to it according to the following criteria:
   a.    Continuation task should be executed regardless of the result of the parent task.
   b.    Continuation task should be executed when the parent task finished without success.
   c.    Continuation task should be executed when the parent task would be finished with fail and parent task thread should be reused for continuation
   d.    Continuation task should be executed outside of the thread pool when the parent task would be cancelled
   Demonstrate the work of the each case with console utility.
*/
using System;
using System.Threading.Tasks;
using System.Threading;

namespace MultiThreading.Task6.Continuation
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Create a Task and attach continuations to it according to the following criteria:");
            Console.WriteLine("a.    Continuation task should be executed regardless of the result of the parent task.");
            Console.WriteLine("b.    Continuation task should be executed when the parent task finished without success.");
            Console.WriteLine("c.    Continuation task should be executed when the parent task would be finished with fail and parent task thread should be reused for continuation.");
            Console.WriteLine("d.    Continuation task should be executed outside of the thread pool when the parent task would be cancelled.");
            Console.WriteLine("Demonstrate the work of the each case with console utility.");
            Console.WriteLine();

            var cts = new CancellationTokenSource();

            var parent = Task.Run(() =>
            {
                Console.WriteLine($"Parent task started on Thread {Thread.CurrentThread.ManagedThreadId}");
                // Uncomment one of these lines to simulate different outcomes:

                // Success
                // Console.WriteLine("Parent completed successfully.");

                // Failure
                // throw new Exception("Simulated failure.");

                // Cancellation
                cts.Token.ThrowIfCancellationRequested();
            }, cts.Token);

            // a. Continue regardless
            var continueRegardless = parent.ContinueWith(t =>
            {
                Console.WriteLine($"[Case 1] Continue Regardless on Thread {Thread.CurrentThread.ManagedThreadId}");
                Console.WriteLine($"Parent Status: {t.Status}");
            }, TaskContinuationOptions.None);

            // b. Continue when parent completed without success
            var continueOnNotSuccess = parent.ContinueWith(t =>
            {
                Console.WriteLine($"[Case 2] Continue if Not Success on Thread {Thread.CurrentThread.ManagedThreadId}");
                Console.WriteLine($"Parent Status: {t.Status}");
            }, TaskContinuationOptions.NotOnRanToCompletion);

            // c. Continue on failure and reuse parent thread
            var continueOnFailureReuseThread = parent.ContinueWith(t =>
            {
                Console.WriteLine($"[Case 3] Continue on Failure (Reuse Thread) on Thread {Thread.CurrentThread.ManagedThreadId}");
                Console.WriteLine($"Parent Status: {t.Status}");
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

            // d. Continue on cancellation outside thread pool
            var continueOnCanceledOutsideThreadPool = parent.ContinueWith(t =>
            {
                Console.WriteLine($"[Case 4] Continue on Cancellation (LongRunning) on Thread {Thread.CurrentThread.ManagedThreadId}");
                Console.WriteLine($"Parent Status: {t.Status}");
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.LongRunning,
            TaskScheduler.Default);

            // Simulate cancellation
            // cts.Cancel();

            try
            {
                Task.WaitAll(continueRegardless, continueOnNotSuccess, continueOnFailureReuseThread, continueOnCanceledOutsideThreadPool);
            }
            catch (AggregateException ex)
            {
                foreach (var inner in ex.InnerExceptions)
                {
                    Console.WriteLine($"Exception: {inner.Message}");
                }
            }

            Console.WriteLine("\nAll continuations finished. Press any key to exit...");
            Console.ReadLine();
        }
    }
}