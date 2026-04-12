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
        // In Player.cs
        public readonly float PaddleAngle => AimAngle + MathF.PI;


        ///<summary>
        /// Angle that defines the size of the paddle arc
        /// </summary>
        public float PaddleArcAngle;

        [SpacetimeDB.Default(30u)]
        public int PaddleRadius;
        [SpacetimeDB.Default(15f)]
        public float PlayerRadius; // Radius of the player circle
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

        // Insert new player at random position (centered coordinate system: -500 to 500)
        ctx.Db.Player.Insert(new Player
        {
            Id = ctx.Sender,
            X = (ctx.Rng.NextSingle() - 0.5f) * ctx.Db.GameSettings.Iter().FirstOrDefault().WorldWidth, // -500 to 500
            Y = (ctx.Rng.NextSingle() - 0.5f) * ctx.Db.GameSettings.Iter().FirstOrDefault().WorldHeight, // -500 to 500
            Name = playerName,
            AimAngle = randomAngle,
            PaddleArcAngle = MathF.PI / 4, // 45 degree paddle arc
            PaddleRadius = 30
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
}