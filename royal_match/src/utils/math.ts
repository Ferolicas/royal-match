export function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

export function lerp(start: number, end: number, t: number): number {
  return start + (end - start) * t;
}

export function euclideanMod(n: number, d: number): number {
  return ((n % d) + d) % d;
}

export function degToRad(degrees: number): number {
  return (degrees * Math.PI) / 180;
}

export function radToDeg(radians: number): number {
  return (radians * 180) / Math.PI;
}

export function distanceSq(
  a: { x: number; y: number },
  b: { x: number; y: number }
): number {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  return dx * dx + dy * dy;
}

export function manhattanDistance(
  a: { row: number; col: number },
  b: { row: number; col: number }
): number {
  return Math.abs(a.row - b.row) + Math.abs(a.col - b.col);
}
