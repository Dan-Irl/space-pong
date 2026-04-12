using SpacetimeDB;

public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Paddle", Public = true)]
    public partial struct Paddle
    {
        [SpacetimeDB.PrimaryKey]
        public Identity PlayerId; // Links paddle to its parent player

        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;

        // Paddle-specific fields
        public float Angle; // Direction the paddle is facing (radians)
        public float ArcAngle; // Angular width of the paddle arc (radians)

        [SpacetimeDB.Default(0f)]
        public float InnerRadius; // Inner radius of the paddle arc
        [SpacetimeDB.Default(30f)]
        public float OuterRadius; // Outer radius of the paddle arc

        // Helper properties for physics (not serialized by SpacetimeDB)
        public readonly Shape Shape => Shape.CircularArc(InnerRadius, OuterRadius, Angle, ArcAngle);
        public readonly bool IsStatic => false; // Paddles are dynamic (follow players)
    }
}
