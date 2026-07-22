import { useRouter } from "expo-router";
import { useState } from "react";
import {
    Image,
    StyleSheet,
    Text,
    TextInput,
    TouchableOpacity,
    View,
} from "react-native";
import { useTheme } from "../contexts/theme-context";

export default function PersonalPageEditScreen() {
  const router = useRouter();
  const [nickname, setNickname] = useState("用户昵称");
  const [bio, setBio] = useState("这个人很懒，什么都没写...");
  const { colors } = useTheme();
  const testImage = require("../../assets/images/testImage.png");

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
    marginLeft: 14,
    fontSize: 17,
    fontWeight: "600",
    borderRadius: 8,
    padding: 8,
  },
  bioInput: {
    fontSize: 14,
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingTop: 12,
    height: 80,
  },
  saveButton: {
    height: 48,
    borderRadius: 8,
    justifyContent: "center",
    alignItems: "center",
  },
  saveText: { color: "#fff", fontSize: 16, fontWeight: "600" },
});
