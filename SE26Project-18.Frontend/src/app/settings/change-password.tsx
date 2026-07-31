import { useRouter } from "expo-router";
import { useState } from "react";
import {
  Alert,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from "react-native";
import { changePassword } from "../../api/api";
import { useTheme } from "../../contexts/theme-context";
import { tokenStorage } from "../../api/token-storage";

export default function ChangePasswordScreen() {
  const router = useRouter();
  const { colors } = useTheme();
  const [oldPassword, setOldPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    if (!oldPassword || !newPassword || !confirmPassword) {
      showError("请填写所有字段");
      return;
    }
    if (newPassword !== confirmPassword) {
      showError("两次输入的新密码不一致");
      return;
    }
    if (newPassword.length < 6) {
      showError("新密码长度不能少于 6 位");
      return;
    }

    setLoading(true);
    try {
      await changePassword(oldPassword, newPassword);
      await tokenStorage.clearTokens();
      Alert.alert("成功", "密码修改成功，请重新登录", [
        { text: "确定", onPress: () => router.replace("/(auth)/login") },
      ]);
    } catch (e: any) {
      showError(e.message || "修改失败");
    } finally {
      setLoading(false);
    }
  };

  const showError = (msg: string) => {
    Alert.alert("错误", msg);
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <TouchableOpacity style={styles.back} onPress={() => router.back()}>
        <Text style={[styles.backText, { color: colors.primary }]}>← 返回</Text>
      </TouchableOpacity>
      <Text style={[styles.header, { color: colors.text }]}>修改密码</Text>

      <View style={[styles.form, { backgroundColor: colors.card }]}>
        <Text style={[styles.label, { color: colors.text }]}>旧密码</Text>
        <TextInput
          style={[styles.input, { backgroundColor: colors.inputBackground, color: colors.inputText, borderColor: colors.inputBorder }]}
          value={oldPassword}
          onChangeText={setOldPassword}
          secureTextEntry
          placeholder="请输入旧密码"
          placeholderTextColor={colors.textTertiary}
        />

        <Text style={[styles.label, { color: colors.text }]}>新密码</Text>
        <TextInput
          style={[styles.input, { backgroundColor: colors.inputBackground, color: colors.inputText, borderColor: colors.inputBorder }]}
          value={newPassword}
          onChangeText={setNewPassword}
          secureTextEntry
          placeholder="请输入新密码（至少 6 位）"
          placeholderTextColor={colors.textTertiary}
        />

        <Text style={[styles.label, { color: colors.text }]}>确认新密码</Text>
        <TextInput
          style={[styles.input, { backgroundColor: colors.inputBackground, color: colors.inputText, borderColor: colors.inputBorder }]}
          value={confirmPassword}
          onChangeText={setConfirmPassword}
          secureTextEntry
          placeholder="请再次输入新密码"
          placeholderTextColor={colors.textTertiary}
        />

        <TouchableOpacity
          style={[styles.submitBtn, { backgroundColor: colors.primary, opacity: loading ? 0.6 : 1 }]}
          onPress={handleSubmit}
          disabled={loading}
          activeOpacity={0.8}
        >
          <Text style={styles.submitText}>{loading ? "提交中..." : "确认修改"}</Text>
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
  form: {
    marginTop: 12,
    padding: 16,
    gap: 12,
  },
  label: { fontSize: 14, fontWeight: "600" },
  input: {
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
  },
  submitBtn: {
    borderRadius: 8,
    paddingVertical: 14,
    alignItems: "center",
    marginTop: 8,
  },
  submitText: {
    color: "#fff",
    fontSize: 16,
    fontWeight: "600",
  },
});