import type { Infer } from 'spacetimedb';
import type PaddleRow from '../module_bindings/paddle_table';
import type { Identity } from 'spacetimedb';

type Paddle = Infer<typeof PaddleRow>;

export class PaddleRenderer {
  private ctx: CanvasRenderingContext2D;

  constructor(ctx: CanvasRenderingContext2D) {
    this.ctx = ctx;
  }

  draw(paddle: Paddle, isCurrentPlayer: boolean) {
    const color = isCurrentPlayer ? '#00ff00' : '#0088ff';
    this.drawPaddleArc(paddle, color);
  }

  private drawPaddleArc(paddle: Paddle, color: string) {
    this.ctx.strokeStyle = color;
    this.ctx.lineWidth = 5;
    this.ctx.beginPath();
    this.ctx.arc(
      paddle.x,
      paddle.y,
      paddle.outerRadius,
      paddle.angle - paddle.arcAngle / 2,
      paddle.angle + paddle.arcAngle / 2
    );
    this.ctx.stroke();
  }

  drawAll(
    paddles: readonly Paddle[],
    currentPlayerIdentity: Identity | undefined
  ) {
    paddles.forEach(paddle => {
      const isCurrentPlayer = currentPlayerIdentity?.isEqual(paddle.playerId) ?? false;
      this.draw(paddle, isCurrentPlayer);
    });
  }
}
