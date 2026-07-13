import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Colors, Radius, Spacing, Typography } from '@/constants/theme';

type Props = {
  label: string;
  onPress?: () => void;
  color?: string;
  variant?: 'solid' | 'ghost' | 'outline';
  icon?: React.ReactNode;
  disabled?: boolean;
  size?: 'sm' | 'md' | 'lg';
};

export function Button({
  label,
  onPress,
  color = Colors.primary[400],
  variant = 'solid',
  icon,
  disabled,
  size = 'md',
}: Props) {
  const bg =
    variant === 'solid'
      ? color
      : variant === 'outline'
      ? 'transparent'
      : `${color}1F`;
  const border = variant === 'outline' ? { borderColor: color, borderWidth: 1.5 } : null;
  const fg = variant === 'solid' ? Colors.ink[900] : color;
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      style={({ pressed }) => [
        styles.btn,
        size === 'sm' && styles.sm,
        size === 'lg' && styles.lg,
        { backgroundColor: bg },
        border,
        pressed && styles.pressed,
        disabled && styles.disabled,
      ]}
    >
      {icon}
      <Text style={[styles.label, { color: fg }, size === 'sm' && styles.smLabel, size === 'lg' && styles.lgLabel]}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  btn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.xs,
    paddingVertical: Spacing.md,
    paddingHorizontal: Spacing.lg,
    borderRadius: Radius.md,
  },
  sm: { paddingVertical: Spacing.sm, paddingHorizontal: Spacing.md },
  lg: { paddingVertical: Spacing.lg, paddingHorizontal: Spacing.xxl },
  label: { fontFamily: Typography.fontFamilyBodyBold, fontSize: Typography.sizes.base },
  smLabel: { fontSize: Typography.sizes.xs },
  lgLabel: { fontSize: Typography.sizes.lg },
  pressed: { opacity: 0.82, transform: [{ scale: 0.98 }] },
  disabled: { opacity: 0.4 },
});
