/*
 * 1.	Write a program, which creates an array of 100 Tasks, runs them and waits all of them are not finished.
 * Each Task should iterate from 1 to 1000 and print into the console the following string:
 * “Task #0 – {iteration number}”.
 */
using System;
using System.Threading.Tasks;

namespace MultiThreading.Task1._100Tasks
{
    class Program
    {
        const int TaskAmount = 100;
        const int MaxIterationsCount = 1000;

        static void Main(string[] args)
        {
            Console.WriteLine(".Net Mentoring Program. Multi threading V1.");
            Console.WriteLine("1.	Write a program, which creates an array of 100 Tasks, runs them and waits all of them are not finished.");
            Console.WriteLine("Each Task should iterate from 1 to 1000 and print into the console the following string:");
            Console.WriteLine("“Task #0 – {iteration number}”.");
            Console.WriteLine();
            
            HundredTasks();

            Console.ReadLine();
        }

        static void HundredTasks()
        {
            // Creating an array of 100 tasks
            Task[] tasks = new Task[TaskAmount];

            for (int i = 0; i < TaskAmount; i++)
            {
                int taskNumber = i;
                tasks[taskNumber] = new Task(() => 
                    {
                        for (int j = 1; j <= MaxIterationsCount; j++)
                        {
                            Output(taskNumber, j);
                        }
                    }
                );
            }

            // Running all tasks
            foreach (Task task in tasks)
            {
                task.Start();
            }

            // Waiting for all tasks
            Task.WaitAll(tasks);
        }

        static void Output(int taskNumber, int iterationNumber)
        {
            Console.WriteLine($"Task #{taskNumber} – {iterationNumber}");
        }
    }
}
