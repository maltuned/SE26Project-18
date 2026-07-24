import { useRouter } from "expo-router";
import { useState } from "react";
import {
    StyleSheet,
    Text,
    TextInput,
    TouchableOpacity,
    View,
} from "react-native";
import { useTheme } from "../contexts/theme-context";

export default function FeedbackScreen() {
  const router = useRouter();
  const [text, setText] = useState("");
  const { colors } = useTheme();

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <TouchableOpacity style={styles.back} onPress={() => router.back()}>
        <Text style={[styles.backText, { color: colors.primary }]}>← 返回</Text>
      </TouchableOpacity>
      <Text style={[styles.header, { color: colors.text }]}>反馈</Text>
      <View style={styles.body}>
        <TextInput
          style={[
            styles.textArea,
            {
              backgroundColor: colors.card,
              color: colors.inputText,
              borderColor: colors.borderLight,
            },
          ]}
          placeholder="请描述你的问题或建议..."
          placeholderTextColor={colors.textTertiary}
          multiline
          value={text}
          onChangeText={setText}
          textAlignVertical="top"
        />
        <TouchableOpacity
          style={[styles.submitButton, { backgroundColor: colors.primary }]}
        >
          <Text style={styles.submitText}>提交反馈</Text>
        </TouchableOpacity>
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
  header: {
    fontSize: 20,
    fontWeight: "bold",
    textAlign: "center",
    paddingVertical: 14,
  },
  body: { padding: 16 },
  textArea: {
    height: 160,
    borderRadius: 8,
    padding: 12,
    fontSize: 15,
    marginBottom: 16,
    borderWidth: 1,
  },
  submitButton: {
    borderRadius: 8,
    height: 48,
    justifyContent: "center",
    alignItems: "center",
  },
  submitText: { color: "#fff", fontSize: 16, fontWeight: "600" },
});
