import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export function SettingsScreen() {
  const navigate = useNavigate();
  const [soundEnabled, setSoundEnabled] = useState(true);
  const [musicEnabled, setMusicEnabled] = useState(true);
  const [vibrationEnabled, setVibrationEnabled] = useState(true);

  return (
    <div className="screen screen--settings">
      <div className="settings__header">
        <button
          type="button"
          className="btn btn--secondary"
          onClick={() => navigate('/main-menu')}
        >
          ← Back
        </button>
        <h2>Settings</h2>
      </div>

      <div className="settings__list">
        <ToggleRow
          label="Sound"
          value={soundEnabled}
          onChange={setSoundEnabled}
        />
        <ToggleRow
          label="Music"
          value={musicEnabled}
          onChange={setMusicEnabled}
        />
        <ToggleRow
          label="Vibration"
          value={vibrationEnabled}
          onChange={setVibrationEnabled}
        />
      </div>
    </div>
  );
}

function ToggleRow({
  label,
  value,
  onChange,
}: {
  label: string;
  value: boolean;
  onChange: (value: boolean) => void;
}) {
  return (
    <div className="setting-row">
      <span>{label}</span>
      <div
        className={`toggle ${value ? 'toggle--on' : 'toggle--off'}`}
        onClick={() => onChange(!value)}
      >
        <div className="toggle__knob" />
      </div>
    </div>
  );
}
