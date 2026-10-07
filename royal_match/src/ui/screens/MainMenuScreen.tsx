import { useNavigate } from 'react-router-dom';

export function MainMenu() {
  const navigate = useNavigate();

  return (
    <div className="screen screen--main-menu">
      <div className="menu__logo">
        <span className="menu__title">ROYAL MATCH</span>
      </div>

      <div className="menu__buttons">
        <button
          type="button"
          className="btn btn--primary btn--lg"
          onClick={() => navigate('/level-select')}
        >
          Play
        </button>

        <button
          type="button"
          className="btn btn--secondary"
          onClick={() => navigate('/settings')}
        >
          Settings
        </button>

      </div>

      <div className="menu__footer">
        <span className="menu__version">v0.1.0</span>
      </div>
    </div>
  );
}
