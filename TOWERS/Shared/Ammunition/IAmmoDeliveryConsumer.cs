// Narrow stock endpoint for non-weapon buildings served by the existing Depot courier dispatcher.
public interface IAmmoDeliveryConsumer
{
    AmmoInventory DeliveryInventory { get; }
    bool CanReceiveAmmo { get; }
    void SetAmmoDeliveryStatus(bool requested, bool inFlight);
}
