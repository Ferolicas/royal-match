import type { AnimationCommand } from '../types/GameState';

export type AnimationStatus = 'PENDING' | 'RUNNING' | 'COMPLETED' | 'CANCELLED';

export interface QueuedAnimation {
  command: AnimationCommand;
  status: AnimationStatus;
  startTime: number;
  actualDelay: number;
  actualDuration: number;
}

export class AnimationQueue {
  private queue: QueuedAnimation[] = [];
  private currentTime: number = 0;
  private totalDuration: number = 0;

  enqueue(command: AnimationCommand): void {
    const actualDelay = command.delay ?? 0;
    const actualDuration = command.duration;
    const entry: QueuedAnimation = {
      command,
      status: 'PENDING',
      startTime: this.totalDuration + actualDelay,
      actualDelay,
      actualDuration,
    };
    this.queue.push(entry);
    this.totalDuration = entry.startTime + actualDuration;
  }

  tick(deltaTime: number): QueuedAnimation[] {
    this.currentTime += deltaTime;
    const completed: QueuedAnimation[] = [];

    for (const entry of this.queue) {
      if (entry.status === 'PENDING' && this.currentTime >= entry.startTime) {
        entry.status = 'RUNNING';
      }
      if (
        entry.status === 'RUNNING' &&
        this.currentTime >= entry.startTime + entry.actualDuration
      ) {
        entry.status = 'COMPLETED';
        completed.push(entry);
      }
    }

    return completed;
  }

  getRunning(): QueuedAnimation[] {
    return this.queue.filter((e) => e.status === 'RUNNING' || e.status === 'PENDING');
  }

  getActiveCommands(): AnimationCommand[] {
    return this.queue
      .filter((e) => e.status === 'RUNNING')
      .map((e) => e.command);
  }

  isComplete(): boolean {
    return this.queue.every((e) => e.status === 'COMPLETED');
  }

  clear(): void {
    this.queue = [];
    this.currentTime = 0;
    this.totalDuration = 0;
  }

  peek(): AnimationCommand | null {
    const next = this.queue.find((e) => e.status === 'PENDING' || e.status === 'RUNNING');
    return next ? next.command : null;
  }

  getProgress(): number {
    if (this.queue.length === 0) return 1;
    const active = this.queue.find((e) => e.status === 'RUNNING');
    if (!active) return 0;
    const elapsed = this.currentTime - active.startTime;
    return elapsed / active.actualDuration;
  }
}
