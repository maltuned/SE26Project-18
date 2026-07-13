import { StyleSheet, Text, View, FlatList } from 'react-native';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { chatSessions } from '@/data/mock';
import { ScreenHeader } from '@/components/ScreenHeader';
import { ChatListItem } from '@/components/ChatListItem';
import { MessageCircle } from 'lucide-react-native';
import { useRouter } from 'expo-router';

export default function ChatScreen() {
  const router = useRouter();

  return (
    <View style={styles.screen}>
      <ScreenHeader large subtitle="消息" title="聊天" />

      <FlatList
        data={chatSessions}
        keyExtractor={(item) => item.id.toString()}
        showsVerticalScrollIndicator={false}
        contentContainerStyle={{ paddingBottom: 100 }}
        ItemSeparatorComponent={() => (
          <View style={styles.separator} />
        )}
        renderItem={({ item }) => (
          <ChatListItem
            session={item}
            onPress={() => router.push(`/chat/${item.id}`)}
          />
        )}
        ListEmptyComponent={
          <View style={styles.emptyState}>
            <MessageCircle color={Colors.neutral[400]} size={44} />
            <Text style={styles.emptyTitle}>暂无聊天记录</Text>
            <Text style={styles.emptySub}>加入小队后即可开始聊天</Text>
          </View>
        }
      />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  separator: {
    height: 1, backgroundColor: Colors.border,
    marginLeft: Spacing.xxl + Spacing.xl + 52 + Spacing.md,
  },
  emptyState: {
    alignItems: 'center', paddingTop: Spacing.huge * 2, gap: Spacing.md,
  },
  emptyTitle: {
    color: Colors.neutral[100], fontFamily: Typography.fontFamilyDisplay,
    fontSize: Typography.sizes.lg,
  },
  emptySub: {
    color: Colors.neutral[300], fontSize: Typography.sizes.sm,
    fontFamily: Typography.fontFamilyBody,
  },
});
