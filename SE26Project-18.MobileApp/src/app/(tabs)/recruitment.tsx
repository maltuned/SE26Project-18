import { useFocusEffect, useRouter } from "expo-router";
import { useCallback, useRef, useState } from "react";
import {
    ActivityIndicator,
    FlatList,
    StyleSheet,
    Text,
    TouchableOpacity,
    View,
} from "react-native";
import { getRecruitmentsByPublisherPage, RecruitmentData, RecruitmentStatusDto } from "../../api/api";
import RecruitmentManageCard from "../../components/recruitment-manage-card";
import { useAuth } from "../../contexts/auth-context";
import { useTheme } from "../../contexts/theme-context";

const STATUS_FILTERS = [
  { label: "全部", value: undefined },
  { label: "招募中", value: RecruitmentStatusDto.Open },
  { label: "已关闭", value: RecruitmentStatusDto.Closed },
];

export default function RecruitmentListScreen() {
  const router = useRouter();
  const { colors } = useTheme();
  const [statusFilter, setStatusFilter] = useState<RecruitmentStatusDto | undefined>();
  const [recruitments, setRecruitments] = useState<RecruitmentData[]>([]);
  const { userId } = useAuth();
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const generationRef = useRef(0);
  const inFlightRef = useRef(false);

  const loadRecruitments = useCallback(async (generation = generationRef.current) => {
    if (userId) {
      if (inFlightRef.current) return;
      inFlightRef.current = true;
      try {
        const data = await getRecruitmentsByPublisherPage(userId, 1, 20, statusFilter);
        if (generation !== generationRef.current) return;
        const ids = new Set<number>();
        setRecruitments(data.items.filter((item) => !ids.has(item.id) && Boolean(ids.add(item.id))));
        setPage(1);
        setHasMore(data.page < data.totalPages);
      } catch {
        if (generation === generationRef.current) setRecruitments([]);
      } finally {
        if (generation === generationRef.current) inFlightRef.current = false;
      }
    } else {
      setRecruitments([]);
    }
  }, [userId, statusFilter]);

  const loadMore = async () => {
    if (!userId || !hasMore || inFlightRef.current) return;
    const generation = generationRef.current;
    inFlightRef.current = true;
    setLoadingMore(true);
    try {
      const next = await getRecruitmentsByPublisherPage(userId, page + 1, 20, statusFilter);
      if (generation !== generationRef.current) return;
      setRecruitments((previous) => {
        const ids = new Set(previous.map((item) => item.id));
        return [...previous, ...next.items.filter((item) => !ids.has(item.id) && Boolean(ids.add(item.id)))];
      });
      setPage(next.page);
      setHasMore(next.page < next.totalPages);
    } finally {
      if (generation === generationRef.current) {
        inFlightRef.current = false;
        setLoadingMore(false);
      }
    }
  };

  useFocusEffect(
    useCallback(() => {
      const generation = ++generationRef.current;
      inFlightRef.current = false;
      void loadRecruitments(generation);
      return () => {
        generationRef.current += 1;
        inFlightRef.current = false;
      };
    }, [loadRecruitments]),
  );

  const openCard = (item: RecruitmentData) => {
    router.push({
      pathname: '/recruitment-detail' as any,
      params: { recruitmentId: item.id.toString() }
    });
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <View style={[styles.filterRow, { backgroundColor: colors.card }]}>
        {STATUS_FILTERS.map((filter) => (
          <TouchableOpacity
            key={filter.label}
            style={[
              styles.filterButton,
              statusFilter === filter.value
                ? { backgroundColor: colors.primary }
                : { backgroundColor: colors.filterInactive },
            ]}
            onPress={() => setStatusFilter(filter.value)}
          >
            <Text
              style={[
                styles.filterText,
                statusFilter === filter.value
                  ? { color: colors.primaryText }
                  : { color: colors.filterTextInactive },
              ]}
            >
              {filter.label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>
      <FlatList
        data={recruitments}
        keyExtractor={(item) => String(item.id)}
        renderItem={({ item }) => (
          <RecruitmentManageCard recruitment={item} onPress={openCard} />
        )}
        contentContainerStyle={styles.listContent}
        onEndReached={loadMore}
        onEndReachedThreshold={0.4}
        ListFooterComponent={loadingMore ? <ActivityIndicator color={colors.primary} /> : null}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  filterRow: { flexDirection: "row", paddingHorizontal: 16, paddingBottom: 10 },
  filterButton: {
    paddingHorizontal: 16,
    paddingVertical: 6,
    borderRadius: 16,
    marginTop: 10,
    marginRight: 10,
  },
  filterText: { fontSize: 13 },
  listContent: { padding: 16 },
});
