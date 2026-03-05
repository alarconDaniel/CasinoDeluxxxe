using System;
using System.Linq;

public enum YahtzeeCategory
{
    Ones, Twos, Threes, Fours, Fives, Sixes,
    ThreeOfKind, FourOfKind, FullHouse,
    SmallStraight, LargeStraight, Yahtzee, Chance
}

public class YahtzeeScoreCard
{
    // 13 categorías
    public int?[] scores = new int?[13];

    public bool IsUsed(YahtzeeCategory c) => scores[(int)c].HasValue;

    public void Set(YahtzeeCategory c, int value) => scores[(int)c] = value;

    public int UpperSum()
    {
        int sum = 0;
        for (int i = 0; i <= 5; i++) sum += scores[i] ?? 0;
        return sum;
    }

    public int UpperBonus() => UpperSum() >= 63 ? 35 : 0;

    public int Total()
    {
        int sum = scores.Sum(s => s ?? 0);
        return sum + UpperBonus();
    }
}

public static class YahtzeeScoring
{
    public static int Score(YahtzeeCategory cat, int[] dice)
    {
        Array.Sort(dice);
        int sum = dice.Sum();

        int[] counts = new int[7];
        foreach (int d in dice) counts[d]++;

        bool has3 = counts.Any(c => c >= 3);
        bool has4 = counts.Any(c => c >= 4);
        bool has5 = counts.Any(c => c >= 5);

        bool fullHouse = counts.Any(c => c == 3) && counts.Any(c => c == 2);

        bool smallStraight = HasStraight(dice, 4);
        bool largeStraight = HasStraight(dice, 5);

        switch (cat)
        {
            case YahtzeeCategory.Ones: return counts[1] * 1;
            case YahtzeeCategory.Twos: return counts[2] * 2;
            case YahtzeeCategory.Threes: return counts[3] * 3;
            case YahtzeeCategory.Fours: return counts[4] * 4;
            case YahtzeeCategory.Fives: return counts[5] * 5;
            case YahtzeeCategory.Sixes: return counts[6] * 6;

            case YahtzeeCategory.ThreeOfKind: return has3 ? sum : 0;
            case YahtzeeCategory.FourOfKind: return has4 ? sum : 0;
            case YahtzeeCategory.FullHouse: return fullHouse ? 25 : 0;
            case YahtzeeCategory.SmallStraight: return smallStraight ? 30 : 0;
            case YahtzeeCategory.LargeStraight: return largeStraight ? 40 : 0;
            case YahtzeeCategory.Yahtzee: return has5 ? 50 : 0;
            case YahtzeeCategory.Chance: return sum;
        }

        return 0;
    }

    public static int MaxPossible(YahtzeeCategory cat)
    {
        return cat switch
        {
            YahtzeeCategory.Ones => 5,
            YahtzeeCategory.Twos => 10,
            YahtzeeCategory.Threes => 15,
            YahtzeeCategory.Fours => 20,
            YahtzeeCategory.Fives => 25,
            YahtzeeCategory.Sixes => 30,

            YahtzeeCategory.ThreeOfKind => 30,
            YahtzeeCategory.FourOfKind => 30,
            YahtzeeCategory.FullHouse => 25,
            YahtzeeCategory.SmallStraight => 30,
            YahtzeeCategory.LargeStraight => 40,
            YahtzeeCategory.Yahtzee => 50,
            YahtzeeCategory.Chance => 30,
            _ => 0
        };
    }

    static bool HasStraight(int[] diceSorted, int len)
    {
        // Unique
        var u = diceSorted.Distinct().ToArray();
        int best = 1, run = 1;

        for (int i = 1; i < u.Length; i++)
        {
            if (u[i] == u[i - 1] + 1) run++;
            else run = 1;

            if (run > best) best = run;
        }

        return best >= len;
    }

    public static string Pretty(YahtzeeCategory c)
    {
        return c switch
        {
            YahtzeeCategory.Ones => "Ones (1)",
            YahtzeeCategory.Twos => "Twos (2)",
            YahtzeeCategory.Threes => "Threes (3)",
            YahtzeeCategory.Fours => "Fours (4)",
            YahtzeeCategory.Fives => "Fives (5)",
            YahtzeeCategory.Sixes => "Sixes (6)",
            YahtzeeCategory.ThreeOfKind => "3 of a Kind",
            YahtzeeCategory.FourOfKind => "4 of a Kind",
            YahtzeeCategory.FullHouse => "Full House",
            YahtzeeCategory.SmallStraight => "Small Straight",
            YahtzeeCategory.LargeStraight => "Large Straight",
            YahtzeeCategory.Yahtzee => "YAHTZEE",
            YahtzeeCategory.Chance => "Chance",
            _ => c.ToString()
        };
    }
}