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
import { login as apiLogin } from "../../api/api";
import { useAuth } from "../../contexts/auth-context";
import { useTheme } from "../../contexts/theme-context";

export default function LoginScreen() {
  const { login } = useAuth();
  const router = useRouter();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const { colors } = useTheme();

  const handleLogin = async () => {
    setUsername("zhangsan");
    setPassword("123456");
    if (!username || !password) {
      Alert.alert("登录失败", "请输入用户名和密码");
      return;
    }
    setLoading(true);
    try {
      const user = await apiLogin(username, `hash_${username}`);
      if (user) {
        login(user.id);
      } else {
        Alert.alert("登录失败", "用户名或密码错误");
      }
    } catch (error) {
      Alert.alert("登录失败", "网络错误，请稍后重试");
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.background }]}>
      <View style={styles.center}>
        <Text style={[styles.title, { color: colors.text }]}>登录</Text>
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
          <TouchableOpacity
            style={[styles.loginButton, { backgroundColor: colors.primary }]}
            onPress={handleLogin}
            disabled={loading}
          >
            {loading ? (
              <ActivityIndicator color="#fff" />
            ) : (
              <Text style={styles.loginButtonText}>登录</Text>
            )}
          </TouchableOpacity>
          <View style={styles.linkRow}>
            <TouchableOpacity onPress={() => {}}>
              <Text style={[styles.linkText, { color: colors.primary }]}>
                忘记密码
              </Text>
            </TouchableOpacity>
            <TouchableOpacity onPress={() => router.push("/(auth)/register")}>
              <Text style={[styles.linkText, { color: colors.primary }]}>
                注册账号
              </Text>
            </TouchableOpacity>
          </View>
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
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
  loginButton: {
    width: "100%",
    height: 48,
    borderRadius: 8,
    justifyContent: "center",
    alignItems: "center",
    marginTop: 8,
  },
  loginButtonText: { color: "#fff", fontSize: 16, fontWeight: "600" },
  linkRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    width: "100%",
    marginTop: 16,
  },
  linkText: { fontSize: 14 },
});
