import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CreateReviewDto, ReviewDto } from "../dtos";
import { ReviewData } from "../data-patterns";
import { apiGet, apiPost, handleResponseDirect, handleArrayResponse } from "../fetch";
import { mapReviewDto } from "../mappers";

export const createReview = (dto: CreateReviewDto): Promise<boolean> => {
  const response = apiPost<boolean>("/Review", dto);
  return handleResponseDirect(response).then((r) => r ?? false);
};

export const getReviewsByUser = (userId: number): Promise<ReviewData[]> => {
  const response = apiGet<ReviewDto[]>(`/Review/user/${userId}`);
  return handleArrayResponse<ReviewData>(response, (data: ReviewDto[]) =>
    data.map(mapReviewDto),
  );
};

export const hasReviewed = (userId: number): Promise<boolean> => {
  const response = apiGet<boolean>(`/Review/check/${userId}`);
  return handleResponseDirect(response).then((r) => r ?? false);
};

// ==================== TanStack Query Hooks ====================

export function useReviewsByUserQuery(userId: number) {
  return useQuery({
    queryKey: ["reviews", userId],
    queryFn: () => getReviewsByUser(userId),
    enabled: userId > 0,
  });
}

export function useHasReviewedQuery(userId: number) {
  return useQuery({
    queryKey: ["reviews", "check", userId],
    queryFn: () => hasReviewed(userId),
    enabled: userId > 0,
  });
}

export function useCreateReviewMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createReview,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reviews"] });
    },
  });
}