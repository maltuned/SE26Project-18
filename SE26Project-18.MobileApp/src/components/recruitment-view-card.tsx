import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { RecruitmentData } from "../api/api";
import { useTheme } from "../contexts/theme-context";
import MediaImage from "./media-image";

interface RecruitmentInfoCardProps {
  recruitment: RecruitmentData;
  onPress: (recruitment: RecruitmentData) => void;
}

function RecruitmentViewCard({
  recruitment: recruitment,
  onPress,
}: RecruitmentInfoCardProps) {
  const { colors } = useTheme();

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
          <View style={styles.cardTags}>
            {recruitment.recruitmentTags.map((tag) => (
              <View
                key={tag.id}
                style={[
                  styles.cardTag,
                  { backgroundColor: colors.primaryLight },
                ]}
              >
                <Text style={[styles.cardTagText, { color: colors.primary }]}>
                  {tag.name}
                </Text>
              </View>
            ))}
          </View>
          <Text style={[styles.cardTime, { color: colors.textQuaternary }]}>
            截止 {new Date(recruitment.expiredAt).toLocaleDateString("zh-CN")}
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
  cardTags: {
    flexDirection: "row",
  },
  cardTag: {
    paddingHorizontal: 10,
    paddingVertical: 3,
    borderRadius: 10,
    marginRight: 6,
  },
  cardTagText: {
    fontSize: 12,
  },
  cardTime: {
    fontSize: 12,
    textAlign: "right",
    marginTop: 6,
  },
});

export default RecruitmentViewCard;
