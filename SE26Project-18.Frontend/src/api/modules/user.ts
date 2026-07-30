import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { UserDto, UserSettingsDto } from "../dtos";
import { UserInfo, UserSettings } from "../data-patterns";
import { apiGet, apiPost, apiPut, handleResponse, handleArrayResponse } from "../fetch";
import { mapUserDto } from "../mappers";

export const getUserById = (id: number): Promise<UserInfo | null> => {
  const response = apiGet<UserDto>("/Users/by-id", { id });
  return handleResponse<UserInfo>(response, mapUserDto);
};

export const getUserProfile = async (
  id: number,
): Promise<{ user: UserInfo | null; isPrivate: boolean }> => {
  const res = await apiGet<UserDto>("/Users/profile", { id });
  if (res.status === 403) {
    return { user: res.data ? mapUserDto(res.data) : null, isPrivate: true };
  }
  if (res.status >= 200 && res.status < 300 && res.data) {
    return { user: mapUserDto(res.data), isPrivate: false };
  }
  return { user: null, isPrivate: false };
};

export const getUsers = (): Promise<UserInfo[]> => {
  const response = apiGet<UserDto[]>("/Users");
  return handleArrayResponse<UserInfo>(response, (data: UserDto[]) =>
    data.map(mapUserDto),
  );
};

export const updateUser = (id: number, data: Record<string, any>): Promise<UserInfo | null> => {
  const response = apiPost<UserDto>("/Users/update", { id, data });
  return handleResponse<UserInfo>(response, mapUserDto);
};

export const updateUserSettings = async (settings: {
  pushEnabled: boolean;
  profileVisible: boolean;
  darkMode: boolean;
}): Promise<UserSettings> => {
  const res = await apiPut<UserSettingsDto>("/Users/settings", {
    push_enabled: settings.pushEnabled,
    profile_visible: settings.profileVisible,
    dark_mode: settings.darkMode,
  });
  if (res.status !== 200 || !res.data) {
    throw new Error(res.message || "更新设置失败");
  }
  return {
    pushEnabled: res.data.push_enabled,
    profileVisible: res.data.profile_visible,
    darkMode: res.data.dark_mode,
  };
};

// ==================== TanStack Query Hooks ====================

export function useUserQuery(id: number) {
  return useQuery({
    queryKey: ["users", id],
    queryFn: () => getUserById(id),
    enabled: id > 0,
  });
}

export function useUserProfileQuery(id: number) {
  return useQuery({
    queryKey: ["users", "profile", id],
    queryFn: () => getUserProfile(id),
    enabled: id > 0,
  });
}

export function useUsersQuery() {
  return useQuery({
    queryKey: ["users"],
    queryFn: getUsers,
  });
}

export function useUpdateUserMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: Record<string, any> }) =>
      updateUser(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ["users", variables.id] });
      queryClient.invalidateQueries({ queryKey: ["users"] });
    },
  });
}

export function useUpdateUserSettingsMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: updateUserSettings,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["auth", "me"] });
    },
  });
}