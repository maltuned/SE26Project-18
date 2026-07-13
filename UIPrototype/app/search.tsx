import { useMemo, useRef, useState } from 'react';
import { useRouter } from 'expo-router';
import {
  StyleSheet, Text, View, ScrollView, Pressable, TextInput,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { games, tags as allTags, currentUserId, recordRecentGame } from '@/data/mock';
import { searchState } from '@/data/searchState';
import { Chip } from '@/components/Chip';
import { ChevronLeft, Search, X, Gamepad2, Tag } from 'lucide-react-native';

export default function SearchScreen() {
  const router = useRouter();
  const inputRef = useRef<TextInput>(null);
  const [query, setQuery] = useState('');

  const filteredGames = useMemo(() => {
    const q = query.toLowerCase().trim();
    if (!q) return games;
    return games.filter((g) => g.name.toLowerCase().includes(q));
  }, [query]);

  const filteredTags = useMemo(() => {
    const q = query.toLowerCase().trim();
    if (!q) return allTags;
    return allTags.filter((t) => t.name.toLowerCase().includes(q));
  }, [query]);

  const noResults = filteredGames.length === 0 && filteredTags.length === 0;

  function selectGame(name: string) {
    searchState.selectedGameName = name;
    searchState.selectedTagId = null;
    recordRecentGame(currentUserId, name);
    router.back();
  }

  function selectTag(id: number) {
    searchState.selectedGameName = null;
    searchState.selectedTagId = id;
    router.back();
  }

  return (
    <View style={styles.screen}>
      <SafeAreaView edges={['top']} style={styles.navSafe}>
        <View style={styles.navBar}>
          <Pressable onPress={() => router.back()} style={styles.backBtn}>
            <ChevronLeft color={Colors.neutral[50]} size={24} />
          </Pressable>
          <View style={styles.searchBox}>
            <Search color={Colors.neutral[300]} size={18} />
            <TextInput
              ref={inputRef}
              value={query}
              onChangeText={setQuery}
              placeholder="搜索游戏名称或标签…"
              placeholderTextColor={Colors.neutral[400]}
              style={styles.searchInput}
              autoFocus
              returnKeyType="search"
            />
            {query.length > 0 && (
              <Pressable onPress={() => setQuery('')}>
                <X color={Colors.neutral[400]} size={16} />
              </Pressable>
            )}
          </View>
        </View>
      </SafeAreaView>

      <ScrollView
        showsVerticalScrollIndicator={false}
        contentContainerStyle={{ paddingBottom: 80 }}
        keyboardShouldPersistTaps="handled"
      >
        {noResults ? (
          <View style={styles.emptyState}>
            <Search color={Colors.neutral[400]} size={48} />
            <Text style={styles.emptyTitle}>没有找到结果</Text>
            <Text style={styles.emptySub}>试试其他关键词</Text>
          </View>
        ) : (
          <>
            {/* Games section — rendered as Chips like tags */}
            {filteredGames.length > 0 && (
              <View style={styles.section}>
                <View style={styles.sectionHead}>
                  <Gamepad2 color={Colors.primary[400]} size={18} />
                  <Text style={styles.sectionTitle}>游戏</Text>
                  <Text style={styles.sectionCount}>{filteredGames.length}</Text>
                </View>
                <View style={styles.tagGrid}>
                  {filteredGames.map((g) => {
                    const accent = allTags.find((t) => g.tagIds.includes(t.id))?.accentColor ?? Colors.primary[400];
                    return (
                      <Pressable key={`game-${g.id}`} onPress={() => selectGame(g.name)}>
                        <Chip label={g.name} color={accent} solid size="md" />
                      </Pressable>
                    );
                  })}
                </View>
              </View>
            )}

            {/* Tags section */}
            {filteredTags.length > 0 && (
              <View style={styles.section}>
                <View style={styles.sectionHead}>
                  <Tag color={Colors.accent[400]} size={18} />
                  <Text style={styles.sectionTitle}>标签</Text>
                  <Text style={styles.sectionCount}>{filteredTags.length}</Text>
                </View>
                <View style={styles.tagGrid}>
                  {filteredTags.map((t) => (
                    <Pressable key={`tag-${t.id}`} onPress={() => selectTag(t.id)}>
                      <Chip label={t.name} color={t.accentColor} solid size="md" />
                    </Pressable>
                  ))}
                </View>
              </View>
            )}
          </>
        )}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  navSafe: { backgroundColor: Colors.ink[900] },
  navBar: {
    flexDirection: 'row', alignItems: 'center', paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm, gap: Spacing.md,
    borderBottomWidth: 1, borderBottomColor: Colors.border,
  },
  backBtn: {
    width: 40, height: 40, borderRadius: 999, alignItems: 'center',
    justifyContent: 'center', backgroundColor: Colors.ink[800],
  },
  searchBox: {
    flex: 1, flexDirection: 'row', alignItems: 'center', gap: Spacing.md,
    backgroundColor: Colors.ink[800], borderWidth: 1, borderColor: Colors.border,
    borderRadius: Radius.md, paddingHorizontal: Spacing.lg, height: 44,
  },
  searchInput: {
    flex: 1, color: Colors.neutral[50], fontFamily: Typography.fontFamilyBody,
    fontSize: Typography.sizes.base,
  },
  // Sections
  section: { marginTop: Spacing.lg, paddingHorizontal: Spacing.xl },
  sectionHead: {
    flexDirection: 'row', alignItems: 'center', gap: Spacing.sm,
    marginBottom: Spacing.md,
  },
  sectionTitle: {
    color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay,
    fontSize: Typography.sizes.lg,
  },
  sectionCount: {
    color: Colors.neutral[300], fontSize: Typography.sizes.xs,
    fontFamily: Typography.fontFamilyBodyBold,
    backgroundColor: Colors.ink[800], paddingHorizontal: 8, paddingVertical: 2,
    borderRadius: Radius.pill, overflow: 'hidden',
  },
  // Tag grid
  tagGrid: {
    flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.sm,
  },
  // Empty
  emptyState: {
    alignItems: 'center', paddingVertical: 100, gap: Spacing.md,
  },
  emptyTitle: {
    color: Colors.neutral[100], fontFamily: Typography.fontFamilyDisplay,
    fontSize: Typography.sizes.lg,
  },
  emptySub: {
    color: Colors.neutral[300], fontSize: Typography.sizes.sm,
    fontFamily: Typography.fontFamilyBody,
  },
  pressed: { opacity: 0.7 },
});
