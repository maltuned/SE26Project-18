import { useEffect, useRef, useState } from 'react';
import { useLocalSearchParams, useRouter } from 'expo-router';
import {
  StyleSheet, Text, View, TextInput, Pressable, KeyboardAvoidingView,
  Platform, FlatList,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { LinearGradient } from 'expo-linear-gradient';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { chatSessions, getChat, currentUser } from '@/data/mock';
import { Avatar } from '@/components/Avatar';
import { ChevronLeft, Send, Smile, MoreVertical } from 'lucide-react-native';
import type { ChatMessage } from '@/data/types';

const BOT_REPLIES = [
  '收到，马上到。',
  '哈哈哈可以可以',
  '我准备好了，开打吧',
  '稳，这把有了',
  '等等我先热个手',
  '冲冲冲',
  '今天状态不错',
  '语音频道见',
  '我call中路',
  'nice！',
];

export default function ChatDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const router = useRouter();
  const sessionId = Number(id);
  const session = chatSessions.find((s) => s.id === sessionId);

  const [messages, setMessages] = useState<ChatMessage[]>(() =>
    session ? getChat(session.id) : [],
  );
  const [text, setText] = useState('');
  const [typing, setTyping] = useState(false);
  const listRef = useRef<FlatList<ChatMessage>>(null);

  useEffect(() => {
    if (session) setMessages(getChat(session.id));
  }, [session]);

  if (!session) {
    return (
      <View style={styles.center}>
        <Text style={{ color: Colors.neutral[100] }}>会话未找到</Text>
      </View>
    );
  }

  function send() {
    const t = text.trim();
    if (!t) return;
    const now = new Date();
    const time = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
    const myMsg: ChatMessage = {
      id: `me-${Date.now()}`,
      authorName: '你',
      authorAvatar: currentUser.avatar,
      text: t,
      time,
      isMe: true,
    };
    setMessages((m) => [...m, myMsg]);
    setText('');
    setTyping(true);
    const reply = BOT_REPLIES[Math.floor(Math.random() * BOT_REPLIES.length)];
    const pName = session!.participantName;
    const pAvatar = session!.participantAvatar;
    setTimeout(() => {
      setMessages((m) => [
        ...m,
        {
          id: `b-${Date.now()}`,
          authorName: pName,
          authorAvatar: pAvatar,
          text: reply,
          time,
        },
      ]);
      setTyping(false);
    }, 1100 + Math.random() * 900);
  }

  const accent = Colors.primary[400];

  return (
    <View style={styles.screen}>
      <LinearGradient colors={[`${accent}26`, Colors.ink[900]]} locations={[0, 0.18]} style={StyleSheet.absoluteFill} />

      {/* Header */}
      <SafeAreaView edges={['top']} style={styles.headerSafe}>
        <View style={styles.header}>
          <Pressable onPress={() => router.back()} style={styles.backBtn}>
            <ChevronLeft color={Colors.neutral[50]} size={24} />
          </Pressable>
          <View style={styles.headerCenter}>
            <Avatar uri={session.participantAvatar} name={session.participantName} size={36} online={session.online} />
            <View style={{ flex: 1 }}>
              <Text style={styles.headerTitle} numberOfLines={1}>{session.participantName}</Text>
              <Text style={styles.headerSub}>{session.gameName}</Text>
            </View>
          </View>
          <Pressable style={styles.iconBtn}><MoreVertical color={Colors.neutral[100]} size={18} /></Pressable>
        </View>
      </SafeAreaView>

      {/* Messages */}
      <FlatList
        ref={listRef}
        data={messages}
        keyExtractor={(m) => m.id}
        contentContainerStyle={{ paddingHorizontal: Spacing.lg, paddingTop: Spacing.md, paddingBottom: Spacing.lg }}
        onContentSizeChange={() => listRef.current?.scrollToEnd({ animated: true })}
        onLayout={() => listRef.current?.scrollToEnd({ animated: false })}
        showsVerticalScrollIndicator={false}
        ItemSeparatorComponent={() => <View style={{ height: Spacing.md }} />}
        renderItem={({ item }) =>
          item.isSystem ? (
            <SystemBubble text={item.text} />
          ) : (
            <MessageBubble msg={item} accent={accent} />
          )
        }
        ListFooterComponent={typing ? <TypingIndicator accent={accent} name={session.participantName} /> : null}
      />

      {/* Composer */}
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} enabled>
        <SafeAreaView edges={['bottom']} style={styles.composerSafe}>
          <View style={styles.composer}>
            <Pressable style={styles.composeIcon}><Smile color={Colors.neutral[300]} size={22} /></Pressable>
            <TextInput
              value={text}
              onChangeText={setText}
              placeholder="发送消息…"
              placeholderTextColor={Colors.neutral[400]}
              style={styles.input}
              multiline
              maxLength={500}
            />
            <Pressable
              onPress={send}
              disabled={!text.trim()}
              style={({ pressed }) => [
                styles.sendBtn,
                { backgroundColor: text.trim() ? accent : Colors.ink[700] },
                pressed && { opacity: 0.85 },
              ]}
            >
              <Send color={text.trim() ? Colors.ink[900] : Colors.neutral[400]} size={18} />
            </Pressable>
          </View>
        </SafeAreaView>
      </KeyboardAvoidingView>
    </View>
  );
}

