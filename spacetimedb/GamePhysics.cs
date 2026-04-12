using System;
using SpacetimeDB;

public static partial class Module
{
    /// <summary>
    /// Represents a geometric shape for collision detection
    /// </summary>
    public readonly struct Shape
    {
        public enum ShapeType { Circle, CircularArc }

        public ShapeType Type { get; init; }

        // Circle properties
        public float Radius { get; init; }

        // CircularArc properties
        public float InnerRadius { get; init; }
        public float OuterRadius { get; init; }
        public float Angle { get; init; } // Center angle of the arc in radians
        public float ArcAngle { get; init; } // Angular width of the arc in radians

        public static Shape Circle(float radius) => new Shape
        {
            Type = ShapeType.Circle,
            Radius = radius
        };

        public static Shape CircularArc(float innerRadius, float outerRadius, float angle, float arcAngle) => new Shape
        {
            Type = ShapeType.CircularArc,
            InnerRadius = innerRadius,
            OuterRadius = outerRadius,
            Angle = angle,
            ArcAngle = arcAngle
        };
    }

    /// <summary>
    /// Records a collision event between two physics objects.
    /// Generic to support any types (no interface constraint due to SpacetimeDB limitations).
    /// </summary>
    public readonly struct CollisionEvent<TA, TB>
    {
        public TA ObjectA { get; init; }
        public TB ObjectB { get; init; }
    }

    /// <summary>
    /// Pure physics math functions for collision detection
    /// </summary>
    public static class GamePhysics
    {
        /// <summary>
        /// Pure geometry check: does circle A overlap circle B?
        /// </summary>
        public static bool CheckCircleOverlap(
            float aX, float aY, float aRadius,
            float bX, float bY, float bRadius)
        {
            float dx = bX - aX;
            float dy = bY - aY;
            float distanceSquared = dx * dx + dy * dy;
            float combinedRadius = aRadius + bRadius;

            return distanceSquared <= combinedRadius * combinedRadius;
        }

        /// <summary>
        /// Pure geometry check: does a circle overlap with a circular arc?
        /// </summary>
        public static bool CheckCircleArcOverlap(
            float circleX, float circleY, float circleRadius,
            float arcX, float arcY, float arcInnerRadius, float arcOuterRadius, float arcAngle, float arcArcAngle)
        {
            // Calculate distance between circle center and arc center
            float dx = circleX - arcX;
            float dy = circleY - arcY;
            float distanceSquared = dx * dx + dy * dy;

            // Check if circle is within arc radius range
            float outerRadius = arcOuterRadius + circleRadius;
            float innerRadius = MathF.Max(0, arcInnerRadius - circleRadius);

            if (distanceSquared > outerRadius * outerRadius || distanceSquared < innerRadius * innerRadius)
            {
                return false; // Not in arc radius range
            }

            // Calculate angle from arc center to circle
            float angleToCircle = MathF.Atan2(dy, dx);

            // Normalize angle to 0-2π range
            if (angleToCircle < 0)
            {
                angleToCircle += MathF.PI * 2;
            }

            // Check if circle angle is within arc
            float arcStart = arcAngle - arcArcAngle / 2;
            float arcEnd = arcAngle + arcArcAngle / 2;

            // Normalize arc angles
            arcStart = NormalizeAngle(arcStart);
            arcEnd = NormalizeAngle(arcEnd);

            // Check if angle is within arc
            return IsAngleInArc(angleToCircle, arcStart, arcEnd);
        }

        /// <summary>
        /// Universal shape overlap check
        /// </summary>
        public static bool CheckShapeOverlap(
            Shape shapeA, float aX, float aY,
            Shape shapeB, float bX, float bY)
        {
            // Circle vs Circle
            if (shapeA.Type == Shape.ShapeType.Circle && shapeB.Type == Shape.ShapeType.Circle)
            {
                return CheckCircleOverlap(aX, aY, shapeA.Radius, bX, bY, shapeB.Radius);
            }

            // Circle vs CircularArc (order matters for parameters)
            if (shapeA.Type == Shape.ShapeType.Circle && shapeB.Type == Shape.ShapeType.CircularArc)
            {
                return CheckCircleArcOverlap(
                    aX, aY, shapeA.Radius,
                    bX, bY, shapeB.InnerRadius, shapeB.OuterRadius, shapeB.Angle, shapeB.ArcAngle
                );
            }

            // CircularArc vs Circle (swap order)
            if (shapeA.Type == Shape.ShapeType.CircularArc && shapeB.Type == Shape.ShapeType.Circle)
            {
                return CheckCircleArcOverlap(
                    bX, bY, shapeB.Radius,
                    aX, aY, shapeA.InnerRadius, shapeA.OuterRadius, shapeA.Angle, shapeA.ArcAngle
                );
            }

            // CircularArc vs CircularArc (not implemented, probably won't need it)
            // For now, return false
            return false;
        }

