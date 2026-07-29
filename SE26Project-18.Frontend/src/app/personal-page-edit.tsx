import { useLocalSearchParams, useRouter } from "expo-router";
import { Alert, StyleSheet, Text, TextInput, TouchableOpacity, View } from "react-native";
import { useEffect, useState } from "react";
import * as ImagePicker from "expo-image-picker";
import { getUserById, updateUser, uploadAvatar, UserInfo } from "../api/api";
import RemoteImage from "../components/remote-image";
import { useAuth } from "../contexts/auth-context";
import { useTheme } from "../contexts/theme-context";

export default function PersonalPageEditScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ userId?: string }>();
  const { userId, refreshUser, currentUser } = useAuth();
  const { colors } = useTheme();

  const editUserId = params.userId ? Number(params.userId) : userId;

  const [nickname, setNickname] = useState("");
  const [bio, setBio] = useState("");
  const [avatar, setAvatar] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [fetching, setFetching] = useState(true);

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

  const handlePickAvatar = async () => {
    const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (!permission.granted) {
      Alert.alert("提示", "需要相册权限才能更换头像");
      return;
    }

    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ["images"],
      allowsEditing: true,
      aspect: [1, 1],
      quality: 0.8,
    });

    if (!result.canceled && result.assets?.[0]) {
      const { uri } = result.assets[0];
      setUploading(true);
      const url = await uploadAvatar(uri, editUserId!);
      if (url) {
        const urlWithVersion = `${url}?v=${Date.now()}`;
        setAvatar(urlWithVersion);
        await updateUser(editUserId!, { avatar: urlWithVersion });
        refreshUser();
      } else {
        Alert.alert("错误", "头像上传失败，请稍后重试");
      }
      setUploading(false);
    }
  };

  const handleSave = async () => {
    if (!editUserId) {
      Alert.alert("提示", "请先登录");
      return;
    }
    try {
      const updateData: Record<string, any> = {
        nickname,
        signature: bio,
      };
      await updateUser(editUserId, updateData);
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
            <TouchableOpacity onPress={handlePickAvatar} disabled={uploading}>
              <RemoteImage
                url={avatar || currentUser?.avatar}
                style={[styles.avatar, { backgroundColor: colors.placeholder }] }
              />
              <View style={[styles.avatarOverlay, { backgroundColor: colors.overlay }]}>
                <Text style={styles.avatarOverlayText}>
                  {uploading ? "上传中..." : "更换头像"}
                </Text>
              </View>
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
    justifyContent: "center",
    alignItems: "center",
  },
  avatarOverlay: {
    position: "absolute",
    bottom: 0,
    left: 0,
    right: 0,
    height: 16,
    borderBottomLeftRadius: 4,
    borderBottomRightRadius: 4,
    justifyContent: "center",
    alignItems: "center",
  },
  avatarOverlayText: {
    color: "#fff",
    fontSize: 10,
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