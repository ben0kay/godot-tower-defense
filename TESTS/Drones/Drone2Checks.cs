// Narrow owner-run DRONE2 checks for shared curves, courier callback gates and exact/legacy motion restoration.
using Godot;
using System;
using System.Text.Json;
using static AmmoLoopFixture;
public partial class Drone2Checks : Node
{
    public override void _Ready()
    {
        try
        {
            var config = new ConstructionDroneMasterConfig();
            var builder = new ConstructionDroneMotion(); var common = new DroneFlightMotion();
            Vector2 end = new(300, 80), start = Vector2.Zero;
            builder.BeginFlight(start, end, Vector2.Up, Vector2.Right, config);
            common.BeginFlight(start, end, Vector2.Up, Vector2.Right, 280, 100);
            Vector2 a = start, b = start;
            builder.Fly(.2f, ref a, config); common.Fly(.2f, ref b);
            Require(a == b && builder.Heading == common.Heading && builder.FlightSeconds == common.FlightSeconds,
                "Builder uses the original 280px/s eased curve through the shared primitive");
            var home = new Node2D(); var destination = new Node2D { Position = end };
            AddChild(home); AddChild(destination);
            int arrived = 0, returned = 0, aborted = 0;
            var scene = GD.Load<PackedScene>("res://DRONES/Transport/CargoDrone.tscn");
            CargoDrone drone = scene.Instantiate<CargoDrone>();
            drone.Initialize(home, destination, 190, () => { arrived++; drone.ReturnHome(); }, () => returned++, () => aborted++);
            AddChild(drone); drone._PhysicsProcess(.2);
            Require(drone.Speed == 190 && drone.GlobalPosition != start && arrived == 0, "Courier retains authored speed and an eased outbound leg");
            DroneFlightMotion saved = drone.CaptureFlight(); Vector2 savedPosition = drone.GlobalPosition;
            string json = JsonSerializer.Serialize(saved);
            var copy = JsonSerializer.Deserialize<DroneFlightMotion>(json); copy.ValidateFlight();
            drone.RestoreFlight(false, savedPosition, copy);
            Require(drone.GlobalPosition == savedPosition && arrived == 0, "Restore retains curve/pose without a cargo arrival replay");
            GetTree().Paused = true; drone._PhysicsProcess(100); GetTree().Paused = false;
            Require(drone.GlobalPosition == savedPosition, "Pause freezes courier and its presentation update");
            drone.GetNode<DroneBody>("Body").Config = new DroneVisualConfig { ThrustersEnabled = false, TrailEnabled = false };
            drone._PhysicsProcess(drone.RemainingFlightSeconds + .01);
            Require(arrived == 1 && returned == 0 && drone.Returning, "Visual opt-out still arrives once and retains the return leg");
            drone.ReturnHome(); // Duplicate return must not restart the curve or emit a second arrival.
            drone._PhysicsProcess(drone.RemainingFlightSeconds + .01);
            drone._PhysicsProcess(100);
            Require(arrived == 1 && returned == 1 && aborted == 0, "Courier reports one arrival and one return only");
            CargoDrone legacy = scene.Instantiate<CargoDrone>();
            legacy.Initialize(home, destination, 190, () => arrived++, () => returned++, () => aborted++); AddChild(legacy);
            legacy.RestoreFlight(true, new(120, 30), null);
            Require(legacy.Returning && legacy.GlobalPosition == new Vector2(120, 30) && arrived == 1, "Legacy checkpoint resumes remaining leg without transferring stock");
            legacy._PhysicsProcess(legacy.RemainingFlightSeconds + .01);
            Require(returned == 2 && arrived == 1, "Legacy return completes normally");
            home.QueueFree(); destination.QueueFree(); config.Dispose();
            GD.Print("PASS: DRONE2 shared curve parity, speed, callbacks, pause, visual opt-out and checkpoint flight seams"); GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
