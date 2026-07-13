import { useCallback, useMemo, useState } from 'react';
import { StyleSheet, Text, View, FlatList, Pressable } from 'react-native';
import { useFocusEffect } from '@react-navigation/native';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { posts, currentUserId } from '@/data/mock';
import { ScreenHeader } from '@/components/ScreenHeader';
import { PostCard } from '@/components/PostCard';
import { Button } from '@/components/Button';
import { Plus, FileText } from 'lucide-react-native';
import { useRouter } from 'expo-router';
import type { RecruitPost } from '@/data/types';

type Tab = 'all' | 'active' | 'expired';

function getMyPosts(): RecruitPost[] {
  return posts.filter((p) => p.authorId === currentUserId);
}

export default function PostsScreen() {
  const router = useRouter();
  const [tab, setTab] = useState<Tab>('all');
  const [myPosts, setMyPosts] = useState(() => getMyPosts());

  // Refresh post list every time the tab gains focus (catches new posts from create-post)
  useFocusEffect(
    useCallback(() => {
      setMyPosts(getMyPosts());
    }, []),
  );

  const filtered = useMemo(() => {
    if (tab === 'active') return myPosts.filter((p) => p.status === 'active');
    if (tab === 'expired') return myPosts.filter((p) => p.status === 'expired');
    return myPosts;
  }, [tab, myPosts]);

  return (
    <View style={styles.screen}>
      <ScreenHeader
        large
        subtitle="我的发布"
        title="发布"
      />

      {/* Filter tabs */}
      <View style={styles.tabsRow}>
        {([
          { k: 'all' as Tab, label: '全部' },
          { k: 'active' as Tab, label: '进行中' },
          { k: 'expired' as Tab, label: '已过期' },
        ]).map((t) => {
          const active = t.k === tab;
          return (
            <Pressable
              key={t.k}
              onPress={() => setTab(t.k)}
              style={[styles.tab, active && styles.tabActive]}
            >
              <Text style={[styles.tabText, active && styles.tabTextActive]}>
                {t.label}
              </Text>
            </Pressable>
          );
        })}
      </View>

      <FlatList
        data={filtered}
        keyExtractor={(item) => item.id.toString()}
        showsVerticalScrollIndicator={false}
        contentContainerStyle={[
          styles.listContent,
          filtered.length === 0 && styles.listEmpty,
        ]}
        renderItem={({ item }) => (
          <PostCard
            post={item}
            onPress={() => router.push({ pathname: '/post/[id]', params: { id: String(item.id) } })}
          />
        )}
        ListEmptyComponent={
          <View style={styles.emptyState}>
            <FileText color={Colors.neutral[400]} size={44} />
            <Text style={styles.emptyTitle}>还没有发布招募</Text>
            <Text style={styles.emptySub}>点击下方 + 按钮发布你的第一条招募</Text>
            <Button
              label="立即发布"
              color={Colors.primary[400]}
              icon={<Plus color={Colors.ink[900]} size={18} />}
              onPress={() => router.push('/create-post')}
            />
          </View>
        }
      />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  addBtn: {
    width: 44, height: 44, borderRadius: 999, backgroundColor: Colors.primary[400],
    alignItems: 'center', justifyContent: 'center',
  },
  tabsRow: {
    flexDirection: 'row', gap: Spacing.sm, paddingHorizontal: Spacing.xl, marginBottom: Spacing.lg,
  },
  tab: {
    paddingHorizontal: Spacing.lg, paddingVertical: Spacing.sm, borderRadius: Radius.pill,
    backgroundColor: Colors.ink[800], borderWidth: 1, borderColor: Colors.border,
  },
  tabActive: { backgroundColor: Colors.primary[400], borderColor: Colors.primary[400] },
  tabText: {
    color: Colors.neutral[200], fontSize: Typography.sizes.sm,
    fontFamily: Typography.fontFamilyBodyMedium,
  },
  tabTextActive: {
    color: Colors.ink[900], fontFamily: Typography.fontFamilyBodyBold,
  },
  listContent: { paddingHorizontal: Spacing.xl, paddingBottom: 120 },
  listEmpty: { flex: 1, justifyContent: 'center' },
  emptyState: {
    alignItems: 'center', gap: Spacing.md, paddingVertical: Spacing.huge,
  },
  emptyTitle: {
    color: Colors.neutral[100], fontFamily: Typography.fontFamilyDisplay,
    fontSize: Typography.sizes.lg,
  },
  emptySub: {
    color: Colors.neutral[300], fontSize: Typography.sizes.sm,
    fontFamily: Typography.fontFamilyBody, marginBottom: Spacing.xs,
  },
});
