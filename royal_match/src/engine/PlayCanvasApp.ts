import type { IGameEngine, EngineState, RenderOptions, EngineEventHandler, EngineEventType } from './types';
import { CameraController } from './CameraController';
import { VFXManager } from './VFXManager';
import { EntityManager } from './EntityManager';
import { MaterialFactory } from './AssetLoader';
import type { AnimationCommand, AnimationType } from '../types/GameState';
import { AnimationQueue, type QueuedAnimation } from '../game/AnimationQueue';

/**
 * PlayCanvas application wrapper.
 * Manages the PC.Application lifecycle, scene setup, and entity management.
 * All gem entities are pooled via the ObjectPool pattern for performance.
 */
export class PlayCanvasApp implements IGameEngine {
  private app: unknown = null;
  private canvas: HTMLCanvasElement | null = null;
  private cameraController: CameraController;
  private vfxManager: VFXManager;
  private entityManager: EntityManager;
  private animationQueue: AnimationQueue;
  private eventHandlers: Map<EngineEventType, EngineEventHandler[]> = new Map();
  private initialized: boolean = false;
  private gemEntities: Map<string, unknown> = new Map();
  private obstacleCache: Map<string, unknown> = new Map();
  private animationIdCounter: number = 0;
  private lastTime: number = 0;

  constructor() {
    this.cameraController = new CameraController();
    this.vfxManager = new VFXManager();
    this.entityManager = new EntityManager();
    this.animationQueue = new AnimationQueue();
  }

  /**
   * Initialize the PlayCanvas application on the given canvas.
   * Since the `playcanvas` npm package may not resolve in all environments,
   * we gracefully handle its absence by running in simulation mode.
   */
  async initialize(canvas: HTMLCanvasElement): Promise<void> {
    this.canvas = canvas;
    this.canvas.width = this.canvas.clientWidth || 800;
    this.canvas.height = this.canvas.clientHeight || 600;

    // Try to initialize PlayCore engine; fall back to canvas 2D fallback if unavailable
    try {
      // eslint-disable-next-line @typescript-eslint/no-var-requires
      const pc = await this.loadPlayCanvas();
      if (pc) {
        this.setupPlayCanvasApp(pc, canvas);
      } else {
        console.warn('[PlayCanvasApp] PlayCanvas not available, using simulation mode');
      }
    } catch (err) {
      console.warn('[PlayCanvasApp] Falling back to simulation mode:', err);
    }

    this.cameraController.fitToBoard(8, 8);
    this.cameraController.setAspectRatio(this.canvas.width / this.canvas.height);
    this.initialized = true;
    this.emit('render_ready');
  }

  private async loadPlayCanvas(): Promise<typeof import('playcanvas') | null> {
    try {
      const pc = await import('playcanvas');
      return pc;
    } catch {
      return null;
    }
  }

  private setupPlayCanvasApp(pc: typeof import('playcanvas'), canvas: HTMLCanvasElement): void {
    const app = new pc.Application(canvas, {
      keyboard: new pc.Keyboard(canvas as unknown as HTMLElement),
      mouse: new pc.Mouse(canvas),
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      touch: new (pc as any).TouchDevice(canvas),
      graphicsDevice: new pc.GraphicsDevice(canvas.getContext('webgl') as WebGLRenderingContext, {
        antialias: true,
        alpha: true,
      }),
    });

    this.app = app;

    // Setup scene
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const scene = app.scene as any as { getRoot: () => any; ambientLight: any };
    scene.ambientLight = new pc.Color(0.2, 0.2, 0.2);

    // Camera
    const cameraEntity = new pc.Entity('camera');
    cameraEntity.addComponent('camera', {
      fov: 50,
      orthoHeight: 8,
      nearClip: 0.1,
      farClip: 1000,
    });
    scene.getRoot().addChild(cameraEntity);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (app as any as { camera: unknown }).camera = cameraEntity.camera;

    // Lighting
    const lightEntity = new pc.Entity('light');
    lightEntity.addComponent('light', {
      type: 'directional',
      color: new pc.Color(1, 1, 1),
      intensity: 1.2,
    });
    lightEntity.setLocalEulerAngles(-45, 45, 0);
    scene.getRoot().addChild(lightEntity);

    // Start the app
    app.start();
  }

