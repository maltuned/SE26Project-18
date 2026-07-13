import { StyleSheet, Text, View, Pressable } from 'react-native';
import { Colors, Radius, Spacing, Typography } from '@/constants/theme';

const DURATION_OPTIONS = [
  { minutes: 30, label: '30分钟', sub: '快速匹配' },
  { minutes: 1440, label: '24小时', sub: '常规招募' },
  { minutes: 10080, label: '7天', sub: '长期有效' },
] as const;

type Props = {
  value: number;
  onChange: (minutes: number) => void;
  accent?: string;
};

export function DurationPicker({ value, onChange, accent = Colors.primary[400] }: Props) {
  return (
    <View style={styles.row}>
      {DURATION_OPTIONS.map((opt) => {
        const active = value === opt.minutes;
        return (
          <Pressable
            key={opt.minutes}
            onPress={() => onChange(opt.minutes)}
            style={[
              styles.option,
              active && { backgroundColor: accent, borderColor: accent },
            ]}
          >
            <Text
              style={[
                styles.label,
                active && { color: Colors.ink[900], fontFamily: Typography.fontFamilyBodyBold },
              ]}
            >
              {opt.label}
            </Text>
            <Text
              style={[
                styles.sub,
                active && { color: `${Colors.ink[900]}CC` },
              ]}
            >
              {opt.sub}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    gap: Spacing.sm,
  },
  option: {
    flex: 1,
    alignItems: 'center',
    paddingVertical: Spacing.md,
    borderRadius: Radius.md,
    borderWidth: 1,
    borderColor: Colors.border,
    backgroundColor: Colors.ink[800],
    gap: 2,
  },
  label: {
    color: Colors.neutral[100],
    fontFamily: Typography.fontFamilyBodyMedium,
    fontSize: Typography.sizes.sm,
  },
  sub: {
    color: Colors.neutral[400],
    fontFamily: Typography.fontFamilyBody,
    fontSize: Typography.sizes.xs,
  },
});
