import { useRouter } from "expo-router";
import { useEffect, useRef } from "react";
import {
  Animated,
  PanResponder,
  StyleSheet,
  Text,
  TouchableOpacity,
  useWindowDimensions,
  View,
} from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import RemoteImage from "./remote-image";
import { useTheme } from "../contexts/theme-context";

type ToastMessage = {
  id: string;
  chatId: number;
  senderName: string;
  senderAvatar: string;
  content: string;
  createdAt: number;
};

const TOAST_DURATION = 5000;
const DISMISS_THRESHOLD = 60;

export { type ToastMessage };

export default function MessageToast({
  toast,
  onDismiss,
}: {
  toast: ToastMessage | null;
  onDismiss: () => void;
}) {
  const router = useRouter();
  const { colors } = useTheme();
  const { width } = useWindowDimensions();
  const insets = useSafeAreaInsets();
  const translateY = useRef(new Animated.Value(-200)).current;
  const translateX = useRef(new Animated.Value(0)).current;
  const opacity = useRef(new Animated.Value(0)).current;
  const dismissTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (toast) {
      translateX.setValue(0);
      translateY.setValue(-200);
      Animated.parallel([
        Animated.spring(translateY, {
          toValue: 0,
          useNativeDriver: true,
          damping: 15,
          stiffness: 120,
        }),
        Animated.timing(opacity, {
          toValue: 1,
          duration: 200,
          useNativeDriver: true,
        }),
      ]).start();

      dismissTimer.current = setTimeout(() => {
        dismiss();
      }, TOAST_DURATION);
    } else {
      translateY.setValue(-200);
      opacity.setValue(0);
    }

    return () => {
      if (dismissTimer.current) clearTimeout(dismissTimer.current);
    };
  }, [toast?.id]);

  const dismiss = () => {
    Animated.parallel([
      Animated.timing(translateY, {
        toValue: -200,
        duration: 250,
        useNativeDriver: true,
      }),
      Animated.timing(opacity, {
        toValue: 0,
        duration: 200,
        useNativeDriver: true,
      }),
    ]).start(() => {
      onDismiss();
    });
  };

  const panResponder = useRef(
    PanResponder.create({
      onMoveShouldSetPanResponder: (_, gesture) =>
        Math.abs(gesture.dy) > 5 || Math.abs(gesture.dx) > 5,
      onPanResponderMove: (_, gesture) => {
        translateY.setValue(Math.min(0, gesture.dy));
        translateX.setValue(gesture.dx);
      },
      onPanResponderRelease: (_, gesture) => {
        if (gesture.dy < -DISMISS_THRESHOLD || Math.abs(gesture.dx) > width * 0.3) {
          Animated.parallel([
            Animated.timing(translateY, {
              toValue: -200,
              duration: 200,
              useNativeDriver: true,
            }),
            Animated.timing(translateX, {
              toValue: gesture.dx > 0 ? width : -width,
              duration: 200,
              useNativeDriver: true,
            }),
            Animated.timing(opacity, {
              toValue: 0,
              duration: 200,
              useNativeDriver: true,
            }),
          ]).start(() => {
            onDismiss();
          });
        } else {
          Animated.parallel([
            Animated.spring(translateY, {
              toValue: 0,
              useNativeDriver: true,
              damping: 15,
              stiffness: 120,
            }),
            Animated.spring(translateX, {
              toValue: 0,
              useNativeDriver: true,
              damping: 15,
              stiffness: 120,
            }),
          ]).start();
        }
      },
    }),
  ).current;

  if (!toast) return null;

  return (
    <Animated.View
      style={[
        styles.container,
        {
          top: insets.top + 8,
          backgroundColor: colors.card,
          borderColor: colors.border,
        },
        { transform: [{ translateY }, { translateX }], opacity },
      ]}
      {...panResponder.panHandlers}
    >
      <TouchableOpacity
        style={styles.touchArea}
        activeOpacity={0.8}
        onPress={() => {
          dismiss();
          router.push(`/chat-room?chatId=${toast.chatId}`);
        }}
      >
        <RemoteImage
          url={toast.senderAvatar}
          style={[styles.avatar, { backgroundColor: colors.primary }]}
        />
        <View style={styles.textContainer}>
          <Text style={[styles.name, { color: colors.text }]} numberOfLines={1}>
            {toast.senderName}
          </Text>
          <Text
            style={[styles.content, { color: colors.textSecondary }]}
            numberOfLines={2}
          >
            {toast.content}
          </Text>
        </View>
      </TouchableOpacity>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  container: {
    position: "absolute",
    left: 12,
    right: 12,
    zIndex: 10000,
    borderRadius: 14,
    borderWidth: 1,
    elevation: 10,
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.15,
    shadowRadius: 8,
  },
  touchArea: {
    flexDirection: "row",
    alignItems: "center",
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
  },
  textContainer: {
    flex: 1,
    marginLeft: 12,
  },
  name: {
    fontSize: 15,
    fontWeight: "600",
    marginBottom: 2,
  },
  content: {
    fontSize: 14,
    lineHeight: 18,
  },
});