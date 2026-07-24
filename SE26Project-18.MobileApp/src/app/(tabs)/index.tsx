import { useFocusEffect, useRouter } from "expo-router";
import { useState, useEffect, useCallback } from "react";
import {
    ActivityIndicator,
    FlatList,
    ScrollView,
    StyleSheet,
    Text,
    TouchableOpacity,
    View,
} from "react-native";
import {
    GameTag,
    getGameTags,
    getRecruitments,
    getRecruitmentTags,
    RecruitmentData,
    RecruitmentTag,
} from "../../api/api";
import GameSearchModal from "../../components/game-search-modal";
import RecruitmentViewCard from "../../components/recruitment-view-card";
import { useTheme } from "../../contexts/theme-context";

export default function HomeScreen() {
  const router = useRouter();
  const { colors } = useTheme();
  const [searchText, setSearchText] = useState("");
  const [selectedGameTags, setSelectedGameTags] = useState<number[]>([]);
  const [selectedRecruitmentTags, setSelectedRecruitmentTags] = useState<
    number[]
  >([]);
  const [recruitmentTagsExpanded, setRecruitmentTagsExpanded] = useState(false);
  const [searchModalVisible, setSearchModalVisible] = useState(false);
  const [recruitments, setRecruitments] = useState<RecruitmentData[]>([]);
  const [gameTags, setGameTags] = useState<GameTag[]>([]);
  const [recruitmentTags, setRecruitmentTags] = useState<RecruitmentTag[]>([]);
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    setLoading(true);
    const [recruitmentsData, gameTagsData, recruitmentTagsData] =
      await Promise.all([
        getRecruitments(),
        getGameTags(),
        getRecruitmentTags(),
      ]);
    setRecruitments(recruitmentsData);
    setGameTags(gameTagsData);
    setRecruitmentTags(recruitmentTagsData);
    setLoading(false);
  };

  useEffect(() => {
    loadData();
  }, []);

  useFocusEffect(
    useCallback(() => {
      loadData();
    }, []),
  );

  const openCard = (item: RecruitmentData) => {
    router.push({
      pathname: '/recruitment-detail' as any,
      params: { recruitmentId: item.id.toString() }
    });
  };

  const loadRecruitments = async (
    gameName: string = searchText,
    gTags: number[] = selectedGameTags,
    rTags: number[] = selectedRecruitmentTags,
  ) => {
    const data = await getRecruitments(gameName, gTags, rTags);
    setRecruitments(data);
  };

  const handleSearchClose = async (text: string) => {
    setSearchText(text);
    await loadRecruitments(text, selectedGameTags, selectedRecruitmentTags);
    setSearchModalVisible(false);
  };

  const toggleGameTag = async (tagId: number) => {
    const newGameTags = selectedGameTags.includes(tagId)
      ? selectedGameTags.filter((id) => id !== tagId)
      : [...selectedGameTags, tagId];
    setSelectedGameTags(newGameTags);
    await loadRecruitments(searchText, newGameTags, selectedRecruitmentTags);
  };

  const toggleRecruitmentTag = async (tagId: number) => {
    const newRecruitmentTags = selectedRecruitmentTags.includes(tagId)
      ? selectedRecruitmentTags.filter((id) => id !== tagId)
      : [...selectedRecruitmentTags, tagId];
    setSelectedRecruitmentTags(newRecruitmentTags);
    await loadRecruitments(searchText, selectedGameTags, newRecruitmentTags);
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      {/* Header */}
      <View style={[styles.header, { backgroundColor: colors.card }]}>
        <TouchableOpacity
          style={[
            styles.searchTouchable,
            { backgroundColor: colors.searchBackground },
          ]}
          onPress={() => setSearchModalVisible(true)}
        >
          <Text
            style={[
              styles.searchPlaceholder,
              { color: searchText ? colors.inputText : colors.textTertiary },
            ]}
          >
            {searchText || "搜索游戏..."}
          </Text>
        </TouchableOpacity>

        {recruitmentTagsExpanded && (
          <>
            <View style={styles.tagContainer}>
              <Text
                style={[
                  styles.tagSectionTitle,
                  { color: colors.textSecondary },
                ]}
              >
                游戏标签
              </Text>
              <ScrollView
                horizontal
                showsHorizontalScrollIndicator={false}
                style={styles.tagScroll}
              >
                {gameTags.map((tag) => (
                  <TouchableOpacity
                    key={tag.id}
                    style={[
                      styles.tag,
                      selectedGameTags.includes(tag.id)
                        ? { backgroundColor: colors.primaryLight }
                        : { backgroundColor: colors.tagBackground },
                    ]}
                    onPress={() => toggleGameTag(tag.id)}
                  >
                    <Text
                      style={[
                        styles.tagText,
                        selectedGameTags.includes(tag.id)
                          ? { color: colors.primary }
                          : { color: colors.tagText },
                      ]}
                    >
                      {tag.name}
                    </Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            </View>
            <View style={styles.tagContainer}>
              <Text
                style={[
                  styles.tagSectionTitle,
                  { color: colors.textSecondary },
                ]}
              >
                招募标签
              </Text>
              <ScrollView
                horizontal
                showsHorizontalScrollIndicator={false}
                style={styles.tagScroll}
              >
                {recruitmentTags.map((tag) => (
                  <TouchableOpacity
                    key={tag.id}
                    style={[
                      styles.tag,
                      selectedRecruitmentTags.includes(tag.id)
                        ? { backgroundColor: colors.primaryLight }
                        : { backgroundColor: colors.tagBackground },
                    ]}
                    onPress={() => toggleRecruitmentTag(tag.id)}
                  >
                    <Text
                      style={[
                        styles.tagText,
                        selectedRecruitmentTags.includes(tag.id)
                          ? { color: colors.primary }
                          : { color: colors.tagText },
                      ]}
                    >
                      {tag.name}
                    </Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
            </View>
          </>
        )}
        <TouchableOpacity
          style={styles.expandButton}
          onPress={() => setRecruitmentTagsExpanded(!recruitmentTagsExpanded)}
        >
          <Text style={[styles.expandButtonText, { color: colors.primary }]}>
            {recruitmentTagsExpanded ? "收起标签 ▲" : "展开标签 ▼"}
          </Text>
        </TouchableOpacity>
      </View>

      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={colors.primary} />
        </View>
      ) : (
        <FlatList
          data={recruitments}
          keyExtractor={(item) => String(item.id)}
          renderItem={({ item }) => (
            <RecruitmentViewCard recruitment={item} onPress={openCard} />
          )}
          contentContainerStyle={styles.listContent}
        />
      )}

      <GameSearchModal
        visible={searchModalVisible}
        initialText={searchText}
        onClose={handleSearchClose}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  header: { paddingHorizontal: 16, paddingTop: 10, paddingBottom: 12 },
  searchBar: { paddingHorizontal: 16, paddingVertical: 10 },
  searchTouchable: {
    height: 40,
    borderRadius: 20,
    paddingHorizontal: 16,
    justifyContent: "center",
  },
  searchPlaceholder: { fontSize: 15 },
  tagSection: { paddingBottom: 8, paddingHorizontal: 16, borderBottomWidth: 1 },
  tagContainer: { marginTop: 10 },
  tagSectionTitle: { fontSize: 12, marginBottom: 6 },
  tagScroll: { flexDirection: "row" },
  tag: {
    paddingHorizontal: 14,
    paddingVertical: 6,
    borderRadius: 16,
    marginRight: 8,
  },
  tagText: { fontSize: 13 },
  expandButton: { paddingHorizontal: 10, paddingVertical: 4, marginTop: 4 },
  expandButtonText: { fontSize: 13 },
  listContent: { padding: 16 },
  loadingContainer: { flex: 1, justifyContent: "center", alignItems: "center" },
});