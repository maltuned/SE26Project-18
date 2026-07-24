import {
  Stack,
  useRootNavigationState,
  useRouter,
  useSegments,
} from "expo-router";
import { useEffect } from "react";
import { StatusBar, StyleSheet, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { initTagCaches } from "../api/api";
import { AuthProvider, useAuth } from "../contexts/auth-context";
import { ThemeProvider, useTheme } from "../contexts/theme-context";

export default function RootLayout() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <RootLayoutMain />
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
  const { isLoggedIn } = useAuth();
  const { colors } = useTheme();
  const segments = useSegments();
  const router = useRouter();
  const navState = useRootNavigationState();
  const insets = useSafeAreaInsets();

  useEffect(() => {
    if (!navState?.key) return;

    const inAuthGroup = segments[0] === "(auth)";

    if (!isLoggedIn && !inAuthGroup) {
      router.replace("/(auth)/login");
    } else if (isLoggedIn && inAuthGroup) {
      router.replace("/(tabs)");
    }
  }, [isLoggedIn, segments, navState?.key]);

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
      </Stack>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
});