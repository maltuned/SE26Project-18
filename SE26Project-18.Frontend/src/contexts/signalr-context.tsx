import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import React, { createContext, useContext, useEffect, useRef, useState } from "react";
import { AppState } from "react-native";
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
  const cancelledRef = useRef(false);
  const connectRef = useRef<() => Promise<void>>(async () => {});
  const handlerRefs = useRef<{
    receiveMessage: ((msg: MessageDto) => void)[];
    newChatMessage: ((msg: MessageDto) => void)[];
  }>({
    receiveMessage: [],
    newChatMessage: [],
  });

  useEffect(() => {
    if (!isLoggedIn) return;

    cancelledRef.current = false;

    const connect = async () => {
      try {
        const token = await tokenStorage.getAccessToken();
        if (!token || cancelledRef.current) return;

        const connection = new HubConnectionBuilder()
          .withUrl(`${API_BASE}/chatHub`, {
            accessTokenFactory: async () =>
              (await tokenStorage.getAccessToken()) || "",
          })
          .configureLogging(LogLevel.Warning)
          .withAutomaticReconnect()
          .build();

        connection.on("ReceiveMessage", (msg: MessageDto) => {
          handlerRefs.current.receiveMessage.forEach((h) => h(msg));
        });

        connection.on("NewChatMessage", (msg: MessageDto) => {
          handlerRefs.current.newChatMessage.forEach((h) => h(msg));
        });

        connection.onreconnecting(() => {
          if (!cancelledRef.current) setIsConnected(false);
        });

        connection.onreconnected(() => {
          if (!cancelledRef.current) setIsConnected(true);
        });

        connection.onclose(() => {
          if (!cancelledRef.current) {
            setIsConnected(false);
            setTimeout(() => {
              if (!cancelledRef.current) connect();
            }, 5000);
          }
        });

        await connection.start();
        if (!cancelledRef.current) {
          connectionRef.current = connection;
          setIsConnected(true);
        }
      } catch (err: any) {
        const isConnReset =
          err?.message?.includes("1006") ||
          err?.message?.includes("connection reset");
        if (!isConnReset) {
          console.error("SignalR connection failed:", err);
        }
        if (!cancelledRef.current) {
          setTimeout(() => {
            if (!cancelledRef.current) connect();
          }, 5000);
        }
      }
    };

    connectRef.current = connect;
    connect();

    return () => {
      cancelledRef.current = true;
      connectionRef.current?.stop();
      connectionRef.current = null;
      setIsConnected(false);
    };
  }, [isLoggedIn]);

  useEffect(() => {
    const sub = AppState.addEventListener("change", (nextState) => {
      if (nextState === "active" && isLoggedIn) {
        const conn = connectionRef.current;
        if (!conn || conn.state === HubConnectionState.Disconnected) {
          connectRef.current();
        }
      }
    });
    return () => sub.remove();
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