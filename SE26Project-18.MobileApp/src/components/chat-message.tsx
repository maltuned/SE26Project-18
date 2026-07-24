import { StyleSheet, Text, View } from "react-native";
import { useTheme } from "../contexts/theme-context";

export interface ChatMessageInfo {
  id: string;
  text: string;
  sender: string;
  created_at?: string;
}

interface ChatMessageProps {
  message: ChatMessageInfo;
}

function ChatMessage({ message }: ChatMessageProps) {
  const { colors } = useTheme();

  return (
    <View
      style={[
        styles.messageBubble,
        message.sender === "me"
          ? [styles.myMessage, { backgroundColor: colors.messageMy }]
          : [styles.otherMessage, { backgroundColor: colors.messageOther }],
      ]}
    >
      <Text
        style={
          message.sender === "me"
            ? [styles.myMessageText, { color: colors.messageMyText }]
            : [styles.otherMessageText, { color: colors.messageOtherText }]
        }
      >
        {message.text}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  messageBubble: {
    maxWidth: "80%",
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderRadius: 18,
    marginVertical: 4,
  },
  myMessage: {
    alignSelf: "flex-end",
  },
  otherMessage: {
    alignSelf: "flex-start",
  },
  myMessageText: {
    fontSize: 15,
  },
  otherMessageText: {
    fontSize: 15,
  },
});

export default ChatMessage;