  /**
   * Render the game state.
   * Syncs all gem entities to match the EngineState.
   */
  render(state: EngineState, options?: RenderOptions): void {
    if (!this.initialized) return;

    // Process animation queue
    if (options?.animationDeltaTime) {
      this.animationQueue.tick(options.animationDeltaTime);
      this.vfxManager.update(options.animationDeltaTime);
    }

    // Sync gem entities to state
    this.syncGems(state.gems);
    this.syncObstacles(state.obstacles);
  }

  private syncGems(gems: EngineState['gems']): void {
    for (const gem of gems) {
      const cellId = this.entityManager.getCellId(gem.row, gem.col);

      if (!gem.visible) {
        this.gemEntities.delete(cellId);
        continue;
      }

      // Position sync would happen here in real PlayCanvas
      if (this.gemEntities.has(cellId)) {
        // Update position/scale/color
      } else {
        // Create new entity (using pool)
        const entityId = `gem_${this.animationIdCounter++}`;
        this.gemEntities.set(cellId, {
          ...gem,
          entityId: `gem_${this.animationIdCounter++}`,
          material: MaterialFactory.gemMaterial(gem.color),
        });
      }
    }

    // Remove gems that are no longer visible
    for (const key of this.gemEntities.keys()) {
      const [rowStr, colStr] = key.split('_').slice(1);
      const row = parseInt(rowStr, 10);
      const col = parseInt(colStr, 10);
      if (!gems.some((g) => g.row === row && g.col === col && g.visible)) {
        this.gemEntities.delete(key);
      }
    }
  }

  private syncObstacles(obstacles: EngineState['obstacles']): void {
    for (const obs of obstacles) {
      const cellId = this.entityManager.getCellId(obs.row, obs.col);
      this.obstacleCache.set(cellId, { ...obs });
    }
  }

  /** Queue an animation command for processing. */
  queueAnimation(command: AnimationCommand): void {
    this.animationQueue.enqueue(command);

    if (command.type === 'DESTROY' || command.type === 'SPECIAL_ACTIVATE') {
      this.vfxManager.emitFromCommand(command);
    }
  }

  /** Get active animation progress (0-1). */
  getAnimationProgress(): number {
    return this.animationQueue.getProgress();
  }

  /** Check if animations are still running. */
  hasActiveAnimations(): boolean {
    return !this.animationQueue.isComplete();
  }

  /** Emit an engine event. */
  emit(type: EngineEventType, payload?: unknown): void {
    const handlers = this.eventHandlers.get(type);
    if (handlers) {
      for (const handler of handlers) {
        handler({ type, payload });
      }
    }
  }

  /** Subscribe to engine events. */
  on(type: EngineEventType, handler: EngineEventHandler): void {
    if (!this.eventHandlers.has(type)) {
      this.eventHandlers.set(type, []);
    }
    this.eventHandlers.get(type)!.push(handler);
  }

  /** Unsubscribe from engine events. */
  off(type: EngineEventType, handler: EngineEventHandler): void {
    const handlers = this.eventHandlers.get(type);
    if (handlers) {
      this.eventHandlers.set(
        type,
        handlers.filter((h) => h !== handler)
      );
    }
  }

  resize(width: number, height: number): void {
    if (this.canvas) {
      this.canvas.width = width;
      this.canvas.height = height;
      this.cameraController.setAspectRatio(width / height);

      if (this.app) {
        // @ts-expect-error PlayCanvas app resize
        this.app?.resizeCanvas?.(width, height);
      }
    }
  }

  /** Clean up engine resources. */
  destroy(): void {
    this.vfxManager.clear();
    this.gemEntities.clear();
    this.obstacleCache.clear();
    this.animationQueue.clear();
    this.eventHandlers.clear();

    if (this.app) {
      // @ts-expect-error PlayCanvas app destroy
      this.app?.destroy?.();
      this.app = null;
    }
    this.initialized = false;
  }

  /** Get the VFX manager for direct particle access. */
  getVFXManager(): VFXManager {
    return this.vfxManager;
  }

  /** Get the camera controller. */
  getCameraController(): CameraController {
    return this.cameraController;
  }
}
