import { StyleSheet, Text, Pressable, View } from 'react-native';
import { Colors, Radius, Spacing, Typography } from '@/constants/theme';
import { ChevronRight } from 'lucide-react-native';

type Props = {
  icon: React.ReactNode;
  label: string;
  value?: string;
  danger?: boolean;
  last?: boolean;
  onPress?: () => void;
};

export function ProfileMenuItem({ icon, label, value, danger, last, onPress }: Props) {
  return (
    <Pressable
      onPress={onPress}
      style={({ pressed }) => [
        styles.row,
        !last && styles.border,
        pressed && { opacity: 0.6 },
      ]}
    >
      <View style={[styles.iconWrap, danger && { backgroundColor: `${Colors.danger[400]}22` }]}>
        {icon}
      </View>
      <Text style={[styles.label, danger && { color: Colors.danger[400] }]}>{label}</Text>
      <View style={styles.right}>
        {value ? (
          <Text style={styles.value}>{value}</Text>
        ) : (
          <ChevronRight color={Colors.neutral[400]} size={18} />
        )}
      </View>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: Spacing.md,
    paddingHorizontal: Spacing.lg,
    gap: Spacing.md,
  },
  border: {
    borderBottomWidth: 1,
    borderBottomColor: Colors.border,
  },
  iconWrap: {
    width: 36,
    height: 36,
    borderRadius: Radius.sm,
    backgroundColor: Colors.ink[700],
    alignItems: 'center',
    justifyContent: 'center',
  },
  label: {
    flex: 1,
    color: Colors.neutral[50],
    fontSize: Typography.sizes.base,
    fontFamily: Typography.fontFamilyBodyMedium,
  },
  right: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  value: {
    color: Colors.neutral[300],
    fontSize: Typography.sizes.sm,
    fontFamily: Typography.fontFamilyBody,
  },
});
