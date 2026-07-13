import { useState } from 'react';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { StyleSheet, Text, View, ScrollView, Pressable } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { LinearGradient } from 'expo-linear-gradient';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { getUserById, currentUserId } from '@/data/mock';
import { Avatar } from '@/components/Avatar';
import { ReportModal } from '@/components/ReportModal';
import { ChevronLeft, Users, FileText, AlertTriangle } from 'lucide-react-native';

export default function UserProfileScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const router = useRouter();
  const user = getUserById(Number(id));

  if (!user) {
    return (
      <View style={styles.center}>
        <Text style={styles.notFound}>用户未找到</Text>
        <Pressable onPress={() => router.back()} style={styles.backBtn}>
          <Text style={styles.backBtnText}>返回</Text>
        </Pressable>
      </View>
    );
  }

  const [showReport, setShowReport] = useState(false);
  const isOwn = user.id === currentUserId;
  const accent = [Colors.primary[400], Colors.accent[400], Colors.secondary[400]][user.id % 3];

  return (
    <View style={styles.screen}>
      <LinearGradient colors={[`${accent}20`, Colors.ink[900]]} locations={[0, 0.25]} style={StyleSheet.absoluteFill} />

      <SafeAreaView edges={['top']} style={styles.navSafe}>
        <View style={styles.navBar}>
          <Pressable onPress={() => router.back()} style={styles.navBack}>
            <ChevronLeft color={Colors.neutral[50]} size={24} />
          </Pressable>
          <Text style={styles.navTitle}>玩家主页</Text>
          {isOwn ? (
            <View style={{ width: 40 }} />
          ) : (
            <Pressable onPress={() => setShowReport(true)} style={styles.navBack}>
              <AlertTriangle color={Colors.neutral[300]} size={18} />
            </Pressable>
          )}
        </View>
      </SafeAreaView>

      <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ paddingBottom: 80 }}>
        {/* Hero */}
        <View style={styles.hero}>
          <Avatar uri={user.avatar} name={user.name} size={88} ring={accent} />
          <Text style={styles.name}>{user.name}</Text>
          <Text style={styles.handle}>{user.handle}</Text>
          {user.bio ? (
            <Text style={styles.bio}>{user.bio}</Text>
          ) : null}

          <View style={styles.statsRow}>
            <View style={styles.statItem}>
              <Users color={accent} size={18} />
              <Text style={styles.statVal}>{user.squads}</Text>
              <Text style={styles.statLabel}>小队</Text>
            </View>
            <View style={styles.statItem}>
              <FileText color={Colors.secondary[400]} size={18} />
              <Text style={styles.statVal}>{user.posts}</Text>
              <Text style={styles.statLabel}>招募帖</Text>
            </View>
          </View>

          {user.recentGames.length > 0 && (
            <View style={styles.recentWrap}>
              <Text style={styles.recentLabel}>最近游戏</Text>
              <View style={styles.recentRow}>
                {user.recentGames.map((g) => (
                  <View key={g} style={styles.recentTag}>
                    <Text style={styles.recentText}>{g}</Text>
                  </View>
                ))}
              </View>
            </View>
          )}
        </View>
      </ScrollView>

      <ReportModal
        visible={showReport}
        onClose={() => setShowReport(false)}
        target={`用户：${user.name}`}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: Colors.ink[900], gap: Spacing.md },
  notFound: { color: Colors.neutral[100], fontSize: Typography.sizes.lg, fontFamily: Typography.fontFamilyDisplay },
  backBtn: { paddingHorizontal: Spacing.lg, paddingVertical: Spacing.sm, backgroundColor: Colors.ink[800], borderRadius: Radius.md },
  backBtnText: { color: Colors.primary[400], fontFamily: Typography.fontFamilyBodyBold },
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
  hero: {
    alignItems: 'center', marginHorizontal: Spacing.xl, marginTop: Spacing.lg,
    backgroundColor: Colors.ink[800], borderRadius: Radius.lg, padding: Spacing.xl,
    borderWidth: 1, borderColor: Colors.border,
  },
  name: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.xxl, marginTop: Spacing.md },
  handle: { color: Colors.neutral[300], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody, marginTop: 2 },
  bio: { color: Colors.neutral[100], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody, marginTop: Spacing.md, textAlign: 'center', lineHeight: 20 },
  statsRow: { flexDirection: 'row', marginTop: Spacing.lg, gap: Spacing.xxl },
  statItem: { alignItems: 'center', gap: 4 },
  statVal: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.xl },
  statLabel: { color: Colors.neutral[300], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody },
  recentWrap: { width: '100%', marginTop: Spacing.lg, borderTopWidth: 1, borderTopColor: Colors.border, paddingTop: Spacing.md },
  recentLabel: { color: Colors.neutral[300], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody, marginBottom: Spacing.sm },
  recentRow: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.sm },
  recentTag: {
    backgroundColor: Colors.ink[700], paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs, borderRadius: Radius.pill,
    borderWidth: 1, borderColor: Colors.border,
  },
  recentText: { color: Colors.neutral[100], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBodyMedium },
});
