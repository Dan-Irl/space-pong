using System;

public static partial class Module
{
    /// <summary>
    /// Pure physics math functions for collision detection
    /// </summary>
    public static class GamePhysics
    {
        /// <summary>
        /// Check if a ball (circle) collides with a player's paddle (arc on a circle)
        /// </summary>
        public static bool CheckBallPaddleCollision(
            float ballX, float ballY, float ballRadius,
            float playerX, float playerY, float playerRadius, float paddleRadius,
            float aimAngle, float paddleSize)
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
            float paddleStart = aimAngle - paddleSize / 2;
            float paddleEnd = aimAngle + paddleSize / 2;
            
            // Normalize paddle angles
            paddleStart = NormalizeAngle(paddleStart);
            paddleEnd = NormalizeAngle(paddleEnd);
            
            // Check if angle is within paddle arc
            return IsAngleInArc(angleToBall, paddleStart, paddleEnd);
        }

        /// <summary>
        /// Calculate reflected velocity when ball hits paddle
        /// </summary>
        public static (float newVx, float newVy) ReflectVelocity(
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
            
            // Reflect velocity: v' = v - 2(v·n)n
            float newVx = ballVx - 2 * dotProduct * normalX;
            float newVy = ballVy - 2 * dotProduct * normalY;
            
            // Add slight speed increase on reflection (optional game feel)
            const float speedMultiplier = 1.05f;
            newVx *= speedMultiplier;
            newVy *= speedMultiplier;
            
            return (newVx, newVy);
        }

        /// <summary>
        /// Normalize angle to 0-2π range
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
