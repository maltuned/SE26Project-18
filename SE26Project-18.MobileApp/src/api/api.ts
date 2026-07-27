// Re-export frontend data patterns (部分类型在 data-patterns.ts 中定义)
export type {
  ChatBrief, ChatData, GameInfo, GameTag, MessageData, RecruitmentData,
  RecruitmentTag, ResponseData, UserInfo
} from "./data-patterns";

// ChatStatus / ResponseStatus 在 data-patterns 中，避免与 api.ts 中的 ChatStatus 冲突
export type { ChatStatus as LegacyChatStatus, ResponseStatus } from "./data-patterns";

// ==================== Config ====================

const API_BASE = "http://localhost:5193";

// ==================== JWT Token 管理 ====================

let _accessToken: string | null = null;

export function setAuthToken(token: string | null) {
  _accessToken = token;
}

// ==================== Fetch Helpers ====================

const buildUrl = (path: string, params?: Record<string, any>) => {
  const url = new URL(path.startsWith("/") ? path : `/${path}`, API_BASE);
  if (params) {
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== "") {
        if (Array.isArray(value)) {
          value.forEach((v) => url.searchParams.append(key, String(v)));
        } else {
          url.searchParams.append(key, String(value));
        }
      }
    });
  }
  return url.toString();
};

const authHeaders = () => {
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  if (_accessToken) headers["Authorization"] = `Bearer ${_accessToken}`;
  return headers;
};

const apiGet = async <T>(path: string, params?: Record<string, any>): Promise<T> => {
  const res = await fetch(buildUrl(path, params), {
    method: "GET",
    headers: authHeaders(),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({ detail: res.statusText }));
    throw new Error(err.detail || err.title || `HTTP ${res.status}`);
  }
  // 204 No Content
  if (res.status === 204) return undefined as T;
  return res.json();
};

