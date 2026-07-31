import { useState } from "react";
import {
  Modal,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from "react-native";
import { useTheme } from "../contexts/theme-context";

const REVIEW_TAGS = [
  "配合默契",
  "技术不错",
  "沟通顺畅",
  "风趣幽默",
  "耐心负责",
  "乐于助人",
];

interface ReviewModalProps {
  visible: boolean;
  onClose: () => void;
  onSubmit: (content: string) => void;
  submitting: boolean;
  targetName: string;
}

export default function ReviewModal({
  visible,
  onClose,
  onSubmit,
  submitting,
  targetName,
}: ReviewModalProps) {
  const { colors } = useTheme();
  const [content, setContent] = useState("");

  const handleTagPress = (tag: string) => {
    setContent((prev) => (prev ? prev + ` #${tag} ` : `#${tag} `));
  };

  const handleSubmit = () => {
    if (!content.trim()) return;
    onSubmit(content.trim());
    setContent("");
  };

  const handleClose = () => {
    setContent("");
    onClose();
  };

  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={handleClose}>
      <Pressable
        style={[styles.overlay, { backgroundColor: colors.overlay }]}
        onPress={handleClose}
      >
        <Pressable
          style={[styles.container, { backgroundColor: colors.card }]}
          onPress={() => {}}
        >
          <Text style={[styles.title, { color: colors.text }]}>评价{targetName}</Text>

          <TextInput
            style={[
              styles.textInput,
              {
                borderColor: colors.inputBorder,
                backgroundColor: colors.textInputBackground,
                color: colors.inputText,
              },
            ]}
            placeholder="写下你的评价..."
            placeholderTextColor={colors.textTertiary}
            value={content}
            onChangeText={setContent}
            multiline
            numberOfLines={4}
            textAlignVertical="top"
          />

          <View style={styles.tagsRow}>
            {REVIEW_TAGS.map((tag) => (
              <TouchableOpacity
                key={tag}
                style={[styles.tag, { borderColor: colors.primary }]}
                onPress={() => handleTagPress(tag)}
              >
                <Text style={[styles.tagText, { color: colors.primary }]}>
                  {tag}
                </Text>
              </TouchableOpacity>
            ))}
          </View>

          <View style={styles.buttonRow}>
            <TouchableOpacity
              style={[styles.cancelButton, { borderColor: colors.inputBorder }]}
              onPress={handleClose}
            >
              <Text style={[styles.cancelText, { color: colors.textSecondary }]}>
                取消
              </Text>
            </TouchableOpacity>
            <TouchableOpacity
              style={[
                styles.submitButton,
                { backgroundColor: submitting ? colors.textTertiary : colors.primary },
              ]}
              onPress={handleSubmit}
              disabled={submitting || !content.trim()}
            >
              <Text style={styles.submitText}>
                {submitting ? "提交中..." : "提交"}
              </Text>
            </TouchableOpacity>
          </View>
        </Pressable>
      </Pressable>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    paddingHorizontal: 24,
  },
  container: {
    width: "100%",
    borderRadius: 16,
    padding: 20,
  },
  title: {
    fontSize: 18,
    fontWeight: "bold",
    marginBottom: 16,
    textAlign: "center",
  },
  textInput: {
    borderWidth: 1,
    borderRadius: 12,
    padding: 12,
    fontSize: 15,
    minHeight: 100,
    marginBottom: 12,
  },
  tagsRow: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8,
    marginBottom: 16,
  },
  tag: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
    borderWidth: 1,
  },
  tagText: {
    fontSize: 13,
  },
  buttonRow: {
    flexDirection: "row",
    justifyContent: "flex-end",
    gap: 12,
  },
  cancelButton: {
    paddingHorizontal: 20,
    paddingVertical: 10,
    borderRadius: 8,
    borderWidth: 1,
  },
  cancelText: {
    fontSize: 15,
  },
  submitButton: {
    paddingHorizontal: 20,
    paddingVertical: 10,
    borderRadius: 8,
  },
  submitText: {
    color: "#fff",
    fontSize: 15,
    fontWeight: "600",
  },
});