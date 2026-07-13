import { useCallback, useMemo, useState } from 'react';
import { StyleSheet, Text, View, FlatList, Pressable } from 'react-native';
import { useRouter } from 'expo-router';
import { useFocusEffect } from '@react-navigation/native';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { tags as allTags, posts as allPosts } from '@/data/mock';
import { searchState } from '@/data/searchState';
import { ScreenHeader } from '@/components/ScreenHeader';
import { PostCard } from '@/components/PostCard';
import { TagFilterBar } from '@/components/TagFilterBar';
import { Avatar } from '@/components/Avatar';
import { Search, Bell, TrendingUp, Flame, X } from 'lucide-react-native';
import type { RecruitPost } from '@/data/types';

export default function HomeScreen() {
  const router = useRouter();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);

  // Consume pending search selections when returning from search page
  useFocusEffect(
    useCallback(() => {
      if (searchState.selectedGameName) {
        setSearchQuery(searchState.selectedGameName);
        searchState.selectedGameName = null;
      }
      if (searchState.selectedTagId != null) {
        const tid = searchState.selectedTagId;
        searchState.selectedTagId = null;
        setSelectedTagIds((prev) => (prev.includes(tid) ? prev : [...prev, tid]));
      }
    }, []),
  );

  const filteredPosts = useMemo(() => {
    let result: RecruitPost[] = allPosts.filter((p) => p.status === 'active');

    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      result = result.filter(
        (p) =>
          p.gameName.toLowerCase().includes(q) ||
          p.title.toLowerCase().includes(q),
      );
    }

    if (selectedTagIds.length > 0) {
      result = result.filter((p) =>
        selectedTagIds.some((tid) => p.tagIds.includes(tid)),
      );
    }

    return result;
  }, [searchQuery, selectedTagIds]);

  const toggleTag = (tagId: number) => {
    setSelectedTagIds((prev) =>
      prev.includes(tagId)
        ? prev.filter((id) => id !== tagId)
        : [...prev, tagId],
    );
  };

  return (
    <View style={styles.screen}>
      <ScreenHeader
        large
        subtitle="寻找你的搭子"
        title="PlayMate"
        right={
          <View style={styles.headerRight}>
            <Pressable style={styles.iconBtn}>
              <Bell color={Colors.neutral[100]} size={20} />
              <View style={styles.notifDot} />
            </Pressable>
            <Pressable onPress={() => router.navigate('/profile')}>
              <Avatar
                name="星河玩家"
                size={38}
                ring={Colors.primary[400]}
              />
            </Pressable>
          </View>
        }
      />

      <FlatList
        data={filteredPosts}
        keyExtractor={(item) => item.id.toString()}
        showsVerticalScrollIndicator={false}
        contentContainerStyle={{ paddingBottom: 120 }}
        keyboardShouldPersistTaps="handled"
        ListHeaderComponent={
          <View>
            {/* Search bar — taps through to dedicated search page */}
            <View style={styles.searchRow}>
              <Pressable
                onPress={() => router.push('/search')}
                style={({ pressed }) => [
                  styles.searchBox,
                  pressed && styles.pressed,
                ]}
              >
                <Search color={Colors.neutral[300]} size={18} />
                <Text
                  style={[
                    styles.searchPlaceholder,
                    searchQuery.length > 0 && { color: Colors.neutral[50] },
                  ]}
                >
                  {searchQuery || '搜索游戏名称、标签，或直接输入…'}
                </Text>
                {searchQuery.length > 0 && (
                  <Pressable
                    onPress={(e) => { e.stopPropagation(); setSearchQuery(''); }}
                    hitSlop={8}
                  >
                    <X color={Colors.neutral[400]} size={16} />
                  </Pressable>
                )}
              </Pressable>
            </View>

            {/* Tag filter */}
            <View style={styles.tagRow}>
              <TagFilterBar
                tags={allTags}
                selectedIds={selectedTagIds}
                onToggle={toggleTag}
              />
            </View>

            {/* Live activity banner */}
            <View style={styles.banner}>
              <View style={styles.bannerLeft}>
                <View style={styles.pulseRing}>
                  <View style={styles.pulseDot} />
                </View>
                <View>
                  <Text style={styles.bannerTitle}>此刻 14,238 人正在找搭子</Text>
                  <Text style={styles.bannerSub}>跨平台 · 实时匹配 · 语音开黑</Text>
                </View>
              </View>
              <Flame color={Colors.accent[400]} size={26} />
            </View>

            {/* Section title */}
            <View style={styles.sectionHeaderSmall}>
              <View style={styles.sectionHeaderLeft}>
                <TrendingUp color={Colors.primary[400]} size={18} />
                <Text style={styles.sectionTitle}>
                  {searchQuery.trim() || selectedTagIds.length > 0
                    ? `找到 ${filteredPosts.length} 条招募`
                    : '最新找搭子'}
                </Text>
              </View>
            </View>
          </View>
        }
        renderItem={({ item }) => (
          <View style={{ paddingHorizontal: Spacing.xl }}>
            <PostCard
              post={item}
              onPress={() => router.push({ pathname: '/post/[id]', params: { id: String(item.id) } })}
            />
          </View>
        )}
        ListEmptyComponent={
          <View style={styles.emptyState}>
            <Search color={Colors.neutral[400]} size={40} />
            <Text style={styles.emptyTitle}>没有找到匹配的招募</Text>
            <Text style={styles.emptySub}>试试更换关键词或标签筛选</Text>
          </View>
        }
      />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  headerRight: { flexDirection: 'row', alignItems: 'center', gap: Spacing.md },
  iconBtn: {
    width: 40, height: 40, borderRadius: 999, backgroundColor: Colors.ink[800],
    alignItems: 'center', justifyContent: 'center', borderWidth: 1, borderColor: Colors.border,
  },
  notifDot: {
    position: 'absolute', top: 9, right: 9, width: 7, height: 7, borderRadius: 999,
    backgroundColor: Colors.danger[400], borderWidth: 1.5, borderColor: Colors.ink[800],
  },
  searchRow: { paddingHorizontal: Spacing.xl, marginBottom: Spacing.md },
  searchBox: {
    flexDirection: 'row', alignItems: 'center', gap: Spacing.md,
    backgroundColor: Colors.ink[800], borderWidth: 1, borderColor: Colors.border,
    borderRadius: Radius.md, paddingHorizontal: Spacing.lg, height: 48,
  },
  searchPlaceholder: {
    flex: 1, color: Colors.neutral[400],
    fontFamily: Typography.fontFamilyBody, fontSize: Typography.sizes.base,
  },
  pressed: { opacity: 0.7 },
  tagRow: { paddingLeft: Spacing.xl, marginBottom: Spacing.lg },
  banner: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    marginHorizontal: Spacing.xl, marginBottom: Spacing.xl, padding: Spacing.lg,
    borderRadius: Radius.lg, backgroundColor: Colors.ink[800], borderWidth: 1, borderColor: Colors.border,
  },
  bannerLeft: { flexDirection: 'row', alignItems: 'center', gap: Spacing.md, flex: 1 },
  pulseRing: {
    width: 36, height: 36, borderRadius: 999, borderWidth: 2,
    borderColor: `${Colors.online}55`, alignItems: 'center', justifyContent: 'center',
  },
  pulseDot: { width: 12, height: 12, borderRadius: 999, backgroundColor: Colors.online },
  bannerTitle: {
    color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay,
    fontSize: Typography.sizes.base, lineHeight: 20,
  },
  bannerSub: {
    color: Colors.neutral[300], fontSize: Typography.sizes.xs, marginTop: 2,
    fontFamily: Typography.fontFamilyBody,
  },
  sectionHeaderSmall: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    marginBottom: Spacing.md, paddingHorizontal: Spacing.xl,
  },
  sectionHeaderLeft: { flexDirection: 'row', alignItems: 'center', gap: Spacing.sm },
  sectionTitle: {
    color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay,
    fontSize: Typography.sizes.lg, lineHeight: 22,
  },
  emptyState: {
    alignItems: 'center', paddingVertical: Spacing.huge, gap: Spacing.md,
  },
  emptyTitle: {
    color: Colors.neutral[100], fontFamily: Typography.fontFamilyDisplay,
    fontSize: Typography.sizes.lg,
  },
  emptySub: {
    color: Colors.neutral[300], fontSize: Typography.sizes.sm,
    fontFamily: Typography.fontFamilyBody,
  },
});
