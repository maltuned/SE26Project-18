import { useQuery } from "@tanstack/react-query";
import { GameDto } from "../dtos";
import { GameBrief, GameInfo } from "../data-patterns";
import { apiGet, handleResponse, handleArrayResponse } from "../fetch";
import { mapGameBriefDto, mapGameDto } from "../mappers";

export const getGames = (query: string = ""): Promise<GameBrief[]> => {
  const response = apiGet<GameDto[]>("/Games", { query });
  return handleArrayResponse<GameBrief>(response, (data: GameDto[]) =>
    data.map(mapGameBriefDto),
  );
};

export const getGameById = (id: number): Promise<GameInfo | null> => {
  const response = apiGet<GameDto>("/Games/by-id", { id });
  return handleResponse<GameInfo>(response, (dto: GameDto) => mapGameDto(dto));
};

// ==================== TanStack Query Hooks ====================

export function useGamesQuery(query: string = "") {
  return useQuery({
    queryKey: ["games", query],
    queryFn: () => getGames(query),
    staleTime: 10 * 60 * 1000,
  });
}

export function useGameQuery(id: number) {
  return useQuery({
    queryKey: ["games", id],
    queryFn: () => getGameById(id),
    staleTime: 10 * 60 * 1000,
    enabled: id > 0,
  });
}