import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { SplashScreen } from './screens/SplashScreen';
import { MainMenu } from './screens/MainMenuScreen';
import { LevelSelect } from './screens/LevelSelectScreen';
import { GameScreen } from './screens/GameScreen';
import { LevelComplete } from './screens/LevelCompleteScreen';
import { SettingsScreen } from './screens/SettingsScreen';
import { ShopScreen } from './screens/ShopScreen';

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
 *   /shop                 → Shop
 */
export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<SplashScreen />} />
        <Route path="/main-menu" element={<MainMenu />} />
        <Route path="/level-select" element={<LevelSelect />} />
        <Route path="/game/:levelNumber" element={<GameScreen />} />
        <Route path="/level-complete/:levelNumber" element={<LevelComplete />} />
        <Route path="/settings" element={<SettingsScreen />} />
        <Route path="/shop" element={<ShopScreen />} />
        <Route path="*" element={<Navigate to="/main-menu" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
