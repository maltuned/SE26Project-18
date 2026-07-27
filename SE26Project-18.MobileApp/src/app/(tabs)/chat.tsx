import { useFocusEffect, useRouter } from "expo-router";
import React, { useEffect, useState } from "react";
import {
    ActivityIndicator,
    FlatList,
    StyleSheet,
    Text,
    View,
} from "react-native";
import { getMyChats, ChatResponse } from "../../api/api";
import ChatEntry, { ChatEntryInfo } from "../../components/chat-entry";
import { useAuth } from "../../contexts/auth-context";
import { useTheme } from "../../contexts/theme-context";

// 本地适配数据结构
type ChatBriefLocal = { id: number; lastMessageAt: string; chatData: ChatResponse };

export default function ChatListScreen() {
  const router = useRouter();
  const { colors } = useTheme();
  const { userId } = useAuth();
  const [chats, setChats] = useState<ChatBriefLocal[]>([]);
  const [loading, setLoading] = useState(true);

  const loadChats = React.useCallback(() => {
    setLoading(true);
    getMyChats().then((data) => {
      const mapped: ChatBriefLocal[] = data.map((c) => ({
        id: c.id,
        lastMessageAt: c.lastMessage?.sentAt || "",
        chatData: c,
      }));
      const sorted = mapped.sort((a, b) => {
        return new Date(b.lastMessageAt).getTime() - new Date(a.lastMessageAt).getTime();
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

  const openChat = (chat: ChatBriefLocal) => {
    router.push(`/chat-room?chatId=${chat.id}`);
  };

  const mapToChatEntryInfo = (entry: ChatBriefLocal): ChatEntryInfo => ({
    id: String(entry.id),
    name: `聊天 #${entry.chatData.id}`,
    lastMessage: entry.chatData.lastMessage?.content || "",
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
          renderItem={({ item }: { item: ChatEntryInfo }) => (
            <ChatEntry
              chat={item}
              onPress={() => router.push(`/chat-room?chatId=${item.id}`)}
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
