import { useLocalSearchParams, useRouter } from "expo-router";
import { useState } from "react";
import {
    Alert,
    Modal,
    Pressable,
    StyleSheet,
    Text,
    TextInput,
    TouchableOpacity,
    View,
} from "react-native";
import { submitReport } from "../api/api";
import { useTheme } from "../contexts/theme-context";

const VIOLATION_TYPES = [
    { key: "涉政", label: "涉政" },
    { key: "谩骂", label: "谩骂" },
    { key: "广告", label: "广告" },
    { key: "色情", label: "色情" },
    { key: "欺诈", label: "欺诈" },
    { key: "其他", label: "其他" },
];

export default function ReportScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ targetType?: string; targetId?: string }>();
  const targetType = params.targetType ?? "招募";
  const targetId = params.targetId ? Number(params.targetId) : 0;
  const [selectedViolation, setSelectedViolation] = useState("其他");
  const [text, setText] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [dropdownVisible, setDropdownVisible] = useState(false);
  const { colors } = useTheme();

  const handleSubmit = async () => {
    if (!text.trim()) {
      Alert.alert("提示", "请填写举报内容");
      return;
    }
    setSubmitting(true);
    try {
      await submitReport({
        target_type: targetType,
        target_id: targetId,
        violation_type: selectedViolation,
        content: text.trim(),
      });
      Alert.alert("成功", "举报提交成功，我们会尽快处理", [
        { text: "确定", onPress: () => router.back() },
      ]);
    } catch (e: any) {
      Alert.alert("错误", e.message || "提交失败，请稍后重试");
    } finally {
      setSubmitting(false);
    }
  };

  const selectedLabel = VIOLATION_TYPES.find((t) => t.key === selectedViolation)?.label ?? selectedViolation;

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <TouchableOpacity style={styles.back} onPress={() => router.back()}>
        <Text style={[styles.backText, { color: colors.primary }]}>← 返回</Text>
      </TouchableOpacity>
      <Text style={[styles.header, { color: colors.text }]}>举报</Text>
      <View style={styles.body}>
        <TouchableOpacity
          style={[
            styles.dropdown,
            {
              backgroundColor: colors.card,
              borderColor: colors.borderLight,
            },
          ]}
          onPress={() => setDropdownVisible(true)}
        >
          <Text style={[styles.dropdownText, { color: colors.inputText }]}>
            {selectedLabel}
          </Text>
          <Text style={[styles.dropdownArrow, { color: colors.textTertiary }]}>
            ▼
          </Text>
        </TouchableOpacity>

        <Modal
          visible={dropdownVisible}
          transparent
          animationType="fade"
          onRequestClose={() => setDropdownVisible(false)}
        >
          <Pressable
            style={[styles.overlay, { backgroundColor: colors.overlay }]}
            onPress={() => setDropdownVisible(false)}
          >
            <View
              style={[
                styles.dropdownMenu,
                { backgroundColor: colors.card },
              ]}
            >
              {VIOLATION_TYPES.map((item) => (
                <TouchableOpacity
                  key={item.key}
                  style={[
                    styles.dropdownItem,
                    selectedViolation === item.key && {
                      backgroundColor: colors.primaryLight,
                    },
                  ]}
                  onPress={() => {
                    setSelectedViolation(item.key);
                    setDropdownVisible(false);
                  }}
                >
                  <Text
                    style={[
                      styles.dropdownItemText,
                      selectedViolation === item.key
                        ? { color: colors.primary }
                        : { color: colors.text },
                    ]}
                  >
                    {item.label}
                  </Text>
                  {selectedViolation === item.key && (
                    <Text style={[styles.checkmark, { color: colors.primary }]}>
                      ✓
                    </Text>
                  )}
                </TouchableOpacity>
              ))}
            </View>
          </Pressable>
        </Modal>

        <TextInput
          style={[
            styles.textArea,
            {
              backgroundColor: colors.card,
              color: colors.inputText,
              borderColor: colors.borderLight,
            },
          ]}
          placeholder="请描述举报原因..."
          placeholderTextColor={colors.textTertiary}
          multiline
          value={text}
          onChangeText={setText}
          textAlignVertical="top"
        />
        <TouchableOpacity
          style={[
            styles.submitButton,
            submitting
              ? { backgroundColor: colors.textTertiary }
              : { backgroundColor: colors.primary },
          ]}
          onPress={handleSubmit}
          disabled={submitting}
        >
          <Text style={styles.submitText}>
            {submitting ? "提交中..." : "提交举报"}
          </Text>
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
  dropdown: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    height: 48,
    borderRadius: 8,
    borderWidth: 1,
    paddingHorizontal: 12,
    marginBottom: 16,
  },
  dropdownText: {
    fontSize: 15,
  },
  dropdownArrow: {
    fontSize: 12,
  },
  overlay: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    paddingHorizontal: 40,
  },
  dropdownMenu: {
    width: "100%",
    borderRadius: 12,
    paddingVertical: 4,
    overflow: "hidden",
  },
  dropdownItem: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: 16,
    paddingVertical: 14,
  },
  dropdownItemText: {
    fontSize: 16,
  },
  checkmark: {
    fontSize: 16,
    fontWeight: "600",
  },
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