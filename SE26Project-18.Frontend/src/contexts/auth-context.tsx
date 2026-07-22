import React, { createContext, useContext, useEffect, useState } from "react";
import { getUserById, UserInfo } from "../api/api";

type AuthContextType = {
  isLoggedIn: boolean;
  currentUser: UserInfo | null;
  userId: number | null;
  login: (userId: number) => void;
  logout: () => void;
};

const AuthContext = createContext<AuthContextType>({
  isLoggedIn: false,
  currentUser: null,
  userId: null,
  login: () => {},
  logout: () => {},
});

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [userId, setUserId] = useState<number | null>(null);
  const [currentUser, setCurrentUser] = useState<UserInfo | null>(null);

  useEffect(() => {
    if (userId) {
      getUserById(userId).then((user) => setCurrentUser(user));
    } else {
      setCurrentUser(null);
    }
  }, [userId]);

  return (
    <AuthContext.Provider
      value={{
        isLoggedIn,
        currentUser,
        userId,
        login: (id: number) => {
          setUserId(id);
          setIsLoggedIn(true);
        },
        logout: () => {
          setUserId(null);
          setIsLoggedIn(false);
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
