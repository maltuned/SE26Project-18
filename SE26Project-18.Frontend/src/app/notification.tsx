import { useFocusEffect, useRouter } from "expo-router";
import React, { useCallback, useState } from "react";
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  StyleSheet,
  Text,
  View,
} from "react-native";
import {
  getNotifications,
  markAllNotificationsRead,
  NotificationItem,
} from "../api/api";
import { useTheme } from "../contexts/theme-context";
import NotificationCard from "../components/notification-card";

export default function NotificationScreen() {
  const router = useRouter();
  const { colors } = useTheme();
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState(true);

  const loadNotifications = useCallback(() => {
    setLoading(true);
    getNotifications().then((data) => {
      setNotifications(data);
      setLoading(false);
      markAllNotificationsRead();
    });
  }, []);

  useFocusEffect(loadNotifications);

  const renderItem = ({ item }: { item: NotificationItem }) => (
    <NotificationCard item={item} colors={colors} />
  );

  return (
    <View style={[styles.container, { backgroundColor: colors.background }]}>
      <View style={styles.header}>
        <Pressable onPress={() => router.back()}>
          <Text style={[styles.backButton, { color: colors.primary }]}>
            返回
          </Text>
        </Pressable>
        <Text style={[styles.headerTitle, { color: colors.text }]}>通知</Text>
        <View style={styles.backButton} />
      </View>

      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={colors.primary} />
        </View>
      ) : notifications.length === 0 ? (
        <View style={styles.emptyContainer}>
          <Text style={[styles.emptyText, { color: colors.textSecondary }]}>
            暂无通知
          </Text>
        </View>
      ) : (
        <FlatList
          data={notifications}
          keyExtractor={(item) => String(item.id)}
          renderItem={renderItem}
          contentContainerStyle={styles.list}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  header: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  backButton: { fontSize: 16, width: 50 },
  headerTitle: { fontSize: 20, fontWeight: "bold" },
  loadingContainer: { flex: 1, justifyContent: "center", alignItems: "center" },
  emptyContainer: { flex: 1, justifyContent: "center", alignItems: "center" },
  emptyText: { fontSize: 16 },
  list: { paddingHorizontal: 16, paddingBottom: 24 },
});