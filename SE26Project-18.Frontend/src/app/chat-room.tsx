import { useLocalSearchParams, useRouter } from "expo-router";
import { useEffect, useRef, useState } from "react";
import {
    ActivityIndicator,
    FlatList,
    Image,
    Keyboard,
    KeyboardAvoidingView,
    Modal,
    Platform,
    Pressable,
    StatusBar,
    StyleSheet,
    Text,
    TextInput,
    ToastAndroid,
    TouchableOpacity,
    View,
} from "react-native";
import {
    ChatStatus,
    getChatById,
    getMessagesByChatId,
    getRecruitmentById,
    getUserById,
    markMessagesRead,
    MessageData,
    RecruitmentData,
    sendMessage,
    UserInfo,
} from "../api/api";
import { MessageDto } from "../api/dtos";
import ChatMessage, { ChatMessageInfo } from "../components/chat-message";
import { useAuth } from "../contexts/auth-context";
import { useSignalR } from "../contexts/signalr-context";
import { useTheme } from "../contexts/theme-context";

const testImage = require("../../assets/images/testImage.png");

export default function ChatRoomScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ chatId?: string }>();
  const { colors } = useTheme();
  const { userId } = useAuth();
  const { joinChat, leaveChat, onReceiveMessage, isConnected } = useSignalR();
  const statusBarHeight =
    Platform.OS === "ios" ? 0 : StatusBar.currentHeight || 0;
  const flatListRef = useRef<FlatList>(null);

  const chatId = params.chatId ? Number(params.chatId) : undefined;

  const [messages, setMessages] = useState<ChatMessageInfo[]>([]);
  const [inputText, setInputText] = useState("");
  const [loading, setLoading] = useState(true);
  const [currentScrollOffset, setCurrentScrollOffset] = useState(0);
  const [chatStatus, setChatStatus] = useState<ChatStatus>("限制");
  const [otherUserId, setOtherUserId] = useState<number | null>(null);
  const [otherUser, setOtherUser] = useState<UserInfo | null>(null);
  const [recruitment, setRecruitment] = useState<RecruitmentData | null>(null);
  const [moreMenuVisible, setMoreMenuVisible] = useState(false);

  // 基于实际消息列表判断
  const currentUserSent = messages.some((m) => m.sender === "me");
  const otherUserSent = messages.some((m) => m.sender === "other");

  useEffect(() => {
    if (chatId) {
      setLoading(true);
      getChatById(chatId).then((chat) => {
        if (chat) {
          setChatStatus(chat.chatStatus);
          const chatOtherUser = chat.users?.find((u) => u.userId !== userId);
          setOtherUserId(chatOtherUser?.userId ?? null);
          if (chatOtherUser?.userId) {
            getUserById(chatOtherUser.userId).then((user) => {
              if (user) setOtherUser(user);
            });
          }
          if (chat.recruitmentId) {
            getRecruitmentById(chat.recruitmentId).then((rec) => {
              if (rec) setRecruitment(rec);
            });
          }
        }
      });

      getMessagesByChatId(chatId).then((data) => {
        const chatMessages: ChatMessageInfo[] = data.map(
          (msg: MessageData) => ({
            id: String(msg.id),
            text: msg.content,
            sender: msg.senderId === userId ? "me" : "other",
            created_at: msg.createdAt,
          }),
        );
        setMessages(chatMessages);
        setLoading(false);
        setTimeout(
          () => flatListRef.current?.scrollToEnd({ animated: true }),
          150,
        );
      });
    }
  }, [chatId, userId]);

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

  useEffect(() => {
    if (!chatId || !userId) return;
    joinChat(chatId);
    markMessagesRead(chatId, userId);
    return () => {
      leaveChat(chatId);
    };
  }, [chatId, userId, joinChat, leaveChat]);

  useEffect(() => {
    if (isConnected && chatId) {
      joinChat(chatId);
    }
  }, [isConnected, chatId, joinChat]);

  useEffect(() => {
    const unsub = onReceiveMessage((msg: MessageDto) => {
      if (msg.chat_id !== chatId) return;
      if (msg.sender_id === userId) return;

      const newMsg: ChatMessageInfo = {
        id: String(msg.id),
        text: msg.content,
        sender: "other",
        created_at: msg.created_at,
      };

      setMessages((prev) => {
        if (prev.some((m) => m.id === newMsg.id)) return prev;
        return [...prev, newMsg];
      });

      setTimeout(() => flatListRef.current?.scrollToEnd({ animated: true }), 150);

      if (userId) {
        markMessagesRead(chatId!, userId);
      }
    });

    return unsub;
  }, [chatId, userId, onReceiveMessage]);

  const handleSendMessage = async () => {
    if (!inputText.trim() || !userId || !chatId || !otherUserId) return;
    if (chatStatus === "关闭") return;
    if (chatStatus === "限制" && currentUserSent && !otherUserSent) return;

    const content = inputText.trim();
    setInputText("");

    const optimisticMsg: ChatMessageInfo = {
      id: Date.now().toString(),
      text: content,
      sender: "me",
      created_at: new Date().toISOString(),
    };
    setMessages((prev) => [...prev, optimisticMsg]);
    setTimeout(() => flatListRef.current?.scrollToEnd({ animated: true }), 0);

    try {
      await sendMessage({
        chatId,
        senderId: userId,
        receiverId: otherUserId,
        content,
      });
      // 限制状态下，发送消息后如果对方已发过消息，则更新为开放
      if (chatStatus === "限制" && otherUserSent) {
        setChatStatus("开放");
      }
    } catch (error) {
      console.error("Failed to send message:", error);
    }
  };

  const renderMessage = ({ item }: { item: ChatMessageInfo }) => (
    <ChatMessage message={item} />
  );

  const handleRecruitmentPress = async () => {
    if (!recruitment?.id) return;
    const fullRecruitment = await getRecruitmentById(recruitment.id);
    if (!fullRecruitment) return;
    if (fullRecruitment.status === "已删除") {
      ToastAndroid.show("该招募已被删除", ToastAndroid.SHORT);
      return;
    }
    router.push({
      pathname: '/recruitment-detail' as any,
      params: { recruitmentId: fullRecruitment.id.toString() }
    });
  };

  // 限制+己方发过+对方没发过 → 不能发
  // 限制+对方发过 → 能发（发后变开放）
  // 限制+双方都没发过 → 能发一条
  const canSend =
    chatStatus === "开放" ||
    (chatStatus === "限制" && !(currentUserSent && !otherUserSent));

  const getStatusHint = (): string => {
    if (chatStatus !== "限制") {
      return chatStatus === "关闭" ? "聊天已关闭" : "";
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
          <Image
            source={testImage}
            style={[styles.headerAvatar, { backgroundColor: colors.primary }]}
          />
          <Text style={[styles.headerTitle, { color: colors.text }]}>
            {(otherUser?.nickname || otherUser?.username) ?? "聊天"}
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          onPress={() => setMoreMenuVisible(true)}
          style={styles.moreButtonContainer}
        >
          <Text style={[styles.moreButton, { color: colors.text }]}>⋯</Text>
        </TouchableOpacity>

        <Modal
          visible={moreMenuVisible}
          transparent
          animationType="fade"
          onRequestClose={() => setMoreMenuVisible(false)}
        >
          <Pressable
            style={[styles.overlay, { backgroundColor: colors.overlay }]}
            onPress={() => setMoreMenuVisible(false)}
          >
            <View
              style={[
                styles.dropdownMenu,
                { backgroundColor: colors.card },
              ]}
            >
              <TouchableOpacity
                style={styles.dropdownItem}
                onPress={() => {
                  setMoreMenuVisible(false);
                  router.push({
                    pathname: "/report" as any,
                    params: {
                      targetType: "聊天",
                      targetId: String(chatId ?? 0),
                    },
                  });
                }}
              >
                <Text style={[styles.dropdownItemText, { color: colors.text }]}>
                  举报
                </Text>
              </TouchableOpacity>
            </View>
          </Pressable>
        </Modal>
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
          <Image source={testImage} style={styles.recruitmentIcon} />
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
        onScroll={(e) => setCurrentScrollOffset(e.nativeEvent.contentOffset.y)}
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
          multiline
          editable={canSend}
        />
        <TouchableOpacity
          style={[
            styles.sendButton,
            { backgroundColor: canSend ? colors.primary : colors.textTertiary },
          ]}
          onPress={handleSendMessage}
          disabled={!canSend}
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
  moreButton: { fontSize: 22, fontWeight: "600", lineHeight: 24 },
  moreButtonContainer: { paddingHorizontal: 4, paddingVertical: 2 },
  overlay: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    paddingHorizontal: 40,
  },
  dropdownMenu: {
    width: "100%",
    borderRadius: 12,
    paddingVertical: 4,
    overflow: "hidden",
  },
  dropdownItem: {
    paddingHorizontal: 16,
    paddingVertical: 14,
  },
  dropdownItemText: {
    fontSize: 16,
  },
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