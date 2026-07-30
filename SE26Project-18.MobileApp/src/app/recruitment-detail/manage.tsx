import { useLocalSearchParams, useRouter } from "expo-router";
import { useEffect, useState } from "react";
import {
  ActivityIndicator,
  Image,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import {
  deleteRecruitment,
  deleteResponse,
  getChatByUsers,
  getRecruitmentById,
  getResponses,
  RecruitmentData,
  RecruitmentTag,
  ResponseData,
  updateRecruitment,
} from "../../api/api";
import ResponseRejectModal from "../../components/response-reject-modal";
import { useTheme } from "../../contexts/theme-context";

export default function RecruitmentManageScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ recruitmentId?: string }>();
  const { colors } = useTheme();

  const [recruitment, setRecruitment] = useState<RecruitmentData | null>(null);
  const [closed, setClosed] = useState(false);
  const [responses, setResponses] = useState<ResponseData[]>([]);
  const [loadingResponses, setLoadingResponses] = useState(true);
  const [fetching, setFetching] = useState(true);
  const [rejectModalVisible, setRejectModalVisible] = useState(false);
  const [rejectingResponse, setRejectingResponse] =
    useState<ResponseData | null>(null);

  useEffect(() => {
    const recruitmentId = params.recruitmentId;
    if (recruitmentId) {
      setFetching(true);
      getRecruitmentById(Number(recruitmentId))
        .then((data) => {
          if (data) {
            setRecruitment(data);
            setClosed(data.status === "已关闭");
          }
          setFetching(false);
        })
        .catch(() => {
          setFetching(false);
        });

      setLoadingResponses(true);
      getResponses(Number(recruitmentId)).then((data) => {
        setResponses(data.filter((r) => r.responseStatus !== "已删除"));
        setLoadingResponses(false);
      });
    } else {
      setFetching(false);
    }
  }, [params.recruitmentId]);

  const handleClose = async () => {
    if (!recruitment?.id) return;
    const newStatus = closed ? "招募中" : "已关闭";
    await updateRecruitment(recruitment.id, { status: newStatus });
    setClosed(!closed);
  };

  const handleDelete = async () => {
    if (!recruitment?.id) return;
    await deleteRecruitment(recruitment.id);
    router.dismissAll();
    router.push("/(tabs)/recruitment");
  };

  const handleViewChat = async (res: ResponseData) => {
    if (!recruitment?.publisherId) return;
    const chat = await getChatByUsers([
      res.responserId,
      recruitment.publisherId,
    ]);
    if (chat) {
      router.dismissAll();
      router.push(`/(tabs)/chat`);
      router.push(`/chat-room?chatId=${chat.id}`);
    }
  };

  const handleReject = async (reason: string) => {
    if (!rejectingResponse?.id) return;
    await deleteResponse(rejectingResponse.id, reason);
    setResponses((prev) => prev.filter((r) => r.id !== rejectingResponse.id));
  };

  const testImage = require("../../../assets/images/testImage.png");

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
          招募管理
        </Text>
        <View style={styles.placeholder} />
      </View>

      <ScrollView
        style={styles.body}
        contentContainerStyle={styles.scrollContent}
      >
        <View style={[styles.infoCard, { backgroundColor: colors.card }]}>
          <View
            style={[
              styles.statusBadge,
              closed
                ? { backgroundColor: colors.statusClosed }
                : { backgroundColor: colors.statusRecruiting },
            ]}
          >
            <Text
              style={[
                styles.statusBadgeText,
                closed
                  ? { color: colors.statusClosedText }
                  : { color: colors.statusRecruitingText },
              ]}
            >
              {closed ? "已关闭" : "招募中"}
            </Text>
          </View>
          <View style={styles.topRow}>
            <Image source={testImage} style={styles.coverImage} />
            <View style={styles.topRight}>
              <Text style={[styles.gameName, { color: colors.textSecondary }]}>
                {recruitment.gameName}
              </Text>
              <Text style={[styles.title, { color: colors.text }]}>
                {recruitment.title}
              </Text>
              <View style={styles.tagsRow}>
                {recruitment.recruitmentTags?.map((tag: RecruitmentTag) => (
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
              <Text
                style={[styles.publishTime, { color: colors.textQuaternary }]}
              >
                {recruitment.createdAt}
              </Text>
            </View>
          </View>
          <Text style={[styles.description, { color: colors.descriptionText }]}>
            {recruitment.description}
          </Text>
        </View>

        <View style={[styles.section, { backgroundColor: colors.card }]}>
          <Text style={[styles.sectionTitle, { color: colors.sectionTitle }]}>
            收到回应（{responses.length}）
          </Text>
          {loadingResponses ? (
            <ActivityIndicator
              size="small"
              color={colors.primary}
              style={styles.loadingResponses}
            />
          ) : (
            responses.map((res) => (
              <View
                key={res.id}
                style={[
                  styles.responderRow,
                  { borderBottomColor: colors.border },
                ]}
              >
                <TouchableOpacity
                  style={styles.responderInfo}
                  onPress={() =>
                    router.push(`/personal-page?userId=${res.responserId}`)
                  }
                >
                  <Image
                    source={testImage}
                    style={[
                      styles.responderAvatar,
                      { backgroundColor: colors.primary },
                    ]}
                  />
                  <Text style={[styles.responderName, { color: colors.text }]}>
                    {res.responser?.nickname || res.responser?.username || "未知用户"}
                  </Text>
                </TouchableOpacity>
                <View style={styles.actionButtons}>
                  <TouchableOpacity
                    style={[
                      styles.chatButton,
                      { backgroundColor: colors.primary },
                    ]}
                    onPress={() => handleViewChat(res)}
                  >
                    <Text style={styles.chatButtonText}>查看</Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={[
                      styles.rejectButton,
                      { backgroundColor: colors.danger },
                    ]}
                    onPress={() => {
                      setRejectingResponse(res);
                      setRejectModalVisible(true);
                    }}
                  >
                    <Text style={[styles.rejectButtonText]}>拒绝</Text>
                  </TouchableOpacity>
                </View>
              </View>
            ))
          )}
        </View>

        <ResponseRejectModal
          visible={rejectModalVisible}
          onClose={() => {
            setRejectModalVisible(false);
            setRejectingResponse(null);
          }}
          onSubmit={handleReject}
        />

        <TouchableOpacity
          style={[styles.editButton, { backgroundColor: colors.primary }]}
          onPress={() =>
            router.push(`/recruitment-edit?id=${recruitment.id}`)
          }
        >
          <Text style={styles.editButtonText}>编辑</Text>
        </TouchableOpacity>

        <View style={styles.bottomActions}>
          <TouchableOpacity
            style={[
              styles.actionButton,
              closed
                ? { backgroundColor: colors.success }
                : { backgroundColor: colors.warning },
              styles.actionButtonSpace,
            ]}
            onPress={handleClose}
          >
            <Text style={styles.closeButtonText}>
              {closed ? "继续招募" : "关闭招募"}
            </Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.actionButton, { backgroundColor: colors.danger }]}
            onPress={handleDelete}
          >
            <Text style={styles.deleteButtonText}>删除</Text>
          </TouchableOpacity>
        </View>
      </ScrollView>
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
    position: "relative",
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
  gameName: {
    fontSize: 14,
    marginBottom: 4,
  },
  title: {
    fontSize: 18,
    fontWeight: "600",
    marginBottom: 8,
  },
  tagsRow: {
    flexDirection: "row",
    marginBottom: 4,
  },
  tag: {
    paddingHorizontal: 10,
    paddingVertical: 3,
    borderRadius: 10,
    marginRight: 6,
  },
  tagText: {
    fontSize: 12,
  },
  publishTime: {
    fontSize: 12,
  },
  statusBadge: {
    position: "absolute",
    top: 8,
    right: 8,
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 8,
  },
  statusBadgeText: {
    fontSize: 11,
    fontWeight: "600",
  },
  description: {
    fontSize: 14,
    lineHeight: 22,
  },
  section: {
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: "600",
    marginBottom: 12,
  },
  loadingResponses: {
    paddingVertical: 20,
  },
  responderRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingVertical: 10,
    borderBottomWidth: 1,
  },
  responderInfo: {
    flexDirection: "row",
    alignItems: "center",
  },
  responderAvatar: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: "#007AFF",
    justifyContent: "center",
    alignItems: "center",
    marginRight: 10,
  },
  responderAvatarText: {
    color: "#fff",
    fontSize: 15,
    fontWeight: "bold",
  },
  responderName: {
    fontSize: 15,
  },
  actionButtons: {
    flexDirection: "row",
    gap: 8,
  },
  rejectButton: {
    paddingHorizontal: 16,
    paddingVertical: 6,
    borderRadius: 14,
  },
  rejectButtonText: {
    color: "#fff",
    fontSize: 13,
    fontWeight: "600",
  },
  chatButton: {
    paddingHorizontal: 16,
    paddingVertical: 6,
    borderRadius: 14,
  },
  chatButtonText: {
    color: "#fff",
    fontSize: 13,
    fontWeight: "600",
  },
  editButton: {
    height: 44,
    borderRadius: 8,
    justifyContent: "center",
    alignItems: "center",
    marginBottom: 12,
  },
  editButtonText: {
    color: "#fff",
    fontSize: 16,
    fontWeight: "600",
  },
  bottomActions: {
    flexDirection: "row",
    marginBottom: 20,
  },
  actionButton: {
    flex: 1,
    height: 44,
    borderRadius: 8,
    justifyContent: "center",
    alignItems: "center",
  },
  actionButtonSpace: {
    marginRight: 12,
  },
  closeButtonText: {
    fontSize: 15,
    color: "#fff",
  },
  deleteButtonText: {
    fontSize: 15,
    color: "#fff",
    fontWeight: "600",
  },
  emptyText: {
    fontSize: 16,
    textAlign: "center",
    marginTop: 100,
    color: "#999",
  },
});
