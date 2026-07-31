import React from "react";
import { StyleSheet, Text, View } from "react-native";
import { NotificationItem } from "../api/api";

interface Props {
  item: NotificationItem;
  colors: {
    card: string;
    text: string;
    textSecondary: string;
  };
}

export const formatTime = (createdAt: string): string => {
  const date = new Date(createdAt);
  const pad = (n: number) => n.toString().padStart(2, "0");
  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ` +
    `${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
  );
};

export default function NotificationCard({ item, colors }: Props) {
  return (
    <View style={[styles.card, { backgroundColor: colors.card }]}>
      {!item.isRead && (
        <View style={[styles.unreadDot, { backgroundColor: "red" }]} />
      )}
      <View style={styles.cardHeader}>
        <Text style={[styles.title, { color: colors.text }]} numberOfLines={1}>
          {item.title}
        </Text>
        <Text style={[styles.time, { color: colors.textSecondary }]}>
          {formatTime(item.createdAt)}
        </Text>
      </View>
      <Text
        style={[styles.body, { color: colors.textSecondary }]}
        numberOfLines={3}
      >
        {item.body}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    borderRadius: 10,
    padding: 14,
    marginBottom: 10,
    shadowColor: "#000",
    shadowOpacity: 0.05,
    shadowRadius: 4,
    shadowOffset: { width: 0, height: 2 },
    elevation: 1,
  },
  unreadDot: {
    position: "absolute",
    top: 10,
    right: 10,
    width: 8,
    height: 8,
    borderRadius: 4,
  },
  cardHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 6,
  },
  title: { fontSize: 15, fontWeight: "600", flex: 1, marginRight: 8 },
  time: { fontSize: 12 },
  body: { fontSize: 14, lineHeight: 20 },
});