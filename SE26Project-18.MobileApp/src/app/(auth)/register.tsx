import { useRouter } from "expo-router";
import { useState } from "react";
import {
    ActivityIndicator,
    Alert,
    StyleSheet,
    Text,
    TextInput,
    TouchableOpacity,
    View,
} from "react-native";
import { ApiError } from "../../api/api";
import { useAuth } from "../../contexts/auth-context";
import { useTheme } from "../../contexts/theme-context";

export default function RegisterScreen() {
  const router = useRouter();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const { colors } = useTheme();
  const { register } = useAuth();

  const handleRegister = async () => {
    if (!username || !password || !confirmPassword) {
      Alert.alert("注册失败", "请填写所有字段");
      return;
    }
    if (password !== confirmPassword) {
      Alert.alert("注册失败", "两次输入的密码不一致");
      return;
    }
    if (password.length < 8) {
      Alert.alert("注册失败", "密码长度不能少于8位");
      return;
    }
    setLoading(true);
    try {
      await register(username.trim(), password);
    } catch (error) {
      Alert.alert(
        "注册失败",
        error instanceof ApiError ? error.message : "网络错误，请稍后重试",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.background }]}>
      <TouchableOpacity style={styles.backButton} onPress={() => router.back()}>
        <Text style={[styles.backText, { color: colors.primary }]}>← 返回</Text>
      </TouchableOpacity>
      <View style={styles.center}>
        <Text style={[styles.title, { color: colors.text }]}>注册</Text>
        <View style={styles.placeholder}>
          <TextInput
            style={[
              styles.input,
              {
                backgroundColor: colors.inputBackground,
                color: colors.inputText,
              },
            ]}
            placeholder="用户名"
            placeholderTextColor={colors.textTertiary}
            value={username}
            onChangeText={setUsername}
          />
          <TextInput
            style={[
              styles.input,
              {
                backgroundColor: colors.inputBackground,
                color: colors.inputText,
              },
            ]}
            placeholder="密码"
            placeholderTextColor={colors.textTertiary}
            secureTextEntry
            value={password}
            onChangeText={setPassword}
          />
          <TextInput
            style={[
              styles.input,
              {
                backgroundColor: colors.inputBackground,
                color: colors.inputText,
              },
            ]}
            placeholder="确认密码"
            placeholderTextColor={colors.textTertiary}
            secureTextEntry
            value={confirmPassword}
            onChangeText={setConfirmPassword}
          />
          <TouchableOpacity
            style={[styles.registerButton, { backgroundColor: colors.primary }]}
            onPress={handleRegister}
            disabled={loading}
          >
            {loading ? (
              <ActivityIndicator color="#fff" />
            ) : (
              <Text style={styles.registerButtonText}>注 册</Text>
            )}
          </TouchableOpacity>
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  backButton: {
    position: "absolute",
    top: 0,
    left: 0,
    paddingHorizontal: 16,
    paddingVertical: 12,
    zIndex: 1,
  },
  backText: { fontSize: 16 },
  center: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    paddingHorizontal: 40,
  },
  title: { fontSize: 28, fontWeight: "bold", marginBottom: 30 },
  placeholder: { width: "100%", alignItems: "center" },
  input: {
    width: "100%",
    height: 48,
    borderRadius: 8,
    marginBottom: 16,
    paddingHorizontal: 16,
    fontSize: 15,
  },
  registerButton: {
    width: "100%",
    height: 48,
    borderRadius: 8,
    justifyContent: "center",
    alignItems: "center",
    marginTop: 8,
  },
  registerButtonText: { color: "#fff", fontSize: 16, fontWeight: "600" },
});
