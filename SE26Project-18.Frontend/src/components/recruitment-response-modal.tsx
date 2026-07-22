import { useState } from "react";
import {
  KeyboardAvoidingView,
  Modal,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from "react-native";
import { RecruitmentData } from "../api/api";
import { useTheme } from "../contexts/theme-context";

interface RecruitmentResponseModalProps {
  visible: boolean;
  recruitment: RecruitmentData;
  onClose: () => void;
  onSend?: (greeting: string) => void;
}

const DEFAULT_GREETING = "你好！我对你的招募很感兴趣～";

function RecruitmentResponseModal({
  visible,
  recruitment,
  onClose,
  onSend,
}: RecruitmentResponseModalProps) {
  const { colors } = useTheme();
  const [greeting, setGreeting] = useState(DEFAULT_GREETING);

  const handleSend = () => {
    if (onSend) {
      onSend(greeting);
    }
    onClose();
  };

  return (
    <Modal
      visible={visible}
      transparent
      animationType="fade"
      onRequestClose={onClose}
    >
      <KeyboardAvoidingView
        style={[styles.overlay, { backgroundColor: colors.overlay }]}
        behavior={Platform.OS === "ios" ? "padding" : "height"}
      >
        <Pressable style={styles.overlayInner} onPress={onClose}>
          <Pressable
            style={[
              styles.content,
              { backgroundColor: colors.modalBackground },
            ]}
            onPress={() => {}}
          >
            <ScrollView
              keyboardShouldPersistTaps="handled"
              showsVerticalScrollIndicator={false}
            >
              <View style={styles.header}>
                <Text style={[styles.title, { color: colors.text }]}>
                  向 {recruitment.publisher?.nickname} 打招呼
                </Text>
                <TouchableOpacity onPress={onClose}>
                  <Text
                    style={[styles.closeText, { color: colors.textTertiary }]}
                  >
                    ✕
                  </Text>
                </TouchableOpacity>
              </View>

              <View style={styles.recruitmentInfo}>
                <Text
                  style={[styles.gameName, { color: colors.textSecondary }]}
                >
                  {recruitment.gameName}
                </Text>
                <Text style={[styles.recruitmentTitle, { color: colors.text }]}>
                  {recruitment.title}
                </Text>
              </View>
              <TextInput
                style={[
                  styles.input,
                  {
                    backgroundColor: colors.inputBackground,
                    color: colors.inputText,
                  },
                ]}
                value={greeting}
                onChangeText={setGreeting}
                multiline
                textAlignVertical="top"
                placeholder="输入打招呼内容..."
                placeholderTextColor={colors.textTertiary}
              />

              <View style={styles.buttonRow}>
                <TouchableOpacity
                  style={[styles.cancelButton, { borderColor: colors.border }]}
                  onPress={onClose}
                >
                  <Text
                    style={[styles.cancelText, { color: colors.textTertiary }]}
                  >
                    取消
                  </Text>
                </TouchableOpacity>
                <TouchableOpacity
                  style={[
                    styles.sendButton,
                    { backgroundColor: colors.primary },
                  ]}
                  onPress={handleSend}
                >
                  <Text
                    style={[styles.sendText, { color: colors.primaryText }]}
                  >
                    发送
                  </Text>
                </TouchableOpacity>
              </View>
            </ScrollView>
          </Pressable>
        </Pressable>
      </KeyboardAvoidingView>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
  },
  overlayInner: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    paddingHorizontal: 24,
  },
  content: {
    borderRadius: 16,
    width: "100%",
    padding: 20,
  },
  header: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 16,
  },
  title: {
    fontSize: 18,
    fontWeight: "600",
  },
  closeText: {
    fontSize: 18,
  },
  recruitmentInfo: {
    marginBottom: 16,
  },
  gameName: {
    fontSize: 13,
    marginBottom: 4,
  },
  recruitmentTitle: {
    fontSize: 15,
    fontWeight: "600",
  },
  label: {
    fontSize: 14,
    marginBottom: 8,
  },
  input: {
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 14,
    minHeight: 100,
    marginBottom: 16,
  },
  buttonRow: {
    flexDirection: "row",
    justifyContent: "space-between",
  },
  cancelButton: {
    flex: 1,
    height: 44,
    borderRadius: 8,
    justifyContent: "center",
    alignItems: "center",
    borderWidth: 1,
    marginRight: 8,
  },
  cancelText: {
    fontSize: 15,
    fontWeight: "600",
  },
  sendButton: {
    flex: 1,
    height: 44,
    borderRadius: 8,
    justifyContent: "center",
    alignItems: "center",
    marginLeft: 8,
  },
  sendText: {
    fontSize: 15,
    fontWeight: "600",
  },
});

export default RecruitmentResponseModal;
