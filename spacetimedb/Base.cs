using SpacetimeDB;
using System;

public static partial class Module
{
    private static readonly TimeDuration _gametick = TimeDuration.FromMilliseconds(10);

    [SpacetimeDB.Table(Accessor = "GameSettings", Public = true)]
    public partial struct GameSettings
    {
        [SpacetimeDB.PrimaryKey]
        public ulong Id;
        
        [SpacetimeDB.Default(1000f)]
        public float WorldWidth;
        [SpacetimeDB.Default(1000f)]
        public float WorldHeight;
        
        [SpacetimeDB.Default(250f)]

        public float BallSpeed;
        [SpacetimeDB.Default(10f)]
        public float BallRadius;
        [SpacetimeDB.Default(5f)]
        public float BallLifetimeSeconds;
    }

    [SpacetimeDB.Table(Accessor = "GameTick", Public = true, Scheduled = nameof(GameTickUpdate), ScheduledAt = nameof(ScheduledAt))]
    public partial struct GameTick
    {
        [SpacetimeDB.PrimaryKey]
        public ulong Id;
        
        public ScheduleAt ScheduledAt;
    }

    [SpacetimeDB.Reducer(ReducerKind.Init)]
    public static void Init(ReducerContext ctx)
    {
        // Initialize game settings
        ctx.Db.GameSettings.Insert(new GameSettings
        {
            Id = 1,
            WorldWidth = 1000f,
            WorldHeight = 1000f,
            BallSpeed = 250f,
            BallRadius = 10f,
            BallLifetimeSeconds = 5f
        });
        
        // Initialize the game tick loop
        ctx.Db.GameTick.Insert(new GameTick
        {
            Id = 1,
            ScheduledAt = new ScheduleAt.Interval(_gametick)
        });
        
        Log.Info("Game initialized");
    }
}
