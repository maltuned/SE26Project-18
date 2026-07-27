import { useLocalSearchParams, useRouter } from "expo-router";
import { useEffect, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  Image,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import {
  getRecruitmentsByPublisherId,
  getUserById,
  RecruitmentData,
  UserResponse,
} from "../api/api";
import RecruitmentViewCard from "../components/recruitment-view-card";
import { useAuth } from "../contexts/auth-context";
import { useTheme } from "../contexts/theme-context";

const TABS = ["发布招募", "收到评价"];

const REVIEWS = [
  {
    id: "1",
    reviewer: "张三",
    gameName: "王者荣耀",
    content: "技术不错，配合默契，体验很好！",
    date: "2026-07-10",
  },
  {
    id: "2",
    reviewer: "李四",
    gameName: "原神",
    content: "熟悉剧情，聊天时很有梗，能对上电波！",
    date: "2026-07-08",
  },
  {
    id: "3",
    reviewer: "王五",
    gameName: "英雄联盟",
    content: "一起去了线下活动，玩得很开心！",
    date: "2026-07-05",
  },
];

export default function PersonalPageScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ userId?: string }>();
  const { colors } = useTheme();
  const { currentUser, userId: currentUserId } = useAuth();
  const targetUserId = params.userId ? Number(params.userId) : currentUserId;
  const isOwnPage = targetUserId === currentUserId;

  const [activeTab, setActiveTab] = useState(0);
  const [userRecruitments, setUserRecruitments] = useState<RecruitmentData[]>(
    [],
  );
  const [loading, setLoading] = useState(false);
  const [targetUser, setTargetUser] = useState<UserResponse | null>(null);

  useEffect(() => {
    if (isOwnPage) {
      setTargetUser(currentUser);
    } else if (targetUserId) {
      getUserById(targetUserId).then((user) => setTargetUser(user));
    }
  }, [targetUserId, isOwnPage, currentUser]);

  useEffect(() => {
    if (targetUserId) {
      setLoading(true);
      getRecruitmentsByPublisherId(targetUserId).then((data) => {
        setUserRecruitments(data);
        setLoading(false);
      });
    } else {
      setUserRecruitments([]);
    }
  }, [targetUserId]);

  const handleReport = () => {
    Alert.alert("举报", "举报已提交，我们会尽快处理");
  };

  const openCard = (item: RecruitmentData) => {
    router.push({
      pathname: "/recruitment-detail" as any,
      params: { recruitmentId: item.id.toString() },
    });
  };

  const testImage = require("../../assets/images/testImage.png");

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <TouchableOpacity style={styles.back} onPress={() => router.back()}>
        <Text style={[styles.backText, { color: colors.primary }]}>← 返回</Text>
      </TouchableOpacity>
      {isOwnPage ? (
        <TouchableOpacity
          style={styles.editButton}
          onPress={() =>
            router.push(`/personal-page-edit?userId=${targetUserId}`)
          }
        >
          <Text style={[styles.editText, { color: colors.primary }]}>
            编辑资料
          </Text>
        </TouchableOpacity>
      ) : (
        <TouchableOpacity style={styles.editButton} onPress={handleReport}>
          <Text style={[styles.editText, { color: colors.primary }]}>举报</Text>
        </TouchableOpacity>
      )}

      <View
        style={[
          styles.profileSection,
          { backgroundColor: colors.profileBackground },
        ]}
      >
        <View style={styles.profileTop}>
          <Image
            source={testImage}
            style={[styles.avatar, { backgroundColor: colors.primary }]}
          />
          <View style={styles.profileInfo}>
            <Text style={[styles.nickname, { color: colors.nicknameText }]}>
              {targetUser?.nickname ? `${targetUser.nickname}` : `@${targetUser?.username}` || "空用户名"}
            </Text>
            {targetUser?.nickname && (
            <Text style={[styles.username, { color: colors.textTertiary }]}>
              {`@${targetUser?.username}` || "空用户名"}
            </Text>
            )}
          </View>
        </View>
        <Text style={[styles.bio, { color: colors.bioText }]}>
          {targetUser?.signature || "这个人很懒，什么都没写..."}
        </Text>
      </View>

      <View style={[styles.tabRow, { backgroundColor: colors.card }]}>
        {TABS.map((tab, i) => (
          <TouchableOpacity
            key={tab}
            style={[
              styles.tab,
              activeTab === i
                ? { borderBottomColor: colors.tabActiveBorder }
                : { borderBottomColor: colors.tabInactiveBorder },
            ]}
            onPress={() => setActiveTab(i)}
          >
            <Text
              style={[
                styles.tabText,
                activeTab === i
                  ? [{ color: colors.primary }, styles.tabTextActive]
                  : { color: colors.textTertiary },
              ]}
            >
              {tab}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      <View style={[styles.content, { backgroundColor: colors.surface }]}>
        {activeTab === 0 ? (
          loading ? (
            <View style={styles.loadingContainer}>
              <ActivityIndicator size="large" color={colors.primary} />
            </View>
          ) : (
            <ScrollView style={styles.recruitmentList}>
              {userRecruitments.map((recruitment) => (
                <RecruitmentViewCard
                  key={recruitment.id}
                  recruitment={recruitment}
                  onPress={openCard}
                />
              ))}
            </ScrollView>
          )
        ) : (
          <ScrollView style={styles.reviewList}>
            {REVIEWS.map((review) => (
              <View
                key={review.id}
                style={[styles.reviewCard, { backgroundColor: colors.card }]}
              >
                <View style={styles.reviewTop}>
                  <View style={styles.reviewerInfo}>
                    <Image
                      source={testImage}
                      style={[
                        styles.reviewerAvatar,
                        { backgroundColor: colors.primary },
                      ]}
                    />
                    <Text style={[styles.reviewerName, { color: colors.text }]}>
                      {review.reviewer}
                    </Text>
                  </View>
                  <Text
                    style={[styles.reportButton, { color: colors.primary }]}
                  >
                    举报
                  </Text>
                </View>
                <Text
                  style={[
                    styles.reviewContent,
                    { color: colors.descriptionText },
                  ]}
                >
                  {review.content}
                </Text>
                <View style={styles.reviewBottom}>
                  <TouchableOpacity>
                    <Text
                      style={[styles.reviewGame, { color: colors.primary }]}
                    >
                      {review.gameName}
                    </Text>
                  </TouchableOpacity>
                  <Text
                    style={[
                      styles.reviewDate,
                      { color: colors.textQuaternary },
                    ]}
                  >
                    {review.date}
                  </Text>
                </View>
              </View>
            ))}
          </ScrollView>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  back: {
    position: "absolute",
    top: 0,
    left: 0,
    paddingHorizontal: 16,
    paddingVertical: 12,
    zIndex: 1,
  },
  backText: { fontSize: 16 },
  editButton: {
    position: "absolute",
    top: 0,
    right: 0,
    paddingHorizontal: 16,
    paddingVertical: 12,
    zIndex: 1,
  },
  editText: { fontSize: 16 },
  profileSection: {
    paddingTop: 44,
    paddingHorizontal: 20,
    paddingBottom: 16,
    marginBottom: 12,
  },
  profileTop: {
    flexDirection: "row",
    alignItems: "center",
    marginBottom: 12,
  },
  avatar: {
    width: 72,
    height: 72,
    borderRadius: 36,
    justifyContent: "center",
    alignItems: "center",
  },
  profileInfo: { flex: 1, marginLeft: 16 },
  nickname: { fontSize: 22, fontWeight: "bold" },
  username: { fontSize: 14, fontWeight: "bold" },
  bio: { fontSize: 14, lineHeight: 20 },
  tabRow: {
    flexDirection: "row",
    marginBottom: 1,
  },
  tab: {
    flex: 1,
    paddingVertical: 12,
    alignItems: "center",
    borderBottomWidth: 2,
  },
  tabText: { fontSize: 15 },
  tabTextActive: { fontWeight: "600" },
  content: { flex: 1 },
  recruitmentList: { flex: 1, padding: 16 },
  reviewList: { flex: 1, padding: 16 },
  loadingContainer: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
  },
  reviewCard: {
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
  },
  reviewTop: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 8,
  },
  reviewerInfo: { flexDirection: "row", alignItems: "center" },
  reviewerAvatar: {
    width: 36,
    height: 36,
    borderRadius: 18,
    justifyContent: "center",
    alignItems: "center",
    marginRight: 10,
  },
  reviewerName: { fontSize: 15 },
  reportButton: { fontSize: 13 },
  reviewContent: { fontSize: 14, lineHeight: 22, marginBottom: 10 },
  reviewBottom: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
  },
  reviewGame: { fontSize: 13 },
  reviewDate: { fontSize: 12 },
});
