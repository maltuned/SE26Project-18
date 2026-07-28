import {
  Stack,
  useGlobalSearchParams,
  useRootNavigationState,
  useRouter,
  useSegments,
} from "expo-router";
import { useCallback, useEffect, useRef, useState } from "react";
import { StatusBar, StyleSheet, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { initTagCaches } from "../api/api";
import { MessageDto } from "../api/dtos";
import MessageToast, { ToastMessage } from "../components/message-toast";
import TestMessageButton from "../components/test-message-button";
import { AuthProvider, useAuth } from "../contexts/auth-context";
import { SignalRProvider, useSignalR } from "../contexts/signalr-context";
import { ThemeProvider, useTheme } from "../contexts/theme-context";

export default function RootLayout() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <SignalRProvider>
          <RootLayoutMain />
        </SignalRProvider>
      </AuthProvider>
    </ThemeProvider>
  );
}

function RootLayoutMain() {
  useEffect(() => {
    initTagCaches();
  }, []);

  return <RootLayoutNav />;
}

function RootLayoutNav() {
  const { isLoggedIn, isRestoring, userId } = useAuth();
  const { colors } = useTheme();
  const segments = useSegments();
  const router = useRouter();
  const navState = useRootNavigationState();
  const insets = useSafeAreaInsets();
  const { onNewChatMessage } = useSignalR();
  const globalParams = useGlobalSearchParams();

  const [toast, setToast] = useState<ToastMessage | null>(null);
  const toastQueueRef = useRef<ToastMessage[]>([]);
  const toastVisibleRef = useRef(false);

  const showNextToast = useCallback(() => {
    if (toastQueueRef.current.length > 0) {
      const next = toastQueueRef.current.shift()!;
      toastVisibleRef.current = true;
      setToast(next);
    } else {
      toastVisibleRef.current = false;
      setToast(null);
    }
  }, []);

  useEffect(() => {
    const unsub = onNewChatMessage((msg: MessageDto) => {
      const isChatListTab = segments[0] === "(tabs)" && segments[1] === "chat";
      const inChatRoom =
        segments[0] === "chat-room" &&
        globalParams.chatId === String(msg.chat_id);

      if (isChatListTab || inChatRoom) return;

      const newToast: ToastMessage = {
        id: `${msg.id}-${Date.now()}`,
        chatId: msg.chat_id,
        senderName: msg.sender.nickname || msg.sender.username,
        senderAvatar: msg.sender.avatar,
        content: msg.content,
        createdAt: Date.now(),
      };

      toastQueueRef.current.push(newToast);
      if (!toastVisibleRef.current) {
        showNextToast();
      }
    });

    return unsub;
  }, [segments, globalParams.chatId, onNewChatMessage, showNextToast]);

  useEffect(() => {
    if (!navState?.key || isRestoring) return;

    const inAuthGroup = segments[0] === "(auth)";

    if (!isLoggedIn && !inAuthGroup) {
      router.replace("/(auth)/login");
    } else if (isLoggedIn && inAuthGroup) {
      router.replace("/(tabs)");
    }
  }, [isLoggedIn, isRestoring, segments, navState?.key]);

  return (
    <View
      style={[
        styles.container,
        {
          paddingTop: insets.top,
          paddingBottom: insets.bottom,
          backgroundColor: colors.background,
        },
      ]}
    >
      <StatusBar
        barStyle={colors.statusBar as "light-content" | "dark-content"}
      />
      <Stack screenOptions={{ headerShown: false }}>
        <Stack.Screen name="(auth)" />
        <Stack.Screen name="(tabs)" />
        <Stack.Screen name="recruitment-edit" />
        <Stack.Screen name="recruitment-detail" />
        <Stack.Screen name="chat-room" />
        <Stack.Screen name="personal-page" />
        <Stack.Screen name="personal-page-edit" />
        <Stack.Screen name="settings" />
        <Stack.Screen name="feedback" />
        <Stack.Screen name="report" />
      </Stack>
      <MessageToast
        toast={toast}
        onDismiss={() => showNextToast()}
      />
      <TestMessageButton />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
});