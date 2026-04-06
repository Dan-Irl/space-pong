using SpacetimeDB;
using System;

public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Player", Public = true)]
    public partial struct Player
    {
        [SpacetimeDB.PrimaryKey]
        public Identity Id;
        public float X;
        public float Y;
        public string Name;
        public float AimAngle; // Player aim angle in radians (0 to 2π)
        public float PaddleSize;  // Size of the paddle arc
    }

    [SpacetimeDB.Table(Accessor = "Ball", Public = true, Scheduled = nameof(UpdateBall), ScheduledAt = nameof(ScheduledAt))]
    public partial struct Ball
    {
        [SpacetimeDB.PrimaryKey]
        [SpacetimeDB.AutoInc]
        public ulong Id;
        
        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;
        public float Radius;
        public Timestamp CreatedAt;
        public ScheduleAt ScheduledAt;
    }

    [SpacetimeDB.Reducer]
    public static void JoinGame(ReducerContext ctx, string playerName)
    {
        // Check if player already exists
        if (ctx.Db.Player.Id.Find(ctx.Sender) != null)
        {
            throw new InvalidOperationException("Player already in game");
        }

        var randomAngle = (float)(ctx.Rng.NextDouble() * Math.PI * 2);

        // Insert new player
        ctx.Db.Player.Insert(new Player
        {
            Id = ctx.Sender,
            X = MathF.Cos(randomAngle),
            Y = MathF.Sin(randomAngle),
            Name = playerName,
            AimAngle = 0,
            PaddleSize = 0.5f  // Default paddle size
        });

        Log.Info($"Player {playerName} joined at angle {randomAngle}");
    }

    [SpacetimeDB.Reducer]
    public static void LeaveGame(ReducerContext ctx)
    {
        // Check if player exists
        if (ctx.Db.Player.Id.Find(ctx.Sender) == null)
        {
            throw new InvalidOperationException("Player not in game");
        }

        // Remove player from game
        ctx.Db.Player.Id.Delete(ctx.Sender);
        Log.Info($"Player left the game");
    }

    [SpacetimeDB.Reducer]
    public static void MovePlayer(ReducerContext ctx, float newAngle)
    {
        // Find the player
        if (ctx.Db.Player.Id.Find(ctx.Sender) is not Player player)
        {
            throw new InvalidOperationException("Player not in game");
        }

        // Normalize angle to 0-2π range
        var normalizedAngle = (float)(newAngle % (Math.PI * 2));
        if (normalizedAngle < 0)
        {
            normalizedAngle += (float)(Math.PI * 2);
        }

        // Update player angle
        ctx.Db.Player.Id.Update(player with { AimAngle = normalizedAngle });
    }

    [SpacetimeDB.Reducer]
    public static void SpawnBall(ReducerContext ctx)
    {
        // Find the player
        if (ctx.Db.Player.Id.Find(ctx.Sender) is not Player player)
        {
            throw new InvalidOperationException("Player not in game");
        }

        // Calculate ball velocity based on player's aim angle
        const float speed = 2.0f;
        var velocityX = MathF.Cos(player.AimAngle) * speed;
        var velocityY = MathF.Sin(player.AimAngle) * speed;

        // Spawn ball at player position
        var ball = ctx.Db.Ball.Insert(new Ball
        {
            Id = 0,
            X = player.X,
            Y = player.Y,
            VelocityX = velocityX,
            VelocityY = velocityY,
            Radius = 0.1f,
            CreatedAt = ctx.Timestamp,
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(100))
        });

        Log.Info($"Ball {ball.Id} spawned by {player.Name}");
    }

    [SpacetimeDB.Reducer]
    public static void UpdateBall(ReducerContext ctx, Ball ball)
    {
        // Calculate how long the ball has been alive
        TimeDuration lifetime = ctx.Timestamp.TimeDurationSince(ball.CreatedAt);
        
        // Check if ball should despawn (after 5 seconds)
        if (lifetime.Microseconds >= 5_000_000)
        {
            // Delete the ball to stop the interval
            ctx.Db.Ball.Id.Delete(ball.Id);
            Log.Info($"Ball {ball.Id} despawned after {lifetime.Microseconds / 1000000.0:F1} seconds");
            return;
        }

        // Update ball position based on velocity (100ms delta)
        var deltaTime = 0.1f; // 100ms in seconds
        var newX = ball.X + ball.VelocityX * deltaTime;
        var newY = ball.Y + ball.VelocityY * deltaTime;

        // Update ball position (interval continues automatically)
        ctx.Db.Ball.Id.Update(ball with 
        { 
            X = newX, 
            Y = newY
        });
    }
}
