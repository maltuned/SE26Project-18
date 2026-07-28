import { useFocusEffect, useLocalSearchParams, useRouter } from "expo-router";
import { useCallback, useEffect, useState } from "react";
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
  createChat,
  createResponse,
  getChatsByRecruitmentId,
  getRecruitmentById,
  getResponses,
  ResponseData,
  RecruitmentData,
  RecruitmentTag,
  sendMessage,
} from "../../api/api";
import RecruitmentResponseModal from "../../components/recruitment-response-modal";
import RemoteImage from "../../components/remote-image";
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
  const [chatDataLoading, setChatDataLoading] = useState(true);
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
      setChatDataLoading(true);
      Promise.all([
        getChatsByRecruitmentId(recruitment.id),
        getResponses(recruitment.id),
      ]).then(([chats, responses]) => {
        const myChat = chats.find((c) =>
          c.users?.some((u) => u.userId === userId),
        );
        if (myChat) setExistingChat(myChat);
        const myResp = responses.find((r) => r.responserId === userId);
        setMyResponse(myResp || null);
        setChatDataLoading(false);
      }).catch(() => {
        setChatDataLoading(false);
      });
    }
  }, [userId, recruitment?.id]);

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
    if (!userId || chatDataLoading) return;
    // 已关闭且没回应过：不能操作
    if (isClosed && !myResponse) return;
    // 已回应且被拒绝：不能操作
    if (myResponse && myResponse.responseStatus === "已删除") return;
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
    try {
      const chat = await createChat({
        recruitmentId: recruitment.id,
        user1Id: userId,
        user2Id: recruitment.publisherId,
      });
      await createResponse({
        recruitmentId: recruitment.id,
        responserId: userId,
      });
      await sendMessage({
        chatId: chat.id,
        senderId: userId,
        receiverId: recruitment.publisherId,
        content: greeting,
      });
      router.dismissAll();
      router.push(`/(tabs)/chat`);
      router.push(`/chat-room?chatId=${chat.id}`);
    } catch {
      Alert.alert("错误", "无法发起聊天，请稍后重试");
    } finally {
      setLoading(false);
    }
  };

  const handleReport = () => {
    router.push({
      pathname: "/report" as any,
      params: {
        targetType: "招募",
        targetId: String(recruitment?.id ?? 0),
      },
    });
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
            <RemoteImage url={recruitment.gameCover} style={styles.coverImage} />
            <View style={styles.topRight}>
              <View style={styles.nameRow}>
                <Text
                  style={[styles.gameName, { color: colors.textSecondary }]}
                >
                  {recruitment.gameName}
                </Text>
                <TouchableOpacity onPress={handleReport}>
                  <Text style={[styles.reportText, { color: colors.primary }]}>
                    举报
                  </Text>
                </TouchableOpacity>
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
                <RemoteImage
                  url={recruitment.publisher?.avatar}
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
            (myResponse && myResponse.responseStatus === "已删除")
              ? { backgroundColor: colors.textQuaternary }
              : { backgroundColor: colors.primary },
            loading && styles.chatButtonDisabled,
          ]}
          onPress={handleChat}
          disabled={
            loading ||
            chatDataLoading ||
            (isClosed && !myResponse) ||
            !!(myResponse && myResponse.responseStatus === "已删除")
          }
        >
          <Text style={styles.chatButtonText}>
            {loading || chatDataLoading
              ? "加载中..."
              : isClosed && !myResponse
                ? "已关闭"
                : myResponse && myResponse.responseStatus === "已删除"
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