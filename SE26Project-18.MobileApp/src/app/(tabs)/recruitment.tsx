import { useFocusEffect, useRouter } from "expo-router";
import { useCallback, useEffect, useState } from "react";
import {
    FlatList,
    StyleSheet,
    Text,
    TouchableOpacity,
    View,
} from "react-native";
import { getRecruitmentsByPublisherId, RecruitmentData } from "../../api/api";
import RecruitmentManageCard from "../../components/recruitment-manage-card";
import { useAuth } from "../../contexts/auth-context";
import { useTheme } from "../../contexts/theme-context";

const STATUS_FILTERS = ["全部", "招募中", "已关闭"];

export default function RecruitmentListScreen() {
  const router = useRouter();
  const { colors } = useTheme();
  const [statusFilter, setStatusFilter] = useState("全部");
  const [recruitments, setRecruitments] = useState<RecruitmentData[]>([]);
  const [loading, setLoading] = useState(true);
  const { userId } = useAuth();

  const loadRecruitments = useCallback(() => {
    if (userId) {
      setLoading(true);
      getRecruitmentsByPublisherId(userId).then((data) => {
        setRecruitments(data);
        setLoading(false);
      });
    } else {
      setRecruitments([]);
      setLoading(false);
    }
  }, [userId]);

  useEffect(() => {
    loadRecruitments();
  }, [loadRecruitments]);

  useFocusEffect(
    useCallback(() => {
      loadRecruitments();
    }, [loadRecruitments]),
  );

  const openCard = (item: RecruitmentData) => {
    router.push({
      pathname: '/recruitment-detail' as any,
      params: { recruitmentId: item.id.toString() }
    });
  };

  const filteredRecruitments =
    statusFilter === "全部"
      ? recruitments
      : recruitments.filter((recruitment) =>
          statusFilter === "招募中"
            ? recruitment.status === "招募中"
            : recruitment.status === "已关闭",
        );

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <View style={[styles.filterRow, { backgroundColor: colors.card }]}>
        {STATUS_FILTERS.map((filter) => (
          <TouchableOpacity
            key={filter}
            style={[
              styles.filterButton,
              statusFilter === filter
                ? { backgroundColor: colors.primary }
                : { backgroundColor: colors.filterInactive },
            ]}
            onPress={() => setStatusFilter(filter)}
          >
            <Text
              style={[
                styles.filterText,
                statusFilter === filter
                  ? { color: colors.primaryText }
                  : { color: colors.filterTextInactive },
              ]}
            >
              {filter}
            </Text>
          </TouchableOpacity>
        ))}
      </View>
      <FlatList
        data={filteredRecruitments}
        keyExtractor={(item) => String(item.id)}
        renderItem={({ item }) => (
          <RecruitmentManageCard recruitment={item} onPress={openCard} />
        )}
        contentContainerStyle={styles.listContent}
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