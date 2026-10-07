internal sealed class FuelState
{
    public float Current { get; private set; }
    public void Reset(float maximum) { Current = maximum; }
    public bool Modify(float amount, float maximum)
    {
        // Match Mathf.Clamp, including its comparison order, without a Unity dependency.
        float value = Current + amount;
        if (value < 0f) value = 0f;
        else if (value > maximum) value = maximum;
        Current = value;
        return Current <= 0f;
    }
}
