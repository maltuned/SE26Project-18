import { useLocalSearchParams, useRouter } from "expo-router";
import { Alert, Image, StyleSheet, Text, TextInput, TouchableOpacity, View } from "react-native";
import { useEffect, useState } from "react";
import { getUserById, updateUser, UserInfo } from "../api/api";
import { useAuth } from "../contexts/auth-context";
import { useTheme } from "../contexts/theme-context";

export default function PersonalPageEditScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ userId?: string }>();
  const { userId, refreshUser } = useAuth();
  const { colors } = useTheme();

  const editUserId = params.userId ? Number(params.userId) : userId;

  const [nickname, setNickname] = useState("");
  const [bio, setBio] = useState("");
  const [fetching, setFetching] = useState(true);
  const testImage = require("../../assets/images/testImage.png");

  useEffect(() => {
    if (editUserId) {
      getUserById(editUserId).then((user: UserInfo | null) => {
        if (user) {
          setNickname(user.nickname || "");
          setBio(user.signature || "");
        }
        setFetching(false);
      }).catch(() => {
        setFetching(false);
      });
    } else {
      setFetching(false);
    }
  }, [editUserId]);

  const handleSave = async () => {
    if (!editUserId) {
      Alert.alert("提示", "请先登录");
      return;
    }
    try {
      await updateUser(editUserId, {
        nickname,
        signature: bio,
      });
      refreshUser();
      router.back();
    } catch {
      Alert.alert("错误", "更新失败，请稍后重试");
    }
  };

  if (fetching) {
    return (
      <View style={[styles.container, { backgroundColor: colors.surface }]}>
        <Text style={{ color: colors.text, textAlign: "center", marginTop: 100 }}>加载中...</Text>
      </View>
    );
  }

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <TouchableOpacity style={styles.back} onPress={() => router.back()}>
        <Text style={[styles.backText, { color: colors.primary }]}>← 返回</Text>
      </TouchableOpacity>
      <Text style={[styles.header, { color: colors.text }]}>编辑资料</Text>

      <View style={styles.body}>
        <View style={[styles.card, { backgroundColor: colors.card }]}>
          <View style={styles.avatarRow}>
            <TouchableOpacity>
              <Image source={testImage} style={styles.avatar} />
            </TouchableOpacity>
            <TextInput
              style={[
                styles.nicknameInput,
                {
                  backgroundColor: colors.inputBackgroundAlt,
                  color: colors.inputText,
                },
              ]}
              value={nickname}
              onChangeText={setNickname}
              placeholder="昵称"
              placeholderTextColor={colors.textTertiary}
            />
          </View>
          <TextInput
            style={[
              styles.bioInput,
              {
                backgroundColor: colors.inputBackgroundAlt,
                color: colors.inputText,
              },
            ]}
            value={bio}
            onChangeText={setBio}
            placeholder="个性签名"
            placeholderTextColor={colors.textTertiary}
            multiline
            textAlignVertical="top"
          />
        </View>
        <TouchableOpacity
          style={[styles.saveButton, { backgroundColor: colors.primary }]}
          onPress={handleSave}
        >
          <Text style={styles.saveText}>保存</Text>
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
    fontSize: 18,
    fontWeight: "bold",
    textAlign: "center",
    paddingVertical: 12,
  },
  body: { paddingHorizontal: 16, paddingTop: 8 },
  card: { borderRadius: 8, padding: 16, marginBottom: 20 },
  avatarRow: { flexDirection: "row", alignItems: "center", marginBottom: 12 },
  avatar: {
    width: 64,
    height: 64,
    borderRadius: 32,
    backgroundColor: "#007AFF",
    justifyContent: "center",
    alignItems: "center",
  },
  nicknameInput: {
    flex: 1,
    marginLeft: 12,
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontSize: 16,
  },
  bioInput: {
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontSize: 14,
    minHeight: 100,
  },
  saveButton: {
    height: 44,
    borderRadius: 8,
    justifyContent: "center",
    alignItems: "center",
  },
  saveText: {
    color: "#fff",
    fontSize: 16,
    fontWeight: "600",
  },
});