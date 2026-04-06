import { useState, useEffect, useRef } from 'react';
import { useSpacetimeDB, useTable, useReducer } from 'spacetimedb/react';
import { reducers, tables } from './module_bindings';
import './App.css';

function App() {
  const [playerName, setPlayerName] = useState('');
  const [hasJoined, setHasJoined] = useState(false);
  const [aimAngle, setAimAngle] = useState(0);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  
  const { identity, isActive: connected } = useSpacetimeDB();
  const joinGame = useReducer(reducers.joinGame);
  const movePlayer = useReducer(reducers.movePlayer);
  const spawnBall = useReducer(reducers.spawnBall);

  // Subscribe to all players and balls
  const [players] = useTable(tables.Player);
  const [balls] = useTable(tables.Ball);

  // Get current player
  const currentPlayer = identity 
    ? Array.from(players).find(p => p.id.isEqual(identity))
    : null;

  // Check if player has joined
  useEffect(() => {
    if (currentPlayer) {
      setHasJoined(true);
      setAimAngle(currentPlayer.aimAngle);
    } else {
      setHasJoined(false);
    }
  }, [currentPlayer]);

  // Handle join game
  const handleJoin = (e: React.FormEvent) => {
    e.preventDefault();
    if (playerName.trim()) {
      joinGame({ playerName: playerName.trim() });
    }
  };

  // Handle mouse move for aiming
  const handleMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!hasJoined || !currentPlayer || !canvasRef.current) return;

    const canvas = canvasRef.current;
    const rect = canvas.getBoundingClientRect();
    const mouseX = e.clientX - rect.left - canvas.width / 2;
    const mouseY = e.clientY - rect.top - canvas.height / 2;
    
    const angle = Math.atan2(mouseY, mouseX);
    const normalizedAngle = angle < 0 ? angle + Math.PI * 2 : angle;
    
    setAimAngle(normalizedAngle);
    movePlayer({ newAngle: normalizedAngle });
  };

  // Handle shooting
  const handleShoot = () => {
    if (!hasJoined) return;
    spawnBall();
  };

  // Handle keyboard input
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.code === 'Space' && hasJoined) {
        e.preventDefault();
        handleShoot();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [hasJoined]);

  // Render game on canvas
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Clear canvas
    ctx.fillStyle = '#0a0a0a';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    const centerX = canvas.width / 2;
    const centerY = canvas.height / 2;
    const scale = 200; // Scale factor for game coordinates

    // Draw center
    ctx.fillStyle = '#333';
    ctx.beginPath();
    ctx.arc(centerX, centerY, 10, 0, Math.PI * 2);
    ctx.fill();

    // Draw players
    players.forEach(player => {
      const x = centerX + player.x * scale;
      const y = centerY + player.y * scale;
      const isCurrentPlayer = identity?.isEqual(player.id);

      // Draw player position
      ctx.fillStyle = isCurrentPlayer ? '#00ff00' : '#0088ff';
      ctx.beginPath();
      ctx.arc(x, y, 15, 0, Math.PI * 2);
      ctx.fill();

      // Draw player paddle arc
      const radius = Math.sqrt(player.x * player.x + player.y * player.y) * scale;
      ctx.strokeStyle = isCurrentPlayer ? '#00ff00' : '#0088ff';
      ctx.lineWidth = 5;
      ctx.beginPath();
      const paddleAngle = Math.atan2(player.y, player.x);
      ctx.arc(
        centerX, 
        centerY, 
        radius,
        paddleAngle - player.paddleSize / 2,
        paddleAngle + player.paddleSize / 2
      );
      ctx.stroke();

      // Draw aim line for current player
      if (isCurrentPlayer) {
        ctx.strokeStyle = '#ffff00';
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(x, y);
        const aimLength = 50;
        ctx.lineTo(
          x + Math.cos(player.aimAngle) * aimLength,
          y + Math.sin(player.aimAngle) * aimLength
        );
        ctx.stroke();
      }

      // Draw player name
      ctx.fillStyle = '#ffffff';
      ctx.font = '14px sans-serif';
      ctx.textAlign = 'center';
      ctx.fillText(player.name, x, y - 25);
    });

    // Draw balls
    balls.forEach(ball => {
      const x = centerX + ball.x * scale;
      const y = centerY + ball.y * scale;

      ctx.fillStyle = '#ff0000';
      ctx.beginPath();
      ctx.arc(x, y, ball.radius * scale, 0, Math.PI * 2);
      ctx.fill();
    });
  }, [players, balls, identity, aimAngle]);

  if (!connected || !identity) {
    return <div className="loading">Connecting to SpacetimeDB...</div>;
  }

  if (!hasJoined) {
    return (
      <div className="join-screen">
        <h1>Space Pong</h1>
        <form onSubmit={handleJoin}>
          <input
            type="text"
            placeholder="Enter your name"
            value={playerName}
            onChange={(e) => setPlayerName(e.target.value)}
            maxLength={20}
            autoFocus
          />
          <button type="submit">Join Game</button>
        </form>
      </div>
    );
  }

  return (
    <div className="game-container">
      <div className="game-info">
        <h2>Space Pong</h2>
        <p>Player: {currentPlayer?.name}</p>
        <p>Players: {players.length} | Balls: {balls.length}</p>
        <div className="controls">
          <p>Move mouse to aim</p>
          <button onClick={handleShoot}>Shoot Ball (Space)</button>
        </div>
      </div>
      <canvas
        ref={canvasRef}
        width={800}
        height={800}
        onMouseMove={handleMouseMove}
        onClick={handleShoot}
      />
    </div>
  );
}

export default App;
