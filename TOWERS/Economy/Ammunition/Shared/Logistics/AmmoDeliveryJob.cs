// One physical shipment: source stock has already moved into this cargo; incoming space stays reserved until arrival/cancel.
public sealed class AmmoDeliveryJob
{
    public AmmoBuilding Source { get; internal set; }
    public TowerBase Destination { get; internal set; }
    public AmmoInventory Cargo { get; internal set; }
    public TransportDrone Drone { get; internal set; }
    internal AmmoInventory DestinationInventory;
    internal int Reserved;
    internal bool Finished;
}
