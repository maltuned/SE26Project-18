import { useRouter } from "expo-router";
import { useEffect, useState } from "react";
import { StyleSheet, Switch, Text, TouchableOpacity, View } from "react-native";
import { useTheme } from "../../contexts/theme-context";
import { useAuth } from "../../contexts/auth-context";
import { updateUserSettings } from "../../api/api";

export default function PrivacySettingsScreen() {
  const router = useRouter();
  const { colors } = useTheme();
  const { currentUser, refreshUser } = useAuth();
  const [profileVisible, setProfileVisible] = useState(true);

  useEffect(() => {
    if (currentUser?.settings) {
      setProfileVisible(currentUser.settings.profileVisible);
    }
  }, [currentUser?.settings]);

  const handleToggle = async (v: boolean) => {
    setProfileVisible(v);
    try {
      await updateUserSettings({
        pushEnabled: currentUser?.settings?.pushEnabled ?? true,
        profileVisible: v,
        darkMode: currentUser?.settings?.darkMode ?? false,
      });
      refreshUser();
    } catch {
      setProfileVisible(!v);
    }
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <TouchableOpacity style={styles.back} onPress={() => router.back()}>
        <Text style={[styles.backText, { color: colors.primary }]}>← 返回</Text>
      </TouchableOpacity>
      <Text style={[styles.header, { color: colors.text }]}>隐私设置</Text>
      <View style={[styles.list, { backgroundColor: colors.card }]}>
        <View style={[styles.item, { borderBottomColor: colors.border }]}>
          <View style={styles.itemInfo}>
            <Text style={[styles.itemText, { color: colors.text }]}>
              允许他人查看个人空间
            </Text>
          </View>
          <Switch
            value={profileVisible}
            onValueChange={handleToggle}
            trackColor={{ false: colors.disabled, true: colors.primary }}
            thumbColor="#fff"
          />
        </View>
        <TouchableOpacity
          style={[styles.item, { borderBottomColor: colors.border }]}
          onPress={() => router.push("/settings/change-password")}
        >
          <Text style={[styles.itemText, { color: colors.text }]}>修改密码</Text>
          <Text style={[styles.arrow, { color: colors.arrow }]}>›</Text>
        </TouchableOpacity>
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
  arrow: { fontSize: 20 },
});