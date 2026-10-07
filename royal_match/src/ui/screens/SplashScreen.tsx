import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

export function SplashScreen() {
  const navigate = useNavigate();
  const [progress, setProgress] = useState(0);

  useEffect(() => {
    const steps = [0, 25, 50, 75, 100];
    let i = 0;

    const interval = setInterval(() => {
      if (i < steps.length) {
        setProgress(steps[i]);
        i++;
      } else {
        clearInterval(interval);
        setTimeout(() => navigate('/main-menu'), 500);
      }
    }, 200);

    return () => clearInterval(interval);
  }, [navigate]);

  return (
    <div className="screen screen--splash">
      <div className="splash__logo">
        <span className="splash__title">ROYAL MATCH</span>
        <span className="splash__subtitle">Clone Edition</span>
      </div>
      <div className="splash__progress">
        <div
          className="splash__bar"
          style={{ width: `${progress}%` }}
        />
      </div>
      <p className="splash__loading-text">Loading... {progress}%</p>
    </div>
  );
}
