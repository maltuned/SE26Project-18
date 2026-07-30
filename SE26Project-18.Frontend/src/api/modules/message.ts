import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { MessageDto } from "../dtos";
import { MessageData } from "../data-patterns";
import { apiGet, apiPost, handleArrayResponse, handlePostResponse } from "../fetch";
import { mapMessageDto } from "../mappers";

export const getMessagesByChatId = (chatId: number): Promise<MessageData[]> => {
  const response = apiGet<MessageDto[]>("/Messages/by-chat", { chatId });
  return handleArrayResponse<MessageData>(response, (data: MessageDto[]) =>
    data.map(mapMessageDto),
  );
};

export const sendMessage = (data: {
  chatId: number;
  senderId: number;
  receiverId: number;
  content: string;
}): Promise<MessageData> => {
  const response = apiPost<MessageDto>("/Messages", {
    chat_id: data.chatId,
    sender_id: data.senderId,
    receiver_id: data.receiverId,
    content: data.content,
  });
  return handlePostResponse(response, mapMessageDto);
};

export const markMessagesRead = async (
  chatId: number,
  userId: number,
): Promise<boolean> => {
  const response = apiPost<boolean>("/Messages/mark-read", {
    chat_id: chatId,
    user_id: userId,
  });
  return handlePostResponse(response, (d: boolean) => d);
};

// ==================== TanStack Query Hooks ====================

export function useMessagesQuery(chatId: number) {
  return useQuery({
    queryKey: ["messages", chatId],
    queryFn: () => getMessagesByChatId(chatId),
    enabled: chatId > 0,
  });
}

export function useSendMessageMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: sendMessage,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ["messages", data.chatId] });
      queryClient.invalidateQueries({ queryKey: ["chats"] });
    },
  });
}

export function useMarkMessagesReadMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ chatId, userId }: { chatId: number; userId: number }) =>
      markMessagesRead(chatId, userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["chats"] });
    },
  });
}