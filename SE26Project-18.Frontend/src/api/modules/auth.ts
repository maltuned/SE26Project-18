import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ApiResponse, TokenResponse, UserDto } from "../dtos";
import { UserInfo } from "../data-patterns";
import { apiPost, apiPostNoAuth, buildUrlExport } from "../fetch";
import { mapUserDto } from "../mappers";
import { tokenStorage } from "../token-storage";

export const getMe = async (accessToken?: string): Promise<UserInfo | null> => {
  const token = accessToken ?? (await tokenStorage.getAccessToken());
  if (!token) return null;
  const res = await fetch(buildUrlExport("/Auth/me"), {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`获取用户信息失败: HTTP ${res.status} ${text}`);
  }
  const data: ApiResponse<UserDto> = await res.json();
  if (data.status >= 200 && data.status < 300 && data.data) {
    return mapUserDto(data.data);
  }
  throw new Error(`获取用户信息失败: API ${data.status} ${data.message}`);
};

export const login = async (
  username: string,
  password: string,
): Promise<{ token: TokenResponse; user: UserInfo }> => {
  const res = await apiPostNoAuth<TokenResponse>("/Auth/login", { username, password });
  if (res.status !== 200 || !res.data) {
    throw new Error(res.message || "登录失败");
  }
  const token = res.data;
  await tokenStorage.setTokens(
    token.access_token,
    token.refresh_token,
    token.access_token_expires_at,
    token.refresh_token_expires_at,
  );
  const user = await getMe(token.access_token);
  if (!user) throw new Error("登录失败：无法获取用户信息");
  return { token, user };
};

export const register = async (
  username: string,
  password: string,
): Promise<{ token: TokenResponse; user: UserInfo }> => {
  const res = await apiPostNoAuth<TokenResponse>("/Auth/register", { username, password });
  if (res.status !== 200 || !res.data) {
    throw new Error(res.message || "注册失败");
  }
  const token = res.data;
  await tokenStorage.setTokens(
    token.access_token,
    token.refresh_token,
    token.access_token_expires_at,
    token.refresh_token_expires_at,
  );
  const user = await getMe(token.access_token);
  if (!user) throw new Error("注册失败：无法获取用户信息");
  return { token, user };
};

export const changePassword = async (
  oldPassword: string,
  newPassword: string,
): Promise<boolean> => {
  const res = await apiPost<boolean>("/Auth/change-password", {
    old_password: oldPassword,
    new_password: newPassword,
  });
  if (res.status !== 200 || !res.data) {
    throw new Error(res.message || "修改密码失败");
  }
  return true;
};

// ==================== TanStack Query Hooks ====================

export function useMeQuery() {
  return useQuery({
    queryKey: ["auth", "me"],
    queryFn: () => getMe(),
    staleTime: 10 * 60 * 1000,
  });
}

export function useLoginMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ username, password }: { username: string; password: string }) =>
      login(username, password),
    onSuccess: (data) => {
      queryClient.setQueryData(["auth", "me"], data.user);
    },
  });
}

export function useRegisterMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ username, password }: { username: string; password: string }) =>
      register(username, password),
    onSuccess: (data) => {
      queryClient.setQueryData(["auth", "me"], data.user);
    },
  });
}

export function useChangePasswordMutation() {
  return useMutation({
    mutationFn: ({ oldPassword, newPassword }: { oldPassword: string; newPassword: string }) =>
      changePassword(oldPassword, newPassword),
  });
}