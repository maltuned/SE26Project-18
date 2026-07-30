import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { RecruitmentDetailDto } from "../dtos";
import { RecruitmentData } from "../data-patterns";
import { apiGet, apiPost, handleResponse, handleArrayResponse, handlePostResponse } from "../fetch";
import { mapRecruitmentDetailDto } from "../mappers";

export const getRecruitments = (
  gameName: string = "",
  gameTags: number[] = [],
  recruitmentTags: number[] = [],
): Promise<RecruitmentData[]> => {
  const response = apiGet<RecruitmentDetailDto[]>("/Recruitments", {
    gameName,
    gameTags,
    recruitmentTags,
  });
  return handleArrayResponse<RecruitmentData>(
    response,
    (data: RecruitmentDetailDto[]) => data.map(mapRecruitmentDetailDto),
  );
};

export const getRecruitmentsByGame = (gameId: number): Promise<RecruitmentData[]> => {
  const response = apiGet<RecruitmentDetailDto[]>("/Recruitments/by-game", { gameId });
  return handleArrayResponse<RecruitmentData>(
    response,
    (data: RecruitmentDetailDto[]) => data.map(mapRecruitmentDetailDto),
  );
};

export const getRecruitmentById = (id: number): Promise<RecruitmentData | null> => {
  const response = apiGet<RecruitmentDetailDto>("/Recruitments/by-id", { id });
  return handleResponse<RecruitmentData>(response, mapRecruitmentDetailDto);
};

export const getRecruitmentByChatId = async (
  chatId: number,
): Promise<RecruitmentData | null> => {
  const response = apiGet<RecruitmentDetailDto>("/Recruitments/by-chat", { chatId });
  const result = await handleResponse<RecruitmentData>(response, mapRecruitmentDetailDto);
  return result && result.id !== 0 ? result : null;
};

export const getRecruitmentsByPublisherId = (
  publisherId: number | null,
): Promise<RecruitmentData[]> => {
  if (publisherId === null) return getRecruitments();
  const response = apiGet<RecruitmentDetailDto[]>("/Recruitments/by-publisher", {
    publisherId,
  });
  return handleArrayResponse<RecruitmentData>(
    response,
    (data: RecruitmentDetailDto[]) => data.map(mapRecruitmentDetailDto),
  );
};

export const saveRecruitment = (data: {
  id: number;
  publisherId: number;
  gameId: number;
  title: string;
  description: string;
  status: string;
  expiredAt: string;
  maxParticipants: number;
  currentParticipants: number;
  tagsId: number[];
}): Promise<RecruitmentData> => {
  const isNew = data.id <= 0;
  if (isNew) {
    const payload = {
      publisher_id: data.publisherId,
      game_id: data.gameId,
      title: data.title,
      description: data.description,
      status: data.status,
      expired_at: data.expiredAt,
      max_participants: data.maxParticipants,
      current_participants: data.currentParticipants,
      tags_id: data.tagsId,
    };
    const response = apiPost<RecruitmentDetailDto>("/Recruitments", payload);
    return handlePostResponse(response, mapRecruitmentDetailDto);
  }
  const payload = {
    id: data.id,
    data: {
      title: data.title,
      description: data.description,
      status: data.status,
      expired_at: data.expiredAt,
      max_participants: data.maxParticipants,
      current_participants: data.currentParticipants,
      tags_id: data.tagsId,
    },
  };
  const response = apiPost<RecruitmentDetailDto>("/Recruitments/update", payload);
  return handlePostResponse(response, mapRecruitmentDetailDto);
};

export const createRecruitment = (data: {
  publisherId: number;
  gameId: number;
  title: string;
  description: string;
  status: string;
  expiredAt: string;
  maxParticipants: number;
  currentParticipants: number;
  tagsId: number[];
}): Promise<RecruitmentData> => {
  const payload = {
    publisher_id: data.publisherId,
    game_id: data.gameId,
    title: data.title,
    description: data.description,
    status: data.status,
    expired_at: data.expiredAt,
    max_participants: data.maxParticipants,
    current_participants: data.currentParticipants,
    tags_id: data.tagsId,
  };
  const response = apiPost<RecruitmentDetailDto>("/Recruitments", payload);
  return handlePostResponse(response, mapRecruitmentDetailDto);
};

export const updateRecruitment = (
  id: number,
  data: Partial<{
    title: string;
    description: string;
    status: string;
    expired_at: string;
    max_participants: number;
    current_participants: number;
  }>,
): Promise<RecruitmentData | null> => {
  const response = apiPost<RecruitmentDetailDto>("/Recruitments/update", { id, data });
  return handleResponse<RecruitmentData>(response, mapRecruitmentDetailDto);
};

export const deleteRecruitment = (id: number): Promise<boolean> => {
  const response = apiPost<boolean>("/Recruitments/delete", { id });
  return handlePostResponse(response, (data: boolean) => data);
};

// ==================== TanStack Query Hooks ====================

export function useRecruitmentsQuery(
  gameName: string = "",
  gameTags: number[] = [],
  recruitmentTags: number[] = [],
) {
  return useQuery({
    queryKey: ["recruitments", { gameName, gameTags, recruitmentTags }],
    queryFn: () => getRecruitments(gameName, gameTags, recruitmentTags),
  });
}

export function useRecruitmentsByGameQuery(gameId: number) {
  return useQuery({
    queryKey: ["recruitments", "by-game", gameId],
    queryFn: () => getRecruitmentsByGame(gameId),
    enabled: gameId > 0,
  });
}

export function useRecruitmentQuery(id: number) {
  return useQuery({
    queryKey: ["recruitments", id],
    queryFn: () => getRecruitmentById(id),
    enabled: id > 0,
  });
}

export function useRecruitmentByChatQuery(chatId: number) {
  return useQuery({
    queryKey: ["recruitments", "by-chat", chatId],
    queryFn: () => getRecruitmentByChatId(chatId),
    enabled: chatId > 0,
  });
}

export function useRecruitmentsByPublisherQuery(publisherId: number | null) {
  return useQuery({
    queryKey: ["recruitments", "by-publisher", publisherId],
    queryFn: () => getRecruitmentsByPublisherId(publisherId),
    enabled: publisherId !== null,
  });
}

export function useSaveRecruitmentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: saveRecruitment,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ["recruitments"] });
      queryClient.setQueryData(["recruitments", data.id], data);
    },
  });
}

export function useCreateRecruitmentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createRecruitment,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ["recruitments"] });
      queryClient.setQueryData(["recruitments", data.id], data);
    },
  });
}

export function useUpdateRecruitmentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: any }) => updateRecruitment(id, data),
    onSuccess: (data, variables) => {
      queryClient.invalidateQueries({ queryKey: ["recruitments"] });
      if (data) {
        queryClient.setQueryData(["recruitments", data.id], data);
      }
    },
  });
}

export function useDeleteRecruitmentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: deleteRecruitment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recruitments"] });
    },
  });
}