function MessageBubble({ msg, accent }: { msg: ChatMessage; accent: string }) {
  return (
    <View style={[styles.msgRow, msg.isMe && styles.msgRowMe]}>
      {!msg.isMe && <Avatar uri={msg.authorAvatar} name={msg.authorName} size={34} ring={accent} />}
      <View style={[styles.msgCol, msg.isMe && styles.msgColMe]}>
        {!msg.isMe && <Text style={styles.msgAuthor}>{msg.authorName}</Text>}
        <View style={[styles.bubble, msg.isMe ? [styles.bubbleMe, { backgroundColor: accent }] : styles.bubbleThem]}>
          <Text style={[styles.bubbleText, msg.isMe && styles.bubbleTextMe]}>{msg.text}</Text>
        </View>
        <Text style={styles.msgTime}>{msg.time}</Text>
      </View>
    </View>
  );
}

function SystemBubble({ text }: { text: string }) {
  return (
    <View style={styles.systemWrap}>
      <View style={styles.systemBubble}>
        <Text style={styles.systemText}>{text}</Text>
      </View>
    </View>
  );
}

function TypingIndicator({ accent, name }: { accent: string; name: string }) {
  return (
    <View style={styles.msgRow}>
      <Avatar name={name} size={34} ring={accent} />
      <View style={styles.msgCol}>
        <View style={[styles.bubble, styles.bubbleThem, styles.typingBubble]}>
          <View style={styles.typingDot} />
          <View style={styles.typingDot} />
          <View style={styles.typingDot} />
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center' },
  headerSafe: { backgroundColor: 'transparent' },
  header: {
    flexDirection: 'row', alignItems: 'center', paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm, gap: Spacing.xs,
  },
  backBtn: { width: 40, height: 40, borderRadius: 999, alignItems: 'center', justifyContent: 'center' },
  headerCenter: { flex: 1, flexDirection: 'row', alignItems: 'center', gap: Spacing.sm },
  headerTitle: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.base },
  headerSub: { color: Colors.neutral[200], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody },
  iconBtn: { width: 36, height: 36, borderRadius: 999, alignItems: 'center', justifyContent: 'center' },
  // Messages
  msgRow: { flexDirection: 'row', gap: Spacing.sm, maxWidth: '100%' },
  msgRowMe: { flexDirection: 'row-reverse' },
  msgCol: { flex: 1, maxWidth: '78%' },
  msgColMe: { alignItems: 'flex-end' },
  msgAuthor: { color: Colors.neutral[200], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBodyMedium, marginBottom: 3 },
  bubble: { paddingHorizontal: Spacing.md, paddingVertical: Spacing.md, borderRadius: Radius.lg },
  bubbleThem: { backgroundColor: Colors.ink[800], borderBottomLeftRadius: Radius.sm, borderWidth: 1, borderColor: Colors.border },
  bubbleMe: { borderBottomRightRadius: Radius.sm },
  bubbleText: { color: Colors.neutral[50], fontSize: Typography.sizes.base, fontFamily: Typography.fontFamilyBody, lineHeight: 21 },
  bubbleTextMe: { color: Colors.ink[900], fontFamily: Typography.fontFamilyBodyMedium },
  msgTime: { color: Colors.neutral[400], fontSize: 10, marginTop: 3, fontFamily: Typography.fontFamilyBody },
  systemWrap: { alignItems: 'center', paddingVertical: Spacing.xs },
  systemBubble: {
    backgroundColor: `${Colors.ink[700]}AA`, paddingHorizontal: Spacing.md,
    paddingVertical: 6, borderRadius: Radius.pill,
  },
  systemText: { color: Colors.neutral[200], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBodyMedium },
  typingBubble: { flexDirection: 'row', alignItems: 'center', gap: 4, paddingVertical: Spacing.md },
  typingDot: { width: 7, height: 7, borderRadius: 999, backgroundColor: Colors.neutral[300] },
  // Composer
  composerSafe: { backgroundColor: Colors.ink[850], borderTopWidth: 1, borderTopColor: Colors.border },
  composer: {
    flexDirection: 'row', alignItems: 'flex-end', gap: Spacing.sm,
    paddingHorizontal: Spacing.md, paddingVertical: Spacing.sm, minHeight: 56,
  },
  composeIcon: { width: 40, height: 40, borderRadius: 999, alignItems: 'center', justifyContent: 'center' },
  input: {
    flex: 1, color: Colors.neutral[50], fontFamily: Typography.fontFamilyBody,
    fontSize: Typography.sizes.base, backgroundColor: Colors.ink[800],
    borderRadius: Radius.lg, paddingHorizontal: Spacing.md, paddingVertical: Spacing.md,
    maxHeight: 100, borderWidth: 1, borderColor: Colors.border,
  },
  sendBtn: { width: 40, height: 40, borderRadius: 999, alignItems: 'center', justifyContent: 'center' },
});
