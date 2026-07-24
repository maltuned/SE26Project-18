import { useEffect, useState } from "react";
import {
  ActivityIndicator,
  Image,
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { GameInfo, getGameById } from "../api/api";
import { useTheme } from "../contexts/theme-context";

interface GameInfoModalProps {
  visible: boolean;
  gameId: number | null;
  onClose: () => void;
}

const testImage = require("../../assets/images/testImage.png");

function GameInfoModal({ visible, gameId, onClose }: GameInfoModalProps) {
  const { colors } = useTheme();
  const [game, setGame] = useState<GameInfo | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (visible && gameId !== null) {
      setLoading(true);
      getGameById(gameId).then((data) => {
        setGame(data);
        setLoading(false);
      });
    } else {
      setGame(null);
    }
  }, [visible, gameId]);

  return (
    <Modal
      visible={visible}
      transparent
      animationType="fade"
      onRequestClose={onClose}
    >
      <Pressable
        style={[styles.overlay, { backgroundColor: colors.overlay }]}
        onPress={onClose}
      >
        <Pressable
          style={[
            styles.content,
            { backgroundColor: colors.gameModalBackground },
          ]}
          onPress={() => {}}
        >
          <ScrollView>
            {loading ? (
              <View style={styles.loadingContainer}>
                <ActivityIndicator size="small" color={colors.primary} />
              </View>
            ) : (
              <>
                <View style={styles.body}>
                  <Image
                    source={testImage}
                    style={[
                      styles.image,
                      { backgroundColor: colors.placeholder },
                    ]}
                  />
                  <View style={styles.info}>
                    <View style={styles.nameRow}>
                      <Text style={[styles.name, { color: colors.text }]}>
                        {game?.name}
                      </Text>
                      <TouchableOpacity>
                        <Text
                          style={[
                            styles.feedbackText,
                            { color: colors.primary },
                          ]}
                        >
                          反馈
                        </Text>
                      </TouchableOpacity>
                    </View>
                    <Text
                      style={[styles.company, { color: colors.textSecondary }]}
                    >
                      {game?.company}
                    </Text>
                    <View style={styles.tags}>
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
                    </View>
                  </View>
                </View>
                <Text
                  style={[
                    styles.description,
                    { color: colors.descriptionText },
                  ]}
                >
                  {game?.description}
                </Text>
              </>
            )}
          </ScrollView>
        </Pressable>
      </Pressable>
    </Modal>
  );
}

export const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    paddingHorizontal: 24,
  },
  content: {
    borderRadius: 16,
    width: "100%",
    maxHeight: "80%",
    padding: 16,
  },
  body: {
    flexDirection: "row",
    marginBottom: 12,
  },
  image: {
    width: 80,
    height: 110,
    borderRadius: 8,
  },
  info: {
    flex: 1,
    marginLeft: 12,
    justifyContent: "center",
  },
  nameRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
  },
  name: {
    fontSize: 18,
    fontWeight: "bold",
  },
  feedbackText: {
    fontSize: 14,
  },
  company: {
    fontSize: 14,
    marginTop: 4,
  },
  tags: {
    flexDirection: "row",
    marginTop: 8,
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
  description: {
    fontSize: 14,
    lineHeight: 22,
  },
  loadingContainer: {
    padding: 24,
    alignItems: "center",
  },
});

export default GameInfoModal;