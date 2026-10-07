import { useNavigate, useParams } from 'react-router-dom';
import { useGameEndState } from '../hooks/useGameState';

export function LevelComplete() {
  const { levelNumber } = useParams<{ levelNumber: string }>();
  const navigate = useNavigate();
  const numericLevel = levelNumber ? parseInt(levelNumber, 10) : 1;

  const { stars, isWin } = useGameEndState();
  const nextLevel = numericLevel + 1;
  const canContinue = numericLevel < 1000;

  const handleNext = () => {
    if (canContinue) {
      navigate(`/game/${nextLevel}`);
    } else {
      navigate('/level-select');
    }
  };

  const handleMenu = () => {
    navigate('/main-menu');
  };

  const handleRetry = () => {
    navigate(`/game/${numericLevel}`);
  };

  return (
    <div className="screen screen--level-complete">
      <div className="level-complete__content">
        <h1>{isWin ? 'Level Complete!' : 'Level Failed'}</h1>

        {isWin && (
          <>
            <div className="level-complete__stars">
              {Array.from({ length: 3 }).map((_, i) => (
                <span key={i} className={`star ${i < stars ? 'star--filled' : 'star--empty'}`}>
                  ★
                </span>
              ))}
            </div>

            <div className="level-complete__reward">
              <p>Reward: {stars * 10} coins</p>
            </div>
          </>
        )}

        <div className="level-complete__buttons">
          {isWin && canContinue && (
            <button type="button" className="btn btn--primary" onClick={handleNext}>
              Next Level ({nextLevel})
            </button>
          )}
          <button type="button" className="btn btn--secondary" onClick={handleRetry}>
            Retry
          </button>
          <button type="button" className="btn btn--secondary" onClick={handleMenu}>
            Main Menu
          </button>
        </div>
      </div>
    </div>
  );
}