        /// <summary>
        /// Check if a ball (circle) collides with a player's paddle (arc on a circle)
        /// </summary>
        public static bool CheckBallPaddleCollision(
            float ballX, float ballY, float ballRadius,
            float playerX, float playerY, float playerRadius, float paddleRadius,
            float aimAngle, float paddleArcAngle)
        {
            // Calculate distance between ball center and player center
            float dx = ballX - playerX;
            float dy = ballY - playerY;
            float distanceSquared = dx * dx + dy * dy;

            // Check if ball is within paddle radius range
            float outerRadius = paddleRadius + ballRadius;
            float innerRadius = MathF.Max(0, paddleRadius - ballRadius);

            if (distanceSquared > outerRadius * outerRadius || distanceSquared < innerRadius * innerRadius)
            {
                return false; // Not in paddle radius range
            }

            // Calculate angle from player to ball
            float angleToBall = MathF.Atan2(dy, dx);

            // Normalize angle to 0-2π range
            if (angleToBall < 0)
            {
                angleToBall += MathF.PI * 2;
            }

            // Check if ball angle is within paddle arc
            float paddleStart = aimAngle - paddleArcAngle / 2;
            float paddleEnd = aimAngle + paddleArcAngle / 2;

            // Normalize paddle angles
            paddleStart = NormalizeAngle(paddleStart);
            paddleEnd = NormalizeAngle(paddleEnd);

            // Check if angle is within paddle arc
            return IsAngleInArc(angleToBall, paddleStart, paddleEnd);
        }

        /// <summary>
        /// Calculate reflected velocity when ball hits paddle.
        /// Returns null if ball is moving away from paddle (prevents stuck balls).
        /// </summary>
        public static (float newVx, float newVy)? ReflectVelocity(
            float ballVx, float ballVy,
            float ballX, float ballY,
            float playerX, float playerY)
        {
            // Calculate normal vector (from player to ball)
            float dx = ballX - playerX;
            float dy = ballY - playerY;
            float length = MathF.Sqrt(dx * dx + dy * dy);

            if (length < 0.0001f)
            {
                // Avoid division by zero - reflect back in opposite direction
                return (-ballVx, -ballVy);
            }

            float normalX = dx / length;
            float normalY = dy / length;

            // Calculate dot product of velocity and normal
            float dotProduct = ballVx * normalX + ballVy * normalY;

            // Only reflect if ball is moving TOWARDS the paddle (dot product < 0)
            // This prevents the ball from getting stuck by repeated reflections
            if (dotProduct >= 0)
            {
                return null; // Ball moving away from paddle, no reflection needed
            }

            // Reflect velocity: v' = v - 2(v·n)n
            float newVx = ballVx - 2 * dotProduct * normalX;
            float newVy = ballVy - 2 * dotProduct * normalY;

            return (newVx, newVy);
        }

        /// <summary>
        /// Check and handle collision with world borders.
        /// Reflects velocity when hitting borders and clamps position within bounds.
        /// </summary>
        /// <param name="x">Current X position</param>
        /// <param name="y">Current Y position</param>
        /// <param name="vx">Current X velocity</param>
        /// <param name="vy">Current Y velocity</param>
        /// <param name="radius">Object radius</param>
        /// <param name="worldWidth">World width (centered at 0)</param>
        /// <param name="worldHeight">World height (centered at 0)</param>
        /// <returns>Corrected position and velocity after border collision</returns>
        public static (float newX, float newY, float newVx, float newVy) CheckWorldBorderCollision(
            float x, float y, float vx, float vy, float radius,
            float worldWidth, float worldHeight)
        {
            float newX = x;
            float newY = y;
            float newVx = vx;
            float newVy = vy;

            // Calculate world boundaries (centered at origin)
            float halfWidth = worldWidth / 2f;
            float halfHeight = worldHeight / 2f;

            // Check left/right borders
            if (x - radius < -halfWidth)
            {
                newX = -halfWidth + radius; // Clamp position
                newVx = MathF.Abs(vx); // Reflect velocity (make positive)
            }
            else if (x + radius > halfWidth)
            {
                newX = halfWidth - radius; // Clamp position
                newVx = -MathF.Abs(vx); // Reflect velocity (make negative)
            }

            // Check top/bottom borders
            if (y - radius < -halfHeight)
            {
                newY = -halfHeight + radius; // Clamp position
                newVy = MathF.Abs(vy); // Reflect velocity (make positive)
            }
            else if (y + radius > halfHeight)
            {
                newY = halfHeight - radius; // Clamp position
                newVy = -MathF.Abs(vy); // Reflect velocity (make negative)
            }

            return (newX, newY, newVx, newVy);
        }

        /// <summary>
        /// Normalize angle in radians to 0-2π range
        /// </summary>
        private static float NormalizeAngle(float angle)
        {
            angle = angle % (MathF.PI * 2);
            if (angle < 0)
            {
                angle += MathF.PI * 2;
            }
            return angle;
        }

        /// <summary>
        /// Check if an angle is within an arc (handles wrapping around 0/2π)
        /// </summary>
        private static bool IsAngleInArc(float angle, float start, float end)
        {
            if (start <= end)
            {
                return angle >= start && angle <= end;
            }
            else
            {
                // Arc wraps around 0/2π
                return angle >= start || angle <= end;
            }
        }
    }
}
