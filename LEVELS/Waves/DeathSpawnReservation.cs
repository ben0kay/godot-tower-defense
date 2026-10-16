// A finite wave release reservation; attach before retiring so no transient empty wave can complete.
public sealed class DeathSpawnReservation
{
    internal WaveManager Owner { get; }
    internal int Epoch { get; }
    public int Remaining { get; internal set; }
    public bool Active => Godot.GodotObject.IsInstanceValid(Owner) && Owner.IsInsideTree() && Owner.ReservationActive(this);
    internal DeathSpawnReservation(WaveManager owner, int epoch, int count) { Owner = owner; Epoch = epoch; Remaining = count; }
    internal void Attach(Enemy child) { if (Active) Owner.AttachDeathChild(this, child); }
    internal void Discard(Enemy child) { if (Active) Owner.DiscardDeathChild(child); }
    internal void Retire() { if (Active) Owner.RetireDeathChild(this); }
    internal void Cancel() { while (Active) Retire(); }
}
