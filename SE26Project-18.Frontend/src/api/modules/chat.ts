import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChatBriefDto, ChatDto } from "../dtos";
import { ChatBrief, ChatData } from "../data-patterns";
import { apiGet, apiPost, handleResponse, handleArrayResponse, handlePostResponse } from "../fetch";
import { mapChatBriefDto, mapChatDto } from "../mappers";

export const getChats = (userId: number): Promise<ChatBrief[]> => {
  const response = apiGet<ChatBriefDto[]>("/Chats/by-user", { userId });
  return handleArrayResponse<ChatBrief>(response, (data: ChatBriefDto[]) =>
    data.map(mapChatBriefDto),
  );
};

export const getChatById = (chatId: number, userId?: number): Promise<ChatData | null> => {
  const response = apiGet<ChatDto>("/Chats/by-id", { chatId, userId });
  return handleResponse<ChatData>(response, mapChatDto);
};

export const getChatByUsers = (userIds: number[]): Promise<ChatData | null> => {
  const response = apiGet<ChatDto>("/Chats/by-users", { userIds });
  return handleResponse<ChatData>(response, mapChatDto);
};

export const getChatsByRecruitmentId = (recruitmentId: number): Promise<ChatData[]> => {
  const response = apiGet<ChatDto[]>("/Chats/by-recruitment", { recruitmentId });
  return handleArrayResponse<ChatData>(response, (data: ChatDto[]) =>
    data.map(mapChatDto),
  );
};

export const createChat = (data: {
  recruitmentId: number;
  user1Id: number;
  user2Id: number;
}): Promise<ChatData> => {
  const response = apiPost<ChatDto>("/Chats/create", {
    recruitment_id: data.recruitmentId,
    user1_id: data.user1Id,
    user2_id: data.user2Id,
  });
  return handlePostResponse(response, mapChatDto);
};

export const closeChat = (id: number): Promise<boolean> => {
  const response = apiPost<boolean>("/Chats/close", { id });
  return handlePostResponse(response, (data: boolean) => data);
};

// ==================== TanStack Query Hooks ====================

export function useChatsQuery(userId: number) {
  return useQuery({
    queryKey: ["chats", userId],
    queryFn: () => getChats(userId),
    enabled: userId > 0,
  });
}

export function useChatQuery(chatId: number, userId?: number) {
  return useQuery({
    queryKey: ["chats", "detail", chatId],
    queryFn: () => getChatById(chatId, userId),
    enabled: chatId > 0,
  });
}

export function useChatsByRecruitmentQuery(recruitmentId: number) {
  return useQuery({
    queryKey: ["chats", "by-recruitment", recruitmentId],
    queryFn: () => getChatsByRecruitmentId(recruitmentId),
    enabled: recruitmentId > 0,
  });
}

export function useCreateChatMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createChat,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ["chats"] });
      queryClient.setQueryData(["chats", "detail", data.id], data);
    },
  });
}

export function useCloseChatMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: closeChat,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["chats"] });
    },
  });
}