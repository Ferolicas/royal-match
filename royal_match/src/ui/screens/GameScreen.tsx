import { useNavigate, useParams } from 'react-router-dom';
import { useEffect, useCallback, useState } from 'react';
import { useGameStore } from '../store';
import { useGameState, useGameStatus } from '../hooks/useGameState';
import { GameStatus } from '../../game/GameState';
import { HUD } from '../components/HUD';
import { PlayCanvasApp } from '../../engine/PlayCanvasApp';
import { GameRenderer } from '../../engine/GameRenderer';

export function GameScreen() {
  const { levelNumber } = useParams<{ levelNumber: string }>();
  const navigate = useNavigate();
  const numericLevel = levelNumber ? parseInt(levelNumber, 10) : 1;

  const { loadLevel, trySwap, resolveTurn } = useGameStore(
    (s) => ({
      loadLevel: s.loadLevel,
      trySwap: s.trySwap,
      resolveTurn: s.resolveCurrentTurn,
    }),
  );

  const { status, gridCols, gridRows, isAnimating } = useGameState();
  const gameStatus = useGameStatus();
  const gameManager = useGameStore((s) => s.gameManager);

  const [engine, setEngine] = useState<PlayCanvasApp | null>(null);
  const [renderer, setRenderer] = useState<GameRenderer | null>(null);

  // Load level on mount
  useEffect(() => {
    if (gameStatus === GameStatus.IDLE) {
      loadLevel(numericLevel);
    }
  }, [numericLevel, gameStatus, loadLevel]);

  // Initialize engine
  useEffect(() => {
    const canvas = document.getElementById('game-canvas') as HTMLCanvasElement | null;
    if (!canvas || engine) return;

    const app = new PlayCanvasApp();
    const render = new GameRenderer();

    app.initialize(canvas).then(() => {
      setEngine(app);
      setRenderer(render);
    });

    return () => {
      app.destroy();
    };
  }, []);

  // Handle swap input
  const handleCellClick = useCallback(
    (row: number, col: number) => {
      if (gameStatus !== GameStatus.WAITING_INPUT) return;
      // Input handler manages selection internally
      // For now, we pass clicks to the GameManager via store
    },
    [gameStatus],
  );

  // Render engine loop
  useEffect(() => {
    if (!engine || !renderer || !gameManager) return;

    const state = gameManager.getState();
    const engineState = renderer.render(state, gameManager.getBoard());
    engine.render(engineState);
  }, [engine, renderer, gameManager, status, isAnimating]);

  // Handle win/lose
  useEffect(() => {
    if (status === GameStatus.WIN) {
      localStorage.setItem(`level_${numericLevel}_stars`, '3');
      setTimeout(() => navigate(`/level-complete/${numericLevel}`), 500);
    } else if (status === GameStatus.LOSE) {
      setTimeout(() => navigate(`/level-complete/${numericLevel}`), 500);
    }
  }, [status, navigate, numericLevel]);

  return (
    <div className="screen screen--game">
      <canvas
        id="game-canvas"
        className="game__canvas"
        width={gridCols * 64}
        height={gridRows * 64}
      />

      <HUD />

      {/* Touch overlay for input */}
      <BoardOverlay
        rows={gridRows}
        cols={gridCols}
        onClickedCell={handleCellClick}
      />
    </div>
  );
}

function BoardOverlay({
  rows,
  cols,
  onClickedCell,
}: {
  rows: number;
  cols: number;
  onClickedCell: (row: number, col: number) => void;
}) {
  return (
    <div className="board-overlay">
      {Array.from({ length: rows }).map((_, row) =>
        Array.from({ length: cols }).map((_, col) => (
          <div
            key={`${row}-${col}`}
            className="board-cell"
            data-row={row}
            data-col={col}
            onClick={() => onClickedCell(row, col)}
          />
        ))
      )}
    </div>
  );
}
