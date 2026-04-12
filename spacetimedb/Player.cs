using SpacetimeDB;

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
        public readonly float PaddleAngle => AimAngle + MathF.PI; // Convenience property for paddle angle

        public float VelocityX;
        public float VelocityY;

        [SpacetimeDB.Default(15f)]
        public float PlayerRadius; // Radius of the player circle

        // Helper properties for physics (not serialized by SpacetimeDB)
        public readonly Shape Shape => Shape.Circle(PlayerRadius);
        public readonly bool IsStatic => false; // Players are dynamic (can move)
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
        var startX = (ctx.Rng.NextSingle() - 0.5f) * ctx.Db.GameSettings.Iter().FirstOrDefault().WorldWidth;
        var startY = (ctx.Rng.NextSingle() - 0.5f) * ctx.Db.GameSettings.Iter().FirstOrDefault().WorldHeight;

        // Insert new player at random position (centered coordinate system: -500 to 500)
        ctx.Db.Player.Insert(new Player
        {
            Id = ctx.Sender,
            X = startX,
            Y = startY,
            Name = playerName,
            AimAngle = randomAngle,
            VelocityX = 0,
            VelocityY = 0
        });

        // Create paddle for the player
        ctx.Db.Paddle.Insert(new Paddle
        {
            PlayerId = ctx.Sender,
            X = startX,
            Y = startY,
            VelocityX = 0,
            VelocityY = 0,
            Angle = randomAngle + MathF.PI, // Paddle faces opposite of aim
            ArcAngle = MathF.PI / 4, // 45 degree paddle arc
            InnerRadius = 0,
            OuterRadius = 30
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

        // Remove player's paddle first
        if (ctx.Db.Paddle.PlayerId.Find(ctx.Sender) is Paddle paddle)
        {
            ctx.Db.Paddle.PlayerId.Delete(ctx.Sender);
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
}