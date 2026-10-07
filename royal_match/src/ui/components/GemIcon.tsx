import type { GemColor } from '../../data/colors';
import { GEM_COLORS } from '../../data/colors';

export interface GemIconProps {
  color: GemColor;
  size?: number;
  special?: string | null;
  className?: string;
}

export function GemIcon({ color, size = 24, special = null, className = '' }: GemIconProps) {
  const gemColor = GEM_COLORS[color];
  const hex = gemColor?.hex ?? '#FFFFFF';
  const name = gemColor?.name ?? 'Gem';

  // Determine special type styling
  const isSpecial = special !== null && special !== 'NONE';

  const specialGlow = isSpecial
    ? '0 0 8px var(--gem-glow), 0 0 12px var(--gem-glow)'
    : 'none';

  return (
    <div
      className={`gem-icon ${isSpecial ? 'gem-icon--special' : ''} ${className}`}
      style={{
        width: size,
        height: size,
        borderRadius: '50%',
        backgroundColor: hex,
        boxShadow: `0 0 6px ${hex},${specialGlow}`,
        position: 'relative',
        display: 'inline-block',
      }}
      title={`${name}${isSpecial ? ` (${special})` : ''}`}
    >
      {isSpecial && (
        <span
          style={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)',
            fontSize: size * 0.5,
          }}
        >
          ⭐
        </span>
      )}
    </div>
  );
}
