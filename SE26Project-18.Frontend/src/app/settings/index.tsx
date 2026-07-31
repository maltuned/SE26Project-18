import { useRouter } from "expo-router";
import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { useTheme } from "../../contexts/theme-context";

const SETTING_ITEMS = [
  { key: "notification", label: "通知设置" },
  { key: "privacy", label: "隐私设置" },
  { key: "general", label: "通用设置" },
  { key: "about", label: "关于我们" },
];

export default function SettingsScreen() {
  const router = useRouter();
  const { colors } = useTheme();

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <TouchableOpacity style={styles.back} onPress={() => router.back()}>
        <Text style={[styles.backText, { color: colors.primary }]}>← 返回</Text>
      </TouchableOpacity>
      <Text style={[styles.header, { color: colors.text }]}>设置</Text>
      <View style={[styles.list, { backgroundColor: colors.card }]}>
        {SETTING_ITEMS.map((item) => (
          <TouchableOpacity
            key={item.key}
            style={[styles.item, { borderBottomColor: colors.border }]}
            onPress={() => router.push(`/settings/${item.key}` as any)}
          >
            <Text style={[styles.itemText, { color: colors.text }]}>
              {item.label}
            </Text>
            <Text style={[styles.arrow, { color: colors.arrow }]}>›</Text>
          </TouchableOpacity>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  back: {
    position: "absolute",
    top: 0,
    left: 0,
    paddingHorizontal: 16,
    paddingVertical: 12,
    zIndex: 1,
  },
  backText: { fontSize: 16 },
  header: {
    fontSize: 20,
    fontWeight: "bold",
    textAlign: "center",
    paddingVertical: 14,
  },
  list: { marginTop: 12 },
  item: {
    flexDirection: "row",
    alignItems: "center",
    paddingHorizontal: 16,
    paddingVertical: 14,
    borderBottomWidth: 1,
  },
  itemText: { flex: 1, fontSize: 16 },
  arrow: { fontSize: 20 },
});