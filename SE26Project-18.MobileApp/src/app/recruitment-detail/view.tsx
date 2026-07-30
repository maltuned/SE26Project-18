import { useLocalSearchParams, useRouter } from "expo-router";
import { useEffect, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import {
  ChatData,
  createResponse,
  getChatByUser,
  getRecruitmentById,
  ResponseData,
  RecruitmentData,
  RecruitmentTag,
  sendGreeting,
} from "../../api/api";
import RecruitmentResponseModal from "../../components/recruitment-response-modal";
import MediaImage from "../../components/media-image";
import { useAuth } from "../../contexts/auth-context";
import { useTheme } from "../../contexts/theme-context";

export default function RecruitmentViewScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ recruitmentId?: string }>();
  const { colors } = useTheme();
  const { userId } = useAuth();

  const [recruitment, setRecruitment] = useState<RecruitmentData | null>(null);
  const [existingChat, setExistingChat] = useState<ChatData | null>(null);
  const [myResponse, setMyResponse] = useState<ResponseData | null>(null);
  const [loading, setLoading] = useState(false);
  const [fetching, setFetching] = useState(true);
  const [showGreetingModal, setShowGreetingModal] = useState(false);

  useEffect(() => {
    const recruitmentId = params.recruitmentId;
    if (recruitmentId) {
      setFetching(true);
      getRecruitmentById(Number(recruitmentId))
        .then((data) => {
          setRecruitment(data);
          setFetching(false);
        })
        .catch(() => {
          setFetching(false);
        });
    } else {
      setFetching(false);
    }
  }, [params.recruitmentId]);

  useEffect(() => {
    if (userId && recruitment?.id) {
      const response = recruitment.responses.find((item) => item.responserId === userId);
      setMyResponse(response || null);
      if (response) {
        getChatByUser(recruitment.publisherId, userId)
          .then(setExistingChat)
          .catch(() => setExistingChat(null));
      }
    }
  }, [userId, recruitment]);

  if (fetching) {
    return (
      <View
        style={[
          styles.container,
          styles.loadingContainer,
          { backgroundColor: colors.surface },
        ]}
      >
        <ActivityIndicator size="large" color={colors.primary} />
      </View>
    );
  }

  if (!recruitment) {
    return (
      <View style={[styles.container, { backgroundColor: colors.surface }]}>
        <TouchableOpacity onPress={() => router.back()}>
          <Text style={[styles.backButton, { color: colors.primary }]}>
            ← 返回
          </Text>
        </TouchableOpacity>
        <Text style={[styles.emptyText, { color: colors.textTertiary }]}>
          暂无信息
        </Text>
      </View>
    );
  }

  const isClosed = recruitment.status === "已关闭";

  const handleChat = async () => {
    if (!userId) return;
    // 已关闭且没回应过：不能操作
    if (isClosed && !myResponse) return;
    // 已回应且被拒绝：不能操作
    if (myResponse && myResponse.responseStatus === "已拒绝") return;
    // 回应过但没有聊天：不能操作
    if (myResponse && !existingChat) return;
    // 有聊天：继续聊天
    if (myResponse && existingChat) {
      router.dismissAll();
      router.push(`/(tabs)/chat`);
      router.push(`/chat-room?chatId=${existingChat.id}`);
      return;
    }
    // 没回应过：弹出打招呼
    setShowGreetingModal(true);
  };

  const handleGreetingSent = async (greeting: string) => {
    if (!userId) return;
    setLoading(true);
    let responseCreated = false;
    try {
      const response = await createResponse(recruitment.id);
      responseCreated = true;
      setMyResponse(response);
      const chat = await getChatByUser(recruitment.publisherId, userId);
      setExistingChat(chat);
      await sendGreeting(chat.id, greeting);
      router.dismissAll();
      router.push(`/(tabs)/chat`);
      router.push(`/chat-room?chatId=${chat.id}`);
    } catch {
      Alert.alert(
        "错误",
        responseCreated
          ? "回应已创建，但问候消息发送失败。你可以进入聊天后重新发送。"
          : "无法发起聊天，请稍后重试",
      );
    } finally {
      setLoading(false);
    }
  };

  // Kept for when reporting is enabled again.
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const handleReport = () => {
    Alert.alert("举报", "举报已提交，我们会尽快处理", [{ text: "确定" }]);
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <View
        style={[
          styles.header,
          {
            backgroundColor: colors.card,
            borderBottomColor: colors.headerBorder,
          },
        ]}
      >
        <TouchableOpacity onPress={() => router.back()}>
          <Text style={[styles.backButton, { color: colors.primary }]}>
            ← 返回
          </Text>
        </TouchableOpacity>
        <Text style={[styles.headerTitle, { color: colors.text }]}>
          招募详情
        </Text>
        <View style={styles.placeholder} />
      </View>

      <ScrollView
        style={styles.body}
        contentContainerStyle={styles.scrollContent}
      >
        <View style={[styles.infoCard, { backgroundColor: colors.card }]}>
          <View style={styles.topRow}>
            <MediaImage uri={recruitment.gameCover || recruitment.gameIcon} style={styles.coverImage} />
            <View style={styles.topRight}>
              <View style={styles.nameRow}>
                <Text
                  style={[styles.gameName, { color: colors.textSecondary }]}
                >
                  {recruitment.gameName}
                </Text>
                {/* Report UI is hidden until the backend supports reports. */}
              </View>
              <Text style={[styles.title, { color: colors.text }]}>
                {recruitment.title}
              </Text>
              <View style={styles.tagsRow}>
                {recruitment.recruitmentTags.map((tag: RecruitmentTag) => (
                  <View
                    key={tag.id}
                    style={[
                      styles.tag,
                      { backgroundColor: colors.primaryLight },
                    ]}
                  >
                    <Text style={[styles.tagText, { color: colors.primary }]}>
                      {tag.name}
                    </Text>
                  </View>
                ))}
              </View>
              <TouchableOpacity
                style={styles.userRow}
                onPress={() =>
                  router.push(
                    `/personal-page?userId=${recruitment.publisherId}`,
                  )
                }
              >
                <MediaImage
                  uri={recruitment.publisher.avatar}
                  style={[styles.avatar, { backgroundColor: colors.primary }]}
                />
                <Text
                  style={[styles.avatarName, { color: colors.textSecondary }]}
                >
                  {recruitment.publisher?.nickname ||
                    recruitment.publisher?.username ||
                    "未知用户"}
                </Text>
              </TouchableOpacity>
            </View>
          </View>
          <Text style={[styles.description, { color: colors.descriptionText }]}>
            {recruitment.description}
          </Text>
        </View>

        <TouchableOpacity
          style={[
            styles.chatButton,
            (isClosed && !myResponse) ||
            (myResponse && myResponse.responseStatus === "已拒绝")
              ? { backgroundColor: colors.textQuaternary }
              : { backgroundColor: colors.primary },
            loading && styles.chatButtonDisabled,
          ]}
          onPress={handleChat}
          disabled={
            loading ||
            (isClosed && !myResponse) ||
            !!(myResponse && myResponse.responseStatus === "已拒绝")
          }
        >
          <Text style={styles.chatButtonText}>
            {loading
              ? "加载中..."
              : isClosed && !myResponse
                ? "已关闭"
                : myResponse && myResponse.responseStatus === "已拒绝"
                  ? "对方已拒绝"
                  : myResponse && !existingChat
                    ? "已回应"
                    : myResponse && existingChat
                      ? "继续聊天"
                      : "聊一聊"}
          </Text>
        </TouchableOpacity>
      </ScrollView>

      <RecruitmentResponseModal
        visible={showGreetingModal}
        recruitment={recruitment}
        onClose={() => setShowGreetingModal(false)}
        onSend={handleGreetingSent}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  loadingContainer: { justifyContent: "center", alignItems: "center" },
  header: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
  },
  backButton: { fontSize: 16 },
  headerTitle: { fontSize: 18, fontWeight: "bold" },
  placeholder: { width: 50 },
  body: { flex: 1 },
  scrollContent: { padding: 16 },
  infoCard: {
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
  },
  topRow: {
    flexDirection: "row",
    marginBottom: 12,
  },
  coverImage: {
    width: 80,
    height: 110,
    borderRadius: 8,
  },
  topRight: {
    flex: 1,
    marginLeft: 12,
  },
  nameRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
  },
  gameName: {
    fontSize: 14,
    marginBottom: 4,
  },
  reportText: {
    fontSize: 13,
  },
  title: {
    fontSize: 18,
    fontWeight: "600",
    marginBottom: 8,
  },
  tagsRow: {
    flexDirection: "row",
    flexWrap: "wrap",
  },
  tag: {
    paddingHorizontal: 10,
    paddingVertical: 3,
    borderRadius: 10,
    marginRight: 6,
    marginBottom: 4,
  },
  tagText: {
    fontSize: 12,
  },
  userRow: {
    flexDirection: "row",
    alignItems: "center",
    marginTop: 8,
  },
  avatar: {
    width: 24,
    height: 24,
    borderRadius: 12,
    marginRight: 6,
  },
  avatarName: {
    fontSize: 13,
  },
  description: {
    fontSize: 14,
    lineHeight: 22,
  },
  chatButton: {
    height: 44,
    borderRadius: 8,
    justifyContent: "center",
    alignItems: "center",
    marginBottom: 12,
  },
  chatButtonDisabled: {
    opacity: 0.5,
  },
  chatButtonText: {
    color: "#fff",
    fontSize: 16,
    fontWeight: "600",
  },
  emptyText: {
    fontSize: 16,
    textAlign: "center",
    marginTop: 100,
    color: "#999",
  },
});
