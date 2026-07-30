import { useCallback, useEffect, useRef, useState } from "react";
import { ActivityIndicator, Alert, FlatList, Platform, StyleSheet, Text, TextInput, TouchableOpacity, View } from "react-native";
import { ApiError, forceTakeDownRecruitment, getAdminRecruitments, RecruitmentData, RecruitmentStatusDto } from "../../api/api";
import AdminScreen from "../../components/admin-screen";
import MediaImage from "../../components/media-image";
import { useTheme } from "../../contexts/theme-context";

const filters = [{ label: "全部", value: undefined }, { label: "招募中", value: RecruitmentStatusDto.Open }, { label: "已关闭", value: RecruitmentStatusDto.Closed }, { label: "已下架", value: RecruitmentStatusDto.Deleted }];

export default function AdminRecruitmentsScreen() {
  const { colors } = useTheme();
  const [input, setInput] = useState("");
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState<RecruitmentStatusDto | undefined>();
  const [items, setItems] = useState<RecruitmentData[]>([]);
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
      const result = await getAdminRecruitments({ query, status, page: nextPage, pageSize: 20 });
      if (generation !== generationRef.current) return;
      setItems((previous) => {
        const base = nextPage === 1 ? [] : previous;
        const ids = new Set(base.map((item) => item.id));
        return [...base, ...result.items.filter((item) => !ids.has(item.id) && Boolean(ids.add(item.id)))];
      });
      setPage(result.page); setHasMore(result.page < result.totalPages);
    } catch (reason) { if (generation === generationRef.current) setError(reason instanceof Error ? reason.message : "加载招募失败"); }
    finally { if (generation === generationRef.current) { inFlightRef.current = false; setLoading(false); setLoadingMore(false); } }
  }, [query, status]);
  useEffect(() => { const generation = ++generationRef.current; inFlightRef.current = false; void load(1, generation); }, [load]);

  const takeDown = (item: RecruitmentData) => {
    const perform = async () => {
      try {
        await forceTakeDownRecruitment(item.id);
        setItems((old) => old.map((value) => value.id === item.id ? { ...value, status: "已删除" } : value));
      } catch (reason) { Alert.alert("下架失败", reason instanceof ApiError ? reason.message : reason instanceof Error ? reason.message : "请稍后重试"); }
    };
    const message = `确认强制下架“${item.title}”？此操作会将其标记为已删除。`;
    if (Platform.OS === "web") {
      if (globalThis.confirm(message)) void perform();
      return;
    }
    Alert.alert("强制下架", message, [
      { text: "取消", style: "cancel" },
      { text: "强制下架", style: "destructive", onPress: perform },
    ]);
  };

  return <AdminScreen title="招募审核">
    <View style={styles.toolbar}><View style={styles.searchRow}><TextInput value={input} onChangeText={setInput} onSubmitEditing={() => setQuery(input.trim())} placeholder="标题、游戏或发布者" placeholderTextColor={colors.textTertiary} style={[styles.input, { backgroundColor: colors.card, borderColor: colors.inputBorder, color: colors.text }]} /><TouchableOpacity onPress={() => setQuery(input.trim())} style={[styles.search, { backgroundColor: colors.primary }]}><Text style={{ color: colors.primaryText }}>搜索</Text></TouchableOpacity></View><View style={styles.filters}>{filters.map((filter) => <TouchableOpacity key={filter.label} onPress={() => setStatus(filter.value)} style={[styles.filter, { backgroundColor: status === filter.value ? colors.primary : colors.filterInactive }]}><Text style={{ color: status === filter.value ? colors.primaryText : colors.filterTextInactive }}>{filter.label}</Text></TouchableOpacity>)}</View>{!!error && <Text style={{ color: colors.danger }}>{error}</Text>}</View>
    {loading ? <ActivityIndicator style={styles.loader} color={colors.primary} /> : <FlatList data={items} keyExtractor={(item) => String(item.id)} contentContainerStyle={styles.list} onEndReached={() => hasMore && load(page + 1)} onEndReachedThreshold={0.4} ListFooterComponent={loadingMore ? <ActivityIndicator color={colors.primary} /> : null} renderItem={({ item }) => <View style={[styles.card, { backgroundColor: colors.card, borderColor: colors.borderLight }]}><View style={styles.top}><MediaImage uri={item.gameIcon} style={styles.icon} /><View style={styles.info}><Text style={[styles.title, { color: colors.text }]} numberOfLines={2}>{item.title}</Text><Text style={{ color: colors.textSecondary }}>{item.gameName} · {item.publisher.nickname || item.publisher.username}</Text></View><Text style={{ color: item.status === "招募中" ? colors.success : item.status === "已删除" ? colors.danger : colors.textTertiary }}>{item.status}</Text></View><View style={styles.bottom}><Text style={{ color: colors.textTertiary }}>截止 {new Date(item.expiredAt).toLocaleString("zh-CN")}</Text>{item.status !== "已删除" && <TouchableOpacity onPress={() => takeDown(item)} style={[styles.danger, { borderColor: colors.danger }]}><Text style={{ color: colors.danger }}>强制下架</Text></TouchableOpacity>}</View></View>} />}
  </AdminScreen>;
}

const styles = StyleSheet.create({
  toolbar: { padding: 14, gap: 10 }, searchRow: { flexDirection: "row", gap: 8 }, input: { flex: 1, height: 42, borderWidth: 1, borderRadius: 10, paddingHorizontal: 12 }, search: { justifyContent: "center", borderRadius: 10, paddingHorizontal: 18 }, filters: { flexDirection: "row", flexWrap: "wrap", gap: 8 }, filter: { borderRadius: 16, paddingHorizontal: 12, paddingVertical: 7 }, loader: { marginTop: 60 }, list: { padding: 14, paddingTop: 0 }, card: { borderWidth: 1, borderRadius: 12, padding: 13, marginBottom: 10 }, top: { flexDirection: "row", alignItems: "center" }, icon: { width: 42, height: 42, borderRadius: 9, marginRight: 10 }, info: { flex: 1 }, title: { fontSize: 16, fontWeight: "700", marginBottom: 3 }, bottom: { marginTop: 12, flexDirection: "row", alignItems: "center", justifyContent: "space-between", gap: 8 }, danger: { borderWidth: 1, borderRadius: 8, paddingHorizontal: 11, paddingVertical: 6 },
});
