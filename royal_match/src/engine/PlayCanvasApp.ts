import type { IGameEngine, EngineState, RenderOptions, EngineEventHandler, EngineEventType } from './types';
import { CameraController } from './CameraController';
import { VFXManager } from './VFXManager';
import { EntityManager } from './EntityManager';
import { MaterialFactory } from './AssetLoader';
import { GEM_COLORS } from '../data/colors';
import type { AnimationCommand } from '../types/GameState';
import { AnimationQueue } from '../game/AnimationQueue';
import { SpecialType } from '../types/SpecialType';

/**
 * PlayCanvas application wrapper.
 * Manages the PC.Application lifecycle, scene setup, and entity management.
 * All gem entities are pooled via the ObjectPool pattern for performance.
 */
export class PlayCanvasApp implements IGameEngine {
  private app: import('playcanvas').Application | null = null;
  private pc: typeof import('playcanvas') | null = null;
  private canvas: HTMLCanvasElement | null = null;
  private cameraController: CameraController;
  private vfxManager: VFXManager;
  private entityManager: EntityManager;
  private animationQueue: AnimationQueue;
  private eventHandlers: Map<EngineEventType, EngineEventHandler[]> = new Map();
  private initialized: boolean = false;
  private destroyed: boolean = false;
  private gemEntities: Map<string, import('playcanvas').Entity> = new Map();
  private obstacleCache: Map<string, import('playcanvas').Entity> = new Map();
  private gemPool: import('playcanvas').Entity[] = [];
  private obstaclePool: import('playcanvas').Entity[] = [];
  private materials: Map<string, import('playcanvas').StandardMaterial> = new Map();
  private cameraEntity: import('playcanvas').Entity | null = null;
  constructor() {
    this.cameraController = new CameraController();
    this.vfxManager = new VFXManager();
    this.entityManager = new EntityManager();
    this.animationQueue = new AnimationQueue();
  }

  /**
   * Initialize the PlayCanvas application on the given canvas.
   * Initialization failures are surfaced to the UI. A blank simulation is
   * not a valid renderer for a PlayCanvas game.
   */
  async initialize(canvas: HTMLCanvasElement): Promise<void> {
    this.canvas = canvas;
    this.canvas.width = this.canvas.clientWidth || 800;
    this.canvas.height = this.canvas.clientHeight || 600;

    const pc = await import('playcanvas');
    if (this.destroyed) throw new DOMException('PlayCanvas initialization was cancelled', 'AbortError');
    this.pc = pc;
    this.setupPlayCanvasApp(pc, canvas);

    this.cameraController.fitToBoard(8, 8);
    this.cameraController.setAspectRatio(this.canvas.width / this.canvas.height);
    this.initialized = true;
    this.canvas.dataset.playcanvasReady = 'true';
    this.emit('render_ready');
  }

  private setupPlayCanvasApp(pc: typeof import('playcanvas'), canvas: HTMLCanvasElement): void {
    const app = new pc.Application(canvas, {
      keyboard: new pc.Keyboard(window),
      mouse: new pc.Mouse(canvas),
      touch: new pc.TouchDevice(canvas),
      graphicsDeviceOptions: {
        antialias: true,
        alpha: true,
      },
    });

    this.app = app;

    // Setup scene
    app.scene.ambientLight = new pc.Color(0.35, 0.35, 0.4);

    // Camera
    const cameraEntity = new pc.Entity('camera');
    cameraEntity.addComponent('camera', {
      projection: pc.PROJECTION_ORTHOGRAPHIC,
      orthoHeight: this.getOrthoHeight(),
      nearClip: 0.1,
      farClip: 1000,
      clearColor: new pc.Color(0.059, 0.047, 0.161),
    });
    cameraEntity.setPosition(0, 0, 10);
    app.root.addChild(cameraEntity);
    this.cameraEntity = cameraEntity;

    // Lighting
    const lightEntity = new pc.Entity('light');
    lightEntity.addComponent('light', {
      type: 'directional',
      color: new pc.Color(1, 1, 1),
      intensity: 1.2,
    });
    lightEntity.setLocalEulerAngles(-45, 45, 0);
    app.root.addChild(lightEntity);

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
    if (this.canvas) {
      this.canvas.dataset.renderedGems = String(state.gems.filter((gem) => gem.visible).length);
      this.canvas.dataset.renderedObstacles = String(state.obstacles.length);
    }
  }

