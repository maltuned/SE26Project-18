import { StyleSheet, Text, View, ScrollView } from 'react-native';
import { useRouter } from 'expo-router';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { currentUser } from '@/data/mock';
import { ScreenHeader } from '@/components/ScreenHeader';
import { ToolButton } from '@/components/ToolButton';
import {
  Shield, MessageSquareHeart, Headphones, ShieldCheck,
} from 'lucide-react-native';

const tools = [
  { icon: <Shield color="#A78BFA" size={24} />, label: '账号绑定', color: '#A78BFA' },
  { icon: <MessageSquareHeart color="#34D399" size={24} />, label: '反馈建议', color: '#34D399' },
  { icon: <Headphones color="#F43F5E" size={24} />, label: '客服支持', color: '#F43F5E' },
];

export default function ToolsScreen() {
  const router = useRouter();

  return (
    <View style={styles.screen}>
      <ScreenHeader large subtitle="实用工具" title="工具" />

      <ScrollView
        showsVerticalScrollIndicator={false}
        contentContainerStyle={styles.grid}
      >
        {/* Admin entry */}
        {currentUser.isAdmin && (
          <>
            <Text style={styles.sectionTitle}>管理</Text>
            <View style={styles.gridRow}>
              <ToolButton
                icon={<ShieldCheck color="#FBBF24" size={24} />}
                label="管理员面板"
                color="#FBBF24"
                onPress={() => router.push('/admin')}
              />
            </View>
            <View style={styles.divider} />
          </>
        )}

        <Text style={styles.hint}>以下为预设工具，功能开发中</Text>
        <View style={styles.gridRow}>
          {tools.map((tool, i) => (
            <ToolButton
              key={i}
              icon={tool.icon}
              label={tool.label}
              color={tool.color}
              onPress={() => {}}
            />
          ))}
        </View>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  grid: { paddingHorizontal: Spacing.xl, paddingBottom: 120 },
  sectionTitle: {
    color: Colors.accent[400], fontSize: Typography.sizes.sm,
    fontFamily: Typography.fontFamilyBodyBold, marginBottom: Spacing.md,
  },
  divider: {
    height: 1, backgroundColor: Colors.border,
    marginVertical: Spacing.lg,
  },
  hint: {
    color: Colors.neutral[400], fontSize: Typography.sizes.xs,
    fontFamily: Typography.fontFamilyBody, textAlign: 'center',
    marginBottom: Spacing.lg, marginTop: Spacing.xs,
  },
  gridRow: {
    flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.md, justifyContent: 'space-between',
  },
});
