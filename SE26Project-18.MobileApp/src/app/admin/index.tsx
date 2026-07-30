import { useRouter } from "expo-router";
import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import AdminScreen from "../../components/admin-screen";
import { useTheme } from "../../contexts/theme-context";

const sections = [
  { route: "/admin/users", title: "用户管理", detail: "检索账号、角色与封禁状态" },
  { route: "/admin/games", title: "游戏管理", detail: "维护游戏资料与媒体" },
  { route: "/admin/tags", title: "标签目录", detail: "创建三类平台标签" },
  { route: "/admin/recruitments", title: "招募审核", detail: "查看并强制下架招募" },
] as const;

export default function AdminDashboard() {
  const router = useRouter();
  const { colors } = useTheme();
  return (
    <AdminScreen title="管理后台">
      <View style={styles.content}>
        <Text style={[styles.eyebrow, { color: colors.primary }]}>ADMIN CONSOLE</Text>
        <Text style={[styles.heading, { color: colors.text }]}>平台运营</Text>
        <Text style={[styles.subheading, { color: colors.textSecondary }]}>所有操作仍由服务端管理员策略授权。</Text>
        <View style={styles.grid}>
          {sections.map((section, index) => (
            <TouchableOpacity key={section.route} style={[styles.card, { backgroundColor: colors.card, borderColor: colors.borderLight }]} onPress={() => router.push(section.route as any)}>
              <Text style={[styles.number, { color: colors.primary }]}>0{index + 1}</Text>
              <Text style={[styles.cardTitle, { color: colors.text }]}>{section.title}</Text>
              <Text style={{ color: colors.textSecondary }}>{section.detail}</Text>
            </TouchableOpacity>
          ))}
        </View>
      </View>
    </AdminScreen>
  );
}

const styles = StyleSheet.create({
  content: { padding: 20 }, eyebrow: { fontSize: 12, fontWeight: "800", letterSpacing: 2 },
  heading: { fontSize: 30, fontWeight: "800", marginTop: 5 }, subheading: { marginTop: 6, marginBottom: 22 },
  grid: { flexDirection: "row", flexWrap: "wrap", gap: 12 },
  card: { minWidth: 250, flexGrow: 1, flexBasis: "46%", borderWidth: 1, borderRadius: 14, padding: 18 },
  number: { fontSize: 12, fontWeight: "800", marginBottom: 20 }, cardTitle: { fontSize: 19, fontWeight: "700", marginBottom: 6 },
});
