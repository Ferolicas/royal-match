# Assets — Royal Match Clone

## Visual Asset Strategy

This project is built to run and be **understood** by other developers/models.
All visual assets use **procedurally generated materials + colors** defined in
code. No external image files are required for the core game. Later, a
texture pack can be dropped in `public/assets/` and wired into `AssetLoader`.

## Gem Colors & Palette

Defined in `src/data/colors.ts`:

| Gem Type | Color Name | Hex | HSV |
|----------|-----------|-----|-----|
| RED | Ruby Red | `#FF3B3B` | 0°, 100%, 100% |
| BLUE | Sapphire Blue | `#4A90FF` | 210°, 100%, 100% |
| GREEN | Emerald Green | `#34C759` | 120°, 65%, 100% |
| YELLOW | Amber Yellow | `#FFCC00` | 45°, 100%, 100% |
| PURPLE | Amethyst Purple | `#AF52DE` | 270°, 85%, 100% |
| CYAN | Aquamarine | `#32ADEF` | 196°, 85%, 100% |

These are **iOS system color** derivatives — bright, saturated, readable on
both light and dark backgrounds. Matches the Royal Match aesthetic.

## Gem Materials

Each gem is a PlayCanvas `cube` primitive with:
- **Diffuse** = gem color (50% opacity for translucency).
- **Emissive** = gem color (full strength, creates glow).
- **Specular** = white (20% intensity) for highlight sparkle.

Special pieces have distinct material modifiers:
- **Line Clear**: white emissive + pulse animation.
- **Bomb**: dark gray base + radial gradient texture.
- **Color Bomb**: white base + rainbow emissive scroll.
- **Rocket**: metallic silver + directional glow.

## Particle Effects (VFX)

All VFX are **procedural** (PlayCanvas particles), defined in
`src/engine/VFXManager.ts`:

| Effect | Particles | Lifetime | Color |
|--------|-----------|----------|-------|
| Match pop | 12 | 500ms | Gem color |
| Cascade sparkle | 4 per gem | 800ms | White → gem color |
| Special explosion | 32 | 700ms | White → gold |
| Level complete | 50 | 1500ms | Rainbow |

## Board Theme Layers

The PlayCanvas scene has these layers (bottom to top):
1. **Background plane** — dark gradient (deep blue → purple).
2. **Grid backdrop** — subtle hexagon/diamond pattern.
3. **Gems layer** — the match-3 pieces (Z = 0).
4. **Obstacle layer** — blockers (Z = 0, above gems on match).
5. **VFX layer** — particles (Z = 1, always on top).
6. **UI overlay** — React DOM (separate from canvas, positioned above).

## UI Assets

- UI uses **SVG + CSS** — no bitmap images needed. Icons are inline SVGs.
- Fonts: System font stack (san-serif), matching mobile app feel.
- Buttons: CSS variables for color, hover/active states.

## Asset Loading Pipeline (Future)

For when image/audio assets are added:
1. `scripts/build-assets.ts` scans `public/assets/` and generates a
   manifest JSON.
2. `src/engine/AssetLoader.ts` preloads assets via PlayCanvas `ResourceManager`
   and reports progress.
3. UI shows a progress bar during load.

## Audio (Planned)

Audio is **not implemented** in this phase but the architecture supports it:
- `src/engine/AudioManager.ts` (future) wraps Web Audio API.
- Sound events are emitted by the game logic layer (match made, swap invalid,
  level complete, etc.).
