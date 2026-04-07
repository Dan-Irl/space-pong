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
        public float PaddleSize;  // Size of the paddle arc
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
}