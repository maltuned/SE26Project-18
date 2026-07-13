import { ImageBackground, Pressable, StyleSheet, Text, View } from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { Colors, Radius, Spacing, Typography } from '@/constants/theme';
import { Users, Radio } from 'lucide-react-native';
import type { Game } from '@/data/types';

type Props = {
  game: Game;
  accent?: string;
  onPress: () => void;
};

export function GameCard({ game, accent = Colors.primary[400], onPress }: Props) {
  return (
    <Pressable onPress={onPress} style={({ pressed }) => [styles.wrap, pressed && styles.pressed]}>
      <ImageBackground source={{ uri: game.coverUrl }} style={styles.cover} imageStyle={styles.coverImg}>
        <LinearGradient
          colors={['transparent', `${accent}66`, Colors.ink[900]]}
          locations={[0, 0.55, 1]}
          style={styles.gradient}
        >
          <View style={styles.topRow}>
            <View style={[styles.onlinePill, { backgroundColor: `${Colors.ink[900]}AA` }]}>
              <Radio color={Colors.online} size={12} />
              <Text style={styles.onlineText}>{game.onlineCount.toLocaleString()} 在线</Text>
            </View>
          </View>
          <View style={styles.bottom}>
            <Text style={styles.name} numberOfLines={1}>{game.name}</Text>
            <Text style={styles.tagline} numberOfLines={1}>{game.tagline}</Text>
            <View style={styles.metaRow}>
              <Users color={accent} size={13} />
              <Text style={[styles.metaText, { color: accent }]}>{(game.memberCount / 1000).toFixed(1)}k</Text>
              <Text style={styles.metaDim}>社区成员</Text>
            </View>
          </View>
        </LinearGradient>
      </ImageBackground>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  wrap: { width: 200, height: 250, marginRight: Spacing.md, borderRadius: Radius.lg, overflow: 'hidden' },
  pressed: { opacity: 0.85, transform: [{ scale: 0.98 }] },
  cover: { flex: 1 },
  coverImg: { borderRadius: Radius.lg },
  gradient: { flex: 1, justifyContent: 'space-between', padding: Spacing.md },
  topRow: { flexDirection: 'row', justifyContent: 'flex-end' },
  onlinePill: { flexDirection: 'row', alignItems: 'center', gap: 4, paddingHorizontal: Spacing.sm, paddingVertical: 4, borderRadius: Radius.pill },
  onlineText: { color: Colors.neutral[50], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBodyMedium },
  bottom: { gap: 2 },
  name: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.lg, lineHeight: 21 },
  tagline: { color: Colors.neutral[200], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody },
  metaRow: { flexDirection: 'row', alignItems: 'center', gap: 4, marginTop: 4 },
  metaText: { fontFamily: Typography.fontFamilyBodyBold, fontSize: Typography.sizes.xs },
  metaDim: { color: Colors.neutral[300], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody },
});
