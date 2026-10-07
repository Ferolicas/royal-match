import type { AnimationType } from '../types/GameState';

/**
 * Engine-specific type aliases and interfaces.
 * The engine layer communicates with game logic via these contracts.
 */

export type EngineEventType =
  | 'gem_clicked'
  | 'swap_started'
  | 'swap_confirmed'
  | 'swap_cancelled'
  | 'animation_complete'
  | 'render_ready';

export interface RenderableGem {
  entityId: string;
  row: number;
  col: number;
  color: string;
  special: string | null;
  visible: boolean;
  scale: number;
}

export interface EngineState {
  gems: RenderableGem[];
  obstacles: ObstacleRenderable[];
  camera: CameraState;
  backgroundColor: string;
  isAnimating: boolean;
}

export interface ObstacleRenderable {
  row: number;
  col: number;
  content: string;
  health: number;
}

export interface CameraState {
  position: { x: number; y: number; z: number };
  rotation: { x: number; y: number; z: number };
  fov: number;
  isOrtho: boolean;
}

export interface RenderOptions {
  animationDeltaTime?: number;
  forceReinit?: boolean;
}

export interface EngineEvent {
  type: EngineEventType;
  payload?: unknown;
}

export type EngineEventHandler = (event: EngineEvent) => void;

/**
 * Interface that the engine implementation must satisfy.
 * This allows the UI layer to interact with any engine (PlayCanvas, Canvas2D, etc).
 */
export interface IGameEngine {
  initialize(canvas: HTMLCanvasElement): Promise<void>;
  render(state: EngineState, options?: RenderOptions): void;
  on(event: EngineEventType, handler: EngineEventHandler): void;
  off(event: EngineEventType, handler: EngineEventHandler): void;
  resize(width: number, height: number): void;
  destroy(): void;
}

export { AnimationType };
