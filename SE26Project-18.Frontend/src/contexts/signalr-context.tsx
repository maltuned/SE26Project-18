import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import React, { createContext, useContext, useEffect, useRef, useState } from "react";
import { MessageDto } from "../api/dtos";
import { tokenStorage } from "../api/token-storage";
import { API_BASE } from "../api/config";
import { useAuth } from "./auth-context";

type SignalRContextType = {
  connection: HubConnection | null;
  isConnected: boolean;
  joinChat: (chatId: number) => Promise<void>;
  leaveChat: (chatId: number) => Promise<void>;
  onReceiveMessage: (handler: (msg: MessageDto) => void) => () => void;
  onNewChatMessage: (handler: (msg: MessageDto) => void) => () => void;
};

const SignalRContext = createContext<SignalRContextType>({
  connection: null,
  isConnected: false,
  joinChat: async () => {},
  leaveChat: async () => {},
  onReceiveMessage: () => () => {},
  onNewChatMessage: () => () => {},
});

export function SignalRProvider({ children }: { children: React.ReactNode }) {
  const { isLoggedIn } = useAuth();
  const connectionRef = useRef<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const handlerRefs = useRef<{
    receiveMessage: ((msg: MessageDto) => void)[];
    newChatMessage: ((msg: MessageDto) => void)[];
  }>({
    receiveMessage: [],
    newChatMessage: [],
  });

  useEffect(() => {
    if (!isLoggedIn) return;

    let cancelled = false;

    const connect = async () => {
      try {
        const token = await tokenStorage.getAccessToken();
        if (!token) return;

        const connection = new HubConnectionBuilder()
          .withUrl(`${API_BASE}/chatHub`, {
            accessTokenFactory: () => token,
          })
          .configureLogging(LogLevel.Information)
          .withAutomaticReconnect()
          .build();

        connection.on("ReceiveMessage", (msg: MessageDto) => {
          handlerRefs.current.receiveMessage.forEach((h) => h(msg));
        });

        connection.on("NewChatMessage", (msg: MessageDto) => {
          handlerRefs.current.newChatMessage.forEach((h) => h(msg));
        });

        connection.onreconnecting(() => {
          if (!cancelled) setIsConnected(false);
        });

        connection.onreconnected(() => {
          if (!cancelled) setIsConnected(true);
        });

        connection.onclose(() => {
          if (!cancelled) setIsConnected(false);
        });

        await connection.start();
        if (!cancelled) {
          connectionRef.current = connection;
          setIsConnected(true);
        }
      } catch (err) {
        console.error("SignalR connection failed:", err);
        if (!cancelled) {
          setTimeout(() => {
            if (!cancelled) connect();
          }, 5000);
        }
      }
    };

    connect();

    return () => {
      cancelled = true;
      connectionRef.current?.stop();
      connectionRef.current = null;
      setIsConnected(false);
    };
  }, [isLoggedIn]);

  const joinChat = async (chatId: number) => {
    if (connectionRef.current?.state === HubConnectionState.Connected) {
      await connectionRef.current.invoke("JoinChat", chatId);
    }
  };

  const leaveChat = async (chatId: number) => {
    if (connectionRef.current?.state === HubConnectionState.Connected) {
      await connectionRef.current.invoke("LeaveChat", chatId);
    }
  };

  const onReceiveMessage = (handler: (msg: MessageDto) => void) => {
    handlerRefs.current.receiveMessage.push(handler);
    return () => {
      handlerRefs.current.receiveMessage = handlerRefs.current.receiveMessage.filter(
        (h) => h !== handler,
      );
    };
  };

  const onNewChatMessage = (handler: (msg: MessageDto) => void) => {
    handlerRefs.current.newChatMessage.push(handler);
    return () => {
      handlerRefs.current.newChatMessage = handlerRefs.current.newChatMessage.filter(
        (h) => h !== handler,
      );
    };
  };

  return (
    <SignalRContext.Provider
      value={{
        connection: connectionRef.current,
        isConnected,
        joinChat,
        leaveChat,
        onReceiveMessage,
        onNewChatMessage,
      }}
    >
      {children}
    </SignalRContext.Provider>
  );
}

export const useSignalR = () => useContext(SignalRContext);