import { useRouter } from "expo-router";
import type { ReactNode } from "react";
import { Platform, StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { useTheme } from "../contexts/theme-context";

export default function AdminScreen({ title, children, action }: { title: string; children: ReactNode; action?: ReactNode }) {
  const router = useRouter();
  const { colors } = useTheme();
  return (
    <View style={[styles.screen, { backgroundColor: colors.surface }]}>
      <View style={[styles.header, { backgroundColor: colors.card, borderBottomColor: colors.borderLight }]}>
        <TouchableOpacity onPress={() => router.back()} style={styles.back}>
          <Text style={{ color: colors.primary }}>← 返回</Text>
        </TouchableOpacity>
        <Text style={[styles.title, { color: colors.text }]}>{title}</Text>
        <View style={styles.action}>{action}</View>
      </View>
      <View style={styles.center}>{children}</View>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1 },
  header: { height: 52, borderBottomWidth: 1, flexDirection: "row", alignItems: "center", paddingHorizontal: 14 },
  back: { width: 70, paddingVertical: 10 },
  title: { flex: 1, textAlign: "center", fontSize: 18, fontWeight: "700" },
  action: { width: 70, alignItems: "flex-end" },
  center: { flex: 1, width: "100%", maxWidth: Platform.OS === "web" ? 920 : undefined, alignSelf: "center" },
});
