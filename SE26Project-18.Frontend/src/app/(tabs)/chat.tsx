import { useFocusEffect, useRouter } from "expo-router";
import React, { useCallback, useEffect, useState } from "react";
import {
    ActivityIndicator,
    FlatList,
    Pressable,
    StyleSheet,
    Text,
    View,
} from "react-native";
import { ChatBrief, getChats, getUnreadNotificationCount, markMessagesRead } from "../../api/api";
import { MessageDto } from "../../api/dtos";
import ChatEntry, { ChatEntryInfo } from "../../components/chat-entry";
import { useAuth } from "../../contexts/auth-context";
import { useChatUnread } from "../../contexts/chat-unread-context";
import { useSignalR } from "../../contexts/signalr-context";
import { useTheme } from "../../contexts/theme-context";

export default function ChatListScreen() {
  const router = useRouter();
  const { colors } = useTheme();
  const { userId } = useAuth();
  const { onNewChatMessage } = useSignalR();
  const { setUnreadCount } = useChatUnread();
  const [chats, setChats] = useState<ChatBrief[]>([]);
  const [loading, setLoading] = useState(true);
  const [unreadNotifCount, setUnreadNotifCount] = useState(0);

  const loadChats = useCallback(() => {
    setLoading(true);
    getChats(userId!).then((data) => {
      const sorted = [...data].sort((a, b) => {
        const timeA = a.lastMessageAt || "";
        const timeB = b.lastMessageAt || "";
        return new Date(timeB).getTime() - new Date(timeA).getTime();
      });
      setChats(sorted);
      setLoading(false);
      const totalUnread = sorted.reduce((sum, c) => sum + c.unreadCount, 0);
      setUnreadCount(totalUnread);
    });
  }, [userId, setUnreadCount]);

  useEffect(() => {
    if (userId) {
      loadChats();
    } else {
      setChats([]);
      setLoading(false);
    }
  }, [userId, loadChats]);

  useFocusEffect(
    useCallback(() => {
      if (userId) {
        loadChats();
        getUnreadNotificationCount().then(setUnreadNotifCount);
      }
    }, [userId, loadChats]),
  );

  useEffect(() => {
    const unsub = onNewChatMessage((msg: MessageDto) => {
      setChats((prev) => {
        const exists = prev.some((c) => c.id === msg.chat_id);
        if (!exists) {
          loadChats();
          return prev;
        }
        const updated = prev
          .map((c) =>
            c.id === msg.chat_id
              ? {
                  ...c,
                  lastMessageContent: msg.content,
                  lastMessageAt: msg.created_at,
                  unreadCount: c.unreadCount + 1,
                }
              : c,
          )
          .sort((a, b) => {
            const timeA = a.lastMessageAt || "";
            const timeB = b.lastMessageAt || "";
            return new Date(timeB).getTime() - new Date(timeA).getTime();
          });
        const totalUnread = updated.reduce((sum, c) => sum + c.unreadCount, 0);
        setUnreadCount(totalUnread);
        return updated;
      });
    });

    return unsub;
  }, [onNewChatMessage, loadChats, setUnreadCount]);

  const openChat = (chat: ChatBrief) => {
    if (chat.unreadCount > 0 && userId) {
      markMessagesRead(chat.id, userId);
    }
    router.push(`/chat-room?chatId=${chat.id}`);
  };

  const mapToChatEntryInfo = (entry: ChatBrief): ChatEntryInfo => ({
    id: String(entry.id),
    name: entry.otherUserName,
    avatar: entry.otherUserAvatar,
    lastMessage: entry.lastMessageContent,
    time: formatTime(entry.lastMessageAt),
    unreadCount: entry.unreadCount,
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
      <View style={styles.header}>
        <Text style={[styles.title, { color: colors.text }]}>聊天</Text>
        <Pressable
          style={styles.notifButton}
          onPress={() => router.push("/notification")}
        >
          <Text style={[styles.notifIcon, { color: colors.text }]}>🔔</Text>
          {unreadNotifCount > 0 && (
            <View style={[styles.badge, { backgroundColor: "red" }]} />
          )}
        </Pressable>
      </View>
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
  header: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  title: {
    fontSize: 24,
    fontWeight: "bold",
  },
  notifButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: "center",
    alignItems: "center",
  },
  notifIcon: { fontSize: 22 },
  badge: {
    position: "absolute",
    top: 4,
    right: 4,
    width: 10,
    height: 10,
    borderRadius: 5,
  },
  loadingContainer: { flex: 1, justifyContent: "center", alignItems: "center" },
});