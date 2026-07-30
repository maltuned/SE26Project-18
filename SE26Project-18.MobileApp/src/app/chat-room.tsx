import { useLocalSearchParams, useRouter } from "expo-router";
import { useEffect, useRef, useState } from "react";
import {
    ActivityIndicator,
    Alert,
    FlatList,
    Keyboard,
    KeyboardAvoidingView,
    Platform,
    StatusBar,
    StyleSheet,
    Text,
    TextInput,
    TouchableOpacity,
    View,
} from "react-native";
import {
    ChatStatus,
    ApiError,
    getChatById,
    getMessagesPage,
    getRecruitmentById,
    MessageData,
    RecruitmentData,
    openChatSocket,
    UserInfo,
} from "../api/api";
import ChatMessage, { ChatMessageInfo } from "../components/chat-message";
import MediaImage from "../components/media-image";
import { useAuth } from "../contexts/auth-context";
import { useTheme } from "../contexts/theme-context";

export default function ChatRoomScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ chatId?: string }>();
  const { colors } = useTheme();
  const { userId } = useAuth();
  const statusBarHeight =
    Platform.OS === "ios" ? 0 : StatusBar.currentHeight || 0;
  const flatListRef = useRef<FlatList>(null);
  const socketRef = useRef<WebSocket | null>(null);
  const pendingTextRef = useRef<string | null>(null);
  const historyInitializedRef = useRef(false);
  const messagesRef = useRef<ChatMessageInfo[]>([]);

  const chatId = params.chatId ? Number(params.chatId) : undefined;

  const [messages, setMessages] = useState<ChatMessageInfo[]>([]);
  const [inputText, setInputText] = useState("");
  const [loading, setLoading] = useState(true);
  const [currentScrollOffset, setCurrentScrollOffset] = useState(0);
  const [chatStatus, setChatStatus] = useState<ChatStatus>("限制");
  const [otherUserId, setOtherUserId] = useState<number | null>(null);
  const [otherUser, setOtherUser] = useState<UserInfo | null>(null);
  const [recruitment, setRecruitment] = useState<RecruitmentData | null>(null);
  const [nextMessageCursor, setNextMessageCursor] = useState<string | null>(null);
  const [hasOlderMessages, setHasOlderMessages] = useState(false);
  const [loadingOlder, setLoadingOlder] = useState(false);
  const [pendingText, setPendingText] = useState<string | null>(null);

  // 基于实际消息列表判断
  const currentUserSent = messages.some((m) => m.sender === "me");
  const otherUserSent = messages.some((m) => m.sender === "other");

  useEffect(() => {
    if (!chatId || !userId) return;
    let disposed = false;
    let reconnectTimer: ReturnType<typeof setTimeout> | undefined;
    let stableTimer: ReturnType<typeof setTimeout> | undefined;
    let reconnectAttempt = 0;
    let connecting = false;

    const mapMessage = (message: MessageData): ChatMessageInfo => ({
      id: String(message.id),
      text: message.content,
      sender: message.senderId === userId ? "me" : "other",
      created_at: message.createdAt,
    });
    const mergeMessages = (incoming: ChatMessageInfo[]) => {
      setMessages((previous) => {
        const existingIds = new Set(previous.map((message) => message.id));
        const merged = [...previous, ...incoming.filter((message) => !existingIds.has(message.id))]
          .sort((left, right) => new Date(left.created_at ?? 0).getTime() - new Date(right.created_at ?? 0).getTime());
        messagesRef.current = merged;
        return merged;
      });
    };
    const failPending = () => {
      const pending = pendingTextRef.current;
      if (!pending || disposed) return;
      pendingTextRef.current = null;
      setPendingText(null);
      setInputText((current) => current ? `${pending}\n${current}` : pending);
      Alert.alert("发送失败", "消息未发送，请检查连接后重试");
    };
    const backfillNewest = async () => {
      try {
        const knownIds = new Set(messagesRef.current.map((message) => message.id));
        const incoming: MessageData[] = [];
        let cursor: string | undefined;
        let firstPage: Awaited<ReturnType<typeof getMessagesPage>> | null = null;
        let lastPage: Awaited<ReturnType<typeof getMessagesPage>> | null = null;
        do {
          const page = await getMessagesPage(chatId, cursor);
          firstPage ??= page;
          lastPage = page;
          incoming.push(...page.items);
          if (
            knownIds.size === 0
            || page.items.some((message) => knownIds.has(String(message.id)))
            || !page.hasMore
            || !page.nextCursor
          ) {
            break;
          }
          cursor = page.nextCursor;
        } while (!disposed);
        if (disposed) return;
        mergeMessages(incoming.map(mapMessage));

        const pending = pendingTextRef.current;
        if (pending) {
          const wasPersisted = incoming.some(
            (message) =>
              !knownIds.has(String(message.id))
              && message.senderId === userId
              && message.content === pending,
          );
          if (wasPersisted) {
            pendingTextRef.current = null;
            setPendingText(null);
          } else if (knownIds.size > 0) {
            failPending();
          }
        }

        if (!historyInitializedRef.current) {
          historyInitializedRef.current = true;
          setNextMessageCursor(lastPage?.nextCursor ?? firstPage?.nextCursor ?? null);
          setHasOlderMessages(lastPage?.hasMore ?? firstPage?.hasMore ?? false);
        }
      } catch {
        // A later reconnect or the concurrent initial request will retry the backfill.
      } finally {
        if (!disposed) setLoading(false);
      }
    };
    const scheduleReconnect = () => {
      if (disposed || reconnectTimer) return;
      const delay = Math.min(1000 * (2 ** reconnectAttempt), 30_000);
      reconnectAttempt += 1;
      reconnectTimer = setTimeout(() => {
        reconnectTimer = undefined;
        void connect();
      }, delay);
    };

    const connect = async () => {
      if (disposed || connecting) return;
      connecting = true;
      try {
        const socket = await openChatSocket(chatId);
        if (disposed) return socket.close();
        socketRef.current = socket;
        socket.onopen = () => {
          stableTimer = setTimeout(() => { reconnectAttempt = 0; }, 10_000);
          void backfillNewest();
        };
        socket.onmessage = (event) => {
          const message = JSON.parse(String(event.data)) as {
            id: number;
            senderId: number;
            content: string;
            sentAt: string;
          };
          mergeMessages([mapMessage({ ...message, createdAt: message.sentAt })]);
          if (message.senderId === userId && message.content === pendingTextRef.current) {
            pendingTextRef.current = null;
            setPendingText(null);
          }
          setTimeout(() => flatListRef.current?.scrollToEnd({ animated: true }), 0);
        };
        socket.onclose = () => {
          if (stableTimer) clearTimeout(stableTimer);
          if (socketRef.current === socket) socketRef.current = null;
          scheduleReconnect();
        };
      } catch {
        scheduleReconnect();
      } finally {
        connecting = false;
      }
    };

    setLoading(true);
    setMessages([]);
    messagesRef.current = [];
    pendingTextRef.current = null;
    setPendingText(null);
    historyInitializedRef.current = false;
    void connect();
    Promise.all([getChatById(chatId, userId), getMessagesPage(chatId)])
      .then(([chat, messagePage]) => {
        if (disposed) return;
        setChatStatus(chat.chatStatus);
        setOtherUserId(chat.otherUserId);
        setOtherUser(chat.otherUser);
        if (chat.recruitmentId) {
          void getRecruitmentById(chat.recruitmentId).then(setRecruitment).catch(() => setRecruitment(null));
        }
        mergeMessages(messagePage.items.map(mapMessage));
        if (!historyInitializedRef.current) {
          historyInitializedRef.current = true;
          setNextMessageCursor(messagePage.nextCursor);
          setHasOlderMessages(messagePage.hasMore);
        }
        setLoading(false);
        setTimeout(() => flatListRef.current?.scrollToEnd({ animated: false }), 100);
      })
      .catch(() => setLoading(false));

    return () => {
      disposed = true;
      if (reconnectTimer) clearTimeout(reconnectTimer);
      if (stableTimer) clearTimeout(stableTimer);
      socketRef.current?.close();
      socketRef.current = null;
    };
  }, [chatId, userId]);

  const loadOlderMessages = async () => {
    if (!chatId || !userId || !hasOlderMessages || !nextMessageCursor || loadingOlder) return;
    setLoadingOlder(true);
    try {
      const page = await getMessagesPage(chatId, nextMessageCursor);
      const older = page.items.map((message) => ({
        id: String(message.id),
        text: message.content,
        sender: message.senderId === userId ? "me" : "other",
        created_at: message.createdAt,
      }));
      setMessages((previous) => {
        const ids = new Set(previous.map((message) => message.id));
        const merged = [...older.filter((message) => !ids.has(message.id)), ...previous];
        messagesRef.current = merged;
        return merged;
      });
      setNextMessageCursor(page.nextCursor);
      setHasOlderMessages(page.hasMore);
    } finally {
      setLoadingOlder(false);
    }
  };

  useEffect(() => {
    const showSub = Keyboard.addListener(
      Platform.OS === "ios" ? "keyboardWillShow" : "keyboardDidShow",
      (e) => {
        const targetOffset = currentScrollOffset + e.endCoordinates.height;
        flatListRef.current?.scrollToOffset({
          offset: targetOffset,
          animated: false,
        });
      },
    );
    const hideSub = Keyboard.addListener(
      Platform.OS === "ios" ? "keyboardWillHide" : "keyboardDidHide",
      (e) => {
        const targetOffset = currentScrollOffset - e.endCoordinates.height;
        flatListRef.current?.scrollToOffset({
          offset: targetOffset,
          animated: false,
        });
      },
    );
    return () => {
      showSub.remove();
      hideSub.remove();
    };
  }, [currentScrollOffset]);

  const handleSendMessage = async () => {
    if (!userId || !chatId || !otherUserId || pendingTextRef.current) return;
    if (chatStatus === "限制" && currentUserSent && !otherUserSent) return;

    const content = inputText.trim();
    if (!content) return;
    if (content.length > 4000) {
      Alert.alert("无法发送", "消息最多 4000 个字符");
      return;
    }
    const socket = socketRef.current;
    if (socket?.readyState !== WebSocket.OPEN) {
      Alert.alert("无法发送", "聊天正在重新连接，请稍后重试");
      return;
    }
    pendingTextRef.current = content;
    setPendingText(content);
    setInputText("");
    try {
      socket.send(JSON.stringify({ content }));
    } catch {
      pendingTextRef.current = null;
      setPendingText(null);
      setInputText(content);
      Alert.alert("发送失败", "消息未发送，请检查连接后重试");
    }
  };

  const renderMessage = ({ item }: { item: ChatMessageInfo }) => (
    <ChatMessage message={item} />
  );

  const handleRecruitmentPress = async () => {
    if (!recruitment?.id) return;
    try {
      const fullRecruitment = await getRecruitmentById(recruitment.id);
      router.push({
        pathname: '/recruitment-detail' as any,
        params: { recruitmentId: fullRecruitment.id.toString() }
      });
    } catch (reason) {
      if (reason instanceof ApiError && reason.status === 404) {
        Alert.alert("招募不存在", "该招募已被删除");
        return;
      }
      Alert.alert("加载失败", reason instanceof Error ? reason.message : "请稍后重试");
    }
  };

  // 限制+己方发过+对方没发过 → 不能发
  // 限制+对方发过 → 能发（发后变开放）
  // 限制+双方都没发过 → 能发一条
  const canSend =
    chatStatus === "开放" ||
    (chatStatus === "限制" && !(currentUserSent && !otherUserSent));
  const canSubmit = canSend && !pendingText;

  const getStatusHint = (): string => {
    if (chatStatus !== "限制") {
      return "";
    }
    if (currentUserSent && !otherUserSent) {
      return "等待对方回复中...";
    }
    if (!currentUserSent && !otherUserSent) {
      return "在对方回复前只能发送一条消息";
    }
    return "您可以发送消息";
  };

  if (loading || !otherUser) {
    return (
      <View style={[styles.container, { backgroundColor: colors.surface }]}>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={colors.primary} />
        </View>
      </View>
    );
  }

  return (
    <KeyboardAvoidingView
      style={[styles.container, { backgroundColor: colors.surface }]}
      behavior={Platform.OS === "ios" ? "padding" : "height"}
      keyboardVerticalOffset={statusBarHeight}
    >
      <View
        style={[
          styles.header,
          {
            backgroundColor: colors.card,
            borderBottomColor: colors.headerBorder,
          },
        ]}
      >
        <TouchableOpacity onPress={() => router.back()}>
          <Text style={[styles.backButton, { color: colors.primary }]}>
            ← 返回
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={styles.headerTitleContainer}
          onPress={() => {
            if (otherUserId)
              router.push(`/personal-page?userId=${otherUserId}`);
          }}
        >
          <MediaImage
            uri={otherUser.avatar}
            style={[styles.headerAvatar, { backgroundColor: colors.primary }]}
          />
          <Text style={[styles.headerTitle, { color: colors.text }]}>
            {(otherUser?.nickname || otherUser?.username) ?? "聊天"}
          </Text>
        </TouchableOpacity>
        <View style={styles.placeholder} />
      </View>

      {recruitment && (
        <TouchableOpacity
          style={[
            styles.recruitmentBar,
            {
              backgroundColor: colors.card,
              borderBottomColor: colors.headerBorder,
            },
          ]}
          onPress={handleRecruitmentPress}
        >
          <MediaImage uri={recruitment.gameIcon} style={styles.recruitmentIcon} />
          <Text
            style={[styles.recruitmentTitle, { color: colors.textSecondary }]}
            numberOfLines={1}
          >
            {recruitment.title}
          </Text>
          <Text
            style={[styles.recruitmentArrow, { color: colors.textTertiary }]}
          >
            ›
          </Text>
        </TouchableOpacity>
      )}

      <FlatList
        ref={flatListRef}
        data={messages}
        keyExtractor={(item) => item.id}
        renderItem={renderMessage}
        style={styles.messageList}
        contentContainerStyle={styles.messageContainer}
        ListHeaderComponent={loadingOlder ? <ActivityIndicator color={colors.primary} /> : null}
        maintainVisibleContentPosition={{ minIndexForVisible: 0 }}
        onScroll={(e) => {
          const offset = e.nativeEvent.contentOffset.y;
          setCurrentScrollOffset(offset);
          if (offset <= 20) loadOlderMessages();
        }}
        scrollEventThrottle={16}
      />

      <View
        style={[
          styles.inputBar,
          { backgroundColor: colors.inputBarBackground },
        ]}
      >
        <TextInput
          style={[
            styles.textInput,
            {
              borderColor: colors.inputBorder,
              backgroundColor: colors.textInputBackground,
              color: colors.inputText,
            },
            !canSend && styles.textInputDisabled,
          ]}
          placeholder={canSend ? "输入消息..." : getStatusHint()}
          placeholderTextColor={colors.textTertiary}
          value={inputText}
          onChangeText={setInputText}
          maxLength={4000}
          multiline
          editable={canSend}
        />
        <TouchableOpacity
          style={[
            styles.sendButton,
            { backgroundColor: canSubmit ? colors.primary : colors.textTertiary },
          ]}
          onPress={handleSendMessage}
          disabled={!canSubmit}
        >
          <Text style={styles.sendButtonText}>发送</Text>
        </TouchableOpacity>
      </View>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  header: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
  },
  backButton: { fontSize: 16 },
  headerTitle: { fontSize: 18, fontWeight: "bold" },
  headerTitleContainer: {
    flexDirection: "row",
    alignItems: "center",
    flexShrink: 1,
    paddingHorizontal: 8,
  },
  headerAvatar: { width: 28, height: 28, borderRadius: 14, marginRight: 6 },
  placeholder: { width: 50 },
  recruitmentBar: {
    flexDirection: "row",
    alignItems: "center",
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderBottomWidth: 1,
  },
  recruitmentIcon: { width: 20, height: 20, borderRadius: 4, marginRight: 8 },
  recruitmentTitle: { flex: 1, fontSize: 13 },
  recruitmentArrow: { fontSize: 18, marginLeft: 8 },
  messageList: { flex: 1 },
  messageContainer: { padding: 10 },
  loadingContainer: { flex: 1, justifyContent: "center", alignItems: "center" },
  statusBar: {
    paddingHorizontal: 16,
    paddingVertical: 6,
    alignItems: "center",
  },
  statusText: { fontSize: 13, fontWeight: "500" },
  inputBar: {
    flexDirection: "row",
    alignItems: "flex-end",
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderTopWidth: 0,
  },
  textInput: {
    flex: 1,
    minHeight: 40,
    maxHeight: 100,
    borderWidth: 1,
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 8,
    fontSize: 16,
    textAlignVertical: "center",
  },
  textInputDisabled: { opacity: 0.5 },
  sendButton: {
    marginLeft: 8,
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 20,
  },
  sendButtonText: { color: "#fff", fontSize: 16, fontWeight: "600" },
});
