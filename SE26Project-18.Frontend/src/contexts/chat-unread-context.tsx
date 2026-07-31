import React, { createContext, useCallback, useContext, useState } from "react";

interface ChatUnreadContextValue {
  unreadCount: number;
  setUnreadCount: (count: number) => void;
}

const ChatUnreadContext = createContext<ChatUnreadContextValue>({
  unreadCount: 0,
  setUnreadCount: () => {},
});

export function ChatUnreadProvider({ children }: { children: React.ReactNode }) {
  const [unreadCount, setUnreadCount] = useState(0);
  return (
    <ChatUnreadContext.Provider value={{ unreadCount, setUnreadCount }}>
      {children}
    </ChatUnreadContext.Provider>
  );
}

export function useChatUnread() {
  return useContext(ChatUnreadContext);
}