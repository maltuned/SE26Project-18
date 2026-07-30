import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { useTheme } from "../contexts/theme-context";
import MediaImage from "./media-image";

export interface ChatEntryInfo {
  id: string;
  name: string;
  lastMessage: string;
  time: string;
  avatar?: string;
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
      <MediaImage
        key={chat.id}
        uri={chat.avatar}
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
        <Text
          style={[styles.lastMessage, { color: colors.textTertiary }]}
          numberOfLines={1}
        >
          {chat.lastMessage}
        </Text>
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
  },
});

export default ChatEntry;
