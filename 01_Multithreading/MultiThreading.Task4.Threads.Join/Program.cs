/*
 * 4.	Write a program which recursively creates 10 threads.
 * Each thread should be with the same body and receive a state with integer number, decrement it,
 * print and pass as a state into the newly created thread.
 * Use Thread class for this task and Join for waiting threads.
 * 
 * Implement all of the following options:
 * - a) Use Thread class for this task and Join for waiting threads.
 * - b) ThreadPool class for this task and Semaphore for waiting threads.
 */

using System;
using System.Threading;

namespace MultiThreading.Task4.Threads.Join
{
    class Program
    {
        static Semaphore semaphore = new Semaphore(0, 1);
        static void Main(string[] args)
        {
            Console.WriteLine("4.	Write a program which recursively creates 10 threads.");
            Console.WriteLine("Each thread should be with the same body and receive a state with integer number, decrement it, print and pass as a state into the newly created thread.");
            Console.WriteLine("Implement all of the following options:");
            Console.WriteLine();
            Console.WriteLine("- a) Use Thread class for this task and Join for waiting threads.");
            Console.WriteLine("- b) ThreadPool class for this task and Semaphore for waiting threads.");

            Console.WriteLine();

            Console.WriteLine("Method A:");
            CreateThread(10);
            Console.WriteLine("Method B:");
            CreateThreadPool(10);
            semaphore.WaitOne();

            Console.ReadLine();
        }

        static void CreateThread(int state)
        {
            Thread t = new Thread(state =>
            {
                int currentState = (int)state;
                currentState -= 1;
                Console.WriteLine(currentState);

                if (currentState > 0)
                {
                    CreateThread(currentState);
                }
            });
            t.Start(state);
            t.Join();
        }

        static void CreateThreadPool(int state)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                int currentState = (int)state;
                currentState -= 1;
                Console.WriteLine(currentState);

                if (currentState > 0)
                {
                    CreateThreadPool(currentState);
                }
                else
                {
                    semaphore.Release();
                }
            }, state);
        }
    }
}
