using SpacetimeDB;

public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Ball", Public = true)]
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

        // Helper property for physics (not serialized by SpacetimeDB)
        public readonly Shape Shape => Shape.Circle(Radius);
        public readonly bool IsStatic => false;
    }


    [SpacetimeDB.Reducer]
    public static void SpawnBall(ReducerContext ctx)
    {
        // Find the player
        if (ctx.Db.Player.Id.Find(ctx.Sender) is not Player player)
        {
            throw new InvalidOperationException("Player not in game");
        }

        // Find the player's paddle
        if (ctx.Db.Paddle.PlayerId.Find(ctx.Sender) is not Paddle paddle)
        {
            throw new InvalidOperationException("Player has no paddle");
        }

        // Calculate ball velocity based on player's aim angle
        const float speed = 250.0f;
        var velocityX = MathF.Cos(player.AimAngle) * speed;
        var velocityY = MathF.Sin(player.AimAngle) * speed;

        var gameSettings = ctx.Db.GameSettings.Iter().FirstOrDefault();

        // Spawn ball at edge of paddle
        var spawnDistance = paddle.OuterRadius + gameSettings.BallRadius;
        var ball = ctx.Db.Ball.Insert(new Ball
        {
            Id = 0,
            X = player.X + MathF.Cos(player.AimAngle) * spawnDistance,
            Y = player.Y + MathF.Sin(player.AimAngle) * spawnDistance,
            VelocityX = velocityX,
            VelocityY = velocityY,
            Radius = gameSettings.BallRadius,
            CreatedAt = ctx.Timestamp
        });

        Log.Info($"Ball {ball.Id} spawned by {player.Name}");
    }


}