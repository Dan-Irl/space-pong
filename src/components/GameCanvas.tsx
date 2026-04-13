import { useRef, useEffect } from "react";
import { GameRenderer } from "../game/GameRenderer";
import { useGameInput } from "../game/useGameInput";
import type { Identity } from "spacetimedb";
import type { Ball, Player, Paddle, GameSettings } from "../module_bindings/types";

interface GameCanvasProps {
  players: readonly Player[];
  paddles: readonly Paddle[];
  balls: readonly Ball[];
  gameSettings: GameSettings | undefined;
  identity: Identity;
  hasJoined: boolean;
  onMove: (angle: number) => void;
  onShoot: () => void;
  width?: number;
  height?: number;
}

export function GameCanvas({
  players,
  paddles,
  balls,
  gameSettings,
  identity,
  hasJoined,
  onMove,
  onShoot,
  width = 800,
  height = 800,
}: GameCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const rendererRef = useRef<GameRenderer | null>(null);

  const Player: Player = players.find(p => p.id.isEqual(identity)) as Player;

  // Handle all input via event listeners
  useGameInput({ canvasRef, hasJoined, player: Player, onMove, onShoot });

  // Render game on canvas
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    // Initialize renderer if needed
    if (!rendererRef.current) {
      rendererRef.current = new GameRenderer(ctx, canvas.width, canvas.height);
    }

    // Update world size from game settings
    rendererRef.current.updateWorldSize(gameSettings);

    // Render the frame
    rendererRef.current.renderFrame(players, paddles, balls, identity);
  }, [players, paddles, balls, identity, gameSettings]);

  return <canvas ref={canvasRef} width={width} height={height} />;
}
