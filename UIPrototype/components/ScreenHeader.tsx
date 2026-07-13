import { StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Colors, Spacing, Typography } from '@/constants/theme';

type Props = {
  title: string;
  subtitle?: string;
  right?: React.ReactNode;
  large?: boolean;
};

export function ScreenHeader({ title, subtitle, right, large }: Props) {
  return (
    <SafeAreaView edges={['top']} style={styles.safe}>
      <View style={[styles.bar, large && styles.barLarge]}>
        <View style={styles.titles}>
          {subtitle && <Text style={styles.subtitle}>{subtitle}</Text>}
          <Text style={[styles.title, large && styles.titleLarge]}>{title}</Text>
        </View>
        {right}
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { backgroundColor: Colors.ink[900] },
  bar: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingHorizontal: Spacing.xl, paddingVertical: Spacing.lg },
  barLarge: { paddingVertical: Spacing.xxl },
  titles: { flex: 1, gap: 2 },
  subtitle: { color: Colors.primary[300], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBodyMedium, letterSpacing: 1.5, textTransform: 'uppercase' },
  title: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.xl, lineHeight: 26 },
  titleLarge: { fontSize: Typography.sizes.huge, lineHeight: 38 },
});
