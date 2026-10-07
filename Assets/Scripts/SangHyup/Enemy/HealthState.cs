// Numeric health only. The adapter owns eligibility and the death presentation.
internal sealed class HealthState
{
    public float Maximum { get; set; }
    public float Current { get; set; }
    public bool IsDepleted => Current <= 0f;

    public void Reset(float maximum)
    {
        Maximum = maximum;
        Current = maximum;
    }

    public void ApplyDamage(float amount) { Current -= amount; }

    public static float TimeScaled(float authored, int minutes, float increasePercent, float eventPercent, float multiplier = 1f)
    {
        float rate = increasePercent / 100f;
        float timeScale = 1f + minutes * rate;
        float eventScale = 1f + eventPercent / 100f;
        return authored * timeScale * eventScale * multiplier;
    }
    public static float StageScaled(float fallback, float[] authoredStages, int stageIndex, float eventPercent)
    {
        float authored = authoredStages != null && stageIndex >= 0 && stageIndex < authoredStages.Length && authoredStages[stageIndex] > 0f
            ? authoredStages[stageIndex] : fallback;
        return authored * (1f + eventPercent / 100f);
    }
}
