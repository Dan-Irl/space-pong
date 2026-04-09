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
            CreatedAt = ctx.Timestamp
        });

        Log.Info($"Ball {ball.Id} spawned by {player.Name}");
    }

    
}