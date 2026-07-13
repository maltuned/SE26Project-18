import { useCallback, useState } from 'react';
import { useRouter } from 'expo-router';
import {
  StyleSheet, Text, View, ScrollView, Pressable, Alert,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useFocusEffect } from '@react-navigation/native';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import {
  posts, reports, bannedUserIds, mockUsers,
  deletePost, banUser, unbanUser,
} from '@/data/mock';
import { Button } from '@/components/Button';
import { Chip } from '@/components/Chip';
import {
  ChevronLeft, Shield, Flag, FileText, Users, Trash2, Check, X,
  ShieldOff, ShieldCheck,
} from 'lucide-react-native';

type Tab = 'reports' | 'posts' | 'users';

export default function AdminScreen() {
  const router = useRouter();
  const [tab, setTab] = useState<Tab>('reports');
  const [localReports, setLocalReports] = useState(reports);
  const [localP, setLocalP] = useState(posts);

  useFocusEffect(
    useCallback(() => {
      setLocalReports([...reports]);
      setLocalP([...posts]);
    }, []),
  );

  function handleReport(id: number) {
    const r = reports.find((r) => r.id === id);
    if (r) r.handled = true;
    setLocalReports([...reports]);
    Alert.alert('已处理', '该举报已标记为已处理');
  }

  function handleDeletePost(id: number) {
    Alert.alert('确认删除', '确定要删除这条招募帖吗？此操作不可撤销。', [
      { text: '取消', style: 'cancel' },
      {
        text: '删除', style: 'destructive',
        onPress: () => {
          deletePost(id);
          setLocalP([...posts]);
          Alert.alert('已删除', '帖子已被删除');
        },
      },
    ]);
  }

  function handleBanUser(userId: number, name: string) {
    if (bannedUserIds.includes(userId)) {
      Alert.alert('已封禁', `${name} 已被封禁`); // Already banned
      return;
    }
    Alert.alert('确认封禁', `确定要封禁用户「${name}」吗？`, [
      { text: '取消', style: 'cancel' },
      {
        text: '封禁', style: 'destructive',
        onPress: () => {
          banUser(userId);
          // Also remove their posts
          const theirPosts = posts.filter((p) => p.authorId === userId);
          theirPosts.forEach((p) => deletePost(p.id));
          setLocalReports([...reports]);
          setLocalP([...posts]);
          Alert.alert('已封禁', `${name} 已被封禁，其所有帖子已删除`);
        },
      },
    ]);
  }

  function handleUnbanUser(userId: number, name: string) {
    unbanUser(userId);
    setLocalReports([...reports]); // trigger re-render
    Alert.alert('已解封', `${name} 已被解封`);
  }

  return (
    <View style={styles.screen}>
      <SafeAreaView edges={['top']} style={styles.navSafe}>
        <View style={styles.navBar}>
          <Pressable onPress={() => router.back()} style={styles.navBack}>
            <ChevronLeft color={Colors.neutral[50]} size={24} />
          </Pressable>
          <Text style={styles.navTitle}>管理员面板</Text>
          <Shield color={Colors.accent[400]} size={22} />
        </View>
      </SafeAreaView>

      {/* Tab bar */}
      <View style={styles.tabsRow}>
        {([
          { k: 'reports' as Tab, label: '举报受理', icon: <Flag color={tab === 'reports' ? Colors.ink[900] : Colors.neutral[200]} size={16} /> },
          { k: 'posts' as Tab, label: '帖子管理', icon: <FileText color={tab === 'posts' ? Colors.ink[900] : Colors.neutral[200]} size={16} /> },
          { k: 'users' as Tab, label: '用户管理', icon: <Users color={tab === 'users' ? Colors.ink[900] : Colors.neutral[200]} size={16} /> },
        ]).map((t) => {
          const active = t.k === tab;
          return (
            <Pressable
              key={t.k}
              onPress={() => setTab(t.k)}
              style={[styles.tab, active && styles.tabActive]}
            >
              {t.icon}
              <Text style={[styles.tabText, active && styles.tabTextActive]}>{t.label}</Text>
            </Pressable>
          );
        })}
      </View>

      <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ paddingBottom: 80 }}>
        {/* ── Reports ── */}
        {tab === 'reports' && (
          <View style={styles.section}>
            {localReports.length === 0 ? (
              <View style={styles.empty}>
                <Flag color={Colors.neutral[400]} size={40} />
                <Text style={styles.emptyTitle}>暂无举报</Text>
              </View>
            ) : (
              localReports.map((r) => {
                const isPostReport = r.target.startsWith('帖子');
                const targetName = r.target.replace(/^(帖子|用户)：/, '');
                // Find matching post or user for ban actions
                const matchPost = isPostReport ? posts.find((p) => p.title === targetName) : null;
                const matchUser = !isPostReport
                  ? Object.values(mockUsers).find((u) => u.name === targetName)
                  : matchPost ? Object.values(mockUsers).find((u) => u.id === matchPost.authorId) : null;

                return (
                  <View key={r.id} style={[styles.card, r.handled && styles.cardHandled]}>
                    <View style={styles.cardHead}>
                      <Text style={styles.cardTitle}>{r.target}</Text>
                      <Chip label={r.handled ? '已处理' : '待处理'} color={r.handled ? Colors.neutral[400] : Colors.danger[400]} size="md" />
                    </View>
                    <Text style={styles.cardBody}>原因：{r.reason}{r.detail ? `\n补充：${r.detail}` : ''}</Text>
                    <Text style={styles.cardMeta}>举报时间：{r.createdAt}</Text>
                    {!r.handled && (
                      <View style={styles.cardActions}>
                        <Button label="标记已处理" color={Colors.secondary[400]} icon={<Check color={Colors.ink[900]} size={12} />} size="sm" onPress={() => handleReport(r.id)} />
                        {matchPost && (
                          <Button label="删除帖子" color={Colors.danger[400]} icon={<Trash2 color={Colors.ink[900]} size={12} />} size="sm" onPress={() => {
                            deletePost(matchPost.id);
                            handleReport(r.id);
                            setLocalP([...posts]);
                            Alert.alert('已删除', `帖子「${matchPost.title}」已被删除`);
                          }} />
                        )}
                        {matchUser && (
                          <Button label={`封禁 ${matchUser.name}`} color={Colors.danger[400]} icon={<ShieldOff color={Colors.ink[900]} size={12} />} size="sm" onPress={() => {
                            banUser(matchUser.id);
                            const theirPosts = posts.filter((p) => p.authorId === matchUser.id);
                            theirPosts.forEach((p) => deletePost(p.id));
                            handleReport(r.id);
                            setLocalP([...posts]);
                            setLocalReports([...reports]);
                            Alert.alert('已封禁', `${matchUser.name} 已被封禁，其所有帖子已删除`);
                          }} />
                        )}
                      </View>
                    )}
                  </View>
                );
              })
            )}
          </View>
        )}

        {/* ── Posts ── */}
        {tab === 'posts' && (
          <View style={styles.section}>
            {localP.length === 0 ? (
              <View style={styles.empty}>
                <FileText color={Colors.neutral[400]} size={40} />
                <Text style={styles.emptyTitle}>暂无帖子</Text>
              </View>
            ) : (
              localP.map((p) => {
                const banned = bannedUserIds.includes(p.authorId);
                return (
                  <View key={p.id} style={[styles.card, banned && styles.cardBanned]}>
                    <View style={styles.cardHead}>
                      <Text style={styles.cardTitle} numberOfLines={1}>{p.title}</Text>
                      <Chip label={banned ? '作者已封禁' : p.status} color={banned ? Colors.danger[400] : Colors.neutral[400]} size="md" />
                    </View>
                    <Text style={styles.cardBody}>作者：{p.authorName} (ID:{p.authorId}) · 游戏：{p.gameName}</Text>
                    <View style={styles.cardActions}>
                      <Button label="删除帖子" color={Colors.danger[400]} icon={<Trash2 color={Colors.ink[900]} size={16} />} size="md" onPress={() => handleDeletePost(p.id)} />
                    </View>
                  </View>
                );
              })
            )}
          </View>
        )}

        {/* ── Users ── */}
        {tab === 'users' && (
          <View style={styles.section}>
            {Object.values(mockUsers).map((u) => {
              const banned = bannedUserIds.includes(u.id);
              return (
                <View key={u.id} style={[styles.card, banned && styles.cardBanned]}>
                  <View style={styles.cardHead}>
                    <View>
                      <Text style={styles.cardTitle}>{u.name}</Text>
                      <Text style={styles.cardBody}>{u.handle}</Text>
                    </View>
                    {banned && <Chip label="已封禁" color={Colors.danger[400]} size="md" />}
                  </View>
                  <View style={styles.cardActions}>
                    {banned ? (
                      <Button label="解封" color={Colors.secondary[400]} icon={<ShieldCheck color={Colors.ink[900]} size={16} />} size="md" onPress={() => handleUnbanUser(u.id, u.name)} />
                    ) : (
                      <Button label="封禁用户" color={Colors.danger[400]} icon={<ShieldOff color={Colors.ink[900]} size={16} />} size="md" onPress={() => handleBanUser(u.id, u.name)} />
                    )}
                  </View>
                </View>
              );
            })}
          </View>
        )}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  navSafe: { backgroundColor: Colors.ink[900] },
  navBar: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingHorizontal: Spacing.lg, paddingVertical: Spacing.sm,
    borderBottomWidth: 1, borderBottomColor: Colors.border,
  },
  navBack: {
    width: 40, height: 40, borderRadius: 999, backgroundColor: Colors.ink[800],
    alignItems: 'center', justifyContent: 'center',
  },
  navTitle: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.lg },
  tabsRow: {
    flexDirection: 'row', gap: Spacing.sm, paddingHorizontal: Spacing.xl,
    paddingVertical: Spacing.md, borderBottomWidth: 1, borderBottomColor: Colors.border,
  },
  tab: {
    flexDirection: 'row', alignItems: 'center', gap: Spacing.xs,
    paddingHorizontal: Spacing.md, paddingVertical: Spacing.sm, borderRadius: Radius.pill,
    backgroundColor: Colors.ink[800], borderWidth: 1, borderColor: Colors.border,
  },
  tabActive: { backgroundColor: Colors.primary[400], borderColor: Colors.primary[400] },
  tabText: { color: Colors.neutral[200], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBodyMedium },
  tabTextActive: { color: Colors.ink[900], fontFamily: Typography.fontFamilyBodyBold },
  section: { paddingHorizontal: Spacing.xl, paddingTop: Spacing.lg, gap: Spacing.md },
  card: {
    backgroundColor: Colors.ink[800], borderRadius: Radius.md, padding: Spacing.lg,
    borderWidth: 1, borderColor: Colors.border, gap: Spacing.sm,
  },
  cardHandled: { opacity: 0.6 },
  cardBanned: { borderColor: Colors.danger[400], borderWidth: 1 },
  cardHead: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', gap: Spacing.sm },
  cardTitle: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyBodyBold, fontSize: Typography.sizes.base, flex: 1 },
  cardBody: { color: Colors.neutral[200], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody, lineHeight: 20 },
  cardMeta: { color: Colors.neutral[400], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody },
  cardActions: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.sm, marginTop: Spacing.xs },
  empty: { alignItems: 'center', paddingVertical: 60, gap: Spacing.md },
  emptyTitle: { color: Colors.neutral[400], fontSize: Typography.sizes.lg, fontFamily: Typography.fontFamilyDisplay },
});
