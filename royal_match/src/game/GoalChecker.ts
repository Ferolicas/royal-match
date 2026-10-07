import type { ObjectiveData, RuntimeObjective } from '../levels/LevelData';
import type { Match } from '../types/GameState';

export type CheckResult = {
  completed: boolean;
  objectiveProgress: { index: number; current: number }[];
};

export class GoalChecker {
  static check(
    objectives: RuntimeObjective[],
    matches: Match[],
    scoreDelta: number
  ): CheckResult {
    const objectiveProgress: { index: number; current: number }[] = [];

    for (let i = 0; i < objectives.length; i++) {
      const obj = objectives[i];

      switch (obj.type) {
        case 'collect_color': {
          let collected = 0;
          for (const match of matches) {
            if (match.color === obj.color) {
              collected += match.length;
            }
          }
          obj.current += collected;
          break;
        }

        case 'reach_score': {
          obj.current += scoreDelta;
          break;
        }

        case 'remove_blocker': {
          obj.current += matches.length;
          break;
        }

        case 'collect_item': {
          obj.current += Math.floor(matches.length / 3);
          break;
        }

        case 'drop_down': {
          obj.current += matches.length;
          break;
        }
      }

      objectiveProgress.push({ index: i, current: obj.current });
    }

    const completed = objectives.every((obj) => obj.current >= obj.target);

    return { completed, objectiveProgress };
  }

  static isComplete(objectives: RuntimeObjective[]): boolean {
    return objectives.every((obj) => obj.current >= obj.target);
  }

  static isFailed(movesRemaining: number, objectives: RuntimeObjective[]): boolean {
    return movesRemaining <= 0 && !GoalChecker.isComplete(objectives);
  }

  static getCompletionPercentage(objectives: RuntimeObjective[]): number {
    let totalProgress = 0;
    let totalTarget = 0;

    for (const obj of objectives) {
      totalProgress += Math.min(obj.current, obj.target);
      totalTarget += obj.target;
    }

    if (totalTarget === 0) return 100;
    return Math.round((totalProgress / totalTarget) * 100);
  }
}
