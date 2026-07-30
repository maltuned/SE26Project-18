import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { RecruitmentData } from "../api/api";
import { useTheme } from "../contexts/theme-context";
import MediaImage from "./media-image";

interface RecruitmentManageCardProps {
  recruitment: RecruitmentData;
  onPress: (recruitment: RecruitmentData) => void;
}

function RecruitmentManageCard({
  recruitment,
  onPress,
}: RecruitmentManageCardProps) {
  const { colors } = useTheme();
  const closed = recruitment.status === "已关闭";
  const responseCount = recruitment.responses.length;

  return (
    <TouchableOpacity onPress={() => onPress(recruitment)} activeOpacity={0.8}>
      <View style={[styles.card, { backgroundColor: colors.card }]}>
        <MediaImage
          uri={recruitment.gameCover || recruitment.gameIcon}
          style={[styles.cardImage, { backgroundColor: colors.placeholder }]}
        />
        <View style={styles.cardRight}>
          <Text style={[styles.cardGameName, { color: colors.textSecondary }]}>
            {recruitment.gameName}
          </Text>
          <Text style={[styles.cardTitle, { color: colors.text }]}>
            {recruitment.title}
          </Text>
          <View style={styles.responseRow}>
            <Text style={[styles.cardResponse, { color: colors.textTertiary }]}>
              {responseCount > 0 ? `有${responseCount}条回应` : "暂无回应"}
            </Text>
          </View>
          <Text style={[styles.cardTime, { color: colors.textQuaternary }]}>
            截止 {new Date(recruitment.expiredAt).toLocaleDateString("zh-CN")}
          </Text>
        </View>
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
      </View>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: "row",
    borderRadius: 12,
    padding: 12,
    marginBottom: 12,
  },
  cardImage: {
    width: 80,
    height: 110,
    borderRadius: 8,
  },
  cardRight: {
    flex: 1,
    marginLeft: 12,
    justifyContent: "center",
  },
  cardGameName: {
    fontSize: 14,
    fontWeight: "600",
    marginBottom: 4,
  },
  cardTitle: {
    fontSize: 18,
    marginBottom: 8,
  },
  cardResponse: {
    fontSize: 13,
  },
  responseRow: {
    flexDirection: "row",
    alignItems: "center",
    marginBottom: 4,
  },
  redDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    marginLeft: 6,
  },
  cardTime: {
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
});

export default RecruitmentManageCard;
