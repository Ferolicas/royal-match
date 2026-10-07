import { useNavigate, useParams } from 'react-router-dom';
import { useEffect, useCallback, useState } from 'react';
import { useGameStore } from '../store';
import { useGameEndState, useGameState, useGameStatus } from '../hooks/useGameState';
import { GameStatus } from '../../game/GameState';
import { HUD } from '../components/HUD';
import { PlayCanvasApp } from '../../engine/PlayCanvasApp';
import { GameRenderer } from '../../engine/GameRenderer';

export function GameScreen() {
  const { levelNumber } = useParams<{ levelNumber: string }>();
  const navigate = useNavigate();
  const numericLevel = levelNumber ? parseInt(levelNumber, 10) : 1;

  const { loadLevel, trySwap } = useGameStore(
    (s) => ({
      loadLevel: s.loadLevel,
      trySwap: s.trySwap,
    }),
  );

  const { status, gridCols, gridRows, isAnimating, score, movesRemaining } = useGameState();
  const gameStatus = useGameStatus();
  const { stars } = useGameEndState();
  const gameManager = useGameStore((s) => s.gameManager);

  const [engine, setEngine] = useState<PlayCanvasApp | null>(null);
  const [renderer, setRenderer] = useState<GameRenderer | null>(null);
  const [selectedCell, setSelectedCell] = useState<{ row: number; col: number } | null>(null);
  const [engineError, setEngineError] = useState<string | null>(null);

  // Load level on mount
  useEffect(() => {
    if (numericLevel >= 1 && numericLevel <= 1000) {
      loadLevel(numericLevel);
      setSelectedCell(null);
    } else if (numericLevel < 1 || numericLevel > 1000) {
      navigate('/level-select', { replace: true });
    }
  }, [numericLevel, loadLevel, navigate]);

  // Initialize engine
  useEffect(() => {
    const canvas = document.getElementById('game-canvas') as HTMLCanvasElement | null;
    if (!canvas || engine) return;

    const app = new PlayCanvasApp();
    const render = new GameRenderer();
    let cancelled = false;

    app.initialize(canvas).then(() => {
      if (cancelled) {
        app.destroy();
        return;
      }
      setEngine(app);
      setRenderer(render);
    }).catch((error: unknown) => {
      if (!cancelled && !(error instanceof DOMException && error.name === 'AbortError')) {
        setEngineError(error instanceof Error ? error.message : String(error));
      }
    });

    return () => {
      cancelled = true;
      app.destroy();
    };
  }, []);

  // Handle swap input
  const handleCellClick = useCallback(
    (row: number, col: number) => {
      if (gameStatus !== GameStatus.WAITING_INPUT) return;
      if (!selectedCell) {
        setSelectedCell({ row, col });
        return;
      }

      const adjacent = Math.abs(selectedCell.row - row) + Math.abs(selectedCell.col - col) === 1;
      if (adjacent) {
        const accepted = trySwap(selectedCell.row, selectedCell.col, row, col);
        if (accepted) provideMoveFeedback();
        setSelectedCell(null);
      } else {
        setSelectedCell({ row, col });
      }
    },
    [gameStatus, selectedCell, trySwap],
  );

  // Render engine loop
  useEffect(() => {
    if (!engine || !renderer || !gameManager) return;

    const state = gameManager.getState();
    const engineState = renderer.render(state, gameManager.getBoard());
    engine.render(engineState);
  }, [engine, renderer, gameManager, status, isAnimating, score, movesRemaining]);

  useEffect(() => {
    if (!engine) return;
    const canvas = document.getElementById('game-canvas') as HTMLCanvasElement | null;
    if (!canvas) return;
    const observer = new ResizeObserver(([entry]) => {
      const width = Math.max(1, Math.round(entry.contentRect.width));
      const height = Math.max(1, Math.round(entry.contentRect.height));
      engine.resize(width, height);
    });
    observer.observe(canvas);
    return () => observer.disconnect();
  }, [engine]);

  // Handle win/lose
  useEffect(() => {
    if (status === GameStatus.WIN) {
      const storageKey = `level_${numericLevel}_stars`;
      const previous = Number(localStorage.getItem(storageKey) ?? 0);
      localStorage.setItem(storageKey, String(Math.max(previous, stars)));
      setTimeout(() => navigate(`/level-complete/${numericLevel}`), 500);
    } else if (status === GameStatus.LOSE) {
      setTimeout(() => navigate(`/level-complete/${numericLevel}`), 500);
    }
  }, [status, navigate, numericLevel, stars]);

  return (
    <div className="screen screen--game">
      <HUD />
      <div className="game__board">
        <canvas
          id="game-canvas"
          className="game__canvas"
          width={gridCols * 64}
          height={gridRows * 64}
        />

        {engineError && <div className="game__engine-error" role="alert">Rendering failed: {engineError}</div>}

        <BoardOverlay
          rows={gridRows}
          cols={gridCols}
          onClickedCell={handleCellClick}
          selectedCell={selectedCell}
        />
      </div>
    </div>
  );
}

function provideMoveFeedback(): void {
  if (localStorage.getItem('royal_match_vibration_enabled') !== 'false') {
    navigator.vibrate?.(20);
  }
  if (localStorage.getItem('royal_match_sound_enabled') === 'false') return;

  const AudioContextClass = window.AudioContext;
  if (!AudioContextClass) return;
  const context = new AudioContextClass();
  const oscillator = context.createOscillator();
  const gain = context.createGain();
  oscillator.type = 'sine';
  oscillator.frequency.setValueAtTime(520, context.currentTime);
  gain.gain.setValueAtTime(0.035, context.currentTime);
  gain.gain.exponentialRampToValueAtTime(0.001, context.currentTime + 0.08);
  oscillator.connect(gain).connect(context.destination);
  oscillator.start();
  oscillator.stop(context.currentTime + 0.08);
  oscillator.addEventListener('ended', () => void context.close(), { once: true });
}

function BoardOverlay({
  rows,
  cols,
  onClickedCell,
  selectedCell,
}: {
  rows: number;
  cols: number;
  onClickedCell: (row: number, col: number) => void;
  selectedCell: { row: number; col: number } | null;
}) {
  return (
    <div className="board-overlay">
      {Array.from({ length: rows }).map((_, row) =>
        Array.from({ length: cols }).map((_, col) => (
          <button
            type="button"
            key={`${row}-${col}`}
            className={`board-cell ${selectedCell?.row === row && selectedCell.col === col ? 'board-cell--selected' : ''}`}
            data-row={row}
            data-col={col}
            aria-label={`Row ${row + 1}, column ${col + 1}`}
            onClick={() => onClickedCell(row, col)}
          />
        ))
      )}
    </div>
  );
}
