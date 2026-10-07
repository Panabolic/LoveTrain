internal sealed class DeathLifecycle
{
    public bool IsAlive { get; set; } = true;
    public bool RewardGranted { get; set; }
    public bool PresentationActive { get; set; }

    public void Reset()
    {
        IsAlive = true;
        RewardGranted = false;
        PresentationActive = false;
    }

    public bool TryBegin()
    {
        if (!IsAlive) return false;
        IsAlive = false;
        PresentationActive = true;
        return true;
    }

    public bool TryGrantReward()
    {
        if (RewardGranted) return false;
        RewardGranted = true;
        IsAlive = false;
        return true;
    }

    public void CompletePresentation() { PresentationActive = false; }
}
