import React, { createContext, useCallback, useContext, useEffect, useState } from "react";
import { updateUserSettings } from "../api/api";
import { useAuth } from "./auth-context";

type NotificationContextType = {
  pushEnabled: boolean;
  setPushEnabled: (v: boolean) => void;
};

const NotificationContext = createContext<NotificationContextType>({
  pushEnabled: true,
  setPushEnabled: () => {},
});

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const { currentUser } = useAuth();
  const [pushEnabled, setPushEnabledState] = useState(true);

  useEffect(() => {
    if (currentUser?.settings) {
      setPushEnabledState(currentUser.settings.pushEnabled);
    }
  }, [currentUser?.settings]);

  const setPushEnabled = useCallback(async (v: boolean) => {
    setPushEnabledState(v);
    try {
      await updateUserSettings({
        pushEnabled: v,
        profileVisible: currentUser?.settings?.profileVisible ?? true,
        darkMode: currentUser?.settings?.darkMode ?? false,
      });
    } catch {
      setPushEnabledState(!v);
    }
  }, [currentUser?.settings]);

  return (
    <NotificationContext.Provider value={{ pushEnabled, setPushEnabled }}>
      {children}
    </NotificationContext.Provider>
  );
}

export function useNotification() {
  return useContext(NotificationContext);
}