import React, { createContext, useContext, useEffect, useState } from "react";
import { getMe, setAuthExpiredHandler, setLogoutInProgress, UserInfo } from "../api/api";
import { tokenStorage } from "../api/token-storage";

type AuthContextType = {
  isLoggedIn: boolean;
  isRestoring: boolean;
  loggingOut: boolean;
  currentUser: UserInfo | null;
  userId: number | null;
  login: (user: UserInfo) => void;
  logout: () => Promise<void>;
  refreshUser: () => void;
};

const AuthContext = createContext<AuthContextType>({
  isLoggedIn: false,
  isRestoring: true,
  loggingOut: false,
  currentUser: null,
  userId: null,
  login: () => {},
  logout: async () => {},
  refreshUser: () => {},
});

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [isRestoring, setIsRestoring] = useState(true);
  const [loggingOut, setLoggingOut] = useState(false);
  const [userId, setUserId] = useState<number | null>(null);
  const [currentUser, setCurrentUser] = useState<UserInfo | null>(null);

  useEffect(() => {
    const restoreSession = async () => {
      try {
        const token = await tokenStorage.getAccessToken();
        if (token) {
          const user = await getMe();
          if (user) {
            setCurrentUser(user);
            setUserId(user.id);
            setIsLoggedIn(true);
          } else {
            await tokenStorage.clearTokens();
          }
        }
      } catch {
        await tokenStorage.clearTokens();
      } finally {
        setIsRestoring(false);
      }
    };
    restoreSession();

    setAuthExpiredHandler(() => {
      setUserId(null);
      setIsLoggedIn(false);
      setCurrentUser(null);
    });
  }, []);

  useEffect(() => {
    if (userId) {
      getMe().then((user) => {
        if (user) setCurrentUser(user);
      });
    } else {
      setCurrentUser(null);
    }
  }, [userId]);

  return (
    <AuthContext.Provider
      value={{
        isLoggedIn,
        isRestoring,
        loggingOut,
        currentUser,
        userId,
        login: (user: UserInfo) => {
          setUserId(user.id);
          setIsLoggedIn(true);
          setCurrentUser(user);
        },
        logout: async () => {
          setLoggingOut(true);
          setLogoutInProgress(true);
          await tokenStorage.clearTokens();
          setUserId(null);
          setIsLoggedIn(false);
          setCurrentUser(null);
          setLoggingOut(false);
          setLogoutInProgress(false);
        },
        refreshUser: () => {
          if (userId) {
            getMe().then((user) => {
              if (user) setCurrentUser(user);
            });
          }
        },
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  return useContext(AuthContext);
}