import { useRouter } from "expo-router";
import { useEffect, useRef } from "react";
import { ActivityIndicator, StyleSheet, View } from "react-native";
import { getMe } from "../api/api";
import { tokenStorage } from "../api/tokenStorage";
import { useAuth } from "../contexts/auth-context";
import { useTheme } from "../contexts/theme-context";

export default function SplashScreen() {
  const { isLoggedIn, loggingOut, login } = useAuth();
  const router = useRouter();
  const { colors } = useTheme();
  const checking = useRef(false);
  const checked = useRef(false);

  useEffect(() => {
    if (loggingOut) return;

    if (isLoggedIn) {
      router.replace("/(tabs)");
      return;
    }

    if (checking.current || checked.current) return;

    checking.current = true;

    const checkAuth = async () => {
      try {
        const token = await tokenStorage.getAccessToken();
        if (!token) {
          router.replace("/(auth)/login");
          return;
        }

        const user = await getMe();
        if (user) {
          login(user);
        } else {
          await tokenStorage.clearTokens();
          router.replace("/(auth)/login");
        }
      } catch {
        await tokenStorage.clearTokens();
        router.replace("/(auth)/login");
      } finally {
        checked.current = true;
        checking.current = false;
      }
    };

    checkAuth();
  }, [isLoggedIn, loggingOut]);

  return (
    <View style={[styles.container, { backgroundColor: colors.background }]}>
      <ActivityIndicator size="large" color={colors.primary} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
  },
});