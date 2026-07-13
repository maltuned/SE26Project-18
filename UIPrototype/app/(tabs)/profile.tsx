import { useState } from 'react';
import { StyleSheet, Text, View, ScrollView, Pressable, Alert, TextInput, Modal } from 'react-native';
import { useRouter } from 'expo-router';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { currentUser, updateBio } from '@/data/mock';
import { ScreenHeader } from '@/components/ScreenHeader';
import { Avatar } from '@/components/Avatar';
import { Button } from '@/components/Button';
import { ProfileMenuItem } from '@/components/ProfileMenuItem';
import {
  Settings, Users, FileText,
  Bell, Shield, HelpCircle, LogOut, Pencil, Smartphone, RefreshCw, Lock, Check,
} from 'lucide-react-native';

export default function ProfileScreen() {
  const router = useRouter();
  const u = currentUser;
  const [editingBio, setEditingBio] = useState(false);
  const [bioText, setBioText] = useState(u.bio);
  const [showLogout, setShowLogout] = useState(false);

  return (
    <View style={styles.screen}>
      <ScreenHeader
        large
        subtitle="个人中心"
        title="我的"
        right={
          <Pressable style={styles.iconBtn}>
            <Settings color={Colors.neutral[100]} size={20} />
          </Pressable>
        }
      />

      <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ paddingBottom: 120 }}>
        {/* Profile hero */}
        <View style={styles.hero}>
          <View style={styles.heroTop}>
            <Avatar uri={u.avatar} name={u.name} size={72} ring={Colors.primary[400]} online />
            <View style={styles.heroInfo}>
              <Text style={styles.name}>{u.name}</Text>
              <Text style={styles.handle}>{u.handle}</Text>
            </View>
          </View>

          {/* Editable bio */}
          {editingBio ? (
            <View style={styles.bioEditWrap}>
              <TextInput
                value={bioText}
                onChangeText={setBioText}
                placeholder="写一句个人简介…"
                placeholderTextColor={Colors.neutral[400]}
                style={styles.bioInput}
                multiline
                maxLength={60}
                autoFocus
              />
              <View style={styles.bioActions}>
                <Pressable
                  onPress={() => { setEditingBio(false); setBioText(u.bio); }}
                  style={styles.bioCancel}
                >
                  <Text style={styles.bioCancelText}>取消</Text>
                </Pressable>
                <Pressable
                  onPress={() => { updateBio(u.id, bioText.trim()); setEditingBio(false); }}
                  style={styles.bioSave}
                >
                  <Check color={Colors.ink[900]} size={14} />
                  <Text style={styles.bioSaveText}>保存</Text>
                </Pressable>
              </View>
            </View>
          ) : (
            <Pressable onPress={() => setEditingBio(true)} style={styles.bioRow}>
              <Text style={[styles.bioText, !u.bio && styles.bioHint]}>
                {u.bio || '点击添加个人简介…'}
              </Text>
              <Pencil color={Colors.neutral[400]} size={12} />
            </Pressable>
          )}

          <View style={styles.heroActions}>
            <Button label="编辑资料" variant="outline" color={Colors.primary[400]} size="md" onPress={() => Alert.alert('提示', '功能开发中')} />
            <Button label="分享主页" color={Colors.primary[400]} size="md" onPress={() => Alert.alert('提示', '功能开发中')} />
          </View>
        </View>

        {/* Stats — compact 2-up */}
        <View style={styles.statsGrid}>
          <StatCard icon={<Users color={Colors.primary[400]} size={20} />} value={u.squads} label="小队" color={Colors.primary[400]} />
          <StatCard icon={<FileText color={Colors.secondary[400]} size={20} />} value={u.posts} label="招募帖" color={Colors.secondary[400]} />
        </View>

        {/* Recent games */}
        {u.recentGames.length > 0 && (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>最近游戏</Text>
            <View style={styles.recentRow}>
              {u.recentGames.map((g, i) => (
                <View key={g} style={styles.recentTag}>
                  <Text style={styles.recentText}>{g}</Text>
                </View>
              ))}
            </View>
          </View>
        )}

        {/* Settings menu */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>设置</Text>
          <View style={styles.menu}>
            <ProfileMenuItem
              icon={<Pencil color={Colors.neutral[100]} size={18} />}
              label="编辑资料"
              onPress={() => Alert.alert('提示', '功能开发中')}
            />
            <ProfileMenuItem
              icon={<Smartphone color={Colors.neutral[100]} size={18} />}
              label="版本信息"
              value="v1.0.0"
              onPress={() => Alert.alert('提示', '功能开发中')}
            />
            <ProfileMenuItem
              icon={<RefreshCw color={Colors.neutral[100]} size={18} />}
              label="检查更新"
              onPress={() => Alert.alert('提示', '功能开发中')}
            />
            <ProfileMenuItem
              icon={<Lock color={Colors.neutral[100]} size={18} />}
              label="修改密码"
              onPress={() => Alert.alert('提示', '功能开发中')}
            />
            <ProfileMenuItem
              icon={<Bell color={Colors.neutral[100]} size={18} />}
              label="通知设置"
              value="已开启"
              onPress={() => Alert.alert('提示', '功能开发中')}
            />
            <ProfileMenuItem
              icon={<Shield color={Colors.neutral[100]} size={18} />}
              label="隐私与安全"
              onPress={() => Alert.alert('提示', '功能开发中')}
            />
            <ProfileMenuItem
              icon={<HelpCircle color={Colors.neutral[100]} size={18} />}
              label="帮助与反馈"
              onPress={() => Alert.alert('提示', '功能开发中')}
            />
            <ProfileMenuItem
              icon={<LogOut color={Colors.danger[400]} size={18} />}
              label="退出登录"
              danger
              last
              onPress={() => setShowLogout(true)}
            />
          </View>
        </View>

        <Text style={styles.version}>PlayMate v1.0.0 · 找到你的搭子</Text>
      </ScrollView>

      {/* Logout confirmation modal */}
      <Modal visible={showLogout} transparent animationType="fade">
        <View style={styles.overlay}>
          <View style={styles.logoutCard}>
            <Text style={styles.logoutTitle}>退出登录</Text>
            <Text style={styles.logoutBody}>确定要退出当前账号吗？</Text>
            <View style={styles.logoutActions}>
              <Pressable onPress={() => setShowLogout(false)} style={styles.logoutCancel}>
                <Text style={styles.logoutCancelText}>取消</Text>
              </Pressable>
              <Pressable
                onPress={() => { setShowLogout(false); router.replace('/login'); }}
                style={styles.logoutConfirm}
              >
                <LogOut color={Colors.ink[900]} size={16} />
                <Text style={styles.logoutConfirmText}>退出</Text>
              </Pressable>
            </View>
          </View>
        </View>
      </Modal>
    </View>
  );
}

