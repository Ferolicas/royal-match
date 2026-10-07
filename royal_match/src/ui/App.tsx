import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { SplashScreen } from './screens/SplashScreen';
import { MainMenu } from './screens/MainMenuScreen';
import { LevelSelect } from './screens/LevelSelectScreen';
import { LevelComplete } from './screens/LevelCompleteScreen';
import { SettingsScreen } from './screens/SettingsScreen';

// PlayCanvas is the largest dependency in the application. Keep it out of the
// menu bundle and download it only when a player opens a level.
const GameScreen = lazy(() =>
  import('./screens/GameScreen').then(({ GameScreen: Screen }) => ({ default: Screen })),
);

/**
 * Main Application Router.
 *
 * Routes:
 *   /                     → Splash
 *   /main-menu            → Main Menu
 *   /level-select         → Level Select Grid
 *   /game/:levelNumber    → Game Screen
 *   /level-complete/:levelNumber → Level Complete Results
 *   /settings             → Settings
 */
export function App() {
  return (
    <BrowserRouter
      basename={import.meta.env.BASE_URL.replace(/\/$/, '')}
      future={{ v7_startTransition: true, v7_relativeSplatPath: true }}
    >
      <Routes>
        <Route path="/" element={<SplashScreen />} />
        <Route path="/main-menu" element={<MainMenu />} />
        <Route path="/level-select" element={<LevelSelect />} />
        <Route
          path="/game/:levelNumber"
          element={(
            <Suspense fallback={<div className="screen">Loading game…</div>}>
              <GameScreen />
            </Suspense>
          )}
        />
        <Route path="/level-complete/:levelNumber" element={<LevelComplete />} />
        <Route path="/settings" element={<SettingsScreen />} />
        <Route path="*" element={<Navigate to="/main-menu" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
