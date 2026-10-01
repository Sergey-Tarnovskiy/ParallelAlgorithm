using System.Diagnostics;
using System.Threading;

class Program
{
    // Giperparameters for the genetic algorithm
    static int minWeight = 1;
    static int maxWeight = 10;
    static int minW = 40;
    static int maxW = 60;
    static int minValue  = 1;
    static int maxValue  = 10;
    static int countItem = 20;
    static int populationSize = 100;
    static int tournamentSize = 3;
    static double mutationRate = 0.5;
    static int generationCount = 5000;
    static int islandCount = 4;
    static bool debugInfo = false;
    static int migrationInterval = generationCount/100;
    static int migrationSize = populationSize / 10;

    // Structure to represent an item with weight and value
    struct Item
    {
        public int weight;
        public int value;
    }

    // Structure to represent a knapsack task with maximum weight and a list of items
    struct KnapsackTask
    {
        public int W;
        public List<Item> items;

        public static KnapsackTask CreateKnapsackRandom(Random seed)
        {
            KnapsackTask knapsack = new KnapsackTask
            {
                W = seed.Next( minW, maxW + 1 ),
                items = new List<Item>()
            };

            for (int i = 0; i < countItem; i++)
            {
                Item item = new Item
                {
                    weight = seed.Next(minWeight, maxWeight + 1),
                    value  = seed.Next(minValue,  maxValue  + 1)
                };
                knapsack.items.Add(item);
            }

            return knapsack;
        }
    }

    struct Individual
    {
        public List<bool> genes;
        public int sumWeight; // Total sum weight for cache
        public int sumValue;  // Total sum value for cache
        public int fitness;   // Validation of the individ

        public static Individual CreateIndividRandom(KnapsackTask task, Random seed)
        {
            Individual individual = new Individual
            {
                genes = new List<bool>()
            };
            for (int i = 0; i < task.items.Count; i++)
            {
                individual.genes.Add(seed.Next(2) == 1);
            }

            Evaluate( ref individual, task );

            return individual;
        }

        public static void Evaluate(ref Individual individual, KnapsackTask task)
        {
            individual.sumWeight = 0;
            individual.sumValue = 0;
            for (int i = 0; i < individual.genes.Count; i++)
            {
                if (individual.genes[i])
                {
                    individual.sumWeight += task.items[i].weight;
                    individual.sumValue += task.items[i].value;
                }
            }
            individual.fitness = individual.sumWeight <= task.W ? individual.sumValue : 0;
        }
    }

    struct Population
    {
        public List<Individual> individuals;
        public static Population CreatePopulationRandom(KnapsackTask task, Random seed)
        {
            Population population = new Population
            {
                individuals = new List<Individual>()
            };
            for (int i = 0; i < populationSize; i++)
            {
                population.individuals.Add(Individual.CreateIndividRandom(task, seed));
            }
            return population;
        }
    }

    // Get the best individual from the population based on fitness
    static Individual GetBestIndividual(Population population)
    {
        Individual bestIndividual = population.individuals[0];
        foreach (var individual in population.individuals)
        {
            if (individual.fitness > bestIndividual.fitness)
            {
                bestIndividual = individual;
            }
        }
        return bestIndividual;
    }

    static Individual Selection(Population population, Random seed)
    {
        Individual bestIndivid = population.individuals[seed.Next(population.individuals.Count)];

        for( int i = 1; i < tournamentSize; i++)
        {
            Individual individual = population.individuals[seed.Next(population.individuals.Count)];
            if (individual.fitness > bestIndivid.fitness)
            {
                bestIndivid = individual;
            }
        }

        return bestIndivid;
    }

    static Individual Crossover(Individual parent1, Individual parent2, KnapsackTask task, Random seed)
    {
        Individual child = new Individual
        {
            genes = new List<bool>()
        };

        int point = seed.Next( 1, parent1.genes.Count );

        for(int i = 0; i < parent1.genes.Count; i++)
        {
            if (i < point)
            {
                child.genes.Add(parent1.genes[i]);
            }
            else
            {
                child.genes.Add(parent2.genes[i]);
            }
        }

        Individual.Evaluate(ref child, task);

        return child;
    }

    static Individual Mutate(Individual individual, KnapsackTask task, Random seed)
    {
        for (int i = 0; i < individual.genes.Count; i++)
        {
            if (seed.NextDouble() < mutationRate)
            {
                individual.genes[i] = !individual.genes[i];
            }
        }

        Individual.Evaluate(ref individual, task);

        return individual;
    }

    static Population CreateNewPopulation(Population population, KnapsackTask task, int threadCount)
    {
        List<Individual> newIndividuals = new List<Individual>(new Individual[population.individuals.Count]);

        ParallelOptions options = new ParallelOptions
        {
            MaxDegreeOfParallelism = threadCount
        };

        Parallel.For(0, population.individuals.Count, options, i =>
        {
            Random seedPopulation = new Random( i + threadCount);

            Individual parent1 = Selection(population, seedPopulation);
            Individual parent2 = Selection(population, seedPopulation);

            Individual child = Crossover(parent1, parent2, task, seedPopulation);

            child = Mutate(child, task, seedPopulation);

            newIndividuals[i] = child;
        });

        Population newPopulation = new Population
        {
            individuals = newIndividuals
        };

        return newPopulation;
    }

