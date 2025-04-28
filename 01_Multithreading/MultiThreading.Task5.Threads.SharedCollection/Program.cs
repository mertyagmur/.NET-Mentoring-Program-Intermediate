/*
 * 5. Write a program which creates two threads and a shared collection:
 * the first one should add 10 elements into the collection and the second should print all elements
 * in the collection after each adding.
 * Use Thread, ThreadPool or Task classes for thread creation and any kind of synchronization constructions.
 */
using System;
using System.Collections.Generic;
using System.Threading;

namespace MultiThreading.Task5.Threads.SharedCollection
{
    class Program
    {
        static List<int> sharedList = new List<int>();
        static readonly object lockObj = new object();
        static bool finished = false;

        static void Main(string[] args)
        {
            Console.WriteLine("5. Write a program which creates two threads and a shared collection:");
            Console.WriteLine("the first one should add 10 elements into the collection and the second should print all elements in the collection after each adding.");
            Console.WriteLine("Use Thread, ThreadPool or Task classes for thread creation and any kind of synchronization constructions.");
            Console.WriteLine();

            Thread addThread = new Thread(AddElements);
            Thread printThread = new Thread(PrintElementsAfterAdd);

            addThread.Start();
            printThread.Start();

            addThread.Join();
            printThread.Join();

            Console.ReadLine();
        }

        static void AddElements()
        {
            for (int i = 1; i <= 10; i++)
            {
                lock (lockObj)
                {
                    sharedList.Add(i);
                    Monitor.Pulse(lockObj);
                }
                Thread.Sleep(200); 
            }

            lock (lockObj)
            {
                finished = true;
                Monitor.Pulse(lockObj);
            }
        }

        static void PrintElementsAfterAdd()
        {
            int lastPrintedCount = 0;

            while (true)
            {
                lock (lockObj)
                {
                    while (sharedList.Count == lastPrintedCount && !finished)
                    {
                        Monitor.Wait(lockObj); 
                    }

                    if (sharedList.Count > lastPrintedCount)
                    {
                        Console.WriteLine("[" + string.Join(", ", sharedList) + "]");
                        lastPrintedCount = sharedList.Count;
                    }

                    if (finished && sharedList.Count == 10)
                        break;
                }
            }
        }
    }
}
