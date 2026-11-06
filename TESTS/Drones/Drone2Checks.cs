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
            FixedFlightRates();
            var config = new ConstructionDroneMasterConfig();
            var builder = new ConstructionDroneMotion(); var common = new DroneFlightMotion();
            Vector2 end = new(300, 80), start = Vector2.Zero;
            builder.BeginFlight(start, end, Vector2.Up, Vector2.Right, config);
            common.BeginFlight(start, end, Vector2.Up, Vector2.Right, 280, 100);
            Vector2 a = start, b = start;
            builder.Fly(.2f, ref a, config); common.Fly(.2f, ref b);
            Require(a == b && builder.Heading == common.Heading && builder.FlightSeconds == common.FlightSeconds,
                "Builder uses the 280px/s fixed-rate curve through the shared primitive");
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
    // =========================================================
    // Direct primitive checks: trip length changes cruise time, never acceleration/braking rates.
    private static void FixedFlightRates()
    {
        var near = new DroneFlightMotion(); var far = new DroneFlightMotion();
        near.BeginFlight(Vector2.Zero, new(1000, 0), Vector2.Right, Vector2.Right, 280);
        far.BeginFlight(Vector2.Zero, new(10000, 0), Vector2.Right, Vector2.Right, 280);
        Vector2 a = Vector2.Zero, b = Vector2.Zero;
        near.Fly(.2f, ref a); far.Fly(.2f, ref b);
        Require(Mathf.Abs(a.X - 16) < .1f && Mathf.Abs(b.X - 16) < .1f,
            "Short and long trips both accelerate at 800px/s squared");
        near.FlightElapsed = near.FlightSeconds - .21f; far.FlightElapsed = far.FlightSeconds - .21f;
        near.Fly(.01f, ref a); far.Fly(.01f, ref b);
        Require(Mathf.Abs(1000 - a.X - 16) < .1f && Mathf.Abs(10000 - b.X - 16) < .1f,
            "Braking distance at the same time before arrival is independent of total trip length");
        var tiny = new DroneFlightMotion();
        tiny.BeginFlight(Vector2.Zero, new(20, 0), Vector2.Right, Vector2.Right, 280);
        Require(tiny.FlightCruiseSpeed < 280 && tiny.FlightAcceleration == 800 && tiny.FlightDeceleration == 800,
            "Very short trips lower peak speed without stretching acceleration");
        a = Vector2.Zero; Require(tiny.Fly(100, ref a) && a == new Vector2(20, 0), "Large ticks arrive exactly without overshoot");
        var original = new DroneFlightMotion();
        original.BeginFlight(Vector2.Zero, new(1000, 100), Vector2.Up, Vector2.Right, 280);
        a = Vector2.Zero; original.Fly(.2f, ref a);
        var restored = JsonSerializer.Deserialize<DroneFlightMotion>(JsonSerializer.Serialize(original));
        restored.ValidateFlight(); b = a; original.Fly(.2f, ref a); restored.Fly(.2f, ref b);
        Require(a == b, "New speed profiles resume exactly after JSON round-trip and distance-cache rebuild");
        var legacy = new DroneFlightMotion { Start = new(0, 0), Control1 = new(25, 0),
            Control2 = new(75, 0), End = new(100, 0), FlightSeconds = 1 };
        legacy.ValidateFlight(); a = Vector2.Zero; legacy.Fly(.5f, ref a);
        Require(a == new Vector2(50, 0), "Legacy flat snapshots preserve their active smoothstep leg");
    }

}
