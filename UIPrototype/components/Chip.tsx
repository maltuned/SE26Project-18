import { StyleSheet, Text, View } from 'react-native';
import { Colors, Radius, Spacing, Typography } from '@/constants/theme';

type Props = {
  label: string;
  color?: string;
  solid?: boolean;
  size?: 'sm' | 'md';
};

export function Chip({ label, color = Colors.primary[400], solid, size = 'sm' }: Props) {
  const bg = solid ? color : `${color}1A`;
  const fg = solid ? Colors.ink[900] : color;
  return (
    <View style={[styles.chip, { backgroundColor: bg }, size === 'sm' && styles.sm]}>
      <Text style={[styles.text, { color: fg, fontFamily: Typography.fontFamilyBodyMedium }, size === 'sm' && styles.smText]}>
        {label}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  chip: {
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs,
    borderRadius: Radius.pill,
    flexDirection: 'row',
    alignItems: 'center',
  },
  sm: { paddingHorizontal: Spacing.sm, paddingVertical: 3 },
  text: { fontSize: Typography.sizes.sm, lineHeight: Typography.sizes.sm * 1.1 },
  smText: { fontSize: Typography.sizes.xs },
});
