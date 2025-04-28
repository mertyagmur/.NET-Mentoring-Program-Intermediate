/*
 * 2.	Write a program, which creates a chain of four Tasks.
 * First Task – creates an array of 10 random integer.
 * Second Task – multiplies this array with another random integer.
 * Third Task – sorts this array by ascending.
 * Fourth Task – calculates the average value. All this tasks should print the values to console.
 */
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MultiThreading.Task2.Chaining
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(".Net Mentoring Program. MultiThreading V1 ");
            Console.WriteLine("2.	Write a program, which creates a chain of four Tasks.");
            Console.WriteLine("First Task – creates an array of 10 random integer.");
            Console.WriteLine("Second Task – multiplies this array with another random integer.");
            Console.WriteLine("Third Task – sorts this array by ascending.");
            Console.WriteLine("Fourth Task – calculates the average value. All this tasks should print the values to console");
            Console.WriteLine();


            var taskChain = Task.Run(CreateRandomArray)
                            .ContinueWith(n1 => MultiplyArray(n1.Result))
                            .ContinueWith(n2 => SortArrayByAscending(n2.Result))
                            .ContinueWith(n3 => GetAverage(n3.Result));

            taskChain.Wait();

            static int[] CreateRandomArray()
            {
                Random rnd = new Random();

                int[] numbers = Enumerable.Range(0, 10)
                                  .Select(_ => rnd.Next(1, 101))
                                  .ToArray();

                Console.WriteLine("Initial list of numbers:");
                Console.WriteLine(string.Join(", ", numbers));
                return numbers;
            }

            static int[] MultiplyArray(int[] numbers)
            {
                int multiplier = new Random().Next(1, 10);
                numbers = numbers.Select(n => n * multiplier).ToArray();
                Console.WriteLine($"\nMultiplied by {multiplier}:");
                Console.WriteLine(String.Join(", ", numbers));
                return numbers;
            }

            static int[] SortArrayByAscending(int[] numbers)
            {
                numbers = numbers.OrderBy(n => n).ToArray();
                Console.WriteLine("\nSorted by ascending:");
                Console.WriteLine(String.Join(", ", numbers));
                return numbers;
            }

            static double GetAverage(int[] numbers)
            {
                double avg = numbers.Average();
                Console.WriteLine($"\nAverage:");
                Console.WriteLine(avg);
                return avg;
            }

            Console.ReadLine();
        }
    }
}
