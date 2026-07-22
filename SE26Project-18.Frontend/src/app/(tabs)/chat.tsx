import { useFocusEffect, useRouter } from "expo-router";
import React, { useEffect, useState } from "react";
import {
    ActivityIndicator,
    FlatList,
    StyleSheet,
    Text,
    View,
} from "react-native";
import { ChatBrief, getChats } from "../../api/api";
import ChatEntry, { ChatEntryInfo } from "../../components/chat-entry";
import { useAuth } from "../../contexts/auth-context";
import { useTheme } from "../../contexts/theme-context";

export default function ChatListScreen() {
  const router = useRouter();
  const { colors } = useTheme();
  const { userId } = useAuth();
  const [chats, setChats] = useState<ChatBrief[]>([]);
  const [loading, setLoading] = useState(true);

  const loadChats = React.useCallback(() => {
    setLoading(true);
    getChats(userId!).then((data) => {
      const sorted = [...data].sort((a, b) => {
        const timeA = a.lastMessageAt || "";
        const timeB = b.lastMessageAt || "";
        return new Date(timeB).getTime() - new Date(timeA).getTime();
      });
      setChats(sorted);
      setLoading(false);
    });
  }, [userId]);

  useEffect(() => {
    if (userId) {
      loadChats();
    } else {
      setChats([]);
      setLoading(false);
    }
  }, [userId, loadChats]);

  useFocusEffect(
    React.useCallback(() => {
      if (userId) {
        loadChats();
      }
    }, [userId, loadChats]),
  );

  const openChat = (chat: ChatBrief) => {
    router.push(`/chat-room?chatId=${chat.id}`);
  };

  const mapToChatEntryInfo = (entry: ChatBrief): ChatEntryInfo => ({
    id: String(entry.id),
    name: entry.otherUserName,
    lastMessage: entry.lastMessageContent,
    time: formatTime(entry.lastMessageAt),
  });

  const formatTime = (createdAt?: string): string => {
    if (!createdAt) return "";
    const date = new Date(createdAt);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffHours = diffMs / (1000 * 60 * 60);
    if (diffHours < 24) {
      return date.toLocaleTimeString("zh-CN", {
        hour: "2-digit",
        minute: "2-digit",
      });
    } else if (diffHours < 48) {
      return "昨天";
    } else {
      return date.toLocaleDateString("zh-CN", {
        month: "short",
        day: "numeric",
      });
    }
  };

  return (
    <View style={[styles.container, { backgroundColor: colors.background }]}>
      <Text style={[styles.title, { color: colors.text }]}>聊天</Text>
      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={colors.primary} />
        </View>
      ) : (
        <FlatList
          data={chats.map(mapToChatEntryInfo)}
          keyExtractor={(item) => item.id}
          renderItem={({ item }) => (
            <ChatEntry
              chat={item}
              onPress={() =>
                openChat(chats.find((e) => e.id === Number(item.id))!)
              }
            />
          )}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  title: {
    fontSize: 24,
    fontWeight: "bold",
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  loadingContainer: { flex: 1, justifyContent: "center", alignItems: "center" },
});
