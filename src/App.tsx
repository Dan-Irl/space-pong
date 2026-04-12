import { useState, useEffect } from "react";
import { useSpacetimeDB, useTable, useReducer } from "spacetimedb/react";
import { reducers, tables } from "./module_bindings";
import { GameCanvas } from "./components/GameCanvas";
import "./App.css";

function App() {
  const [playerName, setPlayerName] = useState("");
  const [hasJoined, setHasJoined] = useState(false);

  const { identity, isActive: connected } = useSpacetimeDB();
  const joinGame = useReducer(reducers.joinGame);
  const movePlayer = useReducer(reducers.movePlayer);
  const spawnBall = useReducer(reducers.spawnBall);

  // Subscribe to all players, balls, and game settings
  const [players] = useTable(tables.Player);
  const [paddles] = useTable(tables.Paddle);
  const [balls] = useTable(tables.Ball);
  const [gameSettings] = useTable(tables.GameSettings);

  // Get current player
  const currentPlayer = identity
    ? Array.from(players).find((p) => p.id.isEqual(identity))
    : null;

  // Check if player has joined
  useEffect(() => {
    if (currentPlayer) {
      setHasJoined(true);
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

  // Handle player movement
  const handleMove = (angle: number) => {
    movePlayer({ newAngle: angle });
  };

  // Handle shooting
  const handleShoot = () => {
    spawnBall();
  };

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

  const currentGameSettings =
    gameSettings.length > 0 ? gameSettings[0] : undefined;

  return (
    <div className="game-container">
      <div className="game-info">
        <h2>Space Pong</h2>
        <p>Player: {currentPlayer?.name}</p>
        <p>
          Players: {players.length} | Balls: {balls.length}
        </p>
      </div>
      <GameCanvas
        players={players}
        paddles={paddles}
        balls={balls}
        gameSettings={currentGameSettings}
        identity={identity}
        hasJoined={hasJoined}
        onMove={handleMove}
        onShoot={handleShoot}
      />
    </div>
  );
}

export default App;
