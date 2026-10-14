// Local player identity; no campaign or level-session state is stored in this object.
using System;

public sealed class PlayerProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
}
