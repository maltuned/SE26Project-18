import { useLocalSearchParams, useRouter } from "expo-router";
import { useEffect, useMemo, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  Image,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from "react-native";
import {
  GameInfo,
  RecruitmentTag,
  getGames,
  getRecruitmentTags,
  saveRecruitment,
} from "../api/api";
import GameInfoModal from "../components/game-info-modal";
import GameSearchModal from "../components/game-search-modal";
import { useAuth } from "../contexts/auth-context";
import { useTheme } from "../contexts/theme-context";

export default function RecruitmentEditScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ data?: string }>();
  const { colors } = useTheme();
  const { userId } = useAuth();

  const editRecruitment = useMemo(() => {
    try {
      if (params.data) {
        return JSON.parse(decodeURIComponent(params.data));
      }
    } catch {}
    return undefined;
  }, [params.data]);

  const [gameName, setGameName] = useState(editRecruitment?.gameName || "");
  const [title, setTitle] = useState(editRecruitment?.title || "");
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>(
    editRecruitment?.recruitmentTags?.map((tag: RecruitmentTag) => tag.id) ||
      [],
  );
  const [description, setDescription] = useState(
    editRecruitment?.description || "",
  );
  const [selectedGame, setSelectedGame] = useState<GameInfo | null>(null);
  const [gameModalVisible, setGameModalVisible] = useState(false);
  const [searchModalVisible, setSearchModalVisible] = useState(false);
  const [tags, setTags] = useState<RecruitmentTag[]>([]);
  const [loading, setLoading] = useState(true);

  const testImage = require("../../assets/images/testImage.png");

  useEffect(() => {
    getRecruitmentTags().then((data) => {
      setTags(data);
      setLoading(false);
    });
  }, []);

  const toggleTag = (tagId: number) => {
    setSelectedTagIds((prev) =>
      prev.includes(tagId)
        ? prev.filter((id) => id !== tagId)
        : [...prev, tagId],
    );
  };

  const handleSearchClose = async (text: string) => {
    setGameName(text);
    setSearchModalVisible(false);
    if (text.trim()) {
      const games = await getGames(text.trim());
      if (games.length > 0) {
        setSelectedGame({
          id: games[0].id,
          name: games[0].name,
          icon: games[0].icon,
          company: "",
          description: "",
          cover: "",
          tags: [],
          createdAt: "",
          updatedAt: "",
        });
      }
    }
  };

  const handlePublish = async () => {
    if (!userId) {
      Alert.alert("提示", "请先登录");
      return;
    }
    if (!title.trim()) {
      Alert.alert("提示", "请输入标题");
      return;
    }

    const now = new Date();
    const oneDayLater = new Date(now.getTime() + 24 * 60 * 60 * 1000);
    const toISOString = (d: Date) =>
      d
        .toISOString()
        .replace("T", " ")
        .replace(/\.\d+Z$/, "");

    try {
      await saveRecruitment({
        id: editRecruitment?.id ?? -1,
        publisherId: userId,
        gameId: selectedGame?.id ?? editRecruitment?.gameId ?? -1,
        title: title.trim(),
        description,
        status: "招募中",
        expiredAt: toISOString(oneDayLater),
        maxParticipants: 5,
        currentParticipants: editRecruitment?.currentParticipants ?? 0,
        tagsId: selectedTagIds,
      });
      router.replace("/(tabs)");
    } catch {
      Alert.alert("错误", "发布失败，请稍后重试");
    }
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.background }]}>
      <View style={[styles.header, { borderBottomColor: colors.headerBorder }]}>
        <TouchableOpacity onPress={() => router.back()}>
          <Text style={[styles.backButton, { color: colors.primary }]}>
            ← 返回
          </Text>
        </TouchableOpacity>
        <Text style={[styles.headerTitle, { color: colors.text }]}>
          编辑招募
        </Text>
        <View style={styles.placeholder} />
      </View>

      <KeyboardAvoidingView
        style={styles.body}
        behavior={Platform.OS === "ios" ? "padding" : undefined}
      >
        <ScrollView
          style={styles.scroll}
          contentContainerStyle={styles.scrollContent}
          keyboardShouldPersistTaps="handled"
        >
          <View style={styles.topRow}>
            <Image source={testImage} style={styles.coverImage} />
            <View style={styles.topRight}>
              <TouchableOpacity
                style={[
                  styles.topInput,
                  { backgroundColor: colors.searchBackground },
                ]}
                onPress={() => setSearchModalVisible(true)}
              >
                <Text
                  style={[
                    styles.inputPlaceholder,
                    {
                      color: gameName ? colors.inputText : colors.textTertiary,
                    },
                  ]}
                >
                  {gameName || "搜索游戏..."}
                </Text>
              </TouchableOpacity>
              <TextInput
                style={[
                  styles.topInput,
                  {
                    backgroundColor: colors.inputBackgroundAlt,
                    color: colors.inputText,
                  },
                ]}
                placeholder="输入标题..."
                placeholderTextColor={colors.textTertiary}
                value={title}
                onChangeText={setTitle}
              />
            </View>
          </View>

          <View style={styles.tagSection}>
            {loading ? (
              <ActivityIndicator size="small" color={colors.primary} />
            ) : (
              tags?.map((tag) => (
                <TouchableOpacity
                  key={tag.id}
                  style={[
                    styles.tag,
                    selectedTagIds.includes(tag.id)
                      ? { backgroundColor: colors.primaryLight }
                      : { backgroundColor: colors.tagBackground },
                  ]}
                  onPress={() => toggleTag(tag.id)}
                >
                  <Text
                    style={[
                      styles.tagText,
                      selectedTagIds.includes(tag.id)
                        ? { color: colors.primary }
                        : { color: colors.tagText },
                    ]}
                  >
                    {tag.name}
                  </Text>
                </TouchableOpacity>
              ))
            )}
          </View>

          <TextInput
            style={[
              styles.descriptionInput,
              {
                backgroundColor: colors.inputBackgroundAlt,
                color: colors.inputText,
              },
            ]}
            placeholder="输入招募详情...（可选）"
            placeholderTextColor={colors.textTertiary}
            value={description}
            onChangeText={setDescription}
            multiline
            textAlignVertical="top"
          />
        </ScrollView>
      </KeyboardAvoidingView>

      <View style={[styles.footer, { borderTopColor: colors.border }]}>
        <TouchableOpacity
          style={[styles.footerButton, { backgroundColor: colors.primary }]}
          onPress={handlePublish}
        >
          <Text style={styles.footerButtonText}>发布</Text>
        </TouchableOpacity>
      </View>

      <GameInfoModal
        visible={gameModalVisible}
        gameId={selectedGame?.id || null}
        onClose={() => setGameModalVisible(false)}
      />

      <GameSearchModal
        visible={searchModalVisible}
        initialText={gameName}
        onClose={handleSearchClose}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  header: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
  },
  backButton: {
    fontSize: 15,
  },
  headerTitle: {
    fontSize: 17,
    fontWeight: "600",
  },
  placeholder: {
    width: 60,
  },
  body: {
    flex: 1,
  },
  scroll: {
    flex: 1,
  },
  scrollContent: {
    padding: 16,
  },
  topRow: {
    flexDirection: "row",
    marginBottom: 16,
  },
  coverImage: {
    width: 80,
    height: 110,
    borderRadius: 8,
    backgroundColor: "#ddd",
    marginRight: 12,
  },
  topRight: {
    flex: 1,
  },
  topInput: {
    height: 44,
    borderRadius: 8,
    paddingHorizontal: 12,
    justifyContent: "center",
    marginBottom: 8,
    fontSize: 15,
  },
  inputPlaceholder: {
    fontSize: 15,
  },
  tagSection: {
    flexDirection: "row",
    flexWrap: "wrap",
    marginBottom: 16,
  },
  tag: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
    marginRight: 8,
    marginBottom: 8,
  },
  tagText: {
    fontSize: 13,
  },
  descriptionInput: {
    minHeight: 150,
    borderRadius: 8,
    padding: 12,
    fontSize: 15,
    lineHeight: 22,
  },
  footer: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderTopWidth: 1,
  },
  footerButton: {
    alignItems: "center",
    paddingVertical: 12,
    borderRadius: 8,
  },
  footerButtonText: {
    color: "#fff",
    fontSize: 16,
    fontWeight: "600",
  },
});