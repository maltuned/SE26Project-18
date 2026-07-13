import { Image, StyleSheet, Text, View } from 'react-native';
import { Colors, Typography } from '@/constants/theme';

const PALETTE = [
  '#00C8F0', '#F5A623', '#22D3A0', '#FF6B81', '#A78BFA',
  '#34D399', '#FBBF24', '#F43F5E', '#6366F1', '#EC4899',
  '#14B8A6', '#8B5CF6', '#F97316', '#06B6D4', '#84CC16',
];

function getColor(name: string): string {
  let hash = 0;
  for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash);
  return PALETTE[Math.abs(hash) % PALETTE.length];
}

function getInitials(name: string): string {
  if (!name) return '?';
  const first = name.charAt(0);
  // English letter → first 2 chars; otherwise (Chinese etc.) → first char
  return /^[A-Za-z]/.test(first) ? name.slice(0, 2) : first;
}

type Props = {
  uri?: string;
  name?: string;
  size?: number;
  online?: boolean;
  ring?: string;
};

export function Avatar({ uri, name = '', size = 40, online, ring }: Props) {
  const ringStyle = ring ? { borderColor: ring, borderWidth: 2 } : null;
  const bg = getColor(name);

  return (
    <View style={[styles.wrap, { width: size, height: size }]}>
      {uri ? (
        <Image
          source={{ uri }}
          style={[styles.img, ringStyle]}
          accessibilityLabel="avatar"
        />
      ) : (
        <View style={[styles.initialsWrap, { backgroundColor: bg, width: size, height: size }, ringStyle]}>
          <Text style={[styles.initials, { fontSize: size * 0.38 }]}>
            {getInitials(name)}
          </Text>
        </View>
      )}
      {online !== undefined && (
        <View
          style={[
            styles.dot,
            { borderColor: Colors.ink[800], backgroundColor: online ? Colors.online : Colors.offline },
          ]}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { justifyContent: 'center', alignItems: 'center' },
  img: { width: '100%', height: '100%', borderRadius: 999 },
  initialsWrap: { borderRadius: 999, alignItems: 'center', justifyContent: 'center' },
  initials: { color: Colors.ink[900], fontFamily: Typography.fontFamilyDisplay },
  dot: {
    position: 'absolute',
    right: 0,
    bottom: 0,
    width: 11,
    height: 11,
    borderRadius: 999,
    borderWidth: 2,
  },
});
