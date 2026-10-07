/**
 * Generic object pool for reusable objects.
 * Minimizes GC pressure in the render loop.
 */
export class ObjectPool<T> {
  private pool: T[] = [];
  private createFn: () => T;
  private resetFn: (obj: T) => void;
  private inUse: Set<T> = new Set();

  constructor(createFn: () => T, resetFn: (obj: T) => void) {
    this.createFn = createFn;
    this.resetFn = resetFn;
  }

  acquire(): T {
    const obj = this.pool.pop() ?? this.createFn();
    this.inUse.add(obj);
    this.resetFn(obj);
    return obj;
  }

  release(obj: T): void {
    if (!this.inUse.has(obj)) return;
    this.inUse.delete(obj);
    this.pool.push(obj);
  }

  releaseAll(): void {
    for (const obj of this.inUse) {
      this.pool.push(obj);
    }
    this.inUse.clear();
  }

  getPoolSize(): number {
    return this.pool.length;
  }

  getInUseCount(): number {
    return this.inUse.size;
  }

  clear(): void {
    this.pool = [];
    this.inUse.clear();
  }
}
