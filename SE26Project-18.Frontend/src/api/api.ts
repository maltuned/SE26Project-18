import type {
  ApiResponse,
  ChatBriefDto,
  ChatDto,
  CreateReviewDto,
  FeedbackDto,
  GameBriefDto,
  GameDto,
  GameTagDto,
  MessageDto,
  NotificationDto,
  RecruitmentBriefDto,
  RecruitmentDetailDto,
  RecruitmentDto,
  RecruitmentTagDto,
  ReportDto,
  ResponseDto,
  ReviewDto,
  TokenResponse,
  UserBriefDto,
  UserDto,
  UserSettingsDto,
} from "./dtos";
import type {
  ChatBrief,
  ChatData,
  GameBrief,
  GameInfo,
  GameTag,
  MessageData,
  RecruitmentBrief,
  RecruitmentData,
  RecruitmentTag,
  ReviewData,
  ResponseData,
  ResponseStatus,
  UserInfo,
  UserSettings,
} from "./data-patterns";
import { tokenStorage } from "./token-storage";
import { API_BASE } from "./config";

// Re-export frontend data patterns for convenience
export type {
  ChatBrief, ChatData, ChatStatus, GameBrief, GameInfo, GameTag, MessageData, RecruitmentBrief, RecruitmentData,
  RecruitmentTag, ResponseData, ResponseStatus, UserInfo, UserSettings, ReviewData
} from "./data-patterns";

// ==================== Config ====================

let onAuthExpired: (() => void) | null = null;

export const setAuthExpiredHandler = (handler: () => void) => {
  onAuthExpired = handler;
};

let logoutInProgress = false;

export const setLogoutInProgress = (value: boolean) => {
  logoutInProgress = value;
};

// ==================== Fetch Helpers ====================

const buildUrl = (endpoint: string, params?: Record<string, any>) => {
  const url = new URL(API_BASE + endpoint);
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

const getAuthHeaders = async (): Promise<Record<string, string>> => {
  const token = await tokenStorage.getAccessToken();
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }
  return headers;
};

let refreshPromise: Promise<boolean> | null = null;

const tryRefreshToken = async (): Promise<boolean> => {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    try {
      const refreshToken = await tokenStorage.getRefreshToken();
      if (!refreshToken) return false;

      const res = await fetch(buildUrl("/Auth/refresh"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refresh_token: refreshToken }),
      });

      if (!res.ok) return false;

      const data: ApiResponse<TokenResponse> = await res.json();
      if (data.status !== 200 || !data.data) return false;

      await tokenStorage.setTokens(
        data.data.access_token,
        data.data.refresh_token,
        data.data.access_token_expires_at,
        data.data.refresh_token_expires_at,
      );
      return true;
    } catch {
      return false;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
};

const apiGet = async <T>(endpoint: string, params?: Record<string, any>): Promise<ApiResponse<T>> => {
  const headers = await getAuthHeaders();
  const res = await fetch(buildUrl(endpoint, params), {
    method: "GET",
    headers,
  });

  if (res.status === 401) {
    if (logoutInProgress) {
      return { status: 401, data: null as any, message: "" };
    }
    const refreshed = await tryRefreshToken();
    if (refreshed) {
      const retryHeaders = await getAuthHeaders();
      const retryRes = await fetch(buildUrl(endpoint, params), {
        method: "GET",
        headers: retryHeaders,
      });
      if (retryRes.ok) return retryRes.json();
    }
    await tokenStorage.clearTokens();
    onAuthExpired?.();
    throw new Error("认证已过期");
  }

  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
};

const apiPost = async <T>(endpoint: string, body?: any): Promise<ApiResponse<T>> => {
  const headers = await getAuthHeaders();
  const res = await fetch(buildUrl(endpoint), {
    method: "POST",
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });

  if (res.status === 401) {
    if (logoutInProgress) {
      return { status: 401, data: null as any , message: "" };
    }
    const refreshed = await tryRefreshToken();
    if (refreshed) {
      const retryHeaders = await getAuthHeaders();
      const retryRes = await fetch(buildUrl(endpoint), {
        method: "POST",
        headers: retryHeaders,
        body: body ? JSON.stringify(body) : undefined,
      });
      if (retryRes.ok) return retryRes.json();
    }
    await tokenStorage.clearTokens();
    onAuthExpired?.();
    throw new Error("认证已过期");
  }

  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
};

