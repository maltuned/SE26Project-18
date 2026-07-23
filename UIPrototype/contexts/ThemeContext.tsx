import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import * as SystemUI from 'expo-system-ui';
import { themes, type ThemeColors, type ThemeKey, type ThemeMeta } from '@/constants/themes';
import { themeMeta } from '@/constants/themes';

const STORAGE_KEY = 'playmate_theme';

interface ThemeContextValue {
  colors: ThemeColors;
  themeKey: ThemeKey;
  setTheme: (key: ThemeKey) => Promise<void>;
  themeMeta: ThemeMeta;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [themeKey, setThemeKey] = useState<ThemeKey>('arcade');
  const [ready, setReady] = useState(false);
  const colors = themes[themeKey];

  // Load persisted theme on mount
  useEffect(() => {
    (async () => {
      try {
        const stored = await AsyncStorage.getItem(STORAGE_KEY);
        if (stored === 'arcade' || stored === 'midnight' || stored === 'light') {
          setThemeKey(stored);
        }
      } catch {
        // ignore — stick with default
      } finally {
        setReady(true);
      }
    })();
  }, []);

  // Sync system chrome to theme
  useEffect(() => {
    if (!ready) return;
    SystemUI.setBackgroundColorAsync(colors.ink[900]);
  }, [ready, colors]);

  const setTheme = useCallback(async (key: ThemeKey) => {
    setThemeKey(key);
    try {
      await AsyncStorage.setItem(STORAGE_KEY, key);
    } catch {
      // ignore write errors
    }
  }, []);

  if (!ready) return null; // wait until we know which theme to show

  return (
    <ThemeContext.Provider value={{ colors, themeKey, setTheme, themeMeta: themeMeta[themeKey] }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used within ThemeProvider');
  return ctx;
}
