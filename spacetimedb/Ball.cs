using SpacetimeDB;

public static partial class Module
{
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
    public static void SpawnBall(ReducerContext ctx)
    {
        // Find the player
        if (ctx.Db.Player.Id.Find(ctx.Sender) is not Player player)
        {
            throw new InvalidOperationException("Player not in game");
        }

        // Calculate ball velocity based on player's aim angle
        const float speed = 250.0f;
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
            Radius = 10f,
            CreatedAt = ctx.Timestamp,
            ScheduledAt = new ScheduleAt.Interval(_gametick)
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

        var newX = ball.X + ball.VelocityX * _gametick.Microseconds / 1_000_000.0f;
        var newY = ball.Y + ball.VelocityY * _gametick.Microseconds / 1_000_000.0f;

        // Update ball position (interval continues automatically)
        ctx.Db.Ball.Id.Update(ball with 
        { 
            X = newX, 
            Y = newY
        });
    }

    
}