function StatCard({ icon, value, label, color }: { icon: React.ReactNode; value: number | string; label: string; color: string }) {
  return (
    <View style={styles.statCard}>
      <View style={[styles.statIcon, { backgroundColor: `${color}22` }]}>{icon}</View>
      <Text style={styles.statVal}>{value}</Text>
      <Text style={styles.statLabel}>{label}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  iconBtn: {
    width: 40, height: 40, borderRadius: 999, backgroundColor: Colors.ink[800],
    alignItems: 'center', justifyContent: 'center', borderWidth: 1, borderColor: Colors.border,
  },
  hero: {
    marginHorizontal: Spacing.xl, marginBottom: Spacing.xl, backgroundColor: Colors.ink[800],
    borderRadius: Radius.lg, padding: Spacing.lg, borderWidth: 1, borderColor: Colors.border,
  },
  heroTop: { flexDirection: 'row', gap: Spacing.lg, marginBottom: Spacing.lg },
  heroInfo: { flex: 1, justifyContent: 'center', gap: 4 },
  name: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.xxl, lineHeight: 30 },
  handle: { color: Colors.neutral[300], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody },
  rankRow: { flexDirection: 'row', gap: Spacing.sm, marginTop: Spacing.sm },
  rankPill: {
    flexDirection: 'row', alignItems: 'center', gap: 5, backgroundColor: `${Colors.accent[400]}22`,
    paddingHorizontal: Spacing.md, paddingVertical: 5, borderRadius: Radius.pill,
  },
  rankText: { color: Colors.accent[400], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBodyBold },
  xpWrap: { marginBottom: Spacing.lg },
  xpHead: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: Spacing.sm },
  xpLabel: { color: Colors.neutral[200], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBodyMedium },
  xpVal: { color: Colors.neutral[100], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBodyBold },
  xpTrack: { height: 8, borderRadius: 999, backgroundColor: Colors.ink[700], overflow: 'hidden' },
  xpFill: { height: '100%', borderRadius: 999, backgroundColor: Colors.primary[400] },
  xpHint: { color: Colors.neutral[400], fontSize: Typography.sizes.xs, marginTop: Spacing.xs, fontFamily: Typography.fontFamilyBody },
  heroActions: { flexDirection: 'row', gap: Spacing.md },
  bioRow: { flexDirection: 'row', alignItems: 'center', gap: Spacing.xs, marginBottom: Spacing.md },
  bioText: { color: Colors.neutral[100], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody, flex: 1, lineHeight: 20 },
  bioHint: { color: Colors.neutral[400], fontStyle: 'italic' },
  bioEditWrap: { marginBottom: Spacing.md },
  bioInput: {
    backgroundColor: Colors.ink[700], borderRadius: Radius.md, padding: Spacing.md,
    color: Colors.neutral[50], fontFamily: Typography.fontFamilyBody, fontSize: Typography.sizes.sm,
    borderWidth: 1, borderColor: Colors.primary[400], minHeight: 60, textAlignVertical: 'top',
  },
  bioActions: { flexDirection: 'row', justifyContent: 'flex-end', gap: Spacing.sm, marginTop: Spacing.sm },
  bioCancel: { paddingHorizontal: Spacing.md, paddingVertical: Spacing.sm },
  bioCancelText: { color: Colors.neutral[300], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody },
  bioSave: {
    flexDirection: 'row', alignItems: 'center', gap: 4,
    backgroundColor: Colors.primary[400], paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm, borderRadius: Radius.md,
  },
  bioSaveText: { color: Colors.ink[900], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBodyBold },
  statsGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.md, paddingHorizontal: Spacing.xl, marginBottom: Spacing.xxl },
  statCard: {
    width: '47.5%', backgroundColor: Colors.ink[800], borderRadius: Radius.md, padding: Spacing.lg,
    alignItems: 'center', gap: Spacing.xs, borderWidth: 1, borderColor: Colors.border,
  },
  statIcon: { width: 40, height: 40, borderRadius: Radius.md, alignItems: 'center', justifyContent: 'center' },
  statVal: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.xxl, lineHeight: 30 },
  statLabel: { color: Colors.neutral[300], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody },
  section: { marginBottom: Spacing.xxl, paddingHorizontal: Spacing.xl },
  sectionTitle: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.lg, marginBottom: Spacing.md, lineHeight: 22 },
  gamesList: { gap: Spacing.md },
  gameRow: { gap: 6 },
  gameRowHead: { flexDirection: 'row', justifyContent: 'space-between' },
  gameName: { color: Colors.neutral[100], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBodyMedium },
  gameHours: { fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBodyBold },
  gameTrack: { height: 6, borderRadius: 999, backgroundColor: Colors.ink[700], overflow: 'hidden' },
  gameFill: { height: '100%', borderRadius: 999 },
  badge: {
    width: 110, alignItems: 'center', backgroundColor: Colors.ink[800], borderRadius: Radius.md,
    padding: Spacing.md, gap: 6, borderWidth: 1, borderColor: Colors.border,
  },
  badgeIcon: { width: 48, height: 48, borderRadius: Radius.md, alignItems: 'center', justifyContent: 'center', borderWidth: 1 },
  badgeLabel: { color: Colors.neutral[50], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBodyBold, textAlign: 'center' },
  badgeSub: { color: Colors.neutral[300], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody, textAlign: 'center' },
  recentRow: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.sm },
  recentTag: {
    backgroundColor: Colors.ink[700], paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.sm, borderRadius: Radius.pill,
    borderWidth: 1, borderColor: Colors.border,
  },
  recentText: { color: Colors.neutral[100], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBodyMedium },
  menu: { backgroundColor: Colors.ink[800], borderRadius: Radius.md, borderWidth: 1, borderColor: Colors.border, overflow: 'hidden' },
  version: { textAlign: 'center', color: Colors.neutral[400], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody, marginTop: Spacing.md },
  // Logout modal
  overlay: { flex: 1, backgroundColor: 'rgba(0,0,0,0.6)', justifyContent: 'center', alignItems: 'center', padding: Spacing.xl },
  logoutCard: {
    backgroundColor: Colors.ink[800], borderRadius: Radius.lg, padding: Spacing.xl,
    width: '100%', maxWidth: 320, borderWidth: 1, borderColor: Colors.border,
    alignItems: 'center',
  },
  logoutTitle: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.xl, marginBottom: Spacing.sm },
  logoutBody: { color: Colors.neutral[200], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody, marginBottom: Spacing.lg, textAlign: 'center' },
  logoutActions: { flexDirection: 'row', gap: Spacing.md, width: '100%' },
  logoutCancel: {
    flex: 1, alignItems: 'center', paddingVertical: Spacing.md, borderRadius: Radius.md,
    backgroundColor: Colors.ink[700], borderWidth: 1, borderColor: Colors.border,
  },
  logoutCancelText: { color: Colors.neutral[200], fontFamily: Typography.fontFamilyBodyBold, fontSize: Typography.sizes.base },
  logoutConfirm: {
    flex: 1, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: Spacing.xs,
    paddingVertical: Spacing.md, borderRadius: Radius.md, backgroundColor: Colors.danger[400],
  },
  logoutConfirmText: { color: Colors.ink[900], fontFamily: Typography.fontFamilyBodyBold, fontSize: Typography.sizes.base },
});