const apiPostNoAuth = async <T>(endpoint: string, body?: any): Promise<ApiResponse<T>> => {
  const res = await fetch(buildUrl(endpoint), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
};

const apiPut = async <T>(endpoint: string, body?: any): Promise<ApiResponse<T>> => {
  const headers = await getAuthHeaders();
  const res = await fetch(buildUrl(endpoint), {
    method: "PUT",
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });

  if (res.status === 401) {
    if (logoutInProgress) {
      return { status: 401, data: null as any, message: "" };
    }
    const refreshed = await tryRefreshToken();
    if (refreshed) {
      const retryHeaders = await getAuthHeaders();
      const retryRes = await fetch(buildUrl(endpoint), {
        method: "PUT",
        headers: retryHeaders,
        body: body ? JSON.stringify(body) : undefined,
      });
      if (retryRes.ok) return retryRes.json();
    }
    await tokenStorage.clearTokens();
    onAuthExpired?.();
    throw new Error("认证已过期");
  }

  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
};

// ==================== DTO to Frontend Mappers ====================

const mapUserDto = (dto: UserDto): UserInfo => ({
  id: dto.id,
  uid: dto.uid,
  username: dto.username,
  nickname: dto.nickname,
  avatar: dto.avatar,
  signature: dto.signature,
  gender: dto.gender,
  status: dto.status,
  createdAt: dto.created_at,
  updatedAt: dto.updated_at,
  settings: dto.settings ? {
    pushEnabled: dto.settings.push_enabled,
    profileVisible: dto.settings.profile_visible,
    darkMode: dto.settings.dark_mode,
  } : undefined,
});

const mapUserBriefDto = (dto: UserBriefDto): UserInfo => ({
  id: dto.id,
  uid: dto.id,
  username: dto.username,
  nickname: dto.nickname,
  avatar: dto.avatar,
  signature: "",
  gender: "其他",
  status: "正常",
  createdAt: "",
  updatedAt: "",
});

// 标签缓存（启动时从后端抓取，通过id查找）
let gameTagCache: Map<number, GameTag> = new Map();
let recruitmentTagCache: Map<number, RecruitmentTag> = new Map();
let tagsInitialized = false;

const ensureTagsLoaded = async () => {
  if (tagsInitialized) return;
  try {
    const [gameRes, recRes] = await Promise.all([
      apiGet<GameTagDto[]>("/GameTags"),
      apiGet<RecruitmentTagDto[]>("/RecruitmentTags"),
    ]);
    gameRes.data?.forEach((t) => gameTagCache.set(t.id, { id: t.id, name: t.name }));
    recRes.data?.forEach((t) => recruitmentTagCache.set(t.id, { id: t.id, name: t.name }));
    tagsInitialized = true;
  } catch {
    // Tags will be loaded on next call
  }
};

const getGameTagsByIds = (ids: number[]): GameTag[] => {
  ids.forEach((id) => {
    if (!gameTagCache.has(id)) {
      // Placeholder for missing tags
      gameTagCache.set(id, { id, name: `未知标签${id}` });
    }
  });
  return ids
    .map((id) => gameTagCache.get(id))
    .filter((t): t is GameTag => t !== undefined);
};

const getRecruitmentTagsByIds = (ids: number[]): RecruitmentTag[] => {
  ids.forEach((id) => {
    if (!recruitmentTagCache.has(id)) {
      recruitmentTagCache.set(id, { id, name: `未知标签${id}` });
    }
  });
  return ids
    .map((id) => recruitmentTagCache.get(id))
    .filter((t): t is RecruitmentTag => t !== undefined);
};

// RecruitmentDto (without detail) → RecruitmentData
const mapRecruitmentDto = (dto: RecruitmentDto): RecruitmentData => ({
  id: dto.id,
  publisherId: dto.publisher_id,
  gameId: dto.game_id ?? 0,
  gameName: dto.game_name || "",
  gameCover: "",
  gameIcon: "",
  title: dto.title,
  description: dto.description,
  gameTags: [],
  recruitmentTags: getRecruitmentTagsByIds(dto.tags_id),
  status: dto.status,
  createdAt: dto.created_at,
  updatedAt: dto.updated_at,
  expiredAt: dto.expired_at,
  maxParticipants: dto.max_participants,
  currentParticipants: dto.current_participants,
  publisher: {} as UserInfo,
});

// RecruitmentDetailDto → RecruitmentData
// 差异: backend gameTags是对象数组, 前端gameTags是string[]; backend tags_id是recruitment_tags
const mapRecruitmentDetailDto = (dto: RecruitmentDetailDto): RecruitmentData => ({
  id: dto.id,
  publisherId: dto.publisher_id,
  gameId: dto.game_id ?? 0,
  gameName: dto.game?.name || dto.game_name || "",
  gameCover: dto.game?.cover || "",
  gameIcon: dto.game?.icon || "",
  title: dto.title,
  description: dto.description,
  // 差异: backend gameTags是GameTagDto[], 前端需要string[]
  gameTags: dto.gameTags.map((t) => t.name),
  // 差异: backend recruitmentTags是RecruitmentTagDto[], 前端需要RecruitmentTag[]
  recruitmentTags: dto.recruitmentTags.map((t) => ({ id: t.id, name: t.name })),
  status: dto.status,
  createdAt: dto.created_at,
  updatedAt: dto.updated_at,
  expiredAt: dto.expired_at,
  maxParticipants: dto.max_participants,
  currentParticipants: dto.current_participants,
  publisher: mapUserBriefDto(dto.publisher),
});

// ResponseDto → ResponseData (类型一致, 仅字段名差异)
const mapResponseDto = (dto: ResponseDto): ResponseData => ({
  id: dto.id,
  recruitmentId: dto.recruitment_id,
  responserId: dto.responser_id,
  responseStatus: dto.response_status,
  createdAt: dto.created_at,
  updatedAt: dto.updated_at,
  responser: mapUserBriefDto(dto.responser),
});

// RecruitmentBriefDto → RecruitmentBrief
const mapRecruitmentBriefDto = (dto: RecruitmentBriefDto): RecruitmentBrief => ({
  id: dto.id,
  title: dto.title,
  game: dto.game
    ? { id: dto.game.id, name: dto.game.name, nameEn: dto.game.name_en || "", icon: dto.game.icon }
    : { id: 0, name: dto.game_name || "(已删除)", nameEn: "", icon: "" },
});

// GameDto → GameBrief
const mapGameBriefDto = (dto: GameDto): GameBrief => ({
  id: dto.id,
  name: dto.name,
  nameEn: dto.name_en || "",
  icon: dto.icon || "",
});

// GameDto → GameInfo
// 差异: backend tags_id是number[], 前端tags是string[]
const mapGameDto = (dto: GameDto): GameInfo => ({
  id: dto.id,
  name: dto.name,
  nameEn: dto.name_en || "",
  aliases: dto.aliases || "",
  company: dto.company,
  description: dto.description,
  cover: dto.cover || "",
  icon: dto.icon || "",
  // 差异: backend tags_id是number[], 前端需要string[]
  tags: getGameTagsByIds(dto.tags_id).map((t) => t.name),
  createdAt: dto.created_at || "",
  updatedAt: dto.updated_at || "",
});

// MessageDto → MessageData (类型一致, 仅字段名差异)
const mapMessageDto = (dto: MessageDto): MessageData => ({
  id: dto.id,
  chatId: dto.chat_id,
  senderId: dto.sender_id,
  receiverId: dto.receiver_id,
  content: dto.content,
  createdAt: dto.created_at,
  sender: mapUserBriefDto(dto.sender),
  receiver: mapUserBriefDto(dto.receiver),
});

// ChatBriefDto → ChatBrief (类型一致, 仅字段名差异)
const mapChatBriefDto = (dto: ChatBriefDto): ChatBrief => ({
  id: dto.id,
  otherUserAvatar: dto.other_user_avatar,
  otherUserName: dto.other_user_name,
  lastMessageContent: dto.last_message_content,
  lastMessageAt: dto.last_message_at,
  unreadCount: dto.unread_count,
  createdAt: dto.created_at,
});

// ChatDto → ChatData
// 差异: backend recruitment是RecruitmentDto, 前端需要RecruitmentData
const mapChatDto = (dto: ChatDto): ChatData => ({
  id: dto.id,
  recruitmentId: dto.recruitment_id,
  recruitmentTitle: dto.recruitment_title,
  otherUser: mapUserBriefDto(dto.other_user),
  lastMessage: dto.last_message ? mapMessageDto(dto.last_message) : null,
  unreadCount: dto.unread_count,
  chatStatus: dto.chat_status,
  newMessageAt: dto.new_message_at,
  users: dto.users?.map((u) => ({
    userId: u.user_id,
    sentMessage: u.sent_message,
  })),
  recruitment: dto.recruitment ? mapRecruitmentBriefDto(dto.recruitment) : undefined,
});

export interface NotificationItem {
  id: number;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
}

const mapNotificationDto = (dto: NotificationDto): NotificationItem => ({
  id: dto.id,
  title: dto.title,
  body: dto.body,
  isRead: dto.is_read,
  createdAt: dto.created_at,
});

// ==================== Response Helpers ====================

const handleResponse = async <T>(
  promise: Promise<ApiResponse<any>>,
  mapper: (data: any) => T,
): Promise<T | null> => {
  try {
    const res = await promise;
    if (res.status >= 200 && res.status < 300 && res.data !== undefined && res.data !== null) {
      return mapper(res.data);
    }
    return null;
  } catch (e) {
    console.error("API Error:", e);
    return null;
  }
};

const handleResponseDirect = async <T>(
  promise: Promise<ApiResponse<T>>,
): Promise<T | null> => {
  try {
    const res = await promise;
    if (res.status >= 200 && res.status < 300 && res.data !== undefined && res.data !== null) {
      return res.data;
    }
    console.error("API Error:", res);
    return null;
  } catch (e) {
    console.error("API Error:", e);
    return null;
  }
};

const handleArrayResponse = async <T>(
  promise: Promise<ApiResponse<any>>,
  mapper: (data: any) => T[],
): Promise<T[]> => {
  try {
    const res = await promise;
    if (res.status >= 200 && res.status < 300 && Array.isArray(res.data)) {
      return mapper(res.data);
    }
    return [];
  } catch (e) {
    console.error("API Error:", e);
    return [];
  }
};

const handlePostResponse = async <T>(
  promise: Promise<ApiResponse<any>>,
  mapper: (data: any) => T,
): Promise<T> => {
  const res = await promise;
  if (res.status >= 200 && res.status < 300 && res.data !== undefined && res.data !== null) {
    return mapper(res.data);
  }
  throw new Error(`API Error [${res.status}]: ${res.message}`);
};

// ==================== User API ====================

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

export const getMe = async (accessToken?: string): Promise<UserInfo | null> => {
  const token = accessToken ?? await tokenStorage.getAccessToken();
  if (!token) return null;
  const res = await fetch(buildUrl("/Auth/me"), {
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

export const getUserById = (id: number): Promise<UserInfo | null> => {
  const response = apiGet<UserDto>("/Users/by-id", { id });
  return handleResponse<UserInfo>(response, mapUserDto);
};

export const getUserProfile = async (id: number): Promise<{ user: UserInfo | null; isPrivate: boolean }> => {
  const res = await apiGet<UserDto>("/Users/profile", { id });
  if (res.status === 403) {
    return { user: null, isPrivate: true };
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

export const changePassword = async (oldPassword: string, newPassword: string): Promise<boolean> => {
  const res = await apiPost<boolean>("/Auth/change-password", {
    old_password: oldPassword,
    new_password: newPassword,
  });
  if (res.status !== 200 || !res.data) {
    throw new Error(res.message || "修改密码失败");
  }
  return true;
};

export const updateUserSettings = async (settings: { pushEnabled: boolean; profileVisible: boolean; darkMode: boolean }): Promise<UserSettings> => {
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

// ==================== Game API ====================

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

// ==================== Tag API ====================

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

// ==================== Recruitment API ====================

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

export const getRecruitmentsByGame = (
  gameId: number,
): Promise<RecruitmentData[]> => {
  const response = apiGet<RecruitmentDetailDto[]>("/Recruitments/by-game", { gameId });
  return handleArrayResponse<RecruitmentData>(
    response,
    (data: RecruitmentDetailDto[]) => data.map(mapRecruitmentDetailDto),
  );
};

export const getRecruitmentById = (
  id: number,
): Promise<RecruitmentData | null> => {
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

// ==================== Response API ====================

export const getResponses = (
  recruitmentId?: number,
): Promise<ResponseData[]> => {
  if (recruitmentId === undefined) {
    // 获取所有招募并聚合回应
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

export const getResponsesByUserId = (
  userId: number,
): Promise<ResponseData[]> => {
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

export const deleteResponse = (
  id: number,
  reason: string,
): Promise<boolean> => {
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

// ==================== Chat API ====================

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

export const getChatsByRecruitmentId = (
  recruitmentId: number,
): Promise<ChatData[]> => {
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

// ==================== Message API ====================

export const getMessagesByChatId = (chatId: number): Promise<MessageData[]> => {
  const response = apiGet<MessageDto[]>("/Messages/by-chat", { chatId });
  return handleArrayResponse<MessageData>(response, (data: MessageDto[]) =>
    data.map(mapMessageDto),
  );
};

export const sendMessage = (data: {
  chatId: number;
  senderId: number;
  receiverId: number;
  content: string;
}): Promise<MessageData> => {
  const response = apiPost<MessageDto>("/Messages", {
    chat_id: data.chatId,
    sender_id: data.senderId,
    receiver_id: data.receiverId,
    content: data.content,
  });
  return handlePostResponse(response, mapMessageDto);
};

export const markMessagesRead = async (
  chatId: number,
  userId: number,
): Promise<boolean> => {
  const response = apiPost<boolean>("/Messages/mark-read", {
    chat_id: chatId,
    user_id: userId,
  });
  return handlePostResponse(response, (d: boolean) => d);
};

// ==================== Tag Cache Init ====================

export const initTagCaches = async () => {
  await ensureTagsLoaded();
};

// ==================== Feedback & Report API ====================

export const submitFeedback = async (data: FeedbackDto): Promise<boolean> => {
  const response = apiPost<boolean>("/Feedback", data);
  return handlePostResponse(response, (d: boolean) => d);
};

export const submitReport = async (data: ReportDto): Promise<boolean> => {
  const response = apiPost<boolean>("/Report", data);
  return handlePostResponse(response, (d: boolean) => d);
};

// ==================== Image Upload API ====================

export const uploadImage = async (uri: string, folder: string = "avatars"): Promise<string | null> => {
  return new Promise(async (resolve) => {
    const formData = new FormData();
    const filename = uri.split("/").pop() || "image.jpg";
    formData.append("file", {
      uri,
      name: filename,
      type: "image/jpeg",
    } as any);
    formData.append("folder", folder);

    const token = await tokenStorage.getAccessToken();
    const xhr = new XMLHttpRequest();
    xhr.open("POST", `${API_BASE}/Image/upload`);

    if (token) {
      xhr.setRequestHeader("Authorization", `Bearer ${token}`);
    }

    xhr.onload = () => {
      if (xhr.status === 200) {
        try {
          const data: ApiResponse<string> = JSON.parse(xhr.responseText);
          resolve(data.status === 200 ? data.data : null);
        } catch {
          resolve(null);
        }
      } else {
        resolve(null);
      }
    };
    xhr.onerror = () => resolve(null);
    xhr.send(formData);
  });
};

export const uploadAvatar = async (uri: string, userId: number): Promise<string | null> => {
  return new Promise(async (resolve) => {
    const formData = new FormData();
    const filename = uri.split("/").pop() || "avatar.jpg";
    formData.append("file", {
      uri,
      name: filename,
      type: "image/jpeg",
    } as any);
    formData.append("userId", String(userId));

    const token = await tokenStorage.getAccessToken();
    const xhr = new XMLHttpRequest();
    xhr.open("POST", `${API_BASE}/Image/upload-avatar`);

    if (token) {
      xhr.setRequestHeader("Authorization", `Bearer ${token}`);
    }

    xhr.onload = () => {
      if (xhr.status === 200) {
        try {
          const data: ApiResponse<string> = JSON.parse(xhr.responseText);
          resolve(data.status === 200 ? data.data : null);
        } catch {
          resolve(null);
        }
      } else {
        resolve(null);
      }
    };
    xhr.onerror = () => resolve(null);
    xhr.send(formData);
  });
};

// ==================== Notification API ====================

export const getNotifications = (): Promise<NotificationItem[]> => {
  const response = apiGet<NotificationDto[]>("/Notification");
  return handleArrayResponse<NotificationItem>(response, (data: NotificationDto[]) =>
    data.map(mapNotificationDto),
  );
};

export const getUnreadNotificationCount = async (): Promise<number> => {
  const response = apiGet<number>("/Notification/unread-count");
  return (await handleResponseDirect(response)) ?? 0;
};

export const markNotificationRead = (id: number): Promise<boolean> => {
  const response = apiPut<boolean>(`/Notification/${id}/read`);
  return handleResponseDirect(response).then((r) => r ?? false);
};

export const markAllNotificationsRead = (): Promise<boolean> => {
  const response = apiPut<boolean>("/Notification/read-all");
  return handleResponseDirect(response).then((r) => r ?? false);
};

// ==================== Review API ====================

const mapReviewDto = (dto: ReviewDto): ReviewData => ({
  id: dto.id,
  reviewerId: dto.reviewer_id,
  reviewerNickname: dto.reviewer_nickname,
  reviewerAvatar: dto.reviewer_avatar,
  revieweeId: dto.reviewee_id,
  revieweeNickname: dto.reviewee_nickname,
  content: dto.content,
  status: dto.status,
  createdAt: dto.created_at,
});

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