import { Stack } from "expo-router";

export default function SettingsLayout() {
  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Screen name="index" />
      <Stack.Screen name="notification" />
      <Stack.Screen name="privacy" />
      <Stack.Screen name="general" />
      <Stack.Screen name="about" />
      <Stack.Screen name="change-password" />
    </Stack>
  );
}