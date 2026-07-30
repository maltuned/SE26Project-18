import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { NotificationDto } from "../dtos";
import { NotificationItem } from "../data-patterns";
import { apiGet, apiPut, handleResponseDirect, handleArrayResponse } from "../fetch";
import { mapNotificationDto } from "../mappers";

export const getNotifications = (): Promise<NotificationItem[]> => {
  const response = apiGet<NotificationDto[]>("/Notification");
  return handleArrayResponse<NotificationItem>(
    response,
    (data: NotificationDto[]) => data.map(mapNotificationDto),
  );
};

export const getUnreadNotificationCount = async (): Promise<number> => {
  const response = apiGet<number>("/Notification/unread-count");
  return (await handleResponseDirect(response)) ?? 0;
};

export const markNotificationRead = (id: number): Promise<boolean> => {
  const response = apiPut<boolean>(`/Notification/${id}/read`);
  return handleResponseDirect(response).then((r) => r ?? false);
};

export const markAllNotificationsRead = (): Promise<boolean> => {
  const response = apiPut<boolean>("/Notification/read-all");
  return handleResponseDirect(response).then((r) => r ?? false);
};

// ==================== TanStack Query Hooks ====================

export function useNotificationsQuery() {
  return useQuery({
    queryKey: ["notifications"],
    queryFn: getNotifications,
  });
}

export function useUnreadNotificationCountQuery() {
  return useQuery({
    queryKey: ["notifications", "unread-count"],
    queryFn: getUnreadNotificationCount,
    refetchInterval: 30_000,
  });
}

export function useMarkNotificationReadMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: markNotificationRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
    },
  });
}

export function useMarkAllNotificationsReadMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
    },
  });
}