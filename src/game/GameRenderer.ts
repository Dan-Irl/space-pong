import type { Infer } from 'spacetimedb';
import type PlayerRow from '../module_bindings/player_table';
import type BallRow from '../module_bindings/ball_table';
import type { Identity } from 'spacetimedb';
import { PlayerRenderer } from './PlayerRenderer';

type Player = Infer<typeof PlayerRow>;
type Ball = Infer<typeof BallRow>;

export class GameRenderer {
  private ctx: CanvasRenderingContext2D;
  private centerX: number;
  private centerY: number;
  private scale: number;
  private playerRenderer: PlayerRenderer;

  constructor(ctx: CanvasRenderingContext2D, width: number, height: number, scale = 200) {
    this.ctx = ctx;
    this.centerX = width / 2;
    this.centerY = height / 2;
    this.scale = scale;
    this.playerRenderer = new PlayerRenderer(ctx, this.centerX, this.centerY, this.scale);
  }

  clear() {
    this.ctx.fillStyle = '#0a0a0a';
    this.ctx.fillRect(0, 0, this.ctx.canvas.width, this.ctx.canvas.height);
  }

  drawCenter() {
    this.ctx.fillStyle = '#333';
    this.ctx.beginPath();
    this.ctx.arc(this.centerX, this.centerY, 10, 0, Math.PI * 2);
    this.ctx.fill();
  }

  drawBall(ball: Ball) {
    const x = this.centerX + ball.x * this.scale;
    const y = this.centerY + ball.y * this.scale;

    this.ctx.fillStyle = '#ff0000';
    this.ctx.beginPath();
    this.ctx.arc(x, y, ball.radius * this.scale, 0, Math.PI * 2);
    this.ctx.fill();
  }

  renderFrame(
    players: readonly Player[],
    balls: readonly Ball[],
    currentPlayerIdentity: Identity | undefined
  ) {
    this.clear();
    this.drawCenter();

    // Draw all players using PlayerRenderer
    this.playerRenderer.drawAll(players, currentPlayerIdentity);

    // Draw all balls
    balls.forEach(ball => {
      this.drawBall(ball);
    });
  }
}
