import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import RemoteImage from "./remote-image";
import { useTheme } from "../contexts/theme-context";

export interface ChatEntryInfo {
  id: string;
  name: string;
  avatar: string;
  lastMessage: string;
  time: string;
  unreadCount: number;
}

interface ChatEntryProps {
  chat: ChatEntryInfo;
  onPress: (chat: ChatEntryInfo) => void;
}

function ChatEntry({ chat, onPress }: ChatEntryProps) {
  const { colors } = useTheme();

  return (
    <TouchableOpacity
      style={[styles.chatItem, { borderBottomColor: colors.border }]}
      onPress={() => onPress(chat)}
    >
      <RemoteImage
        url={chat.avatar}
        style={[styles.avatar, { backgroundColor: colors.primary }]}
      />
      <View style={styles.chatInfo}>
        <View style={styles.chatTop}>
          <Text style={[styles.chatName, { color: colors.text }]}>
            {chat.name}
          </Text>
          <Text style={[styles.chatTime, { color: colors.textTertiary }]}>
            {chat.time}
          </Text>
        </View>
        <View style={styles.chatBottom}>
          <Text
            style={[styles.lastMessage, { color: colors.textTertiary }]}
            numberOfLines={1}
          >
            {chat.lastMessage}
          </Text>
          {chat.unreadCount > 0 && (
            <View style={[styles.badge, { backgroundColor: colors.primary }]}>
              <Text style={styles.badgeText}>
                {chat.unreadCount > 99 ? "99+" : chat.unreadCount}
              </Text>
            </View>
          )}
        </View>
      </View>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  chatItem: {
    flexDirection: "row",
    alignItems: "center",
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
  },
  avatar: {
    width: 48,
    height: 48,
    borderRadius: 24,
    justifyContent: "center",
    alignItems: "center",
    marginRight: 12,
  },
  chatInfo: {
    flex: 1,
  },
  chatTop: {
    flexDirection: "row",
    justifyContent: "space-between",
    marginBottom: 4,
  },
  chatName: {
    fontSize: 16,
    fontWeight: "600",
  },
  chatTime: {
    fontSize: 12,
  },
  lastMessage: {
    fontSize: 14,
    flex: 1,
  },
  chatBottom: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
  },
  badge: {
    minWidth: 20,
    height: 20,
    borderRadius: 10,
    justifyContent: "center",
    alignItems: "center",
    paddingHorizontal: 6,
  },
  badgeText: {
    color: "#fff",
    fontSize: 11,
    fontWeight: "700",
  },
});

export default ChatEntry;