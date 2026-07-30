import React, { createContext, useCallback, useContext, useEffect, useState } from "react";
import { DarkTheme } from "../constants/dark-theme";
import { LightTheme, ThemeColors } from "../constants/light-theme";
import { updateUserSettings } from "../api/api";
import { useAuth } from "./auth-context";

type ThemeContextType = {
  colors: ThemeColors;
  isDark: boolean;
  toggleTheme: () => void;
};

const ThemeContext = createContext<ThemeContextType>({
  colors: LightTheme,
  isDark: false,
  toggleTheme: () => {},
});

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const { currentUser } = useAuth();
  const [isDark, setIsDark] = useState(false);

  useEffect(() => {
    if (currentUser?.settings) {
      setIsDark(currentUser.settings.darkMode);
    }
  }, [currentUser]);

  const toggleTheme = useCallback(async () => {
    const next = !isDark;
    setIsDark(next);
    try {
      await updateUserSettings({
        pushEnabled: currentUser?.settings?.pushEnabled ?? true,
        profileVisible: currentUser?.settings?.profileVisible ?? true,
        darkMode: next,
      });
    } catch {
      setIsDark(!next);
    }
  }, [isDark, currentUser?.settings]);

  const colors = isDark ? DarkTheme : LightTheme;

  return (
    <ThemeContext.Provider value={{ colors, isDark, toggleTheme }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  return useContext(ThemeContext);
}