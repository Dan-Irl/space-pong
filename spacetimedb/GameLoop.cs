using SpacetimeDB;
using System;
using System.Collections.Generic;
using System.Linq;

public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void GameTickUpdate(ReducerContext ctx, GameTick gameTick)
    {
        // Fetch all data once
        var balls = ctx.Db.Ball.Iter().ToList();
        var players = ctx.Db.Player.Iter().ToList();

        // Update all balls
        UpdateAllBalls(ctx, balls);

        // Check collisions
        CheckCollisions(ctx, balls, players);
    }

    private static void UpdateAllBalls(ReducerContext ctx, List<Ball> balls)
    {
        foreach (var ball in balls)
        {
            // Calculate how long the ball has been alive
            TimeDuration lifetime = ctx.Timestamp.TimeDurationSince(ball.CreatedAt);

            // Check if ball should despawn (after 5 seconds)
            if (lifetime.Microseconds >= 5_000_000)
            {
                ctx.Db.Ball.Id.Delete(ball.Id);
                Log.Info($"Ball {ball.Id} despawned after {lifetime.Microseconds / 1000000.0:F1} seconds");
                continue;
            }

            // Update ball position based on velocity and game tick duration
            var newX = ball.X + ball.VelocityX * _gametick.Microseconds / 1_000_000.0f;
            var newY = ball.Y + ball.VelocityY * _gametick.Microseconds / 1_000_000.0f;

            // Update ball position in database
            ctx.Db.Ball.Id.Update(ball with
            {
                X = newX,
                Y = newY
            });
        }
    }

    private static void CheckCollisions(ReducerContext ctx, List<Ball> balls, List<Player> players)
    {
        // Check collisions between all balls and all players
        foreach (var ball in balls)
        {
            foreach (var player in players)
            {
                // Check if ball collides with player's paddle
                bool collision = GamePhysics.CheckBallPaddleCollision(
                    ball.X, ball.Y, ball.Radius,
                    player.X, player.Y, player.PlayerRadius, player.PaddleRadius,
                    player.PaddleAngle, player.PaddleArcAngle
                );

                if (collision)
                {
                    // Calculate reflected velocity (only if ball is moving towards paddle)
                    var reflection = GamePhysics.ReflectVelocity(
                        ball.VelocityX, ball.VelocityY,
                        ball.X, ball.Y,
                        player.X, player.Y
                    );

                    // Only update if reflection was calculated (ball was moving towards paddle)
                    if (reflection.HasValue)
                    {
                        var (newVx, newVy) = reflection.Value;

                        // Update ball with new velocity
                        ctx.Db.Ball.Id.Update(ball with
                        {
                            VelocityX = newVx,
                            VelocityY = newVy
                        });

                        Log.Info($"Ball {ball.Id} reflected by {player.Name}");

                        // Only reflect once per tick to avoid multiple collisions
                        break;
                    }
                }
            }
        }
    }
}
