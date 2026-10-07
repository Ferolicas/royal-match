import type { Point } from '../types/GameState';

export type InputPhase = 'IDLE' | 'SELECTING' | 'DRAGGING' | 'SWAPPING' | 'LOCKED';

export interface InputAction {
  type: 'select' | 'swap' | 'swap_back';
  from?: Point;
  to?: Point;
  valid: boolean;
}

export class InputHandler {
  private phase: InputPhase = 'IDLE';
  private selectedCell: Point | null = null;
  private dragStart: Point | null = null;
  private dragCurrent: Point | null = null;
  private locked: boolean = false;

  getPhase(): InputPhase {
    return this.phase;
  }

  getSelectedCell(): Point | null {
    return this.selectedCell;
  }

  reset(): void {
    this.phase = 'IDLE';
    this.selectedCell = null;
    this.dragStart = null;
    this.dragCurrent = null;
    this.locked = false;
  }

  lock(): void {
    this.locked = true;
    this.phase = 'LOCKED';
  }

  unlock(): void {
    this.locked = false;
    this.phase = 'IDLE';
    this.selectedCell = null;
  }

  handlePointerDown(row: number, col: number): InputAction | null {
    if (this.locked) return null;

    if (this.phase === 'IDLE' || this.phase === 'SELECTING') {
      if (this.selectedCell && this.isAdjacent(row, col, this.selectedCell)) {
        this.selectedCell = { row, col };
        this.phase = 'SELECTING';
        return { type: 'select', from: this.selectedCell, to: { row, col }, valid: true };
      }
      this.selectedCell = { row, col };
      this.phase = 'SELECTING';
      return { type: 'select', to: { row, col }, valid: true };
    }

    return null;
  }

  handlePointerUp(row: number, col: number): InputAction | null {
    if (this.locked) return null;
    if (!this.selectedCell) return null;

    if (this.isAdjacent(row, col, this.selectedCell)) {
      const action: InputAction = {
        type: 'swap',
        from: this.selectedCell,
        to: { row, col },
        valid: true,
      };
      this.phase = 'SWAPPING';
      return action;
    }

    return null;
  }

  handlePointerMove(row: number, col: number): InputAction | null {
    if (this.locked) return null;
    if (!this.selectedCell) return null;
    if (this.phase !== 'SELECTING' && this.phase !== 'DRAGGING') return null;

    if (this.isAdjacent(row, col, this.selectedCell) && !this.dragStart) {
      this.dragStart = this.selectedCell;
      this.dragCurrent = { row, col };
      this.phase = 'DRAGGING';
      return {
        type: 'swap',
        from: this.selectedCell,
        to: { row, col },
        valid: true,
      };
    }

    return null;
  }

  cancelSwap(): void {
    if (this.phase === 'SWAPPING' && this.selectedCell) {
      this.phase = 'SELECTING';
    }
  }

  confirmSwap(): void {
    this.phase = 'LOCKED';
  }

  private isAdjacent(r1: number, c1: number, r2: Point): boolean {
    return (
      (Math.abs(r1 - r2.row) === 1 && c1 === r2.col) ||
      (Math.abs(c1 - r2.col) === 1 && r1 === r2.row)
    );
  }
}
