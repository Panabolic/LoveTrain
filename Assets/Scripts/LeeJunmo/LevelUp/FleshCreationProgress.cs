using System;

// Presentation-only projection of the wallet's increasing, int-capped creation costs.
public readonly struct FleshCreationProgress
{
    public int AffordableCount { get; }
    public int RemainingFlesh { get; }
    public int NextCost { get; }
    public float Fraction => (float)RemainingFlesh / NextCost;

    private FleshCreationProgress(int affordableCount, int remainingFlesh, int nextCost)
    {
        AffordableCount = affordableCount;
        RemainingFlesh = remainingFlesh;
        NextCost = nextCost;
    }

    public static FleshCreationProgress Calculate(int flesh, int creationCost, int costIncrease)
    {
        int budget = Math.Max(0, flesh);
        int cost = Math.Max(1, creationCost);
        int increase = Math.Max(0, costIncrease);
        int lower = 0;
        int upper = budget / cost;

        // Binary search stays bounded even when one flesh buys an item at a constant cost.
        while (lower < upper)
        {
            int middle = (int)(((long)lower + upper + 1L) / 2L);
            if (TotalCost(middle, cost, increase) <= budget) lower = middle;
            else upper = middle - 1;
        }

        int remaining = (int)(budget - TotalCost(lower, cost, increase));
        int nextCost = (int)Math.Min(int.MaxValue, (long)cost + (long)lower * increase);
        return new FleshCreationProgress(lower, remaining, nextCost);
    }

    private static long TotalCost(int count, int cost, int increase)
    {
        long uncappedCount = count;
        if (increase > 0)
            uncappedCount = Math.Min(uncappedCount, ((long)int.MaxValue - cost) / increase + 1L);

        // Only the uncapped prefix forms an arithmetic progression; all later costs stay at int.MaxValue.
        return uncappedCount * cost
            + (long)increase * uncappedCount * (uncappedCount - 1L) / 2L
            + ((long)count - uncappedCount) * int.MaxValue;
    }
}