    static void Migration(List<Population> islands)
    {
        List<List<Individual>> migrants = new List<List<Individual>>();

        for (int island = 0; island < islandCount; island++)
        {
            List<Individual> bestIndividuals = new List<Individual>(islands[island].individuals);

            bestIndividuals.Sort( (a, b) => b.fitness.CompareTo(a.fitness) );

            bestIndividuals = bestIndividuals.GetRange(0, migrationSize);

            migrants.Add(bestIndividuals);
        }

        for (int island = 0; island < islandCount; island++)
        {
            int ind = (island + 1) % islandCount;

            islands[ind].individuals.Sort( (a, b) => a.fitness.CompareTo(b.fitness) );

            for (int i = 0; i < migrationSize; i++)
            {
                islands[ind].individuals[i] = migrants[island][i];
            }
        }
    }

    static void RunGeneticAlg(KnapsackTask task, Random seed, int threadCount, bool useMigration)
    {
        List<Population> islands = new List<Population>();

        //islandCount = threadCount; // TODO: Replace it
        
        for (int island = 0; island < islandCount; island++)
        {
            islands.Add( Population.CreatePopulationRandom(task, seed) );
        }

        Individual bestIndividual = GetBestIndividual(islands[0]);
        
        object bestLock = new object();

        Barrier barrier = new Barrier(islandCount);

        ParallelOptions options = new ParallelOptions
        {
            MaxDegreeOfParallelism = islandCount
        };


        for (int generation = 0; generation < generationCount; generation++)
        {

            //for( int island = 0; island < islandCount; island++ )
            //{
            Parallel.For(0, islandCount, options, island =>
            {
                islands[island] = CreateNewPopulation(islands[island], task, threadCount);
                Individual bestIslandIndividual = GetBestIndividual(islands[island]);

                lock (bestLock)
                {
                    if (bestIslandIndividual.fitness > bestIndividual.fitness)
                    {
                        bestIndividual = bestIslandIndividual;
                    }
                }

                barrier.SignalAndWait();
                if ( island == 0 && useMigration && (generation + 1) % migrationInterval == 0)
                {
                    Migration(islands);
                }
                barrier.SignalAndWait();
            });

            if (debugInfo)
            { 
                Console.WriteLine();
                Console.WriteLine($"Generation {generation + 1}: Best Fitness = {bestIndividual.fitness}, Weight = {bestIndividual.sumWeight}, Value = {bestIndividual.sumValue}");
                Console.WriteLine("__________________________________________________________");
                Console.WriteLine($"Mask: {string.Join("", bestIndividual.genes.Select( b => b ? "1" : "0"))}");
                Console.WriteLine("__________________________________________________________");
                Console.WriteLine();
            }
        }

        Console.WriteLine("__________________________________________________________");
        Console.WriteLine($"Best Individual: Fitness = {bestIndividual.fitness}, Weight = {bestIndividual.sumWeight}, Value = {bestIndividual.sumValue}");
        Console.WriteLine("__________________________________________________________");

    }

    static int FindOptimalKnapsack( KnapsackTask task )
    {
        int n = task.items.Count;
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
                    weightSum += task.items[i].weight;
                    valueSum += task.items[i].value;
                }
            }
            if ( weightSum <= task.W && valueSum > bestValue)
            {
                bestValue = valueSum;
            }
        }
        return bestValue;
    }


    static void Main()
    {
        Console.WriteLine("Genetic Algorithm for Knapsack Problem");
        Random seed = new Random();
        KnapsackTask task = KnapsackTask.CreateKnapsackRandom(seed);
        Console.WriteLine($"Knapsack Weight Limit: {task.W}");
        Console.WriteLine("Items:");

        for (int i = 0; i < countItem; i++)
        {
            Console.WriteLine($"Item {i + 1}: Weight = {task.items[i].weight}, Value = {task.items[i].value}");
        }
        bool useMigration = true;
        Stopwatch stopwatch1 = new Stopwatch();
        stopwatch1.Start();
        RunGeneticAlg(task, seed, 1, useMigration);
        stopwatch1.Stop();

        Stopwatch stopwatch2 = new Stopwatch();
        stopwatch2.Start();
        RunGeneticAlg(task, seed, 16, useMigration);
        stopwatch2.Stop();

        Stopwatch stopwatch3 = new Stopwatch();
        stopwatch3.Start();
        RunGeneticAlg(task, seed, 32, useMigration);
        stopwatch3.Stop();

        Console.WriteLine("__________________________________________________________");
        Console.WriteLine($"Time taken 1 thread: {stopwatch1.ElapsedMilliseconds} ms");
        Console.WriteLine($"Time taken 16 threads: {stopwatch2.ElapsedMilliseconds} ms");
        Console.WriteLine($"Time taken 32 threads: {stopwatch3.ElapsedMilliseconds} ms");


        Console.WriteLine("Optimal:");
        Console.WriteLine($"Best Value = {FindOptimalKnapsack(task)}");
        Console.WriteLine("__________________________________________________________");
    }
}