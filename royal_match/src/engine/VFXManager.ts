import type { AnimationCommand } from '../types/GameState';

/**
 * Manages particle VFX for match explosions, cascades, and special pieces.
 * All effects are procedural — no texture files required.
 */

export interface Particle {
  id: string;
  x: number;
  y: number;
  z: number;
  vx: number;
  vy: number;
  vz: number;
  life: number;
  maxLife: number;
  color: string;
  size: number;
  gravity: number;
}

export type VFXType =
  | 'match_pop'
  | 'cascade_sparkle'
  | 'special_explosion'
  | 'level_complete'
  | 'gem_spawn';

const VFX_CONFIG: Record<VFXType, VFXConfig> = {
  match_pop: {
    particleCount: 12,
    lifetime: 500,
    speedRange: { min: 2, max: 8 },
    sizeRange: { min: 0.1, max: 0.3 },
    gravity: 0.2,
  },
  cascade_sparkle: {
    particleCount: 4,
    lifetime: 800,
    speedRange: { min: 1, max: 4 },
    sizeRange: { min: 0.08, max: 0.15 },
    gravity: 0.05,
  },
  special_explosion: {
    particleCount: 32,
    lifetime: 700,
    speedRange: { min: 3, max: 12 },
    sizeRange: { min: 0.15, max: 0.4 },
    gravity: 0.1,
  },
  level_complete: {
    particleCount: 50,
    lifetime: 1500,
    speedRange: { min: 2, max: 10 },
    sizeRange: { min: 0.2, max: 0.5 },
    gravity: -0.1,
  },
  gem_spawn: {
    particleCount: 8,
    lifetime: 400,
    speedRange: { min: 1, max: 5 },
    sizeRange: { min: 0.1, max: 0.25 },
    gravity: 0.15,
  },
};

interface VFXConfig {
  particleCount: number;
  lifetime: number;
  speedRange: { min: number; max: number };
  sizeRange: { min: number; max: number };
  gravity: number;
}

export class VFXManager {
  private particles: Particle[] = [];
  private counter: number = 0;

  /** Emit VFX at a position. */
  emit(
    type: VFXType,
    x: number,
    y: number,
    color: string = '#FFFFFF'
  ): void {
    const config = VFX_CONFIG[type];
    for (let i = 0; i < config.particleCount; i++) {
      const angle = (Math.PI * 2 * i) / config.particleCount + (Math.random() - 0.5) * 0.5;
      const speed = config.speedRange.min + Math.random() * (config.speedRange.max - config.speedRange.min);
      const size = config.sizeRange.min + Math.random() * (config.sizeRange.max - config.sizeRange.min);

      this.particles.push({
        id: `vfx_${this.counter++}`,
        x,
        y,
        z: 1,
        vx: Math.cos(angle) * speed,
        vy: Math.sin(angle) * speed,
        vz: Math.random() * 0.5,
        life: 0,
        maxLife: config.lifetime,
        color,
        size,
        gravity: config.gravity,
      });
    }
  }

  /** Emit VFX based on an animation command. */
  emitFromCommand(command: AnimationCommand): void {
    const target = command.targets[0];
    if (!target) return;

    const [colStr, rowStr] = target.split(',');
    const col = parseInt(colStr, 10);
    const row = parseInt(rowStr, 10);

    switch (command.type) {
      case 'DESTROY':
        this.emit('match_pop', col, row, command.params?.color as string | undefined);
        break;
      case 'SPECIAL_ACTIVATE':
        this.emit('special_explosion', col, row);
        break;
      case 'SPAWN':
        this.emit('gem_spawn', col, row);
        break;
    }
  }

  /** Update all particles by delta time (ms). */
  update(deltaTime: number): void {
    for (let i = this.particles.length - 1; i >= 0; i--) {
      const p = this.particles[i];
      p.life += deltaTime;
      p.x += p.vx * (deltaTime / 16);
      p.y += p.vy * (deltaTime / 16);
      p.z += p.vz * (deltaTime / 16);
      p.vy += p.gravity * (deltaTime / 16);
      p.vx *= 0.98;
      p.vz *= 0.98;

      if (p.life >= p.maxLife) {
        this.particles.splice(i, 1);
      }
    }
  }

  /** Get all active particles for rendering. */
  getParticles(): Particle[] {
    return this.particles;
  }

  /** Clear all particles. */
  clear(): void {
    this.particles = [];
  }

  /** Get count of active particles (for perf monitoring). */
  getParticleCount(): number {
    return this.particles.length;
  }
}
