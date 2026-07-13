import { useState } from 'react';
import { useLocalSearchParams, useRouter } from 'expo-router';
import {
  StyleSheet, Text, View, ScrollView, Pressable, Alert, Modal, TextInput,
  KeyboardAvoidingView, Platform,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { LinearGradient } from 'expo-linear-gradient';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { posts, tags, getUserById, chatSessions, chatMessages, currentUserId } from '@/data/mock';
import { Avatar } from '@/components/Avatar';
import { Chip } from '@/components/Chip';
import { Button } from '@/components/Button';
import { ReportModal } from '@/components/ReportModal';
import {
  ChevronLeft, Users, Mic, MicOff, Gamepad2, Clock, MessageSquare,
  Send, TrendingUp, Monitor, Smartphone, Globe, Pencil, AlertTriangle,
} from 'lucide-react-native';
import type { RecruitPost } from '@/data/types';

function tagName(id: number): string {
  return tags.find((t) => t.id === id)?.name ?? '';
}
function tagColor(id: number): string {
  return tags.find((t) => t.id === id)?.accentColor ?? Colors.primary[400];
}
function durationLabel(minutes: number): string {
  if (minutes <= 30) return '30分钟';
  if (minutes <= 1440) return '24小时';
  return '7天';
}
function modeLabel(m: RecruitPost['mode']) {
  return m === 'ranked' ? '排位' : m === 'tournament' ? '比赛' : '休闲';
}
function platformIcon(p: string) {
  if (p.includes('PC') && (p.includes('手机') || p.includes('主机'))) return <Globe color={Colors.neutral[200]} size={14} />;
  if (p.includes('手机')) return <Smartphone color={Colors.neutral[200]} size={14} />;
  return <Monitor color={Colors.neutral[200]} size={14} />;
}

export default function PostDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const router = useRouter();
  const post = posts.find((p) => p.id === Number(id));

  if (!post) {
    return (
      <View style={styles.center}>
        <Text style={styles.notFound}>招募未找到</Text>
        <Pressable onPress={() => router.back()} style={styles.backBtn}>
          <Text style={styles.backBtnText}>返回</Text>
        </Pressable>
      </View>
    );
  }

  const [showReport, setShowReport] = useState(false);
  const [showRespond, setShowRespond] = useState(false);
  const [greeting, setGreeting] = useState('');
  const isOwn = post.authorId === currentUserId;
  const author = getUserById(post.authorId);
  const open = post.filledCount < post.needCount;
  const tagAccent = post.tagIds.length > 0 ? tagColor(post.tagIds[0]) : Colors.primary[400];
  const authorName = post.authorName;
  const gameName = post.gameName;

  function sendGreeting() {
    const msg = greeting.trim();
    if (!msg) return;
    const session = chatSessions.find(
      (s) => s.participantName === authorName || s.gameName === gameName,
    );
    if (session && chatMessages[session.id]) {
      const now = new Date();
      const time = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
      chatMessages[session.id].push({
        id: `hi-${Date.now()}`,
        authorName: '你',
        authorAvatar: '',
        text: msg,
        time,
        isMe: true,
      });
    }
    Alert.alert('发送成功', `已向 ${authorName} 发送打招呼消息`);
    setShowRespond(false);
  }

  return (
    <View style={styles.screen}>
      <LinearGradient colors={[`${tagAccent}20`, Colors.ink[900]]} locations={[0, 0.22]} style={StyleSheet.absoluteFill} />

      {/* Nav */}
      <SafeAreaView edges={['top']} style={styles.navSafe}>
        <View style={styles.navBar}>
          <Pressable onPress={() => router.back()} style={styles.navBack}>
            <ChevronLeft color={Colors.neutral[50]} size={24} />
          </Pressable>
          <Text style={styles.navTitle}>招募详情</Text>
          {isOwn ? (
            <View style={{ width: 40 }} />
          ) : (
            <Pressable onPress={() => setShowReport(true)} style={styles.navBack}>
              <AlertTriangle color={Colors.neutral[300]} size={18} />
            </Pressable>
          )}
        </View>
      </SafeAreaView>

      <ScrollView
        showsVerticalScrollIndicator={false}
        contentContainerStyle={{ paddingBottom: Spacing.md }}
        style={{ flex: 1 }}
      >
        {/* ── Author card (tappable → user profile) ── */}
        <Pressable
          onPress={() => router.push({ pathname: '/user/[id]', params: { id: String(post.authorId) } })}
          style={styles.authorCard}
        >
          <View style={styles.authorCardInner}>
            <Avatar uri={post.authorAvatar} name={post.authorName} size={40} ring={tagAccent} />
            <View style={styles.authorInfo}>
              <View style={styles.authorNameRow}>
                <Text style={styles.authorName}>{post.authorName}</Text>
                <ChevronLeft color={Colors.neutral[400]} size={16} style={{ transform: [{ rotate: '180deg' }] }} />
              </View>
              <Text style={styles.authorHandle}>{author?.handle ?? ''}</Text>
            </View>
          </View>
        </Pressable>

        {/* ── Post details ── */}
        <View style={styles.section}>
          {/* Status + title */}
          <View style={styles.titleRow}>
            <Text style={styles.title}>{post.title}</Text>
            <View style={[styles.statusPill, { backgroundColor: open ? `${Colors.secondary[500]}22` : `${Colors.neutral[400]}22` }]}>
              <View style={[styles.statusDot, { backgroundColor: open ? Colors.secondary[400] : Colors.neutral[300] }]} />
              <Text style={[styles.statusText, { color: open ? Colors.secondary[400] : Colors.neutral[200] }]}>
                {open ? '招募中' : '已满'}
              </Text>
            </View>
          </View>

          {/* Game + time meta */}
          <View style={styles.metaRow}>
            <Gamepad2 color={tagAccent} size={16} />
            <Text style={styles.metaText}>{post.gameName}</Text>
            <Clock color={Colors.neutral[300]} size={16} style={{ marginLeft: Spacing.md }} />
            <Text style={styles.metaText}>{post.createdAt}</Text>
          </View>

          {/* Description */}
          <Text style={styles.desc}>{post.description || '暂无详细描述。'}</Text>

          {/* Tags */}
          <View style={styles.tagRow}>
            {post.tagIds.map((tid) => (
              <Chip key={tid} label={tagName(tid)} color={tagColor(tid)} size="md" />
            ))}
            <Chip label={durationLabel(post.durationMinutes)} color={Colors.neutral[400]} size="md" />
          </View>

          {/* Info grid */}
          <View style={styles.infoGrid}>
            <InfoCell
              icon={<Users color={tagAccent} size={18} />}
              label={`${post.filledCount}/${post.needCount} 已加入`}
              color={tagAccent}
            />
            <InfoCell
              icon={<Gamepad2 color={Colors.neutral[200]} size={18} />}
              label={modeLabel(post.mode)}
            />
            <InfoCell
              icon={post.voice === 'none' ? <MicOff color={Colors.neutral[200]} size={18} /> : <Mic color={Colors.neutral[200]} size={18} />}
              label={post.voice === 'required' ? '语音必开' : post.voice === 'optional' ? '语音可选' : '无需语音'}
            />
            <InfoCell
              icon={platformIcon(post.platform)}
              label={post.platform}
            />
          </View>
        </View>

      </ScrollView>

      {/* ── Fixed bottom button ── */}
      <SafeAreaView edges={['bottom']} style={styles.respondSafe}>
        {isOwn ? (
          <View style={styles.respondArea}>
            <Button
              label="修改招募"
              color={tagAccent}
              icon={<Pencil color={Colors.ink[900]} size={18} />}
              size="lg"
              onPress={() => router.push({ pathname: '/edit-post/[id]', params: { id: String(post.id) } })}
            />
            <Text style={styles.respondHint}>
              修改简介、游戏名、标签等内容
            </Text>
          </View>
        ) : (
          <View style={styles.respondArea}>
            <Button
              label={open ? '回应招募' : '已满，仍要联系'}
              color={open ? tagAccent : Colors.neutral[400]}
              icon={<Send color={Colors.ink[900]} size={18} />}
              size="lg"
              onPress={() => setShowRespond(true)}
            />
            <Text style={styles.respondHint}>
              点击后将自动向 {post.authorName} 发送一条打招呼消息
            </Text>
          </View>
        )}
      </SafeAreaView>

      <ReportModal
        visible={showReport}
        onClose={() => setShowReport(false)}
        target={`帖子：${post.title}`}
      />

      {/* Respond modal */}
      <Modal visible={showRespond} animationType="slide" presentationStyle="pageSheet">
        <View style={styles.modalScreen}>
          <SafeAreaView edges={['top']} style={styles.modalNavSafe}>
            <View style={styles.modalNav}>
              <Pressable onPress={() => setShowRespond(false)} style={styles.modalBack}>
                <Text style={styles.modalBackText}>取消</Text>
              </Pressable>
              <Text style={styles.modalTitle}>打招呼</Text>
              <Pressable onPress={sendGreeting} style={styles.modalSend}>
                <Text style={[styles.modalSendText, !greeting.trim() && { opacity: 0.4 }]}>发送</Text>
              </Pressable>
            </View>
          </SafeAreaView>
          <KeyboardAvoidingView
            behavior={Platform.OS === 'ios' ? 'padding' : undefined}
            style={{ flex: 1 }}
          >
            <View style={styles.modalBody}>
              <Text style={styles.modalHint}>
                向 {authorName} 发送打招呼消息
              </Text>
              <TextInput
                value={greeting}
                onChangeText={setGreeting}
                placeholder={`向 ${authorName} 发送打招呼消息…`}
                placeholderTextColor={Colors.neutral[400]}
                style={styles.greetingInput}
                multiline
                maxLength={200}
                textAlignVertical="top"
                autoFocus
              />
            </View>
          </KeyboardAvoidingView>
        </View>
      </Modal>
    </View>
  );
}

