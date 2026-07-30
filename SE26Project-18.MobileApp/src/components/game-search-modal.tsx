import { useEffect, useRef, useState } from "react";
import {
  ActivityIndicator,
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from "react-native";
import { GameBrief, getGames } from "../api/api";
import { FEATURE_FLAGS } from "../constants/feature-flags";
import { useTheme } from "../contexts/theme-context";
import GameInfoModal from "./game-info-modal";
import MediaImage from "./media-image";

interface GameSearchModalProps {
  visible: boolean;
  initialText: string;
  onClose: (text: string, selected?: boolean) => void;
  onSelect?: (game: GameBrief) => void;
}

function GameSearchModal({
  visible,
  initialText,
  onClose,
  onSelect,
}: GameSearchModalProps) {
  const { colors } = useTheme();
  const [searchText, setSearchText] = useState(initialText);
  const [results, setResults] = useState<GameBrief[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);
  const [selectedGameId, setSelectedGameId] = useState<number | null>(null);
  const [gameInfoVisible, setGameInfoVisible] = useState(false);
  const inputRef = useRef<TextInput>(null);

  useEffect(() => {
    if (!visible) return;

    setSearchText(initialText);
    setResults([]);
    setError(false);
    const timer = setTimeout(() => inputRef.current?.focus(), 300);
    return () => clearTimeout(timer);
  }, [visible, initialText]);

  useEffect(() => {
    if (!visible) return;

    let active = true;
    setLoading(true);
    setError(false);

    const timer = setTimeout(async () => {
      try {
        const data = await getGames(searchText);
        if (active) setResults(data);
      } catch {
        if (active) {
          setResults([]);
          setError(true);
        }
      } finally {
        if (active) setLoading(false);
      }
    }, 300);

    return () => {
      active = false;
      clearTimeout(timer);
    };
  }, [visible, searchText]);

  const handleSelect = (game: GameBrief) => {
    onSelect?.(game);
    onClose(game.name, true);
  };

  const handleClose = () => {
    onClose(searchText);
  };

  const handleOpenGameInfo = (game: GameBrief) => {
    setSelectedGameId(game.id);
    setGameInfoVisible(true);
  };

  return (
    <Modal
      visible={visible}
      transparent
      animationType="fade"
      onRequestClose={handleClose}
    >
      <Pressable
        style={[styles.container, { backgroundColor: colors.overlay }]}
        onPress={handleClose}
      >
        <View
          style={[
            styles.modalContent,
            {
              backgroundColor: colors.card,
            },
          ]}
        >
          <View style={[styles.header, { borderBottomColor: colors.border }]}>
            <View
              style={[
                styles.inputWrapper,
                { backgroundColor: colors.searchBackground },
              ]}
            >
              <TextInput
                ref={inputRef}
                style={[styles.input, { color: colors.inputText }]}
                placeholder="搜索游戏..."
                placeholderTextColor={colors.textTertiary}
                value={searchText}
                onChangeText={setSearchText}
              />
              {searchText.length > 0 && (
                <TouchableOpacity
                  onPress={() => setSearchText("")}
                  style={styles.clearButton}
                >
                  <Text
                    style={[
                      styles.clearButtonText,
                      { color: colors.textTertiary },
                    ]}
                  >
                    ✕
                  </Text>
                </TouchableOpacity>
              )}
            </View>
            <TouchableOpacity onPress={handleClose} style={styles.cancelButton}>
              <Text style={[styles.cancelText, { color: colors.primary }]}>
                确定
              </Text>
            </TouchableOpacity>
          </View>

          {loading ? (
            <View style={styles.loadingContainer}>
              <ActivityIndicator size="small" color={colors.primary} />
            </View>
          ) : error ? (
            <View style={styles.loadingContainer}>
              <Text style={{ color: colors.textTertiary }}>
                游戏列表加载失败，请稍后重试
              </Text>
            </View>
          ) : (
            <ScrollView style={styles.suggestionList}>
              {results.map((s) => (
                <View
                  key={s.id}
                  style={[
                    styles.suggestionItem,
                    { borderBottomColor: colors.border },
                  ]}
                >
                  <TouchableOpacity
                    style={styles.suggestionLeft}
                    onPress={() => handleSelect(s)}
                  >
                    <MediaImage
                      uri={s.icon}
                      style={[
                        styles.suggestionIcon,
                        { backgroundColor: colors.placeholder },
                      ]}
                    />
                    <Text
                      style={[styles.suggestionText, { color: colors.text }]}
                    >
                      {s.name}
                    </Text>
                  </TouchableOpacity>
                  <TouchableOpacity
                    style={styles.suggestionArrow}
                    onPress={() => handleOpenGameInfo(s)}
                  >
                    <Text
                      style={[
                        styles.suggestionArrowText,
                        { color: colors.arrow },
                      ]}
                    >
                      ›
                    </Text>
                  </TouchableOpacity>
                </View>
              ))}
            </ScrollView>
          )}

          {FEATURE_FLAGS.gameFeedback && (
            <View style={[styles.footer, { borderTopColor: colors.border }]}>
              <TouchableOpacity>
                <Text style={[styles.feedbackText, { color: colors.primary }]}>
                  没有游戏？反馈
                </Text>
              </TouchableOpacity>
            </View>
          )}
        </View>

        <GameInfoModal
          visible={gameInfoVisible}
          gameId={selectedGameId}
          onClose={() => setGameInfoVisible(false)}
        />
      </Pressable>
    </Modal>
  );
}

export const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: "flex-start",
  },
  modalContent: {
    borderBottomLeftRadius: 16,
    borderBottomRightRadius: 16,
    maxHeight: "80%",
  },
  header: {
    flexDirection: "row",
    alignItems: "center",
    padding: 16,
    paddingBottom: 12,
    borderBottomWidth: 1,
  },
  inputWrapper: {
    flex: 1,
    borderRadius: 20,
    paddingHorizontal: 16,
    height: 40,
    justifyContent: "center",
  },
  input: {
    fontSize: 15,
    padding: 0,
    paddingRight: 24,
  },
  clearButton: {
    position: "absolute",
    right: 12,
    top: 0,
    bottom: 0,
    justifyContent: "center",
    alignItems: "center",
  },
  clearButtonText: {
    fontSize: 14,
  },
  cancelButton: {
    marginLeft: 12,
    paddingVertical: 8,
    paddingHorizontal: 4,
  },
  cancelText: {
    fontSize: 15,
  },
  suggestionList: {
    maxHeight: 300,
  },
  loadingContainer: {
    padding: 24,
    alignItems: "center",
  },
  suggestionItem: {
    flexDirection: "row",
    alignItems: "center",
    paddingRight: 16,
    borderBottomWidth: 1,
  },
  suggestionLeft: {
    flex: 1,
    flexDirection: "row",
    alignItems: "center",
    paddingVertical: 12,
    paddingLeft: 16,
  },
  suggestionIcon: {
    width: 32,
    height: 32,
    borderRadius: 4,
    marginRight: 12,
  },
  suggestionText: {
    fontSize: 15,
  },
  suggestionArrow: {
    paddingLeft: 12,
    paddingVertical: 12,
  },
  suggestionArrowText: {
    fontSize: 20,
  },
  footer: {
    alignItems: "center",
    paddingVertical: 16,
    borderTopWidth: 1,
  },
  feedbackText: {
    fontSize: 14,
    textDecorationLine: "underline",
  },
});

export default GameSearchModal;
