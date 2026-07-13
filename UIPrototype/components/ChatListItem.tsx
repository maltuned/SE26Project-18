import { StyleSheet, Text, View, Pressable } from 'react-native';
import { Colors, Radius, Spacing, Typography } from '@/constants/theme';
import { Avatar } from './Avatar';
import { MessageCircle } from 'lucide-react-native';
import type { ChatSession } from '@/data/types';

type Props = {
  session: ChatSession;
  onPress: () => void;
};

export function ChatListItem({ session, onPress }: Props) {
  return (
    <Pressable
      onPress={onPress}
      style={({ pressed }) => [styles.wrap, pressed && styles.pressed]}
    >
      <Avatar uri={session.participantAvatar} name={session.participantName} size={52} online={session.online} />
      <View style={styles.info}>
        <View style={styles.topRow}>
          <Text style={styles.name} numberOfLines={1}>{session.participantName}</Text>
          <Text style={styles.time}>{session.lastMessageTime}</Text>
        </View>
        <View style={styles.bottomRow}>
          <Text style={styles.gameTag} numberOfLines={1}>{session.gameName}</Text>
          <Text style={styles.message} numberOfLines={1}>{session.lastMessage}</Text>
        </View>
      </View>
      {session.unreadCount > 0 && (
        <View style={styles.badge}>
          <Text style={styles.badgeText}>{session.unreadCount > 99 ? '99+' : session.unreadCount}</Text>
        </View>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  wrap: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: Spacing.xl,
    paddingVertical: Spacing.md,
    gap: Spacing.md,
  },
  pressed: { opacity: 0.7 },
  info: { flex: 1, gap: 4 },
  topRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  name: {
    color: Colors.neutral[50],
    fontFamily: Typography.fontFamilyDisplay,
    fontSize: Typography.sizes.base,
    flex: 1,
  },
  time: {
    color: Colors.neutral[400],
    fontSize: Typography.sizes.xs,
    fontFamily: Typography.fontFamilyBody,
  },
  bottomRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.sm,
  },
  gameTag: {
    color: Colors.primary[300],
    fontSize: Typography.sizes.xs,
    fontFamily: Typography.fontFamilyBodyMedium,
  },
  message: {
    color: Colors.neutral[300],
    fontSize: Typography.sizes.sm,
    fontFamily: Typography.fontFamilyBody,
    flex: 1,
  },
  badge: {
    minWidth: 22,
    height: 22,
    borderRadius: 11,
    backgroundColor: Colors.danger[400],
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 6,
  },
  badgeText: {
    color: Colors.neutral[50],
    fontSize: 11,
    fontFamily: Typography.fontFamilyBodyBold,
  },
});
