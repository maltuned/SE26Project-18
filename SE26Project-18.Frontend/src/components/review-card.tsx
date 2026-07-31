import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { ReviewData } from "../api/api";
import RemoteImage from "./remote-image";
import { useTheme } from "../contexts/theme-context";

interface ReviewCardProps {
  review: ReviewData;
  onReport?: (review: ReviewData) => void;
}

export default function ReviewCard({ review, onReport }: ReviewCardProps) {
  const { colors } = useTheme();

  return (
    <View style={[styles.card, { backgroundColor: colors.card }]}>
      <View style={styles.topRow}>
        <View style={styles.reviewerInfo}>
          <RemoteImage
            url={review.reviewerAvatar}
            style={[styles.avatar, { backgroundColor: colors.placeholder }]}
          />
          <Text style={[styles.nickname, { color: colors.text }]}>
            {review.reviewerNickname}
          </Text>
        </View>
        <View style={styles.rightRow}>
          {onReport && (
            <TouchableOpacity onPress={() => onReport(review)}>
              <Text style={[styles.reportText, { color: colors.primary }]}>
                举报
              </Text>
            </TouchableOpacity>
          )}
          <Text style={[styles.date, { color: colors.textQuaternary }]}>
            {review.createdAt}
          </Text>
        </View>
      </View>
      <Text style={[styles.content, { color: colors.descriptionText }]}>
        {review.content}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    borderRadius: 12,
    padding: 14,
    marginBottom: 10,
  },
  topRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    marginBottom: 8,
  },
  reviewerInfo: {
    flexDirection: "row",
    alignItems: "center",
  },
  rightRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
  },
  reportText: {
    fontSize: 13,
  },
  avatar: {
    width: 32,
    height: 32,
    borderRadius: 16,
    marginRight: 8,
  },
  nickname: {
    fontSize: 15,
    fontWeight: "600",
  },
  date: {
    fontSize: 12,
  },
  content: {
    fontSize: 14,
    lineHeight: 20,
    marginLeft: 40,
  },
});