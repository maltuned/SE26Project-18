import { useRouter } from "expo-router";
import { StyleSheet, Switch, Text, TouchableOpacity, View } from "react-native";
import { useTheme } from "../../contexts/theme-context";
import { useNotification } from "../../contexts/notification-context";

export default function NotificationSettingsScreen() {
  const router = useRouter();
  const { colors } = useTheme();
  const { pushEnabled, setPushEnabled } = useNotification();

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <TouchableOpacity style={styles.back} onPress={() => router.back()}>
        <Text style={[styles.backText, { color: colors.primary }]}>← 返回</Text>
      </TouchableOpacity>
      <Text style={[styles.header, { color: colors.text }]}>通知设置</Text>
      <View style={[styles.list, { backgroundColor: colors.card }]}>
        <View style={[styles.item, { borderBottomColor: colors.border }]}>
          <View style={styles.itemInfo}>
            <Text style={[styles.itemText, { color: colors.text }]}>
              聊天消息推送
            </Text>
          </View>
          <Switch
            value={pushEnabled}
            onValueChange={setPushEnabled}
            trackColor={{ false: colors.disabled, true: colors.primary }}
            thumbColor="#fff"
          />
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
  itemInfo: { flex: 1, marginRight: 12 },
  itemText: { fontSize: 16 },
  itemDesc: { fontSize: 13, marginTop: 2 },
});