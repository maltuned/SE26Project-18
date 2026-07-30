import { useFocusEffect, useRouter } from "expo-router";
import React, { useRef, useState } from "react";
import {
    ActivityIndicator,
    FlatList,
    StyleSheet,
    Text,
    View,
} from "react-native";
import { ChatBrief, getChatsPage } from "../../api/api";
import ChatEntry, { ChatEntryInfo } from "../../components/chat-entry";
import { useAuth } from "../../contexts/auth-context";
import { useTheme } from "../../contexts/theme-context";

export default function ChatListScreen() {
  const router = useRouter();
  const { colors } = useTheme();
  const { userId } = useAuth();
  const [chats, setChats] = useState<ChatBrief[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const generationRef = useRef(0);
  const inFlightRef = useRef(false);

  const loadChats = React.useCallback(async (refresh = false, generation = generationRef.current) => {
    if (!userId) return;
    if (inFlightRef.current) return;
    inFlightRef.current = true;
    if (refresh) setRefreshing(true);
    else setLoading(true);
    try {
      const page = await getChatsPage(userId);
      if (generation !== generationRef.current) return;
      const ids = new Set<number>();
      setChats(page.items.filter((chat) => !ids.has(chat.id) && Boolean(ids.add(chat.id))));
      setNextCursor(page.nextCursor);
      setHasMore(page.hasMore);
    } catch {
      if (generation === generationRef.current) {
        setChats([]);
        setNextCursor(null);
        setHasMore(false);
      }
    } finally {
      if (generation === generationRef.current) {
        inFlightRef.current = false;
        setLoading(false);
        setRefreshing(false);
      }
    }
  }, [userId]);

  const loadMore = async () => {
    if (!userId || !hasMore || !nextCursor || inFlightRef.current) return;
    const generation = generationRef.current;
    inFlightRef.current = true;
    setLoadingMore(true);
    try {
      const page = await getChatsPage(userId, nextCursor);
      if (generation !== generationRef.current) return;
      setChats((previous) => {
        const ids = new Set(previous.map((chat) => chat.id));
        return [...previous, ...page.items.filter((chat) => !ids.has(chat.id) && Boolean(ids.add(chat.id)))];
      });
      setNextCursor(page.nextCursor);
      setHasMore(page.hasMore);
    } finally {
      if (generation === generationRef.current) {
        inFlightRef.current = false;
        setLoadingMore(false);
      }
    }
  };

  useFocusEffect(
    React.useCallback(() => {
      if (userId) {
        const generation = ++generationRef.current;
        inFlightRef.current = false;
        void loadChats(true, generation);
      } else {
        setChats([]);
        setLoading(false);
      }
      return () => {
        generationRef.current += 1;
        inFlightRef.current = false;
      };
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
    avatar: entry.otherUserAvatar,
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
          refreshing={refreshing}
          onRefresh={() => loadChats(true)}
          onEndReached={loadMore}
          onEndReachedThreshold={0.4}
          ListFooterComponent={loadingMore ? <ActivityIndicator color={colors.primary} /> : null}
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
