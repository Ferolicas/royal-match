import { useEffect, useRef, useCallback } from 'react';
import { useGameStore } from '../store';
import { GameStatus } from '../../game/GameState';

/**
 * Hook that sets up the game tick / animation frame loop.
 * When the game is animating, it ticks the animation queue and VFX manager.
 */
export function useGameLoop() {
  const animationFrameId = useRef<number | null>(null);
  const lastTimeRef = useRef<number>(0);

  const isAnimating = useGameStore((s) => s.isAnimating);
  const status = useGameStore((s) => s.status);
  const startLoop = useCallback(() => {
    if (animationFrameId.current !== null) return;

    lastTimeRef.current = performance.now();

    const tick = (time: number) => {
      const delta = time - lastTimeRef.current;
      lastTimeRef.current = time;

      if (delta > 0) {
        // Game loop tick — update animations, VFX, etc.
        // The actual animation steps come from GameManager.getAnimationSteps()
      }

      animationFrameId.current = requestAnimationFrame(tick);
    };

    animationFrameId.current = requestAnimationFrame(tick);
  }, []);

  const stopLoop = useCallback(() => {
    if (animationFrameId.current !== null) {
      cancelAnimationFrame(animationFrameId.current);
      animationFrameId.current = null;
    }
  }, []);

  // Start/stop loop based on game state
  useEffect(() => {
    if (status === GameStatus.PLAYING || status === GameStatus.RESOLVING || status === GameStatus.WAITING_INPUT) {
      startLoop();
    } else {
      stopLoop();
    }

    return () => stopLoop();
  }, [status, startLoop, stopLoop]);

  // Stop loop on unmount
  useEffect(() => {
    return () => stopLoop();
  }, [stopLoop]);

  return {
    isAnimating,
    isRunning: status === GameStatus.PLAYING || status === GameStatus.WAITING_INPUT,
  };
}
