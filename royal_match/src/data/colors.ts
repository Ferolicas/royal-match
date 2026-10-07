export const GEM_COLORS = {
  RED: { name: 'Red', hex: '#FF3B3B', hsv: { h: 0, s: 100, v: 100 } },
  BLUE: { name: 'Blue', hex: '#4A90FF', hsv: { h: 210, s: 100, v: 100 } },
  GREEN: { name: 'Green', hex: '#34C759', hsv: { h: 120, s: 65, v: 100 } },
  YELLOW: { name: 'Yellow', hex: '#FFCC00', hsv: { h: 45, s: 100, v: 100 } },
  PURPLE: { name: 'Purple', hex: '#AF52DE', hsv: { h: 270, s: 85, v: 100 } },
  CYAN: { name: 'Cyan', hex: '#32ADEF', hsv: { h: 196, s: 85, v: 100 } },
} as const;

export type GemColor = keyof typeof GEM_COLORS;

export const BACKGROUND_COLOR = '#0f0c29';
export const GRID_COLOR = '#1a123d';
export const CELL_INNER_COLOR = '#16214e';
export const UI_PANEL_COLOR = '#1a123d';
export const UI_BORDER_COLOR = '#2e2250';
export const STAR_GOLD_COLOR = '#FFD700';
export const STAR_SILVER_COLOR = '#C0C0C0';
export const STAR_BRONZE_COLOR = '#CD7F32';
