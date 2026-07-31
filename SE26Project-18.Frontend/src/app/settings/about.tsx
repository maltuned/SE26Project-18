import { useRouter } from "expo-router";
import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { useTheme } from "../../contexts/theme-context";

const APP_VERSION = "1.0.0";

export default function AboutScreen() {
  const router = useRouter();
  const { colors } = useTheme();

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <TouchableOpacity style={styles.back} onPress={() => router.back()}>
        <Text style={[styles.backText, { color: colors.primary }]}>← 返回</Text>
      </TouchableOpacity>
      <Text style={[styles.header, { color: colors.text }]}>关于我们</Text>
      <View style={[styles.list, { backgroundColor: colors.card }]}>
        <View style={[styles.item, { borderBottomColor: colors.border }]}>
          <Text style={[styles.itemText, { color: colors.text }]}>
            应用版本
          </Text>
          <Text style={[styles.version, { color: colors.textTertiary }]}>
            v{APP_VERSION}
          </Text>
        </View>
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
    justifyContent: "space-between",
    paddingHorizontal: 16,
    paddingVertical: 14,
    borderBottomWidth: 1,
  },
  itemText: { fontSize: 16 },
  version: { fontSize: 14 },
});