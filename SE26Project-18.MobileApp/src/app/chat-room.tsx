import { useLocalSearchParams, useRouter } from "expo-router";
import { useEffect, useRef, useState } from "react";
import {
    ActivityIndicator,
    FlatList,
    Image,
    Keyboard,
    KeyboardAvoidingView,
    Platform,
    StatusBar,
    StyleSheet,
    Text,
    TextInput,
    ToastAndroid,
    TouchableOpacity,
    View,
} from "react-native";
import {
    getChatById,
    getMessagesByChatId,
    getUserById,
    sendMessage,
    UserResponse,
    ChatResponse,
} from "../api/api";
import ChatMessage, { ChatMessageInfo } from "../components/chat-message";
import { useAuth } from "../contexts/auth-context";
import { useTheme } from "../contexts/theme-context";

const testImage = require("../../assets/images/testImage.png");

// 后端 ChatStatus: "Restricted" | "Free"
type ChatStatus = "Restricted" | "Free" | "Closed";

export default function ChatRoomScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ chatId?: string }>();
  const { colors } = useTheme();
  const { userId } = useAuth();
  const statusBarHeight =
    Platform.OS === "ios" ? 0 : StatusBar.currentHeight || 0;
  const flatListRef = useRef<FlatList>(null);

  const chatId = params.chatId ? Number(params.chatId) : undefined;

  const [messages, setMessages] = useState<ChatMessageInfo[]>([]);
  const [inputText, setInputText] = useState("");
  const [loading, setLoading] = useState(true);
  const [currentScrollOffset, setCurrentScrollOffset] = useState(0);
  const [chatStatus, setChatStatus] = useState<ChatStatus>("Restricted");
  const [otherUserId, setOtherUserId] = useState<number | null>(null);
  const [otherUser, setOtherUser] = useState<UserResponse | null>(null);
  const [recruitmentTitle, setRecruitmentTitle] = useState<string>("");

  const currentUserSent = messages.some((m) => m.sender === "me");
  const otherUserSent = messages.some((m) => m.sender === "other");

  useEffect(() => {
    if (chatId) {
      setLoading(true);
      getChatById(chatId).then((chat) => {
        if (chat) {
          setChatStatus(chat.status === "Free" ? "Free" : "Restricted");
          // 新的 ChatResponse 用 user1Id/user2Id
          setOtherUserId(chat.user1Id === userId ? chat.user2Id : chat.user1Id);
        }
      });

      // Messages 后端待实现，先用空数组
      getMessagesByChatId(chatId).then((data) => {
        const chatMessages: ChatMessageInfo[] = (data || []).map(
          (msg: any) => ({
            id: String(msg.id || Date.now()),
            text: msg.content || msg.text || "",
            sender: msg.senderId === userId ? "me" : "other",
            created_at: msg.created_at || msg.sentAt || new Date().toISOString(),
          }),
        );
        setMessages(chatMessages);
        setLoading(false);
        setTimeout(
          () => flatListRef.current?.scrollToEnd({ animated: false }),
          100,
        );
      });
    }
  }, [chatId, userId]);

  useEffect(() => {
    if (otherUserId) {
      getUserById(otherUserId).then((user) => {
        setOtherUser(user);
      });
    }
  }, [otherUserId]);

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
    if (!inputText.trim() || !userId || !chatId) return;
    if (chatStatus === "Closed") return;
    if (chatStatus === "Restricted" && currentUserSent && !otherUserSent) return;

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
      await sendMessage({ chatId, content });
      if (chatStatus === "Restricted" && otherUserSent) {
        setChatStatus("Free");
      }
    } catch (error) {
      console.error("Failed to send message:", error);
    }
  };

  const renderMessage = ({ item }: { item: ChatMessageInfo }) => (
    <ChatMessage message={item} />
  );

  const handleRecruitmentPress = async () => {
    ToastAndroid.show("招募详情暂不可用", ToastAndroid.SHORT);
  };

  const canSend =
    chatStatus === "Free" ||
    (chatStatus === "Restricted" && !(currentUserSent && !otherUserSent));

  const getStatusHint = (): string => {
    if (chatStatus !== "Restricted") {
      return chatStatus === "Closed" ? "聊天已关闭" : "";
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
        <View style={styles.placeholder} />
      </View>

      {recruitmentTitle ? (
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
            {recruitmentTitle}
          </Text>
          <Text
            style={[styles.recruitmentArrow, { color: colors.textTertiary }]}
          >
            ›
          </Text>
        </TouchableOpacity>
      ) : null}

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