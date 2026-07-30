import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";
import type {
  ChatResponse,
  CursorPagedResponse,
  GameResponse,
  MessageResponse,
  PagedResponse,
  ProblemDetails,
  RecruitmentResponse,
  ResponseResponse,
  TagResponse,
  TokenResponse,
  UserResponse,
  WebSocketTicketResponse,
} from "./dtos";
import {
  ChatStatusDto,
  GenderDto,
  RecruitmentStatusDto,
  ResponseTypeDto,
  UserStatusDto,
} from "./dtos";
import type {
  ChatBrief,
  ChatData,
  GameBrief,
  GameInfo,
  GameTag,
  MessageData,
  RecruitmentData,
  RecruitmentTag,
  ResponseData,
  UserInfo,
} from "./data-patterns";

export type {
  ChatBrief,
  ChatData,
  ChatStatus,
  GameBrief,
  GameInfo,
  GameTag,
  MessageData,
  RecruitmentData,
  RecruitmentStatus,
  RecruitmentTag,
  ResponseData,
  ResponseStatus,
  UserInfo,
} from "./data-patterns";
export type { CursorPagedResponse, GameResponse, PagedResponse, ProblemDetails, TagResponse, TokenResponse, UserResponse } from "./dtos";
export { RecruitmentStatusDto, ResponseTypeDto, UserStatusDto } from "./dtos";

const developmentHost = Platform.OS === "android" ? "10.0.2.2" : "localhost";
const configuredBase = process.env.EXPO_PUBLIC_API_URL?.trim();
const rawBase = configuredBase || `http://${developmentHost}:5193/api/v1`;
const configuredUrl = new URL(rawBase);
if (configuredUrl.pathname === "/") configuredUrl.pathname = "/api/v1";
export const API_BASE_URL = configuredUrl.toString().replace(/\/+$/, "");
export const API_ORIGIN = new URL(API_BASE_URL).origin;
export const WS_BASE_URL = API_BASE_URL.replace(/^http:/, "ws:").replace(/^https:/, "wss:");

const ACCESS_TOKEN_KEY = "auth.accessToken";
const REFRESH_TOKEN_KEY = "auth.refreshToken";
let accessToken: string | null = null;
let refreshToken: string | null = null;
let refreshPromise: Promise<boolean> | null = null;
let mediaCacheVersion = 0;

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem?: ProblemDetails,
  ) {
    super(problem?.detail || problem?.title || `Request failed with status ${status}`);
    this.name = "ApiError";
  }
}

const storage = {
  async get(key: string) {
    if (Platform.OS === "web") return globalThis.sessionStorage?.getItem(key) ?? null;
    return SecureStore.getItemAsync(key);
  },
  async set(key: string, value: string) {
    if (Platform.OS === "web") globalThis.sessionStorage?.setItem(key, value);
    else await SecureStore.setItemAsync(key, value);
  },
  async remove(key: string) {
    if (Platform.OS === "web") globalThis.sessionStorage?.removeItem(key);
    else await SecureStore.deleteItemAsync(key);
  },
};

async function setTokens(tokens: TokenResponse | null) {
  accessToken = tokens?.accessToken ?? null;
  refreshToken = tokens?.refreshToken ?? null;
  if (tokens) {
    await Promise.all([
      storage.set(ACCESS_TOKEN_KEY, tokens.accessToken),
      storage.set(REFRESH_TOKEN_KEY, tokens.refreshToken),
    ]);
  } else {
    await Promise.all([storage.remove(ACCESS_TOKEN_KEY), storage.remove(REFRESH_TOKEN_KEY)]);
  }
}

export async function restoreTokens() {
  [accessToken, refreshToken] = await Promise.all([
    storage.get(ACCESS_TOKEN_KEY),
    storage.get(REFRESH_TOKEN_KEY),
  ]);
  return Boolean(accessToken && refreshToken);
}

function buildUrl(path: string, params?: Record<string, string | number | boolean | number[] | undefined>) {
  const url = /^https?:\/\//.test(path)
    ? new URL(path)
    : new URL(`${API_BASE_URL}/${path.replace(/^\//, "")}`);
  Object.entries(params ?? {}).forEach(([key, value]) => {
    if (value === undefined || value === "") return;
    if (Array.isArray(value)) value.forEach((item) => url.searchParams.append(key, String(item)));
    else url.searchParams.set(key, String(value));
  });
  return url.toString();
}

async function parseError(response: Response) {
  let problem: ProblemDetails | undefined;
  try {
    problem = await response.json();
  } catch {
    problem = undefined;
  }
  return new ApiError(response.status, problem);
}