const apiPost = async <T>(path: string, body?: any): Promise<T> => {
  const res = await fetch(buildUrl(path), {
    method: "POST",
    headers: authHeaders(),
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({ detail: res.statusText }));
    throw new Error(err.detail || err.title || `HTTP ${res.status}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
};

const apiPatch = async <T>(path: string, body?: any): Promise<T> => {
  const res = await fetch(buildUrl(path), {
    method: "PATCH",
    headers: authHeaders(),
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({ detail: res.statusText }));
    throw new Error(err.detail || err.title || `HTTP ${res.status}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
};

// ==================== Auth API ====================

export type TokenResponse = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
};

export type UserResponse = {
  id: number;
  username: string;
  nickname: string;
  signature: string;
  gender: number;
  status: number;
  tags: { id: number; name: string }[];
};

// 登录 → 后端返回 TokenResponse（不含 userId，从 token 中解析或调 /users/me）
export const login = async (
  username: string,
  password: string,
): Promise<TokenResponse> => {
  return apiPost<TokenResponse>("/api/v1/auth/login", { username, password });
};

export const register = async (
  username: string,
  password: string,
): Promise<TokenResponse> => {
  return apiPost<TokenResponse>("/api/v1/auth/register", { username, password });
};

export const refreshToken = async (
  refreshToken: string,
): Promise<string> => {
  const res = await apiPost<TokenResponse>("/api/v1/auth/refresh", { refreshToken });
  return res.accessToken;
};

export const logoutApi = async (): Promise<void> => {
  await apiPost("/api/v1/auth/logout");
};

// ==================== User API ====================

export const getUserMe = async (): Promise<UserResponse> => {
  return apiGet<UserResponse>("/api/v1/users/me");
};

export const getUserById = async (id: number): Promise<UserResponse> => {
  return apiGet<UserResponse>(`/api/v1/users/${id}`);
};

export const updateUserMe = async (data: {
  nickname?: string;
  signature?: string;
  gender?: number;
  tagIds?: number[];
}): Promise<UserResponse> => {
  return apiPatch<UserResponse>("/api/v1/users/me", data);
};

// ==================== Game API ====================

export type GameResponse = {
  id: number;
  name: string;
  description: string;
  tags: { id: number; name: string }[];
};

export const searchGames = async (query?: string): Promise<GameResponse[]> => {
  return apiGet<GameResponse[]>("/api/v1/games", query ? { query } : {});
};

export const getGameById = async (id: number): Promise<GameResponse> => {
  return apiGet<GameResponse>(`/api/v1/games/${id}`);
};

// ==================== Response API ====================

export type ResponseType = "Pending" | "Accepted" | "Rejected";

export type ResponseResponse = {
  id: number;
  recruitmentId: number;
  responderId: number;
  type: ResponseType;
};

// 回应招募
export const createResponse = async (
  recruitmentId: number,
): Promise<ResponseResponse> => {
  return apiPost<ResponseResponse>(
    `/api/v1/recruitments/${recruitmentId}/responses`
  );
};

export const getResponseById = async (id: number): Promise<ResponseResponse> => {
  return apiGet<ResponseResponse>(`/api/v1/responses/${id}`);
};

export const acceptResponse = async (id: number): Promise<ResponseResponse> => {
  return apiPost<ResponseResponse>(`/api/v1/responses/${id}/accept`);
};

export const rejectResponse = async (id: number): Promise<ResponseResponse> => {
  return apiPost<ResponseResponse>(`/api/v1/responses/${id}/reject`);
};

// ==================== Chat API ====================

export type ChatStatus = "Restricted" | "Free";

export type MessageResponse = {
  senderId: number;
  content: string;
  sentAt: string;
};

export type ChatResponse = {
  id: number;
  recruitmentId: number;
  user1Id: number;
  user2Id: number;
  status: ChatStatus;
  newMsgsCntForUser1: number;
  newMsgsCntForUser2: number;
  lastMessage: MessageResponse | null;
};

export const getMyChats = async (): Promise<ChatResponse[]> => {
  return apiGet<ChatResponse[]>("/api/v1/chats/me");
};

export const getChatById = async (id: number): Promise<ChatResponse> => {
  return apiGet<ChatResponse>(`/api/v1/chats/${id}`);
};

export const getChatByUserId = async (userId: number): Promise<ChatResponse> => {
  return apiGet<ChatResponse>(`/api/v1/chats/by-user/${userId}`);
};

// TODO: 后端尚未实现 — 以下 API 暂不可用

// Recruitment CRUD (后端待实现)
export const getRecruitments = async (): Promise<any[]> => {
  console.warn("Recruitment CRUD not yet implemented on backend");
  return [];
};

export const getRecruitmentById = async (_id: number): Promise<any | null> => {
  console.warn("Recruitment CRUD not yet implemented on backend");
  return null;
};

// Tags (后端待实现)
export const getGameTags = async (): Promise<{ id: number; name: string }[]> => {
  console.warn("Tags not yet implemented on backend");
  return [];
};

export const getRecruitmentTags = async (): Promise<{ id: number; name: string }[]> => {
  console.warn("Tags not yet implemented on backend");
  return [];
};

// Messages (后端待实现)
export const getMessagesByChatId = async (_chatId: number): Promise<any[]> => {
  console.warn("Messages not yet implemented on backend");
  return [];
};

export const sendMessage = async (_data: {
  chatId: number;
  content: string;
}): Promise<any> => {
  console.warn("Messages not yet implemented on backend");
  return null;
};

// Tag cache init (stub — tags not yet on backend)
export const initTagCaches = async () => {};

export type GameBrief = { id: number; name: string; icon: string };

// ==================== Backward Compat Wrappers ====================
// 旧函数名 → 新 API 映射，逐步迁移页面后删除

export const getChats = (userId: number): Promise<ChatResponse[]> => getMyChats();

export const getUsers = async (): Promise<UserResponse[]> => {
  console.warn("getUsers not implemented");
  return [];
};

export const updateUser = async (id: number, data: Record<string, any>) => {
  return updateUserMe(data);
};

export const getGames = async (query?: string): Promise<GameBrief[]> => {
  const games = await searchGames(query);
  return games.map(g => ({ id: g.id, name: g.name, icon: "" }));
};

export const getRecruitmentsByPublisherId = async (_publisherId: number | null): Promise<any[]> => {
  console.warn("Recruitment CRUD not yet on backend");
  return [];
};

export const saveRecruitment = async (_data: any): Promise<any> => {
  console.warn("Recruitment CRUD not yet on backend");
  return null;
};

export const createRecruitment = async (_data: any): Promise<any> => {
  console.warn("Recruitment CRUD not yet on backend");
  return null;
};

export const updateRecruitment = async (_id: number, _data: any): Promise<any> => {
  console.warn("Recruitment CRUD not yet on backend");
  return null;
};

export const deleteRecruitment = async (_id: number): Promise<boolean> => {
  console.warn("Recruitment CRUD not yet on backend");
  return false;
};

export const getResponses = async (_recruitmentId?: number): Promise<any[]> => {
  console.warn("Response listing not yet on backend");
  return [];
};

export const getResponsesByUserId = async (_userId: number): Promise<any[]> => {
  console.warn("Response listing not yet on backend");
  return [];
};

export const deleteResponse = async (_id: number, _reason: string): Promise<boolean> => {
  console.warn("Response delete not implemented");
  return false;
};

export const updateResponseStatus = async (_id: number, _status: any): Promise<any> => {
  console.warn("Response status update: use acceptResponse/rejectResponse instead");
  return null;
};

export const getChatByIdLegacy = async (chatId: number, userId?: number): Promise<ChatResponse | null> => {
  return getChatById(chatId);
};

export const getChatByUsers = async (_userIds: number[]): Promise<any | null> => {
  console.warn("getChatByUsers not implemented");
  return null;
};

export const getChatsByRecruitmentId = async (_recruitmentId: number): Promise<any[]> => {
  console.warn("Chats by recruitment not yet on backend");
  return [];
};

export const createChat = async (_data: any): Promise<any> => {
  console.warn("Chat create: backend handles this automatically via acceptResponse");
  return null;
};

export const closeChat = async (_id: number): Promise<boolean> => {
  console.warn("Chat close not implemented");
  return false;
};
