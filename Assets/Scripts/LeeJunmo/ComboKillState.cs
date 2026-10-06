using System.Globalization;

// GameManager owns this run-only state and chooses which game states advance its clock.
public sealed class ComboKillState
{
    public const float Duration = 3f;

    public int Count { get; private set; }
    public float RemainingTime => (float)remainingTime;
    public float Fraction => RemainingTime / Duration;
    private double remainingTime;

    public void RegisterKill()
    {
        if (Count < int.MaxValue) Count++;
        remainingTime = Duration;
    }

    public void Advance(double deltaTime)
    {
        if (Count == 0 || deltaTime <= 0d || double.IsNaN(deltaTime) || double.IsInfinity(deltaTime)) return;
        remainingTime -= deltaTime;
        if (remainingTime <= 0d) Reset();
    }

    public void Reset()
    {
        Count = 0;
        remainingTime = 0d;
    }

    public static string FormatLabel(int count)
    {
        string number = count.ToString(CultureInfo.InvariantCulture);
        return "Combo Kill " + number + new string('!', count > 0 ? number.Length - 1 : 0);
    }
}
