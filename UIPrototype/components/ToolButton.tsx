import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Colors, Radius, Spacing, Typography } from '@/constants/theme';

type Props = {
  icon: React.ReactNode;
  label: string;
  color?: string;
  onPress?: () => void;
};

export function ToolButton({ icon, label, color = Colors.primary[400], onPress }: Props) {
  return (
    <Pressable
      onPress={onPress}
      style={({ pressed }) => [
        styles.wrap,
        { borderColor: `${color}33` },
        pressed && styles.pressed,
      ]}
    >
      <View style={[styles.iconWrap, { backgroundColor: `${color}1A` }]}>
        {icon}
      </View>
      <Text style={styles.label}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  wrap: {
    width: '47%',
    alignItems: 'center',
    paddingVertical: Spacing.lg,
    borderRadius: Radius.lg,
    backgroundColor: Colors.ink[800],
    borderWidth: 1,
    gap: Spacing.md,
  },
  pressed: { opacity: 0.8, transform: [{ scale: 0.97 }] },
  iconWrap: {
    width: 52,
    height: 52,
    borderRadius: Radius.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  label: {
    color: Colors.neutral[100],
    fontFamily: Typography.fontFamilyBodyMedium,
    fontSize: Typography.sizes.sm,
  },
});
