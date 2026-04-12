import type { Infer } from 'spacetimedb';
import type PlayerRow from '../module_bindings/player_table';
import type PaddleRow from '../module_bindings/paddle_table';
import type BallRow from '../module_bindings/ball_table';
import type GameSettingsRow from '../module_bindings/game_settings_table';
import type { Identity } from 'spacetimedb';
import { PlayerRenderer } from './PlayerRenderer';
import { PaddleRenderer } from './PaddleRenderer';

type Player = Infer<typeof PlayerRow>;
type Paddle = Infer<typeof PaddleRow>;
type Ball = Infer<typeof BallRow>;
type GameSettings = Infer<typeof GameSettingsRow>;

export class GameRenderer {
  private ctx: CanvasRenderingContext2D;
  private canvasWidth: number;
  private canvasHeight: number;
  private playerRenderer: PlayerRenderer;
  private paddleRenderer: PaddleRenderer;
  private worldWidth: number = 1000;
  private worldHeight: number = 1000;

  constructor(ctx: CanvasRenderingContext2D, width: number, height: number) {
    this.ctx = ctx;
    this.canvasWidth = width;
    this.canvasHeight = height;
    this.playerRenderer = new PlayerRenderer(ctx);
    this.paddleRenderer = new PaddleRenderer(ctx);
  }

  updateWorldSize(gameSettings: GameSettings | undefined) {
    if (gameSettings) {
      this.worldWidth = gameSettings.worldWidth;
      this.worldHeight = gameSettings.worldHeight;
    }
  }

  clear() {
    this.ctx.fillStyle = '#0a0a0a';
    this.ctx.fillRect(0, 0, this.canvasWidth, this.canvasHeight);
  }

  drawBall(ball: Ball) {
    this.ctx.fillStyle = '#ff0000';
    this.ctx.beginPath();
    this.ctx.arc(ball.x, ball.y, ball.radius, 0, Math.PI * 2);
    this.ctx.fill();
  }

  renderFrame(
    players: readonly Player[],
    paddles: readonly Paddle[],
    balls: readonly Ball[],
    currentPlayerIdentity: Identity | undefined
  ) {
    // Clear the physical screen
    this.clear();

    // Save the default canvas state
    this.ctx.save();

    // Transform world coordinates to canvas coordinates
    // World: (-worldWidth/2, -worldHeight/2) to (worldWidth/2, worldHeight/2)
    // Canvas: (0, 0) to (canvasWidth, canvasHeight)

    // Calculate scale to fit world in canvas
    const scaleX = this.canvasWidth / this.worldWidth;
    const scaleY = this.canvasHeight / this.worldHeight;
    const scale = Math.min(scaleX, scaleY); // Use smaller scale to fit both dimensions

    // Translate to center the world in the canvas
    this.ctx.translate(this.canvasWidth / 2, this.canvasHeight / 2);

    // Apply scale
    this.ctx.scale(scale, scale);

    // Draw all players using PlayerRenderer
    this.playerRenderer.drawAll(players, currentPlayerIdentity);

    // Draw all paddles using PaddleRenderer
    this.paddleRenderer.drawAll(paddles, currentPlayerIdentity);

    // Draw all balls
    balls.forEach(ball => {
      this.drawBall(ball);
    });

    // Restore canvas state
    this.ctx.restore();
  }
}
