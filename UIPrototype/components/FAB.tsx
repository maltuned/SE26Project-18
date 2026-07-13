import { Pressable, StyleSheet, Text } from 'react-native';
import { Colors, Radius, Spacing, Typography, Shadow } from '@/constants/theme';
import { Plus } from 'lucide-react-native';

type Props = {
  onPress: () => void;
  color?: string;
  bottomOffset?: number;
};

export function FAB({ onPress, color = Colors.primary[400], bottomOffset = 80 }: Props) {
  return (
    <Pressable
      onPress={onPress}
      style={({ pressed }) => [
        styles.fab,
        { backgroundColor: color, bottom: bottomOffset },
        pressed && styles.pressed,
      ]}
    >
      <Plus color={Colors.ink[900]} size={28} strokeWidth={2.5} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  fab: {
    position: 'absolute',
    right: Spacing.xl,
    width: 56,
    height: 56,
    borderRadius: 28,
    alignItems: 'center',
    justifyContent: 'center',
    ...Shadow.card,
    zIndex: 100,
  },
  pressed: {
    opacity: 0.85,
    transform: [{ scale: 0.95 }],
  },
});
