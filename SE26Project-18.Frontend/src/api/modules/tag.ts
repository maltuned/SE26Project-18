import { useQuery } from "@tanstack/react-query";
import { GameTagDto, RecruitmentTagDto } from "../dtos";
import { GameTag, RecruitmentTag } from "../data-patterns";
import { apiGet, handleArrayResponse } from "../fetch";
import { initTagCaches } from "../mappers";

export { initTagCaches };

export const getGameTags = (): Promise<GameTag[]> => {
  const response = apiGet<GameTagDto[]>("/GameTags");
  return handleArrayResponse<GameTag>(response, (data: GameTagDto[]) => data);
};

export const getRecruitmentTags = (): Promise<RecruitmentTag[]> => {
  const response = apiGet<RecruitmentTagDto[]>("/RecruitmentTags");
  return handleArrayResponse<RecruitmentTag>(
    response,
    (data: RecruitmentTagDto[]) => data,
  );
};

// ==================== TanStack Query Hooks ====================

export function useGameTagsQuery() {
  return useQuery({
    queryKey: ["tags", "game"],
    queryFn: getGameTags,
    staleTime: 30 * 60 * 1000,
  });
}

export function useRecruitmentTagsQuery() {
  return useQuery({
    queryKey: ["tags", "recruitment"],
    queryFn: getRecruitmentTags,
    staleTime: 30 * 60 * 1000,
  });
}