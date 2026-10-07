export class DeterministicRNG {
  private seed: number;
  private state: number;

  constructor(seed: number) {
    this.seed = seed >>> 0;
    this.state = this.seed;
  }

  next(): number {
    this.state = (this.state * 1664525 + 1013904223) >>> 0;
    return this.state / 4294967296;
  }

  nextInRange(min: number, max: number): number {
    const r = this.next();
    return Math.floor(r * (max - min + 1)) + min;
  }

  nextFloat(min: number = 0, max: number = 1): number {
    return this.next() * (max - min) + min;
  }

  shuffle<T>(array: T[]): T[] {
    const result = [...array];
    for (let i = result.length - 1; i > 0; i--) {
      const j = this.nextInRange(0, i);
      [result[i], result[j]] = [result[j], result[i]];
    }
    return result;
  }

  pickOne<T>(array: T[]): T | undefined {
    if (array.length === 0) return undefined;
    const index = this.nextInRange(0, array.length - 1);
    return array[index];
  }

  weightedPick<T>(items: { value: T; weight: number }[]): T | undefined {
    const totalWeight = items.reduce((sum, item) => sum + item.weight, 0);
    let r = this.nextFloat(0, totalWeight);
    for (const item of items) {
      r -= item.weight;
      if (r <= 0) return item.value;
    }
    return items[items.length - 1]?.value;
  }

  getSeed(): number {
    return this.seed;
  }

  reset(): void {
    this.state = this.seed;
  }
}
