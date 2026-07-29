import { useEffect, useState } from "react";
import {
  ActivityIndicator,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { router, useLocalSearchParams } from "expo-router";
import { GameInfo, getGameById } from "../api/api";
import RemoteImage from "../components/remote-image";
import { useTheme } from "../contexts/theme-context";

export default function GameInfoScreen() {
  const { colors } = useTheme();
  const params = useLocalSearchParams<{ gameId?: string }>();
  const [game, setGame] = useState<GameInfo | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const gameId = params.gameId;
    if (gameId) {
      getGameById(Number(gameId)).then((data) => {
        setGame(data);
        setLoading(false);
      });
    }
  }, [params.gameId]);

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
          游戏详情
        </Text>
        <TouchableOpacity
            onPress={() =>
              router.push(
                `/feedback?prefill=${encodeURIComponent(`关于游戏「${game?.name || ""}」的反馈：`)}` as any,
              )
            }
          >
            <Text style={[styles.feedbackButton, { color: colors.primary }]}>
              反馈
            </Text>
          </TouchableOpacity>
      </View>

      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={colors.primary} />
        </View>
      ) : (
        <ScrollView
          style={styles.body}
          contentContainerStyle={styles.scrollContent}
        >
          <View style={[styles.card, { backgroundColor: colors.card }]}>
            <View style={styles.topRow}>
              <RemoteImage
                url={game?.cover}
                style={[
                  styles.coverImage,
                  { backgroundColor: colors.placeholder },
                ]}
              />
              <View style={styles.info}>
                <Text style={[styles.name, { color: colors.text }]}>
                  {game?.name}
                </Text>
                <Text
                  style={[styles.company, { color: colors.textSecondary }]}
                >
                  {game?.company}
                </Text>
                <ScrollView
                  horizontal
                  showsHorizontalScrollIndicator={false}
                  style={styles.tagsRow}
                >
                  {game?.tags.map((tag) => (
                    <View
                      key={tag}
                      style={[
                        styles.tag,
                        { backgroundColor: colors.primary },
                      ]}
                    >
                      <Text
                        style={[
                          styles.tagText,
                          { color: colors.primaryText },
                        ]}
                      >
                        {tag}
                      </Text>
                    </View>
                  ))}
                </ScrollView>
              </View>
            </View>
          </View>

          <View style={[styles.card, { backgroundColor: colors.card }]}>
            <Text style={[styles.sectionTitle, { color: colors.text }]}>
              游戏介绍
            </Text>
            <Text
              style={[styles.description, { color: colors.descriptionText }]}
            >
              {game?.description}
            </Text>
          </View>
        </ScrollView>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  header: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
  },
  backButton: {
    fontSize: 16,
  },
  headerTitle: {
    fontSize: 17,
    fontWeight: "600",
    position: "absolute",
    left: 0,
    right: 0,
    textAlign: "center",
  },
  feedbackButton: {
    fontSize: 15,
  },
  body: {
    flex: 1,
  },
  scrollContent: {
    padding: 16,
    gap: 12,
  },
  card: {
    borderRadius: 12,
    padding: 16,
  },
  topRow: {
    flexDirection: "row",
  },
  coverImage: {
    width: 80,
    height: 110,
    borderRadius: 8,
  },
  info: {
    flex: 1,
    marginLeft: 12,
    justifyContent: "center",
  },
  name: {
    fontSize: 18,
    fontWeight: "bold",
  },
  company: {
    fontSize: 14,
    marginTop: 4,
  },
  tagsRow: {
    marginTop: 8,
    height: 24,
  },
  tag: {
    paddingHorizontal: 10,
    paddingVertical: 3,
    height: 24,
    borderRadius: 12,
    justifyContent: "center",
    marginRight: 6,
  },
  tagText: {
    fontSize: 12,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: "600",
    marginBottom: 8,
  },
  description: {
    fontSize: 14,
    lineHeight: 22,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
  },
});