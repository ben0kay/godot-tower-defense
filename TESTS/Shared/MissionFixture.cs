// Actual battlefield/session test fixture; only automatic subsystem ticks are stopped, never mission subscriptions.
using Godot;
using System;
using System.Linq;
using static WaveBuilder;

internal sealed class MissionFixture : IDisposable
{
    internal Node Main { get; }
    internal LevelSessionController Session => Main.GetNode<LevelSessionController>("LevelSession");
    internal LevelResults Results => Main.GetNode<LevelResults>("LevelResults");
    internal PauseMenu Pause => Main.GetNode<PauseMenu>("PauseMenu");
    internal WaveManager Waves => Main.GetNode<WaveManager>("Waves");
    internal CitadelHealth Health => Main.GetNode<CitadelSession>("CitadelSession").Health;
    internal SpawnManager Spawns => Main.GetNode<SpawnManager>("World/EnemySystems/Spawning");
    internal GameplayHUD Hud => Main.GetNode<GameplayHUD>("UI/GameplayHUD");
    internal MissionFixture(Node parent, string path = MenuNavigation.Level001Scene)
    {
        Main = GD.Load<PackedScene>(path).Instantiate(); parent.AddChild(Main); StopTicks(Main);
        Results.StatRevealSeconds = .05f; Results.StatGapSeconds = 0;
    }
    private static void StopTicks(Node node)
    { node.SetProcess(false); node.SetPhysicsProcess(false); foreach (Node child in node.GetChildren()) StopTicks(child); }

    // =========================================================
    // Use genuine WaveManager spawns/ownership, not fabricated result signals or manual outcome setters.
    internal Enemy Start(int count = 1)
    {
        Waves.Configure(new[] { Wave(1, Batch(count, 1, 0, Enemy("basic_mk1"))) }, Spawns.Catalog.ToSceneMap());
        Require(Waves.StartWave(), "Mission fixture starts actual authored scheduler");
        Waves.SetPhysicsProcess(false);
        Enemy enemy = Spawns.Registry.Living.Last(); StopTicks(enemy); return enemy;
    }
    internal static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    internal static Button ButtonFor(Node node, string text)
    {
        if (node is Button button && button.Text == text) return button;
        foreach (Node child in node.GetChildren()) { Button found = ButtonFor(child, text); if (found != null) return found; }
        return null;
    }
    public void Dispose() { if (GodotObject.IsInstanceValid(Main)) Main.Free(); }
}
