import type { Infer } from 'spacetimedb';
import type PlayerRow from '../module_bindings/player_table';
import type { Identity } from 'spacetimedb';

type Player = Infer<typeof PlayerRow>;

export class PlayerRenderer {
  private ctx: CanvasRenderingContext2D;
  private centerX: number;
  private centerY: number;
  private scale: number;

  constructor(ctx: CanvasRenderingContext2D, centerX: number, centerY: number, scale: number) {
    this.ctx = ctx;
    this.centerX = centerX;
    this.centerY = centerY;
    this.scale = scale;
  }

  draw(player: Player, isCurrentPlayer: boolean) {
    const x = this.centerX + player.x * this.scale;
    const y = this.centerY + player.y * this.scale;
    const color = isCurrentPlayer ? '#00ff00' : '#0088ff';

    this.drawPlayerCircle(x, y, color);
    this.drawPaddleArc(player, color);
    
    if (isCurrentPlayer) {
      this.drawAimLine(player, x, y);
    }
    
    this.drawPlayerName(player, x, y);
  }

  private drawPlayerCircle(x: number, y: number, color: string) {
    this.ctx.fillStyle = color;
    this.ctx.beginPath();
    this.ctx.arc(x, y, 15, 0, Math.PI * 2);
    this.ctx.fill();
  }

  private drawPaddleArc(player: Player, color: string) {
    const radius = Math.sqrt(player.x * player.x + player.y * player.y) * this.scale;
    const paddleAngle = Math.atan2(player.y, player.x);
    
    this.ctx.strokeStyle = color;
    this.ctx.lineWidth = 5;
    this.ctx.beginPath();
    this.ctx.arc(
      this.centerX,
      this.centerY,
      radius,
      paddleAngle - player.paddleSize / 2,
      paddleAngle + player.paddleSize / 2
    );
    this.ctx.stroke();
  }

  private drawAimLine(player: Player, x: number, y: number) {
    const aimLength = 50;
    
    this.ctx.strokeStyle = '#ffff00';
    this.ctx.lineWidth = 2;
    this.ctx.beginPath();
    this.ctx.moveTo(x, y);
    this.ctx.lineTo(
      x + Math.cos(player.aimAngle) * aimLength,
      y + Math.sin(player.aimAngle) * aimLength
    );
    this.ctx.stroke();
  }

  private drawPlayerName(player: Player, x: number, y: number) {
    this.ctx.fillStyle = '#ffffff';
    this.ctx.font = '14px sans-serif';
    this.ctx.textAlign = 'center';
    this.ctx.fillText(player.name, x, y - 25);
  }

  drawAll(
    players: readonly Player[],
    currentPlayerIdentity: Identity | undefined
  ) {
    players.forEach(player => {
      const isCurrentPlayer = currentPlayerIdentity?.isEqual(player.id) ?? false;
      this.draw(player, isCurrentPlayer);
    });
  }
}
