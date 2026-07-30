import { Redirect, Stack } from "expo-router";
import { ActivityIndicator, StyleSheet, View } from "react-native";
import { useAuth } from "../../contexts/auth-context";
import { useTheme } from "../../contexts/theme-context";

export default function AdminLayout() {
  const { initializing, currentUser } = useAuth();
  const { colors } = useTheme();

  if (initializing) {
    return <View style={[styles.loading, { backgroundColor: colors.background }]}><ActivityIndicator color={colors.primary} /></View>;
  }
  if (!currentUser) return <Redirect href="/(auth)/login" />;
  if (!currentUser.isAdmin) return <Redirect href="/(tabs)/profile" />;

  return <Stack screenOptions={{ headerShown: false }} />;
}

const styles = StyleSheet.create({ loading: { flex: 1, alignItems: "center", justifyContent: "center" } });
