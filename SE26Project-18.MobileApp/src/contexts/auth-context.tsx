import React, { createContext, useContext, useEffect, useState } from "react";
import { getUserMe, setAuthToken, refreshToken as apiRefreshToken } from "../api/api";

type AuthContextType = {
  isLoggedIn: boolean;
  currentUser: any | null;
  userId: number | null;
  accessToken: string | null;
  login: (accessToken: string, refreshToken: string, userId: number) => void;
  logout: () => void;
  refreshUser: () => void;
};

const AuthContext = createContext<AuthContextType>({
  isLoggedIn: false,
  currentUser: null,
  userId: null,
  accessToken: null,
  login: () => {},
  logout: () => {},
  refreshUser: () => {},
});

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [userId, setUserId] = useState<number | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [currentUser, setCurrentUser] = useState<any | null>(null);

  const doLogout = () => {
    setAuthToken(null);
    setAccessToken(null);
    setUserId(null);
    setIsLoggedIn(false);
    setCurrentUser(null);
    localStorage.removeItem("auth");
  };

  // 恢复 session
  useEffect(() => {
    const saved = localStorage.getItem("auth");
    if (saved) {
      try {
        const { accessToken: tok, refreshToken: ref, userId: uid } = JSON.parse(saved);
        setAuthToken(tok);
        setAccessToken(tok);
        setUserId(uid);
        setIsLoggedIn(true);
      } catch {
        localStorage.removeItem("auth");
      }
    }
  }, []);

  // Token 变更时加载用户信息
  useEffect(() => {
    if (accessToken && userId) {
      getUserMe()
        .then((user) => setCurrentUser(user))
        .catch(async () => {
          // Token 过期，尝试刷新
          try {
            const saved = JSON.parse(localStorage.getItem("auth") || "{}");
            if (saved.refreshToken) {
              const newToken = await apiRefreshToken(saved.refreshToken);
              setAuthToken(newToken);
              setAccessToken(newToken);
              const user = await getUserMe();
              setCurrentUser(user);
            }
          } catch {
            doLogout();
          }
        });
    } else {
      setCurrentUser(null);
    }
  }, [accessToken, userId]);

  return (
    <AuthContext.Provider
      value={{
        isLoggedIn,
        currentUser,
        userId,
        accessToken,
        login: (accessToken: string, refreshToken: string, uid: number) => {
          setAuthToken(accessToken);
          setAccessToken(accessToken);
          setUserId(uid);
          setIsLoggedIn(true);
          localStorage.setItem(
            "auth",
            JSON.stringify({ accessToken, refreshToken, userId: uid })
          );
        },
        logout: doLogout,
        refreshUser: () => {
          if (accessToken) {
            getUserMe().then((user) => setCurrentUser(user));
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
