// Optional typed policy for aimed weapons only; private config copies keep choices per placed instance.
public interface ITowerTargetingPolicy
{
    TowerTargeting.Priority TargetPriority { get; set; }
}