async function refreshAccessToken() {
  if (!refreshToken) return false;
  if (!refreshPromise) {
    refreshPromise = (async () => {
      const response = await fetch(buildUrl("auth/refresh"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken }),
      });
      if (response.status === 400 || response.status === 401) {
        await setTokens(null);
        return false;
      }
      if (!response.ok) throw await parseError(response);
      await setTokens((await response.json()) as TokenResponse);
      return true;
    })().finally(() => {
      refreshPromise = null;
    });
  }
  return refreshPromise;
}

type RequestOptions = Omit<RequestInit, "body"> & { body?: unknown; authenticated?: boolean };

async function request<T>(path: string, options: RequestOptions = {}, retry = true): Promise<T> {
  const { body, authenticated = true, headers: suppliedHeaders, ...init } = options;
  const headers = new Headers(suppliedHeaders);
  const tokenForRequest = accessToken;
  if (authenticated && tokenForRequest) headers.set("Authorization", `Bearer ${tokenForRequest}`);
  const isForm = typeof FormData !== "undefined" && body instanceof FormData;
  if (body !== undefined && !isForm) headers.set("Content-Type", "application/json");
  const response = await fetch(buildUrl(path), {
    ...init,
    headers,
    body: body === undefined ? undefined : isForm ? (body as FormData) : JSON.stringify(body),
  });
  if (response.status === 401 && authenticated && retry) {
    if (tokenForRequest && accessToken && tokenForRequest !== accessToken) {
      return request<T>(path, options, false);
    }
    if (await refreshAccessToken()) return request<T>(path, options, false);
  }
  if (!response.ok) throw await parseError(response);
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export function resolveMediaUrl(value?: string | null, cacheBust?: string | number) {
  if (!value) return "";
  const url = new URL(value, API_ORIGIN);
  if (cacheBust !== undefined) url.searchParams.set("v", String(cacheBust));
  return url.toString();
}

const genderLabels = ["其他", "男", "女"] as const;
const userStatusLabels = ["正常", "离线", "封禁"] as const;
const recruitmentStatusLabels = ["招募中", "已关闭", "已删除"] as const;
const responseStatusLabels = ["待处理", "已接受", "已拒绝"] as const;
const chatStatusLabels = ["限制", "开放"] as const;

const mapUser = (user: UserResponse): UserInfo => ({
  id: user.id,
  username: user.username,
  nickname: user.nickname,
  signature: user.signature,
  avatar: resolveMediaUrl(user.avatarUrl, mediaCacheVersion || undefined),
  gender: genderLabels[user.gender] ?? genderLabels[GenderDto.Other],
  status: userStatusLabels[user.status] ?? userStatusLabels[UserStatusDto.Offline],
  isAdmin: user.isAdmin,
  tags: user.tags,
});

const mapResponse = (response: ResponseResponse): ResponseData => ({
  id: response.id,
  recruitmentId: response.recruitmentId,
  responserId: response.responderId,
  responseStatus: responseStatusLabels[response.type] ?? responseStatusLabels[ResponseTypeDto.Pending],
});

const mapRecruitment = (recruitment: RecruitmentResponse): RecruitmentData => ({
  id: recruitment.id,
  publisherId: recruitment.recruiter.id,
  gameId: recruitment.game.id,
  gameName: recruitment.game.name,
  gameIcon: resolveMediaUrl(recruitment.game.iconUrl, mediaCacheVersion || undefined),
  gameCover: resolveMediaUrl(recruitment.game.coverUrl, mediaCacheVersion || undefined),
  title: recruitment.title,
  description: recruitment.description,
  gameTags: recruitment.game.tags.map((tag) => tag.name),
  recruitmentTags: recruitment.tags,
  responses: recruitment.responses.map(mapResponse),
  status: recruitmentStatusLabels[recruitment.status] ?? recruitmentStatusLabels[RecruitmentStatusDto.Open],
  expiredAt: recruitment.expiresAt,
  maxParticipants: recruitment.maxParticipants,
  currentParticipants: recruitment.currParticipants,
  publisher: mapUser(recruitment.recruiter),
});

const mapMessage = (message: MessageResponse): MessageData => ({
  id: message.id,
  senderId: message.senderId,
  content: message.content,
  createdAt: message.sentAt,
});

export async function login(username: string, password: string) {
  const tokens = await request<TokenResponse>("auth/login", {
    method: "POST",
    body: { username, password },
    authenticated: false,
  });
  await setTokens(tokens);
  return tokens;
}

export async function register(username: string, password: string) {
  const tokens = await request<TokenResponse>("auth/register", {
    method: "POST",
    body: { username, password },
    authenticated: false,
  });
  await setTokens(tokens);
  return tokens;
}

export const discardSession = () => setTokens(null);

export const getMe = async () => mapUser(await request<UserResponse>("users/me"));
export const getUserById = async (id: number) => mapUser(await request<UserResponse>(`users/${id}`));

export async function logout() {
  try {
    const revoke = async () => {
      if (!accessToken || !refreshToken) return 204;
      const response = await fetch(buildUrl("auth/logout"), {
        method: "POST",
        headers: {
          Authorization: `Bearer ${accessToken}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ refreshToken }),
      });
      return response.status;
    };
    if ((await revoke()) === 401 && (await refreshAccessToken())) await revoke();
  } finally {
    await setTokens(null);
  }
}

export async function updateUser(data: { nickname?: string; signature?: string; gender?: number; tagIds?: number[] }) {
  return mapUser(await request<UserResponse>("users/me", { method: "PATCH", body: data }));
}

export async function uploadAvatar(asset: { uri: string; fileName?: string | null; mimeType?: string; file?: File }) {
  const form = new FormData();
  if (Platform.OS === "web" && asset.file) form.append("file", asset.file);
  else form.append("file", { uri: asset.uri, name: asset.fileName || "avatar.jpg", type: asset.mimeType || "image/jpeg" } as unknown as Blob);
  await request<void>("users/me/avatar", { method: "PUT", body: form });
  mediaCacheVersion = Date.now();
}

export async function getGames(query = ""): Promise<GameBrief[]> {
  const games = await request<GameResponse[]>(`games${query ? `?query=${encodeURIComponent(query)}` : ""}`);
  return games.map((game) => ({ id: game.id, name: game.name, icon: resolveMediaUrl(game.iconUrl, mediaCacheVersion || undefined) }));
}

export async function getGameById(id: number): Promise<GameInfo> {
  const game = await request<GameResponse>(`games/${id}`);
  return {
    id: game.id,
    name: game.name,
    description: game.description,
    icon: resolveMediaUrl(game.iconUrl, mediaCacheVersion || undefined),
    cover: resolveMediaUrl(game.coverUrl, mediaCacheVersion || undefined),
    tags: game.tags.map((tag) => tag.name),
  };
}

export const getGameTags = () => request<TagResponse[]>("game-tags") as Promise<GameTag[]>;
export const getUserTags = () => request<TagResponse[]>("user-tags");
export const getRecruitmentTags = () => request<TagResponse[]>("recruitment-tags") as Promise<RecruitmentTag[]>;

export interface AdminUserQuery {
  query?: string;
  status?: UserStatusDto;
  isAdmin?: boolean;
  page?: number;
  pageSize?: number;
}

export interface AdminGameQuery {
  query?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminRecruitmentQuery {
  query?: string;
  recruiterId?: number;
  gameId?: number;
  status?: RecruitmentStatusDto;
  page?: number;
  pageSize?: number;
}

export async function getAdminUsers(query: AdminUserQuery = {}) {
  return request<PagedResponse<UserResponse>>(buildUrl("admin/users", { ...query }));
}

export async function getAdminGames(query: AdminGameQuery = {}) {
  const page = await request<PagedResponse<GameResponse>>(buildUrl("admin/games", { ...query }));
  return {
    ...page,
    items: page.items.map((game) => ({
      ...game,
      iconUrl: resolveMediaUrl(game.iconUrl, mediaCacheVersion || undefined),
      coverUrl: resolveMediaUrl(game.coverUrl, mediaCacheVersion || undefined),
    })),
  };
}

export async function getAdminGameById(id: number) {
  const game = await request<GameResponse>(`games/${id}`);
  return {
    ...game,
    iconUrl: resolveMediaUrl(game.iconUrl, mediaCacheVersion || undefined),
    coverUrl: resolveMediaUrl(game.coverUrl, mediaCacheVersion || undefined),
  };
}

export async function getAdminRecruitments(query: AdminRecruitmentQuery = {}) {
  const page = await request<PagedResponse<RecruitmentResponse>>(buildUrl("admin/recruitments", { ...query }));
  return { ...page, items: page.items.map(mapRecruitment) };
}

export const setUserSuspension = (id: number, suspended: boolean) =>
  request<UserResponse>(`users/${id}/suspension`, { method: "PATCH", body: { suspended } });

export interface GameWriteData {
  name: string;
  description: string;
  tagIds: number[];
}

export const createGame = (data: GameWriteData) =>
  request<GameResponse>("games", { method: "POST", body: data });
export const updateGame = (id: number, data: Partial<GameWriteData>) =>
  request<GameResponse>(`games/${id}`, { method: "PATCH", body: data });

export type UploadAsset = {
  uri: string;
  fileName?: string | null;
  mimeType?: string;
  file?: File;
};

async function uploadGameMedia(id: number, kind: "icon" | "cover", asset: UploadAsset) {
  const form = new FormData();
  if (Platform.OS === "web" && asset.file) form.append("file", asset.file);
  else form.append("file", { uri: asset.uri, name: asset.fileName || `${kind}.jpg`, type: asset.mimeType || "image/jpeg" } as unknown as Blob);
  await request<void>(`games/${id}/${kind}`, { method: "PUT", body: form });
  mediaCacheVersion = Date.now();
}

async function deleteGameMedia(id: number, kind: "icon" | "cover") {
  await request<void>(`games/${id}/${kind}`, { method: "DELETE" });
  mediaCacheVersion = Date.now();
}

export const uploadGameIcon = (id: number, asset: UploadAsset) => uploadGameMedia(id, "icon", asset);
export const uploadGameCover = (id: number, asset: UploadAsset) => uploadGameMedia(id, "cover", asset);
export const deleteGameIcon = (id: number) => deleteGameMedia(id, "icon");
export const deleteGameCover = (id: number) => deleteGameMedia(id, "cover");

const createTag = (catalog: "game-tags" | "user-tags" | "recruitment-tags", name: string) =>
  request<TagResponse>(catalog, { method: "POST", body: { name } });
export const createGameTag = (name: string) => createTag("game-tags", name);
export const createUserTag = (name: string) => createTag("user-tags", name);
export const createRecruitmentTag = (name: string) => createTag("recruitment-tags", name);

export interface RecruitmentQuery {
  gameId?: number;
  gameTagIds?: number[];
  recruitmentTagIds?: number[];
  page?: number;
  pageSize?: number;
}

export async function searchRecruitments(query: RecruitmentQuery = {}) {
  const page = await request<PagedResponse<RecruitmentResponse>>(
    buildUrl("recruitments", { ...query, pageSize: Math.min(query.pageSize ?? 20, 100) }),
  );
  return { ...page, items: page.items.map(mapRecruitment) };
}

export const getRecruitments = async (
  gameId?: number,
  gameTagIds: number[] = [],
  recruitmentTagIds: number[] = [],
) => {
  const first = await searchRecruitments({
    gameId,
    gameTagIds,
    recruitmentTagIds,
    page: 1,
    pageSize: 100,
  });
  const items = [...first.items];
  for (let page = 2; page <= first.totalPages; page++) {
    items.push(
      ...(await searchRecruitments({
        gameId,
        gameTagIds,
        recruitmentTagIds,
        page,
        pageSize: 100,
      })).items,
    );
  }
  return items;
};

export const getRecruitmentById = async (id: number) => mapRecruitment(await request<RecruitmentResponse>(`recruitments/${id}`));
export const recordRecruitmentView = (id: number) =>
  request<void>(`recruitments/${id}/views`, { method: "POST" });

export async function getRecruitmentsByPublisherId(recruiterId: number) {
  const first = await request<PagedResponse<RecruitmentResponse>>(
    `recruitments/recruiters/${recruiterId}?page=1&pageSize=100`,
  );
  const items = [...first.items];
  for (let page = 2; page <= first.totalPages; page++) {
    const next = await request<PagedResponse<RecruitmentResponse>>(
      `recruitments/recruiters/${recruiterId}?page=${page}&pageSize=100`,
    );
    items.push(...next.items);
  }
  return items.map(mapRecruitment);
}

export async function getRecruitmentsByPublisherPage(
  recruiterId: number,
  page = 1,
  pageSize = 20,
  status?: RecruitmentStatusDto,
) {
  const result = await request<PagedResponse<RecruitmentResponse>>(
    buildUrl(`recruitments/recruiters/${recruiterId}`, { page, pageSize, status }),
  );
  return { ...result, items: result.items.map(mapRecruitment) };
}

export interface RecruitmentWriteData {
  gameId: number;
  title: string;
  description: string;
  maxParticipants: number;
  expiresAt: string;
  recruitmentTagIds: number[];
  status?: RecruitmentStatusDto;
}

export async function createRecruitment(data: RecruitmentWriteData) {
  return mapRecruitment(await request<RecruitmentResponse>("recruitments", { method: "POST", body: data }));
}

export async function updateRecruitment(id: number, data: Partial<Omit<RecruitmentWriteData, "gameId">>) {
  return mapRecruitment(await request<RecruitmentResponse>(`recruitments/${id}`, { method: "PATCH", body: data }));
}

export const deleteRecruitment = (id: number) => updateRecruitment(id, { status: RecruitmentStatusDto.Deleted });
export const forceTakeDownRecruitment = (id: number) =>
  request<void>(`recruitments/${id}/close`, { method: "POST" });

export const createResponse = async (recruitmentId: number) => mapResponse(
  await request<ResponseResponse>(`recruitments/${recruitmentId}/responses`, { method: "POST" }),
);
export const acceptResponse = async (id: number) => mapResponse(await request<ResponseResponse>(`responses/${id}/accept`, { method: "POST" }));
export const rejectResponse = async (id: number) => mapResponse(await request<ResponseResponse>(`responses/${id}/reject`, { method: "POST" }));

async function mapChat(chat: ChatResponse, currentUserId: number): Promise<ChatData> {
  const otherUserId = chat.user1Id === currentUserId ? chat.user2Id : chat.user1Id;
  const otherUser = await getUserById(otherUserId);
  return {
    id: chat.id,
    recruitmentId: chat.recruitmentId,
    otherUserId,
    otherUser,
    otherUserAvatar: otherUser.avatar,
    otherUserName: otherUser.nickname || otherUser.username,
    lastMessage: chat.lastMessage ? mapMessage(chat.lastMessage) : null,
    lastMessageContent: chat.lastMessage?.content ?? "",
    lastMessageAt: chat.lastMessage?.sentAt ?? "",
    unreadCount: chat.user1Id === currentUserId ? chat.newMsgsCntForUser1 : chat.newMsgsCntForUser2,
    chatStatus: chatStatusLabels[chat.status] ?? chatStatusLabels[ChatStatusDto.Restricted],
  };
}

export async function getChatsPage(currentUserId: number, before?: string, limit = 20) {
  const page = await request<CursorPagedResponse<ChatResponse>>(buildUrl("chats/me", { before, limit }));
  return { ...page, items: await Promise.all(page.items.map((chat) => mapChat(chat, currentUserId))) };
}
export async function getChats(currentUserId: number): Promise<ChatBrief[]> {
  return (await getChatsPage(currentUserId)).items;
}
export const getChatById = async (id: number, currentUserId: number) => mapChat(await request<ChatResponse>(`chats/${id}`), currentUserId);
export const getChatByUser = async (otherUserId: number, currentUserId: number) => mapChat(await request<ChatResponse>(`chats/by-user/${otherUserId}`), currentUserId);
export async function getMessagesPage(chatId: number, before?: string, limit = 50) {
  const page = await request<CursorPagedResponse<MessageResponse>>(
    buildUrl(`chats/${chatId}/messages`, { before, limit }),
  );
  return { ...page, items: page.items.map(mapMessage) };
}
export const getMessagesByChatId = async (chatId: number) => (await getMessagesPage(chatId)).items;
export const getWebSocketTicket = (chatId: number) => request<WebSocketTicketResponse>(`chats/${chatId}/ws-ticket`, { method: "POST" });

export async function openChatSocket(chatId: number) {
  const { ticket } = await getWebSocketTicket(chatId);
  return new WebSocket(`${WS_BASE_URL}/chats/${chatId}/ws?ticket=${encodeURIComponent(ticket)}`);
}

export async function sendGreeting(chatId: number, content: string) {
  const message = content.trim();
  if (!message || message.length > 4000) throw new Error("Greeting must be 1-4000 characters");
  const socket = await openChatSocket(chatId);
  await new Promise<void>((resolve, reject) => {
    let settled = false;
    const finish = (error?: Error) => {
      if (settled) return;
      settled = true;
      clearTimeout(timeout);
      if (error) reject(error);
      else resolve();
    };
    const timeout = setTimeout(() => {
      finish(new Error("Greeting timed out"));
      socket.close();
    }, 10_000);
    socket.onopen = () => socket.send(JSON.stringify({ content: message }));
    socket.onmessage = (event) => {
      const message = JSON.parse(String(event.data)) as MessageResponse;
      if (message.content === content.trim()) {
        finish();
        socket.close();
      }
    };
    socket.onerror = () => finish(new Error("Unable to send greeting"));
    socket.onclose = () => finish(new Error("Chat closed before the greeting was sent"));
  });
}
