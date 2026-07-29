import { useRouter } from "expo-router";
import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import RemoteImage from "../../components/remote-image";
import { useAuth } from "../../contexts/auth-context";
import { useTheme } from "../../contexts/theme-context";

const MENU_ITEMS = [
  { key: "personal", label: "个人主页", icon: "👤" },
  { key: "settings", label: "设置", icon: "⚙️" },
  { key: "feedback", label: "反馈", icon: "💬" },
];

export default function MoreScreen() {
  const { logout, currentUser } = useAuth();
  const router = useRouter();
  const { colors } = useTheme();

  const handlePress = (key: string) => {
    if (key === "logout") {
      logout();
      router.replace("/(auth)/login");
      return;
    }
    if (key === "personal") {
      router.push("/personal-page");
    } else if (key === "settings") {
      router.push("/settings");
    } else if (key === "feedback") {
      router.push("/feedback");
    }
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <View
        style={[
          styles.profileCard,
          { backgroundColor: colors.profileBackground },
        ]}
      >
        <RemoteImage url={currentUser?.avatar} style={[styles.avatar, { backgroundColor: colors.placeholder }] } />
        <Text style={[styles.nickname, { color: colors.text }]}>
          {currentUser?.nickname || currentUser?.username || "未登录"}
        </Text>
        <Text style={[styles.bio, { color: colors.bioText }]}>
          {currentUser?.signature || "这个人很懒，什么都没写..."}
        </Text>
      </View>
      <View style={[styles.menuList, { backgroundColor: colors.card }]}>
        {MENU_ITEMS.map((item) => (
          <TouchableOpacity
            key={item.key}
            style={[styles.menuItem, { borderBottomColor: colors.menuBorder }]}
            onPress={() => handlePress(item.key)}
          >
            <Text style={styles.menuIcon}>{item.icon}</Text>
            <Text style={[styles.menuLabel, { color: colors.text }]}>
              {item.label}
            </Text>
            <Text style={[styles.menuArrow, { color: colors.arrow }]}>›</Text>
          </TouchableOpacity>
        ))}
        <TouchableOpacity
          style={[styles.menuItem, { borderBottomColor: colors.menuBorder }]}
          onPress={() => handlePress("logout")}
        >
          <Text style={styles.menuIcon}>🚪</Text>
          <Text style={[styles.logoutLabel, { color: colors.danger }]}>
            退出登录
          </Text>
          <Text style={[styles.menuArrow, { color: colors.arrow }]}>›</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  profileCard: { alignItems: "center", paddingVertical: 24, marginBottom: 12 },
  avatar: {
    width: 72,
    height: 72,
    borderRadius: 36,
    justifyContent: "center",
    alignItems: "center",
    marginBottom: 12,
  },
  avatarText: { color: "#fff", fontSize: 28, fontWeight: "bold" },
  nickname: { fontSize: 20, fontWeight: "bold", marginBottom: 4 },
  bio: { fontSize: 14 },
  menuList: {},
  menuItem: {
    flexDirection: "row",
    alignItems: "center",
    paddingHorizontal: 16,
    paddingVertical: 14,
    borderBottomWidth: 1,
  },
  menuIcon: { fontSize: 20, marginRight: 12 },
  menuLabel: { flex: 1, fontSize: 16 },
  menuArrow: { fontSize: 20 },
  logoutLabel: { flex: 1, fontSize: 16 },
});