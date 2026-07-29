import { useLocalSearchParams, useRouter } from "expo-router";
import { useEffect, useState } from "react";
import {
  ActivityIndicator,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import {
  getRecruitmentsByPublisherId,
  getUserById,
  getReviewsByUser,
  RecruitmentData,
  ReviewData,
  UserInfo,
} from "../api/api";
import RemoteImage from "../components/remote-image";
import RecruitmentViewCard from "../components/recruitment-view-card";
import ReviewCard from "../components/review-card";
import { useAuth } from "../contexts/auth-context";
import { useTheme } from "../contexts/theme-context";

const TABS = ["发布招募", "收到评价"];

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
  const [targetUser, setTargetUser] = useState<UserInfo | null>(null);
  const [reviews, setReviews] = useState<ReviewData[]>([]);

  useEffect(() => {
    if (isOwnPage) {
      setTargetUser(currentUser);
    } else if (targetUserId) {
      getUserById(targetUserId).then((user) => setTargetUser(user));
    }
  }, [targetUserId, isOwnPage, currentUser]);

  useEffect(() => {
    if (targetUserId) {
      getReviewsByUser(targetUserId).then((data) => {
        setReviews(data);
      });
    }
  }, [targetUserId]);

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
    router.push({
      pathname: "/report" as any,
      params: {
        targetType: "用户",
        targetId: String(targetUserId),
      },
    });
  };

  const openCard = (item: RecruitmentData) => {
    router.push({
      pathname: "/recruitment-detail" as any,
      params: { recruitmentId: item.id.toString() },
    });
  };

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
          <RemoteImage
            url={targetUser?.avatar}
            style={[styles.avatar, { backgroundColor: colors.placeholder }]}
          />
          <View style={styles.profileInfo}>
            <Text style={[styles.nickname, { color: colors.nicknameText }]}>
              {targetUser?.nickname
                ? `${targetUser.nickname}`
                : `@${targetUser?.username}` || "空用户名"}
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
            <ScrollView
              style={styles.recruitmentList}
              contentContainerStyle={{ flexGrow: 1 }}
            >
              {userRecruitments.length === 0 ? (
                <View style={styles.empty}>
                  <Text
                    style={[styles.emptyText, { color: colors.textTertiary }]}
                  >
                    {isOwnPage
                      ? "暂无发布招募，快去发布一个吧！"
                      : "暂无发布招募"}
                  </Text>
                </View>
              ) : (
                userRecruitments.map((recruitment) => (
                  <RecruitmentViewCard
                    key={recruitment.id}
                    recruitment={recruitment}
                    onPress={openCard}
                  />
                ))
              )}
            </ScrollView>
          )
        ) : (
          <ScrollView
            style={styles.reviewList}
            contentContainerStyle={{ flexGrow: 1 }}
          >
            {reviews.length === 0 ? (
              <View style={styles.empty}>
                <Text
                  style={[styles.emptyText, { color: colors.textTertiary }]}
                >
                  暂无收到评价
                </Text>
              </View>
            ) : (
              reviews.map((review) => (
                <ReviewCard
                  key={review.id}
                  review={review}
                  onReport={(r) => {
                    router.push({
                      pathname: "/report" as any,
                      params: {
                        targetType: "评价",
                        targetId: String(r.id),
                      },
                    });
                  }}
                />
              ))
            )}
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
  empty: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
  },
  emptyText: {
    fontSize: 15,
  },
});
