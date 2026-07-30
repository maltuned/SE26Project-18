import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ResponseDto } from "../dtos";
import { ResponseData, ResponseStatus } from "../data-patterns";
import { apiGet, apiPost, handleResponse, handleArrayResponse, handlePostResponse } from "../fetch";
import { mapResponseDto } from "../mappers";
import { getRecruitments } from "./recruitment";

export const getResponses = (recruitmentId?: number): Promise<ResponseData[]> => {
  if (recruitmentId === undefined) {
    return getRecruitments().then(async (recruitments) => {
      const allResponses: ResponseData[] = [];
      for (const r of recruitments) {
        const responses = await getResponses(r.id);
        allResponses.push(...responses);
      }
      return allResponses;
    });
  }
  const response = apiGet<ResponseDto[]>("/Responses/by-recruitment", {
    recruitmentId,
  });
  return handleArrayResponse<ResponseData>(response, (data: ResponseDto[]) =>
    data.map(mapResponseDto),
  );
};

export const getResponsesByUserId = (userId: number): Promise<ResponseData[]> => {
  const response = apiGet<ResponseDto[]>("/Responses/by-user", { userId });
  return handleArrayResponse<ResponseData>(response, (data: ResponseDto[]) =>
    data.map(mapResponseDto),
  );
};

export const createResponse = (data: {
  recruitmentId: number;
  responserId: number;
}): Promise<ResponseData> => {
  const response = apiPost<ResponseDto>("/Responses", {
    recruitment_id: data.recruitmentId,
    responser_id: data.responserId,
  });
  return handlePostResponse(response, mapResponseDto);
};

export const deleteResponse = (id: number, reason: string): Promise<boolean> => {
  const response = apiPost<boolean>("/Responses/delete", { id, reason });
  return handlePostResponse(response, (data: boolean) => data);
};

export const updateResponseStatus = (
  id: number,
  responseStatus: ResponseStatus,
): Promise<ResponseData | null> => {
  const response = apiPost<ResponseDto>("/Responses/status", {
    id,
    response_status: responseStatus,
  });
  return handleResponse<ResponseData>(response, mapResponseDto);
};

// ==================== TanStack Query Hooks ====================

export function useResponsesQuery(recruitmentId?: number) {
  return useQuery({
    queryKey: ["responses", recruitmentId],
    queryFn: () => getResponses(recruitmentId),
    enabled: recruitmentId !== undefined,
  });
}

export function useResponsesByUserQuery(userId: number) {
  return useQuery({
    queryKey: ["responses", "by-user", userId],
    queryFn: () => getResponsesByUserId(userId),
    enabled: userId > 0,
  });
}

export function useCreateResponseMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createResponse,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ["responses", data.recruitmentId] });
      queryClient.invalidateQueries({ queryKey: ["recruitments"] });
    },
  });
}

export function useDeleteResponseMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: number; reason: string }) => deleteResponse(id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["responses"] });
    },
  });
}

export function useUpdateResponseStatusMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: number; status: ResponseStatus }) =>
      updateResponseStatus(id, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["responses"] });
    },
  });
}