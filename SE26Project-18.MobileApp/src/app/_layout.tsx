import { Stack } from "expo-router";
import { ActivityIndicator, StatusBar, StyleSheet, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { FEATURE_FLAGS } from "../constants/feature-flags";
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
  return <RootLayoutNav />;
}

function RootLayoutNav() {
  const { initializing, isLoggedIn } = useAuth();
  const { colors } = useTheme();
  const insets = useSafeAreaInsets();

  if (initializing) {
    return (
      <View style={[styles.loading, { backgroundColor: colors.background }]}>
        <ActivityIndicator color={colors.primary} size="large" />
      </View>
    );
  }

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
        <Stack.Protected guard={!isLoggedIn}>
          <Stack.Screen name="(auth)" />
        </Stack.Protected>
        <Stack.Protected guard={isLoggedIn}>
          <Stack.Screen name="(tabs)" />
          <Stack.Screen name="admin" />
          <Stack.Screen name="recruitment-edit" />
          <Stack.Screen name="recruitment-detail" />
          <Stack.Screen name="chat-room" />
          <Stack.Screen name="personal-page" />
          <Stack.Screen name="personal-page-edit" />
          <Stack.Screen name="settings" />
        </Stack.Protected>
        <Stack.Protected guard={isLoggedIn && FEATURE_FLAGS.feedback}>
          <Stack.Screen name="feedback" />
        </Stack.Protected>
      </Stack>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  loading: { flex: 1, alignItems: "center", justifyContent: "center" },
});
