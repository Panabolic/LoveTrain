internal sealed class AttackReservation
{
    public bool IsReserved { get; private set; }
    public bool TryReserve()
    {
        if (IsReserved) return false;
        IsReserved = true;
        return true;
    }
    public void Complete() { IsReserved = false; }
    public void Cancel() { IsReserved = false; }
}
