import { useState } from "react";
import { StyleSheet, Text, TouchableOpacity } from "react-native";
import { getChatByUsers, getUsers, sendMessage } from "../api/api";

export default function TestMessageButton() {
  const [loading, setLoading] = useState(false);

  const handlePress = async () => {
    if (loading) return;
    setLoading(true);
    try {
      const users = await getUsers();
      const sender = users.find((u) => u.username === "rbhd");
      const receiver = users.find((u) => u.username === "zhangsan");

      if (!sender || !receiver) {
        console.warn("[TestButton] 找不到用户 rbhd 或 zhangsan");
        return;
      }

      let chat = await getChatByUsers([sender.id, receiver.id]);
      let chatId: number;

      if (chat) {
        chatId = chat.id;
      } else {
        console.warn(
          "[TestButton] rbhd 和 zhangsan 之间没有聊天，请先通过招募创建聊天",
        );
        return;
      }

      await sendMessage({
        chatId,
        senderId: sender.id,
        receiverId: receiver.id,
        content: "button",
      });
      console.log("[TestButton] 消息已发送: rbhd → zhangsan: button");
    } catch (e) {
      console.error("[TestButton] 发送失败:", e);
    } finally {
      setLoading(false);
    }
  };

  return (
    <TouchableOpacity
      style={[styles.button, loading && styles.buttonDisabled]}
      onPress={handlePress}
      activeOpacity={0.7}
      disabled={loading}
    >
      <Text style={styles.text}>{loading ? "..." : "TEST"}</Text>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  button: {
    position: "absolute",
    bottom: 100,
    right: 16,
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: "#ff6b35",
    justifyContent: "center",
    alignItems: "center",
    elevation: 8,
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 6,
    zIndex: 10001,
  },
  buttonDisabled: {
    opacity: 0.5,
  },
  text: {
    color: "#fff",
    fontSize: 12,
    fontWeight: "700",
  },
});