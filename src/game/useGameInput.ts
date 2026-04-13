import { useEffect, type RefObject } from 'react';
import type { Player } from '../module_bindings/types';

interface UseGameInputProps {
  canvasRef: RefObject<HTMLCanvasElement | null>;
  hasJoined: boolean;
  player: Player;
  onMove: (angle: number) => void;
  onShoot: () => void;
}

export const useGameInput = ({
  canvasRef,
  hasJoined,
  player,
  onMove,
  onShoot,
}: UseGameInputProps) => {
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    let lastMouseTime = 0;

    // --- MOUSE HANDLERS ---
    const handleMouseMove = (event: MouseEvent) => {
      if (!hasJoined) return;

      const now = Date.now();
      if (now - lastMouseTime > 16) { // ~60fps throttle
        const rect = canvas.getBoundingClientRect();
        const mouseX = event.clientX - rect.left - canvas.width / 2;
        const mouseY = event.clientY - rect.top - canvas.height / 2;
        
        const angle = Math.atan2(mouseY, mouseX);
        const normalizedAngle = angle < 0 ? angle + Math.PI * 2 : angle;
        
        onMove(normalizedAngle);
        lastMouseTime = now;
      }
    };

    const handleClick = () => {
      if (!hasJoined) return;
      onShoot();
    };

    // --- KEYBOARD HANDLERS ---
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.code === 'Space' && hasJoined) {
        event.preventDefault();
        onShoot();
      }
    };

    // --- ATTACH LISTENERS ---
    canvas.addEventListener('mousemove', handleMouseMove);
    canvas.addEventListener('click', handleClick);
    window.addEventListener('keydown', handleKeyDown);

    // --- CLEANUP ---
    return () => {
      canvas.removeEventListener('mousemove', handleMouseMove);
      canvas.removeEventListener('click', handleClick);
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [canvasRef, hasJoined, onMove, onShoot]);
};
