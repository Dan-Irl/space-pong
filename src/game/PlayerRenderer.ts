import type { Infer } from 'spacetimedb';
import type PlayerRow from '../module_bindings/player_table';
import type { Identity } from 'spacetimedb';

type Player = Infer<typeof PlayerRow>;

export class PlayerRenderer {
  private ctx: CanvasRenderingContext2D;

  constructor(ctx: CanvasRenderingContext2D) {
    this.ctx = ctx;
  }

  draw(player: Player, isCurrentPlayer: boolean) {
    const color = isCurrentPlayer ? '#00ff00' : '#0088ff';

    this.drawPlayerCircle(player, color);
    this.drawPaddleArc(player, color);
    
    if (isCurrentPlayer) {
      this.drawAimLine(player);
    }
    
    this.drawPlayerName(player);
  }

  private drawPlayerCircle(player: Player, color: string) {
    this.ctx.fillStyle = color;
    this.ctx.beginPath();
    this.ctx.arc(player.x, player.y, player.playerRadius, 0, Math.PI * 2);
    this.ctx.fill();
  }

  private drawPaddleArc(player: Player, color: string) {
    const paddleCenterAngle = player.aimAngle + Math.PI; // 180 degrees from aim angle
    
    this.ctx.strokeStyle = color;
    this.ctx.lineWidth = 5;
    this.ctx.beginPath();
    this.ctx.arc(
      player.x,
      player.y,
      player.paddleRadius,
      paddleCenterAngle - player.paddleSize / 2,
      paddleCenterAngle + player.paddleSize / 2
    );
    this.ctx.stroke();
  }

  private drawAimLine(player: Player) {
    const aimLength = 50;
    
    this.ctx.strokeStyle = '#ffff00';
    this.ctx.lineWidth = 2;
    this.ctx.beginPath();
    this.ctx.moveTo(player.x, player.y);
    this.ctx.lineTo(
      player.x + Math.cos(player.aimAngle) * aimLength,
      player.y + Math.sin(player.aimAngle) * aimLength
    );
    this.ctx.stroke();
  }

  private drawPlayerName(player: Player) {
    this.ctx.fillStyle = '#ffffff';
    this.ctx.font = '14px sans-serif';
    this.ctx.textAlign = 'center';
    this.ctx.fillText(player.name, player.x, player.y - 25);
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
