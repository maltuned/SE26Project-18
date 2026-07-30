import { useCallback, useEffect, useRef, useState } from "react";
import { ActivityIndicator, Alert, FlatList, Platform, StyleSheet, Text, TextInput, TouchableOpacity, View } from "react-native";
import { ApiError, getAdminUsers, resolveMediaUrl, setUserSuspension, UserResponse, UserStatusDto } from "../../api/api";
import AdminScreen from "../../components/admin-screen";
import MediaImage from "../../components/media-image";
import { useAuth } from "../../contexts/auth-context";
import { useTheme } from "../../contexts/theme-context";

const statuses = [{ label: "全部状态", value: undefined }, { label: "在线", value: UserStatusDto.Online }, { label: "离线", value: UserStatusDto.Offline }, { label: "已封禁", value: UserStatusDto.Suspended }];
const roles = [{ label: "全部角色", value: undefined }, { label: "管理员", value: true }, { label: "普通用户", value: false }];
const statusText = ["在线", "离线", "已封禁"];

export default function AdminUsersScreen() {
  const { colors } = useTheme();
  const { userId } = useAuth();
  const [input, setInput] = useState("");
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState<UserStatusDto | undefined>();
  const [isAdmin, setIsAdmin] = useState<boolean | undefined>();
  const [users, setUsers] = useState<UserResponse[]>([]);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState("");
  const generationRef = useRef(0);
  const inFlightRef = useRef(false);

  const load = useCallback(async (nextPage = 1, generation = generationRef.current) => {
    if (inFlightRef.current) return;
    inFlightRef.current = true;
    if (nextPage > 1) setLoadingMore(true); else setLoading(true);
    setError("");
    try {
      const result = await getAdminUsers({ query, status, isAdmin, page: nextPage, pageSize: 20 });
      if (generation !== generationRef.current) return;
      setUsers((previous) => {
        const base = nextPage === 1 ? [] : previous;
        const ids = new Set(base.map((user) => user.id));
        return [...base, ...result.items.filter((user) => !ids.has(user.id) && Boolean(ids.add(user.id)))];
      });
      setPage(result.page);
      setHasMore(result.page < result.totalPages);
    } catch (reason) {
      if (generation === generationRef.current) setError(reason instanceof Error ? reason.message : "加载用户失败");
    } finally {
      if (generation === generationRef.current) {
        inFlightRef.current = false;
        setLoading(false); setLoadingMore(false);
      }
    }
  }, [query, status, isAdmin]);

  useEffect(() => {
    const generation = ++generationRef.current;
    inFlightRef.current = false;
    void load(1, generation);
  }, [load]);

  const changeSuspension = (user: UserResponse) => {
    const suspended = user.status !== UserStatusDto.Suspended;
    const perform = async () => {
      try {
        const updated = await setUserSuspension(user.id, suspended);
        setUsers((items) => items.map((item) => item.id === updated.id ? updated : item));
      } catch (reason) {
        Alert.alert("操作失败", reason instanceof ApiError ? reason.message : reason instanceof Error ? reason.message : "请稍后重试");
      }
    };
    const message = `确认${suspended ? "封禁" : "解除封禁"} ${user.nickname || user.username}？`;
    if (Platform.OS === "web") {
      if (globalThis.confirm(message)) void perform();
      return;
    }
    Alert.alert(suspended ? "封禁用户" : "解除封禁", message, [
      { text: "取消", style: "cancel" },
      { text: suspended ? "封禁" : "解除", style: suspended ? "destructive" : "default", onPress: perform },
    ]);
  };

  return (
    <AdminScreen title="用户管理">
      <View style={styles.toolbar}>
        <View style={styles.searchRow}>
          <TextInput value={input} onChangeText={setInput} onSubmitEditing={() => setQuery(input.trim())} placeholder="用户名或昵称" placeholderTextColor={colors.textTertiary} style={[styles.input, { backgroundColor: colors.card, borderColor: colors.inputBorder, color: colors.text }]} />
          <TouchableOpacity style={[styles.searchButton, { backgroundColor: colors.primary }]} onPress={() => setQuery(input.trim())}><Text style={{ color: colors.primaryText }}>搜索</Text></TouchableOpacity>
        </View>
        <View style={styles.chips}>{statuses.map((item) => <TouchableOpacity key={item.label} onPress={() => setStatus(item.value)} style={[styles.chip, { backgroundColor: status === item.value ? colors.primary : colors.filterInactive }]}><Text style={{ color: status === item.value ? colors.primaryText : colors.filterTextInactive }}>{item.label}</Text></TouchableOpacity>)}</View>
        <View style={styles.chips}>{roles.map((item) => <TouchableOpacity key={item.label} onPress={() => setIsAdmin(item.value)} style={[styles.chip, { backgroundColor: isAdmin === item.value ? colors.primary : colors.filterInactive }]}><Text style={{ color: isAdmin === item.value ? colors.primaryText : colors.filterTextInactive }}>{item.label}</Text></TouchableOpacity>)}</View>
        {!!error && <Text style={{ color: colors.danger }}>{error}</Text>}
      </View>
      {loading ? <ActivityIndicator style={styles.loader} color={colors.primary} /> : (
        <FlatList data={users} keyExtractor={(item) => String(item.id)} contentContainerStyle={styles.list} onEndReached={() => hasMore && load(page + 1)} onEndReachedThreshold={0.4} ListFooterComponent={loadingMore ? <ActivityIndicator color={colors.primary} /> : null} renderItem={({ item }) => {
          const actionable = item.id !== userId && !item.isAdmin;
          return <View style={[styles.card, { backgroundColor: colors.card, borderColor: colors.borderLight }]}>
            <MediaImage uri={resolveMediaUrl(item.avatarUrl)} style={styles.avatar} />
            <View style={styles.info}><Text style={[styles.name, { color: colors.text }]}>{item.nickname || item.username}</Text><Text style={{ color: colors.textSecondary }}>@{item.username}</Text><View style={styles.meta}><Text style={[styles.badge, { color: item.status === UserStatusDto.Suspended ? colors.danger : colors.success }]}>{statusText[item.status]}</Text><Text style={[styles.badge, { color: item.isAdmin ? colors.warning : colors.textSecondary }]}>{item.isAdmin ? "管理员" : "普通用户"}</Text></View></View>
            {actionable && <TouchableOpacity onPress={() => changeSuspension(item)} style={[styles.action, { borderColor: item.status === UserStatusDto.Suspended ? colors.success : colors.danger }]}><Text style={{ color: item.status === UserStatusDto.Suspended ? colors.success : colors.danger }}>{item.status === UserStatusDto.Suspended ? "解除" : "封禁"}</Text></TouchableOpacity>}
          </View>;
        }} />
      )}
    </AdminScreen>
  );
}

const styles = StyleSheet.create({
  toolbar: { padding: 14, gap: 10 }, searchRow: { flexDirection: "row", gap: 8 }, input: { flex: 1, height: 42, borderWidth: 1, borderRadius: 10, paddingHorizontal: 12 },
  searchButton: { justifyContent: "center", paddingHorizontal: 18, borderRadius: 10 }, chips: { flexDirection: "row", flexWrap: "wrap", gap: 8 }, chip: { paddingHorizontal: 11, paddingVertical: 7, borderRadius: 16 },
  loader: { marginTop: 50 }, list: { padding: 14, paddingTop: 0 }, card: { borderWidth: 1, borderRadius: 12, padding: 12, marginBottom: 10, flexDirection: "row", alignItems: "center" },
  avatar: { width: 48, height: 48, borderRadius: 24, marginRight: 12 }, info: { flex: 1 }, name: { fontSize: 16, fontWeight: "700" }, meta: { flexDirection: "row", gap: 10, marginTop: 5 }, badge: { fontSize: 12, fontWeight: "600" }, action: { borderWidth: 1, borderRadius: 8, paddingHorizontal: 12, paddingVertical: 7 },
});
