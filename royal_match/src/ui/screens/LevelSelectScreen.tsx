import { useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { getDifficulty } from '../../levels/Difficulty';
import { Difficulty } from '../../types/Difficulty';

export function LevelSelect() {
  const navigate = useNavigate();
  const [selectedWorld, setSelectedWorld] = useState(1);

  const worlds = [
    { id: 1, name: 'Tutorial Island', levels: [1, 100] },
    { id: 2, name: 'Forest Realm', levels: [101, 400] },
    { id: 3, name: 'Mountain Realm', levels: [401, 700] },
    { id: 4, name: 'Castle Realm', levels: [701, 1000] },
  ];

  const selectedRange = worlds.find((world) => world.id === selectedWorld)?.levels ?? [1, 100];
  const [startLevel, endLevel] = selectedRange;
  const levels = Array.from({ length: endLevel - startLevel + 1 }, (_, i) => startLevel + i);

  const getLevelStars = (levelNum: number): number => {
    // In a real app, this would read from saved progress
    const saved = localStorage.getItem(`level_${levelNum}_stars`);
    return saved ? parseInt(saved, 10) : 0;
  };

  const isLevelUnlocked = (levelNum: number): boolean => {
    if (levelNum === 1) return true;
    const prevSaved = localStorage.getItem(`level_${levelNum - 1}_stars`);
    return prevSaved ? parseInt(prevSaved, 10) > 0 : false;
  };

  const handleLevelClick = (levelNum: number) => {
    if (!isLevelUnlocked(levelNum)) return;
    navigate(`/game/${levelNum}`);
  };

  return (
    <div className="screen screen--level-select">
      <div className="level-select__header">
        <button
          type="button"
          className="btn btn--secondary"
          onClick={() => navigate('/main-menu')}
        >
          ← Back
        </button>
        <h2>World Map</h2>
      </div>

      <div className="level-select__worlds">
        {worlds.map((world) => (
          <button
            key={world.id}
            type="button"
            className={`world-btn ${selectedWorld === world.id ? 'world-btn--active' : ''}`}
            onClick={() => setSelectedWorld(world.id)}
          >
            {world.name}
          </button>
        ))}
      </div>

      <div className="level-select__grid">
        {levels.map((levelNum) => {
          const stars = getLevelStars(levelNum);
          const unlocked = isLevelUnlocked(levelNum);
          const difficulty = getDifficulty(levelNum);
          const isHard = difficulty === Difficulty.HARD;
          const isSuperHard = difficulty === Difficulty.SUPER_HARD;

          return (
            <button
              key={levelNum}
              type="button"
              className={`level-btn ${unlocked ? 'level-btn--unlocked' : 'level-btn--locked'} ${
                isHard ? 'level-btn--hard' : isSuperHard ? 'level-btn--super-hard' : ''
              }`}
              onClick={() => handleLevelClick(levelNum)}
              disabled={!unlocked}
              title={`Level ${levelNum} (${difficulty})`}
            >
              <span className="level-btn__number">{levelNum}</span>
              <span
                className={`level-btn__stars ${unlocked ? '' : 'level-btn__stars--locked'}`}
              >
                {'★'.repeat(stars)}{'☆'.repeat(3 - stars)}
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
