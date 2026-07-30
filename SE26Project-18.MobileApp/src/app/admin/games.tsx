import { useCallback, useEffect, useRef, useState } from "react";
import { ActivityIndicator, FlatList, StyleSheet, Text, TextInput, TouchableOpacity, View } from "react-native";
import { GameResponse, getAdminGames } from "../../api/api";
import AdminScreen from "../../components/admin-screen";
import MediaImage from "../../components/media-image";
import { useRouter } from "expo-router";
import { useTheme } from "../../contexts/theme-context";

export default function AdminGamesScreen() {
  const router = useRouter();
  const { colors } = useTheme();
  const [input, setInput] = useState("");
  const [query, setQuery] = useState("");
  const [games, setGames] = useState<GameResponse[]>([]);
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
      const result = await getAdminGames({ query, page: nextPage, pageSize: 20 });
      if (generation !== generationRef.current) return;
      setGames((previous) => {
        const base = nextPage === 1 ? [] : previous;
        const ids = new Set(base.map((game) => game.id));
        return [...base, ...result.items.filter((game) => !ids.has(game.id) && Boolean(ids.add(game.id)))];
      });
      setPage(result.page); setHasMore(result.page < result.totalPages);
    } catch (reason) { if (generation === generationRef.current) setError(reason instanceof Error ? reason.message : "加载游戏失败"); }
    finally { if (generation === generationRef.current) { inFlightRef.current = false; setLoading(false); setLoadingMore(false); } }
  }, [query]);
  useEffect(() => { const generation = ++generationRef.current; inFlightRef.current = false; void load(1, generation); }, [load]);

  return <AdminScreen title="游戏管理" action={<TouchableOpacity onPress={() => router.push("/admin/game-edit" as any)}><Text style={{ color: colors.primary, fontWeight: "700" }}>新建</Text></TouchableOpacity>}>
    <View style={styles.toolbar}><View style={styles.searchRow}><TextInput value={input} onChangeText={setInput} onSubmitEditing={() => setQuery(input.trim())} placeholder="搜索游戏" placeholderTextColor={colors.textTertiary} style={[styles.input, { backgroundColor: colors.card, borderColor: colors.inputBorder, color: colors.text }]} /><TouchableOpacity onPress={() => setQuery(input.trim())} style={[styles.searchButton, { backgroundColor: colors.primary }]}><Text style={{ color: colors.primaryText }}>搜索</Text></TouchableOpacity></View>{!!error && <Text style={{ color: colors.danger }}>{error}</Text>}</View>
    {loading ? <ActivityIndicator style={styles.loader} color={colors.primary} /> : <FlatList data={games} keyExtractor={(item) => String(item.id)} contentContainerStyle={styles.list} onEndReached={() => hasMore && load(page + 1)} onEndReachedThreshold={0.4} ListFooterComponent={loadingMore ? <ActivityIndicator color={colors.primary} /> : null} renderItem={({ item }) => <TouchableOpacity style={[styles.card, { backgroundColor: colors.card, borderColor: colors.borderLight }]} onPress={() => router.push(`/admin/game-edit?id=${item.id}` as any)}><MediaImage uri={item.iconUrl} style={styles.icon} /><View style={styles.info}><Text style={[styles.name, { color: colors.text }]}>{item.name}</Text><Text numberOfLines={2} style={{ color: colors.textSecondary }}>{item.description || "暂无简介"}</Text><Text style={[styles.tags, { color: colors.primary }]}>{item.tags.map((tag) => tag.name).join(" · ") || "无标签"}</Text></View><Text style={{ color: colors.arrow, fontSize: 22 }}>›</Text></TouchableOpacity>} />}
  </AdminScreen>;
}

const styles = StyleSheet.create({
  toolbar: { padding: 14, gap: 8 }, searchRow: { flexDirection: "row", gap: 8 }, input: { flex: 1, height: 42, borderWidth: 1, borderRadius: 10, paddingHorizontal: 12 }, searchButton: { justifyContent: "center", borderRadius: 10, paddingHorizontal: 18 }, loader: { marginTop: 50 }, list: { padding: 14, paddingTop: 0 }, card: { flexDirection: "row", alignItems: "center", borderWidth: 1, borderRadius: 12, padding: 12, marginBottom: 10 }, icon: { width: 58, height: 58, borderRadius: 12, marginRight: 12 }, info: { flex: 1 }, name: { fontSize: 17, fontWeight: "700", marginBottom: 3 }, tags: { fontSize: 12, marginTop: 6 },
});
