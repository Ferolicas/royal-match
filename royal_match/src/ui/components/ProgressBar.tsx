export type ProgressBarVariant = 'moves' | 'score' | 'health';

export interface ProgressBarProps {
  label?: string;
  value: number;
  max: number;
  variant?: ProgressBarVariant;
  showValue?: boolean;
  animated?: boolean;
  className?: string;
}

export function ProgressBar({
  label,
  value,
  max,
  variant = 'moves',
  showValue = true,
  animated = true,
  className = '',
}: ProgressBarProps) {
  const percentage = Math.min(100, Math.max(0, (value / max) * 100));
  const variantClass = `progress--${variant}`;

  const getColorByVariant = (): string => {
    switch (variant) {
      case 'moves':
        return percentage > 50 ? '#4CAF50' : percentage > 20 ? '#FF9800' : '#F44336';
      case 'score':
        return '#FFD700';
      case 'health':
        return percentage > 50 ? '#4CAF50' : percentage > 20 ? '#FF9800' : '#F44336';
      default:
        return '#4CAF50';
    }
  };

  return (
    <div className={`progress-wrapper ${className}`}>
      {label && <span className="progress__label">{label}</span>}
      <div className={`progress ${variantClass}`}>
        <div
          className={`progress__fill ${animated ? 'progress__fill--animated' : ''}`}
          style={{
            width: `${percentage}%`,
            backgroundColor: getColorByVariant(),
          }}
        />
      </div>
      {showValue && (
        <span className="progress__value">
          {value} / {max}
        </span>
      )}
    </div>
  );
}
