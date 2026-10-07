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

Each gem is a PlayCanvas `sphere` primitive with a colored StandardMaterial,
emissive contribution, gloss and a small metallic contribution.

Special pieces have distinct material modifiers:
- **Line Clear**: white emissive.
- **Bomb**: orange emissive.
- **Color Bomb**: pink emissive.
- **Rocket**: cyan emissive.

## Particle Effects (VFX)

`src/engine/VFXManager.ts` defines a procedural particle simulation model:

| Effect | Particles | Lifetime | Color |
|--------|-----------|----------|-------|
| Match pop | 12 | 500ms | Gem color |
| Cascade sparkle | 4 per gem | 800ms | White → gem color |
| Special explosion | 32 | 700ms | White → gold |
| Level complete | 50 | 1500ms | Rainbow |

These particles are not yet projected into PlayCanvas entities, so they are
not a visible runtime feature.

## Board Theme Layers

The current scene uses an orthographic camera, ambient/directional lighting,
gem spheres, obstacle boxes and a separate React DOM control/HUD overlay.

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

## Audio

A short Web Audio feedback tone and optional device vibration are triggered by
an accepted move. Both preferences persist in local storage. Music and a full
audio event/mixing system are not implemented.
