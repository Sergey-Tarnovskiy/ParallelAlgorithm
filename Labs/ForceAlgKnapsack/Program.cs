using System.Diagnostics;

class Program
{
    static readonly object lockObj = new object();

    static int Knapsack( int[] weights, int[] values, int W )
    {
        int n = weights.Length;
        int totalCombinations = 1 << n; // 2^n
        int bestValue = 0;

        for (int mask = 0; mask < totalCombinations; mask++)
        {
            int weightSum = 0;
            int valueSum = 0;

            for (int i = 0; i < n; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    weightSum += weights[i];
                    valueSum += values[i];
                }
            }
            if (weightSum <= W && valueSum > bestValue)
            {
                bestValue = valueSum;
            }
        }
        return bestValue;
    }

    static int ParallelKnapsack(int[] weights, int[] values, int W )
    {
        int n = weights.Length;
        int totalCombinations = 1 << n; // 2^n
        int bestValue = 0;

        Parallel.For(0, totalCombinations, mask =>
        {
            int weightSum = 0;
            int valueSum  = 0;
            for (int i = 0; i < n; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    weightSum += weights[i];
                    valueSum  += values[i];
                }
            }
            if (weightSum <= W)
            {
                lock (lockObj)
                {
                    if (valueSum > bestValue)
                    {
                        bestValue = valueSum;
                    }
                }
            }
        }
        );

        return bestValue;
    }

    static void Main(string[] args)
    {
        // Базовый пример для проверки
        int[] weights = { 2, 3, 4, 5 };
        int[] values  = { 3, 4, 5, 6 };
        int W = 5;

        Console.WriteLine("___");
        Console.WriteLine("Simple Knapsack example");
        Console.WriteLine($"Weights: {string.Join(", ", weights)}");
        Console.WriteLine($"Values: {string.Join(", ", values)}");
        Console.WriteLine($"Max weight: {W}");
        Console.WriteLine("___");

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        int bestValue = Knapsack(weights, values, W);
        stopwatch.Stop();
        Console.WriteLine($"Best value (single-threaded): {bestValue}, Time: {stopwatch.ElapsedMilliseconds} ms");
        stopwatch.Restart();
        bestValue = ParallelKnapsack(weights, values, W);
        stopwatch.Stop();
        Console.WriteLine($"Best value (multi-threaded): {bestValue}, Time: {stopwatch.ElapsedMilliseconds} ms");

        Console.WriteLine("___");

        Console.WriteLine("Random Knapsack example");

        Random rand = new Random();

        // Гиперпараметры для тестов
        int maxCountTest = 10;
        
        int minCount = 10;
        int maxCount = 50;

        int minW = 10;
        int maxW = 30;

        int minValueWeights = 1;
        int maxValueWeights = 10;

        int minValueValues = 1;
        int maxValueValues = 10;

        for ( int i = 0; i < maxCountTest; i++)
        {
            int n = rand.Next(minCount, maxCount);
            weights = new int[n];
            values  = new int[n];
            for (int j = 0; j < n; j++)
            {
                weights[j] = rand.Next(minValueWeights, maxValueWeights);
                values[j]  = rand.Next(minValueValues,  maxValueValues);
            }
            W = rand.Next(minW, maxW);
            Console.WriteLine("___");
            Console.WriteLine($"Test {i + 1}:");
            Console.WriteLine($"Weights: {string.Join(", ", weights)}");
            Console.WriteLine($"Values: {string.Join(", ", values)}");
            Console.WriteLine($"Max weight: {W}");
            stopwatch.Restart();
            bestValue = Knapsack(weights, values, W);
            stopwatch.Stop();
            Console.WriteLine($"Best value (single-threaded): {bestValue}, Time: {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Restart();
            bestValue = ParallelKnapsack(weights, values, W);
            stopwatch.Stop();
            Console.WriteLine($"Best value (multi-threaded): {bestValue}, Time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine("___");
        }
    }
}