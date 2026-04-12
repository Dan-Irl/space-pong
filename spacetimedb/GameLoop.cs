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
        var gameSettings = ctx.Db.GameSettings.Iter().FirstOrDefault();

        // Update all players
        UpdateAllPlayers(ctx, players, gameSettings);

        // Update all balls
        UpdateAllBalls(ctx, balls, gameSettings);

        // Check collisions
        CheckCollisions(ctx, balls, players);
    }

    private static void UpdateAllPlayers(ReducerContext ctx, List<Player> players, GameSettings gameSettings)
    {
        foreach (var player in players)
        {
            // Update player position based on velocity and game tick duration
            var newX = player.X + player.VelocityX * _gametick.Microseconds / 1_000_000.0f;
            var newY = player.Y + player.VelocityY * _gametick.Microseconds / 1_000_000.0f;

            // Check border collision and reflect if needed
            var (correctedX, correctedY, correctedVx, correctedVy) = GamePhysics.CheckWorldBorderCollision(
                newX, newY, player.VelocityX, player.VelocityY, player.PlayerRadius,
                gameSettings.WorldWidth, gameSettings.WorldHeight
            );

            // Update player in database
            ctx.Db.Player.Id.Update(player with
            {
                X = correctedX,
                Y = correctedY,
                VelocityX = correctedVx,
                VelocityY = correctedVy
            });
        }
    }

    private static void UpdateAllBalls(ReducerContext ctx, List<Ball> balls, GameSettings gameSettings)
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

            // Check border collision and reflect if needed
            var (correctedX, correctedY, correctedVx, correctedVy) = GamePhysics.CheckWorldBorderCollision(
                newX, newY, ball.VelocityX, ball.VelocityY, ball.Radius,
                gameSettings.WorldWidth, gameSettings.WorldHeight
            );

            // Update ball position and velocity in database
            ctx.Db.Ball.Id.Update(ball with
            {
                X = correctedX,
                Y = correctedY,
                VelocityX = correctedVx,
                VelocityY = correctedVy
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

                        // Calculate momentum transfer to player
                        // Normal vector from player to ball
                        float dx = ball.X - player.X;
                        float dy = ball.Y - player.Y;
                        float length = MathF.Sqrt(dx * dx + dy * dy);

                        if (length > 0.0001f)
                        {
                            float normalX = dx / length;
                            float normalY = dy / length;

                            // Calculate the impact force (velocity component towards player before reflection)
                            float dotProduct = ball.VelocityX * normalX + ball.VelocityY * normalY;

                            // Transfer a small portion of the impact momentum to the player
                            const float momentumTransferFactor = 0.15f; // 15% of impact momentum
                            float playerVelocityDeltaX = normalX * MathF.Abs(dotProduct) * momentumTransferFactor;
                            float playerVelocityDeltaY = normalY * MathF.Abs(dotProduct) * momentumTransferFactor;

                            // Update player velocity
                            ctx.Db.Player.Id.Update(player with
                            {
                                VelocityX = player.VelocityX + playerVelocityDeltaX,
                                VelocityY = player.VelocityY + playerVelocityDeltaY
                            });
                        }

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
