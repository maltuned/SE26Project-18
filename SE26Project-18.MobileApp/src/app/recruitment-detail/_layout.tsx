import { Stack } from "expo-router";

export default function RecruitmentDetailLayout() {
  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Screen name="index" />
      <Stack.Screen name="manage" />
      <Stack.Screen name="view" />
    </Stack>
  );
}