function InfoCell({ icon, label, color }: { icon: React.ReactNode; label: string; color?: string }) {
  return (
    <View style={infoStyles.cell}>
      {icon}
      <Text style={[infoStyles.label, color ? { color } : undefined]}>{label}</Text>
    </View>
  );
}

const infoStyles = StyleSheet.create({
  cell: {
    flexDirection: 'row', alignItems: 'center', gap: Spacing.sm,
    backgroundColor: Colors.ink[800], paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm, borderRadius: Radius.md,
    borderWidth: 1, borderColor: Colors.border, width: '47.5%',
  },
  label: { color: Colors.neutral[200], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody },
});

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: Colors.ink[900], gap: Spacing.md },
  notFound: { color: Colors.neutral[100], fontSize: Typography.sizes.lg, fontFamily: Typography.fontFamilyDisplay },
  backBtn: { paddingHorizontal: Spacing.lg, paddingVertical: Spacing.sm, backgroundColor: Colors.ink[800], borderRadius: Radius.md },
  backBtnText: { color: Colors.primary[400], fontFamily: Typography.fontFamilyBodyBold },
  // Nav
  navSafe: { backgroundColor: 'transparent' },
  navBar: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingHorizontal: Spacing.lg, paddingVertical: Spacing.sm,
  },
  navBack: {
    width: 40, height: 40, borderRadius: 999, backgroundColor: `${Colors.ink[800]}AA`,
    alignItems: 'center', justifyContent: 'center',
  },
  navTitle: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.lg },
  // Author card
  authorCard: {
    marginHorizontal: Spacing.xl, marginTop: Spacing.sm, marginBottom: Spacing.md,
    backgroundColor: Colors.ink[800], borderRadius: Radius.md, borderWidth: 1,
    borderColor: Colors.border,
  },
  authorCardInner: {
    flexDirection: 'row', alignItems: 'center', gap: Spacing.md,
    padding: Spacing.md,
  },
  authorInfo: { flex: 1, gap: 2 },
  authorNameRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  authorName: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.base },
  authorHandle: { color: Colors.neutral[300], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody },
  authorRankRow: { flexDirection: 'row', gap: Spacing.sm, marginTop: Spacing.xs, flexWrap: 'wrap' },
  rankBadge: {
    flexDirection: 'row', alignItems: 'center', gap: 3,
    paddingHorizontal: Spacing.sm, paddingVertical: 2, borderRadius: Radius.pill,
    backgroundColor: Colors.ink[700],
  },
  rankText: { color: Colors.neutral[200], fontSize: 10, fontFamily: Typography.fontFamilyBodyMedium },
  // Post section
  section: { marginHorizontal: Spacing.xl },
  titleRow: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: Spacing.md, marginBottom: Spacing.xs },
  title: { flex: 1, color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.lg, lineHeight: 24 },
  statusPill: { flexDirection: 'row', alignItems: 'center', gap: 4, paddingHorizontal: Spacing.sm, paddingVertical: 4, borderRadius: Radius.pill },
  statusDot: { width: 5, height: 5, borderRadius: 999 },
  statusText: { fontSize: 10, fontFamily: Typography.fontFamilyBodyBold },
  metaRow: { flexDirection: 'row', alignItems: 'center', marginBottom: Spacing.md },
  metaText: { color: Colors.neutral[200], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody, marginLeft: Spacing.xs },
  desc: { color: Colors.neutral[100], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody, lineHeight: 20, marginBottom: Spacing.md },
  tagRow: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.sm, marginBottom: Spacing.md },
  infoGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.sm },
  // Respond
  respondSafe: {
    backgroundColor: Colors.ink[850], borderTopWidth: 1, borderTopColor: Colors.border,
  },
  respondArea: { paddingHorizontal: Spacing.xl, paddingTop: Spacing.md, paddingBottom: Spacing.sm },
  respondHint: {
    textAlign: 'center', color: Colors.neutral[400], fontSize: Typography.sizes.xs,
    fontFamily: Typography.fontFamilyBody, marginTop: Spacing.sm,
  },
  // Respond modal
  modalScreen: { flex: 1, backgroundColor: Colors.ink[900] },
  modalNavSafe: { backgroundColor: Colors.ink[900] },
  modalNav: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingHorizontal: Spacing.lg, paddingVertical: Spacing.sm,
    borderBottomWidth: 1, borderBottomColor: Colors.border,
  },
  modalBack: { paddingHorizontal: Spacing.sm, paddingVertical: Spacing.xs },
  modalBackText: { color: Colors.neutral[300], fontSize: Typography.sizes.base, fontFamily: Typography.fontFamilyBody },
  modalTitle: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.lg },
  modalSend: { paddingHorizontal: Spacing.md, paddingVertical: Spacing.xs },
  modalSendText: { color: Colors.primary[400], fontSize: Typography.sizes.base, fontFamily: Typography.fontFamilyBodyBold },
  modalBody: { padding: Spacing.xl },
  modalHint: { color: Colors.neutral[300], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody, marginBottom: Spacing.md },
  greetingInput: {
    backgroundColor: Colors.ink[800], borderWidth: 1, borderColor: Colors.border,
    borderRadius: Radius.md, padding: Spacing.md, color: Colors.neutral[50],
    fontFamily: Typography.fontFamilyBody, fontSize: Typography.sizes.base,
    minHeight: 120,
  },
});
