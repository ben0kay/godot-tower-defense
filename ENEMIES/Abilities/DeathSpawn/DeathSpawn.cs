// Kill-only optional component: snapshot a release job, then let the dying scene disappear normally.
using Godot;
using System;

public partial class DeathSpawn : Node
{
    [Export] public Enemy Host { get; set; }
    [Export] public DeathSpawnConfig Config { get; set; }
    [Export] public bool Enabled { get; set; } = true;
    private Enemy _host;
    private SpawnManager _spawns;

    // =========================================================
    // No live ability timer or manager registration; cache the host/service and subscribe only to real hull kills.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false); _host = Host;
        for (Node node = GetParent(); _host == null && node != null; node = node.GetParent()) if (node is Enemy enemy) _host = enemy;
        if (_host == null) { GD.PushWarning("DeathSpawn requires an Enemy host."); return; }
        Config = Config == null ? null : (DeathSpawnConfig)Config.Duplicate(true);
        _spawns = _host.GetNodeOrNull<SpawnManager>("../../EnemySystems/Spawning");
        if (Enabled && (Config?.IsValid != true || _spawns?.Catalog.Find(Config.ChildEnemyId) == null))
            GD.PushWarning("DeathSpawn has invalid settings or unknown child ID: " + Config?.ChildEnemyId);
        _host.KilledBeforeRemoval += OnKilled;
    }
    // =========================================================
    // The queue copies all tuning and route/position obligations before Removed can complete the wave.
    private void OnKilled(Enemy host)
    {
        if (!Enabled || !GodotObject.IsInstanceValid(_spawns)) return;
        try { _spawns.DeathReleases.Enqueue(host, Config); }
        catch (Exception error) { GD.PushWarning("DeathSpawn release rejected; parent death continues: " + error.Message); }
    }
    // =========================================================
    // Arrival/free/unload never enqueue; jobs already captured belong solely to the world queue.
    public override void _ExitTree()
    { if (GodotObject.IsInstanceValid(_host)) _host.KilledBeforeRemoval -= OnKilled; _host = null; _spawns = null; }
}
