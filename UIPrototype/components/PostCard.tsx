import { StyleSheet, Text, View, Pressable } from 'react-native';
import { Colors, Radius, Spacing, Typography } from '@/constants/theme';
import { Avatar } from './Avatar';
import { Chip } from './Chip';
import { Users, MessageSquare, Mic, MicOff, Gamepad2, Clock } from 'lucide-react-native';
import { tags } from '@/data/mock';
import type { RecruitPost } from '@/data/types';

type Props = {
  post: RecruitPost;
  accent?: string;
  onPress?: () => void;
};

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

export function PostCard({ post, accent = Colors.primary[400], onPress }: Props) {
  const open = post.filledCount < post.needCount;
  const tagAccent = post.tagIds.length > 0 ? tagColor(post.tagIds[0]) : accent;

  const content = (
    <View style={styles.card}>
      <View style={styles.head}>
        <Avatar uri={post.authorAvatar} name={post.authorName} size={44} ring={tagAccent} />
        <View style={styles.headInfo}>
          <Text style={styles.author} numberOfLines={1}>{post.authorName}</Text>
          <Text style={styles.meta}>
            {post.gameName} · {post.createdAt}
          </Text>
        </View>
        <View style={[styles.statusPill, { backgroundColor: open ? `${Colors.secondary[500]}22` : `${Colors.neutral[400]}22` }]}>
          <View style={[styles.statusDot, { backgroundColor: open ? Colors.secondary[400] : Colors.neutral[300] }]} />
          <Text style={[styles.statusText, { color: open ? Colors.secondary[400] : Colors.neutral[200] }]}>
            {open ? '招募中' : '已满'}
          </Text>
        </View>
      </View>

      <Text style={styles.title} numberOfLines={2}>{post.title}</Text>
      <Text style={styles.body} numberOfLines={3}>{post.description}</Text>

      <View style={styles.tags}>
        {post.tagIds.map((tid) => (
          <Chip key={tid} label={tagName(tid)} color={tagColor(tid)} size="sm" />
        ))}
        <Chip label={durationLabel(post.durationMinutes)} color={Colors.neutral[400]} size="sm" />
      </View>

      <View style={styles.foot}>
        <View style={styles.needRow}>
          <Users color={tagAccent} size={16} />
          <Text style={[styles.needText, { color: tagAccent }]}>
            {post.filledCount}/{post.needCount}
          </Text>
          <Text style={styles.needLabel}>已加入</Text>
        </View>
        <View style={styles.footRight}>
          <View style={styles.modeItem}>
            {post.voice === 'none' ? <MicOff color={Colors.neutral[300]} size={14} /> : <Mic color={Colors.neutral[200]} size={14} />}
            <Text style={styles.modeText}>
              {post.voice === 'required' ? '语音必开' : post.voice === 'optional' ? '语音可选' : '无需语音'}
            </Text>
          </View>
          <View style={styles.modeItem}>
            <Gamepad2 color={Colors.neutral[200]} size={14} />
            <Text style={styles.modeText}>{modeLabel(post.mode)}</Text>
          </View>
          <View style={styles.modeItem}>
            <MessageSquare color={Colors.neutral[200]} size={14} />
            <Text style={styles.modeText}>{post.comments}</Text>
          </View>
        </View>
      </View>
    </View>
  );

  if (onPress) {
    return (
      <Pressable onPress={onPress} style={({ pressed }) => pressed && { opacity: 0.9 }}>
        {content}
      </Pressable>
    );
  }
  return content;
}

function modeLabel(m: RecruitPost['mode']) {
  return m === 'ranked' ? '排位' : m === 'tournament' ? '比赛' : '休闲';
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: Colors.ink[800],
    borderRadius: Radius.lg,
    padding: Spacing.lg,
    marginBottom: Spacing.md,
    borderWidth: 1,
    borderColor: Colors.border,
  },
  head: { flexDirection: 'row', alignItems: 'center', gap: Spacing.md, marginBottom: Spacing.md },
  headInfo: { flex: 1 },
  author: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyBodyBold, fontSize: Typography.sizes.base },
  meta: { color: Colors.neutral[300], fontSize: Typography.sizes.xs, marginTop: 2 },
  statusPill: { flexDirection: 'row', alignItems: 'center', gap: 5, paddingHorizontal: Spacing.sm, paddingVertical: 5, borderRadius: Radius.pill },
  statusDot: { width: 6, height: 6, borderRadius: 999 },
  statusText: { fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBodyMedium },
  title: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.lg, lineHeight: 22, marginBottom: Spacing.xs },
  body: { color: Colors.neutral[200], fontFamily: Typography.fontFamilyBody, fontSize: Typography.sizes.sm, lineHeight: 20 },
  tags: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.sm, marginTop: Spacing.md },
  foot: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: Spacing.lg, paddingTop: Spacing.md, borderTopWidth: 1, borderTopColor: Colors.border },
  needRow: { flexDirection: 'row', alignItems: 'center', gap: Spacing.xs },
  needText: { fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.lg },
  needLabel: { color: Colors.neutral[300], fontSize: Typography.sizes.xs, marginLeft: 4 },
  footRight: { flexDirection: 'row', alignItems: 'center', gap: Spacing.md },
  modeItem: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  modeText: { color: Colors.neutral[200], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody },
});
