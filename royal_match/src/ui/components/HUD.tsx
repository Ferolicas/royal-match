import { useGameState, useGameActions, useGameStatus, useGameEndState } from '../hooks/useGameState';
import { GameStatus } from '../../game/GameState';
import type { ObjectiveData } from '../../levels/LevelData';

/**
 * In-game HUD showing moves remaining, score, objectives, and star rating.
 */
export function HUD() {
  const {
    movesRemaining,
    moveLimit,
    levelNumber,
    score,
    star1,
    star2,
    star3,
    isLevelLoading,
  } = useGameState();

  const status = useGameStatus();
  const { restartLevel } = useGameActions();
  const { isWin, stars } = useGameEndState();

  const showWinLose = status === GameStatus.WIN || status === GameStatus.LOSE;

  return (
    <div className="hud">
      {/* Top bar: moves and score */}
      <div className="hud__top">
        <div className="hud__level">
          <span className="hud__label">LEVEL</span>
          <span className="hud__value">{levelNumber}</span>
        </div>
        <div className="hud__moves">
          <span className="hud__label">MOVES</span>
          <span className="hud__value">{movesRemaining}/{moveLimit}</span>
        </div>

        <div className="hud__score">
          <span className="hud__label">SCORE</span>
          <span className="hud__value">{score.toLocaleString()}</span>
        </div>

        <div className="hud__stars">
          <StarIndicator filled={score >= star1} />
          <StarIndicator filled={score >= star2} />
          <StarIndicator filled={score >= star3} />
        </div>
      </div>

      {/* Objectives panel */}
      <ObjectivesPanel />

      {/* Win/Lose overlay */}
      {showWinLose && (
        <div className="hud__overlay">
          <div className={`hud__result hud__result--${isWin ? 'win' : 'lose'}`}>
            <h2>{isWin ? 'LEVEL COMPLETE' : 'LEVEL FAILED'}</h2>
            {isWin && (
              <div className="hud__stars-result">
                {Array.from({ length: 3 }).map((_, i) => (
                  <StarIndicator key={i} filled={i < stars} />
                ))}
              </div>
            )}
            <button className="btn btn--primary" onClick={restartLevel}>
              {isWin ? 'Next Level' : 'Retry'}
            </button>
          </div>
        </div>
      )}

      {isLevelLoading && (
        <div className="hud__loading">
          <div className="spinner" />
          <span>Loading level...</span>
        </div>
      )}
    </div>
  );
}

function StarIndicator({ filled }: { filled: boolean }) {
  return (
    <span className={`star ${filled ? 'star--filled' : 'star--empty'}`}>
      {filled ? '★' : '☆'}
    </span>
  );
}

function ObjectivesPanel() {
  const objectives = useGameState().objectives as unknown as ObjectiveData[];

  if (!objectives || objectives.length === 0) {
    return null;
  }

  return (
    <div className="hud__objectives">
      {objectives.map((obj, index) => (
        <ObjectiveItem key={index} objective={obj} />
      ))}
    </div>
  );
}

function ObjectiveItem({
  objective,
}: {
  objective: ObjectiveData;
}) {
  const current = (objective as { current: number }).current ?? 0;
  const target = objective.target;
  const progress = Math.min(100, (current / target) * 100);

  return (
    <div className="objective">
      <span className="objective__icon">{getObjectiveIcon(objective.type)}</span>
      <span className="objective__text">{objective.description}</span>
      <span className="objective__progress">
        {current}/{target}
      </span>
      <div className="objective__bar">
        <div
          className="objective__fill"
          style={{ width: `${progress}%` }}
        />
      </div>
    </div>
  );
}

function getObjectiveIcon(type: string): string {
  switch (type) {
    case 'collect_color':
      return '◆';
    case 'reach_score':
      return '★';
    case 'remove_blocker':
      return '▢';
    case 'collect_item':
      return '★';
    case 'drop_down':
      return '↓';
    default:
      return '?';
  }
}