  private syncGems(gems: EngineState['gems']): void {
    if (!this.pc || !this.app) return;
    const visibleCells = new Set<string>();
    for (const gem of gems) {
      const cellId = this.entityManager.getCellId(gem.row, gem.col);

      if (!gem.visible) continue;
      visibleCells.add(cellId);

      const entity = this.gemEntities.get(cellId) ?? this.acquireGemEntity(cellId);
      entity.enabled = true;
      entity.setLocalPosition(gem.col - 3.5, 3.5 - gem.row, 0);
      entity.setLocalScale(0.78 * gem.scale, 0.78 * gem.scale, 0.35);
      const render = entity.render;
      if (render) {
        const baseColor = GEM_COLORS[gem.color as keyof typeof GEM_COLORS]?.hex ?? '#ffffff';
        if (gem.special && gem.special !== SpecialType.NONE) {
          const specialKind = gem.special === SpecialType.COLOR_BOMB
            ? 'colorbomb'
            : gem.special.toLowerCase() as 'line' | 'bomb' | 'rocket';
          const spec = MaterialFactory.specialMaterial(specialKind);
          render.material = this.getMaterial(`${gem.color}-${gem.special}`, spec.emissive);
        } else {
          render.material = this.getMaterial(gem.color, baseColor);
        }
      }
    }

    for (const [key, entity] of this.gemEntities) {
      if (!visibleCells.has(key)) {
        this.gemEntities.delete(key);
        entity.enabled = false;
        this.gemPool.push(entity);
      }
    }
  }

  private syncObstacles(obstacles: EngineState['obstacles']): void {
    if (!this.pc || !this.app) return;
    const visibleCells = new Set<string>();
    for (const obs of obstacles) {
      const cellId = this.entityManager.getCellId(obs.row, obs.col);
      visibleCells.add(cellId);
      const entity = this.obstacleCache.get(cellId) ?? this.acquireObstacleEntity(cellId);
      entity.enabled = true;
      entity.setLocalPosition(obs.col - 3.5, 3.5 - obs.row, 0);
      entity.setLocalScale(0.86, 0.86, 0.5);
      if (entity.render) {
        const spec = MaterialFactory.obstacleMaterial(obs.content);
        entity.render.material = this.getMaterial(obs.content, spec.diffuse);
      }
    }
    for (const [key, entity] of this.obstacleCache) {
      if (!visibleCells.has(key)) {
        this.obstacleCache.delete(key);
        entity.enabled = false;
        this.obstaclePool.push(entity);
      }
    }
  }

  private acquireGemEntity(cellId: string): import('playcanvas').Entity {
    const entity = this.gemPool.pop() ?? this.createPrimitive('sphere');
    entity.name = cellId;
    this.gemEntities.set(cellId, entity);
    return entity;
  }

  private acquireObstacleEntity(cellId: string): import('playcanvas').Entity {
    const entity = this.obstaclePool.pop() ?? this.createPrimitive('box');
    entity.name = `obstacle_${cellId}`;
    this.obstacleCache.set(cellId, entity);
    return entity;
  }

  private createPrimitive(type: 'sphere' | 'box'): import('playcanvas').Entity {
    if (!this.pc || !this.app) throw new Error('PlayCanvas is not initialized');
    const entity = new this.pc.Entity();
    entity.addComponent('render', { type });
    this.app.root.addChild(entity);
    return entity;
  }

  private getMaterial(key: string, hex: string): import('playcanvas').StandardMaterial {
    const existing = this.materials.get(key);
    if (existing) return existing;
    if (!this.pc) throw new Error('PlayCanvas is not initialized');
    const value = parseInt(hex.replace('#', ''), 16);
    const color = new this.pc.Color(((value >> 16) & 255) / 255, ((value >> 8) & 255) / 255, (value & 255) / 255);
    const material = new this.pc.StandardMaterial();
    material.diffuse.copy(color);
    material.emissive.copy(color).mulScalar(0.22);
    material.metalness = 0.15;
    material.gloss = 0.72;
    material.update();
    this.materials.set(key, material);
    return material;
  }

  private getOrthoHeight(): number {
    if (!this.canvas) return 4.8;
    const aspect = this.canvas.width / Math.max(1, this.canvas.height);
    return Math.max(4.8, 4.8 / aspect);
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
      if (this.cameraEntity?.camera) this.cameraEntity.camera.orthoHeight = this.getOrthoHeight();

      if (this.app) {
        this.app.resizeCanvas(width, height);
      }
    }
  }

  /** Clean up engine resources. */
  destroy(): void {
    this.destroyed = true;
    this.vfxManager.clear();
    this.gemEntities.clear();
    this.obstacleCache.clear();
    this.gemPool = [];
    this.obstaclePool = [];
    this.materials.clear();
    this.animationQueue.clear();
    this.eventHandlers.clear();

    if (this.app) {
      this.app.destroy();
      this.app = null;
    }
    this.pc = null;
    this.cameraEntity = null;
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
