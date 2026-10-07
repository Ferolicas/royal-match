import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export function SettingsScreen() {
  const navigate = useNavigate();
  const [soundEnabled, setSoundEnabled] = usePersistentSetting('sound', true);
  const [vibrationEnabled, setVibrationEnabled] = usePersistentSetting('vibration', true);

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
      <button
        type="button"
        role="switch"
        aria-checked={value}
        aria-label={label}
        className={`toggle ${value ? 'toggle--on' : 'toggle--off'}`}
        onClick={() => onChange(!value)}
      >
        <span className="toggle__knob" />
      </button>
    </div>
  );
}

function usePersistentSetting(key: string, defaultValue: boolean) {
  const storageKey = `royal_match_${key}_enabled`;
  const [value, setValue] = useState(() => {
    const stored = localStorage.getItem(storageKey);
    return stored === null ? defaultValue : stored === 'true';
  });

  const update = (nextValue: boolean) => {
    localStorage.setItem(storageKey, String(nextValue));
    setValue(nextValue);
  };

  return [value, update] as const;
}
