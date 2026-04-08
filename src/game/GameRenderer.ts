import type { Infer } from 'spacetimedb';
import type PlayerRow from '../module_bindings/player_table';
import type BallRow from '../module_bindings/ball_table';
import type { Identity } from 'spacetimedb';
import { PlayerRenderer } from './PlayerRenderer';

type Player = Infer<typeof PlayerRow>;
type Ball = Infer<typeof BallRow>;

export class GameRenderer {
  private ctx: CanvasRenderingContext2D;
  private canvasWidth: number;
  private canvasHeight: number;
  private playerRenderer: PlayerRenderer;

  constructor(ctx: CanvasRenderingContext2D, width: number, height: number) {
    this.ctx = ctx;
    this.canvasWidth = width;
    this.canvasHeight = height;
    this.playerRenderer = new PlayerRenderer(ctx);
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
    balls: readonly Ball[],
    currentPlayerIdentity: Identity | undefined
  ) {
    // Clear the physical screen
    this.clear();

    // Find the current player to center the camera on
    const currentPlayer = players.find(p => 
      currentPlayerIdentity?.isEqual(p.id) ?? false
    );

    // Save the default canvas state
    this.ctx.save();

    // Apply camera translation to center on current player
    if (currentPlayer) {
      const cameraOffsetX = (this.canvasWidth / 2) - currentPlayer.x;
      const cameraOffsetY = (this.canvasHeight / 2) - currentPlayer.y;
      this.ctx.translate(cameraOffsetX, cameraOffsetY);
    }

    // Draw all players using PlayerRenderer
    this.playerRenderer.drawAll(players, currentPlayerIdentity);

    // Draw all balls
    balls.forEach(ball => {
      this.drawBall(ball);
    });

    // Restore canvas state (anything drawn after this won't move with camera)
    this.ctx.restore();
    
    // UI elements that should be pinned to screen would go here
  }
}
