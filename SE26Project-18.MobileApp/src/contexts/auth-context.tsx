import React, { createContext, useContext, useEffect, useState } from "react";
import { clearAuthTokens, getUserById, UserInfo } from "../api/api";

type AuthContextType = {
  initializing: boolean;
  isLoggedIn: boolean;
  currentUser: UserInfo | null;
  userId: number | null;
  login: (username: string, password: string) => Promise<void>;
  register: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
};

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [initializing, setInitializing] = useState(true);
  const [currentUser, setCurrentUser] = useState<UserInfo | null>(null);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        if (await restoreTokens()) {
          const user = await getMe();
          if (active) setCurrentUser(user);
        }
      } catch (error) {
        if (error instanceof ApiError && (error.status === 400 || error.status === 401)) {
          await discardSession();
        }
      } finally {
        if (active) setInitializing(false);
      }
    })();
    return () => {
      active = false;
    };
  }, []);

  const authenticate = async (username: string, password: string, isRegister: boolean) => {
    try {
      if (isRegister) await apiRegister(username, password);
      else await apiLogin(username, password);
      setCurrentUser(await getMe());
    } catch (error) {
      await discardSession();
      throw error;
    }
  };

  const logout = async () => {
    try {
      await apiLogout();
    } finally {
      setCurrentUser(null);
    }
  };

  const refreshUser = async () => setCurrentUser(await getMe());

  return (
    <AuthContext.Provider
      value={{
        initializing,
        isLoggedIn: currentUser !== null,
        currentUser,
        userId,
        login: (id: number) => {
          setUserId(id);
          setIsLoggedIn(true);
        },
        logout: () => {
          clearAuthTokens();
          setUserId(null);
          setIsLoggedIn(false);
        },
        refreshUser: () => {
          if (userId) {
            getUserById(userId).then((user) => setCurrentUser(user));
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
