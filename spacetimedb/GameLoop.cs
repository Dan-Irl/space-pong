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
        var paddles = ctx.Db.Paddle.Iter().ToList();
        var gameSettings = ctx.Db.GameSettings.Iter().FirstOrDefault();

        // Update all players
        UpdateAllPlayers(ctx, players, gameSettings);

        // Update all paddles (must happen after players so they follow)
        UpdateAllPaddles(ctx, paddles, players);

        // Update all balls
        UpdateAllBalls(ctx, balls, gameSettings);

        // Check collisions
        CheckCollisions(ctx, balls, paddles, players);
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

    private static void UpdateAllPaddles(ReducerContext ctx, List<Paddle> paddles, List<Player> players)
    {
        foreach (var paddle in paddles)
        {
            // Find the parent player
            var player = players.FirstOrDefault(p => p.Id.Equals(paddle.PlayerId));
            if (player.Equals(default(Player)))
            {
                // Player doesn't exist, delete orphaned paddle
                ctx.Db.Paddle.PlayerId.Delete(paddle.PlayerId);
                continue;
            }

            // Sync paddle with player position, velocity, and angle
            ctx.Db.Paddle.PlayerId.Update(paddle with
            {
                X = player.X,
                Y = player.Y,
                VelocityX = player.VelocityX,
                VelocityY = player.VelocityY,
                Angle = player.PaddleAngle // Paddle faces opposite of aim
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

    private static void CheckCollisions(ReducerContext ctx, List<Ball> balls, List<Paddle> paddles, List<Player> players)
    {
        // PHASE 1: Detect all collisions (pure geometry - iterate directly over entities)
        var ballPaddleCollisions = new List<CollisionEvent<Ball, Paddle>>();
        var ballPlayerCollisions = new List<CollisionEvent<Ball, Player>>();

        foreach (var ball in balls)
        {
            // Check Ball vs Paddle collisions
            foreach (var paddle in paddles)
            {
                if (GamePhysics.CheckShapeOverlap(ball.Shape, ball.X, ball.Y, paddle.Shape, paddle.X, paddle.Y))
                {
                    ballPaddleCollisions.Add(new CollisionEvent<Ball, Paddle>
                    {
                        ObjectA = ball,
                        ObjectB = paddle
                    });
                }
            }

            // Check Ball vs Player collisions (ball hitting player body, not paddle)
            foreach (var player in players)
            {
                if (GamePhysics.CheckShapeOverlap(ball.Shape, ball.X, ball.Y, player.Shape, player.X, player.Y))
                {
                    ballPlayerCollisions.Add(new CollisionEvent<Ball, Player>
                    {
                        ObjectA = ball,
                        ObjectB = player
                    });
                }
            }
        }

        // Future: Check Ball vs PowerUp, Player vs Asteroid, etc.
        // Just add more collision lists for different entity type combinations

        // PHASE 2: Handle collisions based on entity types (game logic)
        foreach (var evt in ballPaddleCollisions)
        {
            HandleBallPaddleCollision(ctx, evt.ObjectA, evt.ObjectB);
        }

        foreach (var evt in ballPlayerCollisions)
        {
            HandleBallPlayerCollision(ctx, evt.ObjectA, evt.ObjectB);
        }

        // Future: Add more collision handlers here
        // foreach (var evt in ballPowerUpCollisions) { HandleBallPowerUpCollision(...); }
        // foreach (var evt in playerAsteroidCollisions) { HandlePlayerAsteroidCollision(...); }
    }

    /// <summary>
    /// Handle collision between ball and paddle (reflects ball and transfers momentum to player)
    /// </summary>
    private static void HandleBallPaddleCollision(ReducerContext ctx, Ball ball, Paddle paddle)
    {
        // Calculate reflected velocity (only if ball is moving towards paddle)
        var reflection = GamePhysics.ReflectVelocity(
            ball.VelocityX, ball.VelocityY,
            ball.X, ball.Y,
            paddle.X, paddle.Y
        );

        // Only update if reflection was calculated (ball was moving towards paddle)
        if (!reflection.HasValue)
        {
            return; // Ball moving away from paddle
        }

        var (newVx, newVy) = reflection.Value;

        // Find the player who owns this paddle
        if (ctx.Db.Player.Id.Find(paddle.PlayerId) is Player player)
        {
            // Calculate momentum transfer to player
            float dx = ball.X - paddle.X;
            float dy = ball.Y - paddle.Y;
            float length = MathF.Sqrt(dx * dx + dy * dy);

            if (length > 0.0001f)
            {
                float normalX = dx / length;
                float normalY = dy / length;

                // Calculate the impact force
                float dotProduct = ball.VelocityX * normalX + ball.VelocityY * normalY;

                // Transfer momentum to player
                const float momentumTransferFactor = 0.15f;
                float playerVelocityDeltaX = normalX * MathF.Abs(dotProduct) * momentumTransferFactor;
                float playerVelocityDeltaY = normalY * MathF.Abs(dotProduct) * momentumTransferFactor;

                // Update player velocity
                ctx.Db.Player.Id.Update(player with
                {
                    VelocityX = player.VelocityX + playerVelocityDeltaX,
                    VelocityY = player.VelocityY + playerVelocityDeltaY
                });
            }

            Log.Info($"Ball {ball.Id} reflected by {player.Name}'s paddle");
        }

        // Update ball with new velocity
        ctx.Db.Ball.Id.Update(ball with
        {
            VelocityX = newVx,
            VelocityY = newVy
        });
    }

    /// <summary>
    /// Handle collision between ball and player body (not paddle)
    /// For now, just log it - could be used for scoring/damage in the future
    /// </summary>
    private static void HandleBallPlayerCollision(ReducerContext ctx, Ball ball, Player player)
    {
        // Ball hit the player body, not the paddle
        // This could mean the player missed the ball
        // For now, just log it - in a real game, might deduct points or health
        Log.Info($"Ball {ball.Id} hit {player.Name} (missed paddle)");
    }
}
