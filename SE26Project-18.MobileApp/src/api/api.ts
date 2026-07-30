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
  ResponseStatus,
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
  RecruitmentTag,
  ResponseData,
  ResponseStatus,
  UserInfo,
} from "./data-patterns";

const API_BASE = "http://10.73.61.199:5111/api/v1";

let accessToken: string | null = null;
let refreshToken: string | null = null;
let currentUserId: number | null = null;

type BackendPaged<T> = {
  items?: T[];
  Items?: T[];
};

type BackendToken = {
  accessToken?: string;
  refreshToken?: string;
  AccessToken?: string;
  RefreshToken?: string;
};

type BackendTag = {
  id?: number;
  Id?: number;
  name?: string;
  Name?: string;
};

type BackendUser = {
  id?: number;
  Id?: number;
  username?: string;
  Username?: string;
  nickname?: string;
  Nickname?: string;
  signature?: string;
  Signature?: string;
  gender?: number | string;
  Gender?: number | string;
  status?: number | string;
  Status?: number | string;
};

type BackendGame = {
  id?: number;
  Id?: number;
  name?: string;
  Name?: string;
  description?: string;
  Description?: string;
  tags?: BackendTag[];
  Tags?: BackendTag[];
};

type BackendRecruitment = {
  id?: number;
  Id?: number;
  game?: BackendGame;
  Game?: BackendGame;
  recruiter?: BackendUser;
  Recruiter?: BackendUser;
  title?: string;
  Title?: string;
  description?: string;
  Description?: string;
  tags?: BackendTag[];
  Tags?: BackendTag[];
  maxParticipants?: number;
  MaxParticipants?: number;
  currParticipants?: number;
  CurrParticipants?: number;
  status?: number | string;
  Status?: number | string;
  expiresAt?: string;
  ExpiresAt?: string;
};

type BackendResponse = {
  id?: number;
  Id?: number;
  recruitmentId?: number;
  RecruitmentId?: number;
  responderId?: number;
  ResponderId?: number;
  type?: number | string;
  Type?: number | string;
};

type BackendChat = {
  id?: number;
  Id?: number;
  recruitmentId?: number;
  RecruitmentId?: number;
  user1Id?: number;
  User1Id?: number;
  user2Id?: number;
  User2Id?: number;
  status?: number | string;
  Status?: number | string;
  newMsgsCntForUser1?: number;
  NewMsgsCntForUser1?: number;
  newMsgsCntForUser2?: number;
  NewMsgsCntForUser2?: number;
  lastMessage?: BackendMessage | null;
  LastMessage?: BackendMessage | null;
};

type BackendMessage = {
  senderId?: number;
  SenderId?: number;
  content?: string;
  Content?: string;
  sentAt?: string;
  SentAt?: string;
};

const prop = <T,>(obj: object | null | undefined, ...keys: string[]) => {
  if (!obj) return undefined;
  const record = obj as Record<string, unknown>;
  for (const key of keys) {
    if (record[key] !== undefined) return record[key] as T;
  }
  return undefined;
};

const buildUrl = (endpoint: string, params?: Record<string, unknown>) => {
  const url = new URL(endpoint.replace(/^\//, ""), `${API_BASE}/`);
  if (params) {
    Object.entries(params).forEach(([key, value]) => {
      if (value === undefined || value === null || value === "") return;
      if (Array.isArray(value)) {
        value.forEach((item) => url.searchParams.append(key, String(item)));
      } else {
        url.searchParams.append(key, String(value));
      }
    });
  }
  return url.toString();
};

const authHeaders = () =>
  accessToken ? { Authorization: `Bearer ${accessToken}` } : {};

const updateTokens = (tokens: BackendToken) => {
  accessToken = prop<string>(tokens, "accessToken", "AccessToken") ?? null;
  refreshToken = prop<string>(tokens, "refreshToken", "RefreshToken") ?? null;
};

export const clearAuthTokens = () => {
  accessToken = null;
  refreshToken = null;
  currentUserId = null;
};

const refreshAccessToken = async () => {
  if (!refreshToken) return false;
  const res = await fetch(buildUrl("/auth/refresh"), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });
  if (!res.ok) {
    clearAuthTokens();
    return false;
  }
  updateTokens((await res.json()) as BackendToken);
  return Boolean(accessToken);
};

const mergeHeaders = (headers?: HeadersInit) => {
  const merged: Record<string, string> = {
    "Content-Type": "application/json",
    ...authHeaders(),
  };
  if (headers instanceof Headers) {
    headers.forEach((value, key) => {
      merged[key] = value;
    });
  } else if (Array.isArray(headers)) {
    headers.forEach(([key, value]) => {
      merged[key] = value;
    });
  } else if (headers) {
    Object.assign(merged, headers);
  }
  return merged;
};

const request = async <T,>(
  endpoint: string,
  options: RequestInit = {},
  retry = true,
): Promise<T> => {
  const res = await fetch(buildUrl(endpoint), {
    ...options,
    headers: mergeHeaders(options.headers),
  });
  if (res.status === 401 && retry && (await refreshAccessToken())) {
    return request<T>(endpoint, options, false);
  }
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
};

const get = <T,>(endpoint: string, params?: Record<string, unknown>) =>
  request<T>(buildUrl(endpoint, params).replace(`${API_BASE}/`, "/"));

const post = <T,>(endpoint: string, body?: unknown) =>
  request<T>(endpoint, {
    method: "POST",
    body: body === undefined ? undefined : JSON.stringify(body),
  });

const patch = <T,>(endpoint: string, body?: unknown) =>
  request<T>(endpoint, {
    method: "PATCH",
    body: body === undefined ? undefined : JSON.stringify(body),
  });

const normalizeNumber = (value: unknown, fallback = 0) =>
  typeof value === "number" ? value : Number(value ?? fallback) || fallback;

const normalizeText = (value: unknown) =>
  typeof value === "string" ? value : "";

const enumName = (value: unknown, names: string[]) => {
  if (typeof value === "number") return names[value] ?? "";
  if (typeof value === "string") return value;
  return "";
};

const mapGender = (value: unknown) => {
  switch (enumName(value, ["Other", "Male", "Female"])) {
    case "Male":
      return "男";
    case "Female":
      return "女";
    default:
      return "其他";
  }
};

const mapUserStatus = (value: unknown) =>
  enumName(value, ["Online", "Offline", "Suspended"]) === "Suspended"
    ? "封禁"
    : "正常";

const mapRecruitmentStatus = (value: unknown) => {
  switch (enumName(value, ["Open", "Closed", "Deleted"])) {
    case "Closed":
      return "已关闭";
    case "Deleted":
      return "已删除";
    default:
      return "招募中";
  }
};

const toRecruitmentStatusValue = (status?: string) => {
  switch (status) {
    case "招募中":
      return 0;
    case "已关闭":
      return 1;
    case "已删除":
      return 2;
    default:
      return undefined;
  }
};

const mapChatStatus = (value: unknown) =>
  enumName(value, ["Restricted", "Free"]) === "Free" ? "开放" : "限制";

const mapResponseStatus = (value: unknown) =>
  enumName(value, ["Pending", "Accepted", "Rejected"]) === "Rejected"
    ? "已删除"
    : "已回应";

const mapTag = (tag: BackendTag): RecruitmentTag => ({
  id: normalizeNumber(prop(tag, "id", "Id")),
  name: normalizeText(prop(tag, "name", "Name")),
});

const mapGameTag = (tag: BackendTag): GameTag => ({
  id: normalizeNumber(prop(tag, "id", "Id")),
  name: normalizeText(prop(tag, "name", "Name")),
});

const uniqueTags = <T extends { id: number }>(tags: T[]) => {
  const seen = new Set<number>();
  return tags.filter((tag) => {
    if (seen.has(tag.id)) return false;
    seen.add(tag.id);
    return true;
  });
};

const emptyUser = (id = 0): UserInfo => ({
  id,
  uid: id,
  username: "",
  nickname: "",
  avatar: "",
  signature: "",
  gender: "其他",
  status: "正常",
  createdAt: "",
  updatedAt: "",
});

const mapUser = (dto: BackendUser): UserInfo => {
  const id = normalizeNumber(prop(dto, "id", "Id"));
  return {
    ...emptyUser(id),
    username: normalizeText(prop(dto, "username", "Username")),
    nickname: normalizeText(prop(dto, "nickname", "Nickname")),
    signature: normalizeText(prop(dto, "signature", "Signature")),
    gender: mapGender(prop(dto, "gender", "Gender")),
    status: mapUserStatus(prop(dto, "status", "Status")),
  };
};

const mapGameBrief = (dto: BackendGame): GameBrief => ({
  id: normalizeNumber(prop(dto, "id", "Id")),
  name: normalizeText(prop(dto, "name", "Name")),
  icon: "",
});

const mapGame = (dto: BackendGame): GameInfo => {
  const tags = prop<BackendTag[]>(dto, "tags", "Tags") ?? [];
  return {
    id: normalizeNumber(prop(dto, "id", "Id")),
    name: normalizeText(prop(dto, "name", "Name")),
    company: "",
    description: normalizeText(prop(dto, "description", "Description")),
    cover: "",
    icon: "",
    tags: tags.map((tag) => mapGameTag(tag).name),
    createdAt: "",
    updatedAt: "",
  };
};

const mapRecruitment = (dto: BackendRecruitment): RecruitmentData => {
  const game = prop<BackendGame>(dto, "game", "Game") ?? {};
  const recruiter = prop<BackendUser>(dto, "recruiter", "Recruiter") ?? {};
  const recruitmentTags = prop<BackendTag[]>(dto, "tags", "Tags") ?? [];
  const gameTags = prop<BackendTag[]>(game, "tags", "Tags") ?? [];
  return {
    id: normalizeNumber(prop(dto, "id", "Id")),
    publisherId: normalizeNumber(prop(recruiter, "id", "Id")),
    gameId: normalizeNumber(prop(game, "id", "Id")),
    gameName: normalizeText(prop(game, "name", "Name")),
    title: normalizeText(prop(dto, "title", "Title")),
    description: normalizeText(prop(dto, "description", "Description")),
    gameTags: gameTags.map((tag) => mapGameTag(tag).name),
    recruitmentTags: recruitmentTags.map(mapTag),
    status: mapRecruitmentStatus(prop(dto, "status", "Status")),
    createdAt: "",
    updatedAt: "",
    expiredAt: normalizeText(prop(dto, "expiresAt", "ExpiresAt")),
    maxParticipants: normalizeNumber(prop(dto, "maxParticipants", "MaxParticipants")),
    currentParticipants: normalizeNumber(prop(dto, "currParticipants", "CurrParticipants")),
    publisher: mapUser(recruiter),
  };
};

const mapResponse = (dto: BackendResponse): ResponseData => ({
  id: normalizeNumber(prop(dto, "id", "Id")),
  recruitmentId: normalizeNumber(prop(dto, "recruitmentId", "RecruitmentId")),
  responserId: normalizeNumber(prop(dto, "responderId", "ResponderId")),
  responseStatus: mapResponseStatus(prop(dto, "type", "Type")),
  createdAt: "",
  updatedAt: "",
  responser: emptyUser(normalizeNumber(prop(dto, "responderId", "ResponderId"))),
});

const mapMessage = (
  dto: BackendMessage,
  chatId: number,
  index: number,
  receiverId = 0,
): MessageData => {
  const senderId = normalizeNumber(prop(dto, "senderId", "SenderId"));
  const createdAt = normalizeText(prop(dto, "sentAt", "SentAt"));
  return {
    id: Number.isFinite(Date.parse(createdAt)) ? Date.parse(createdAt) + index : index,
    chatId,
    senderId,
    receiverId,
    content: normalizeText(prop(dto, "content", "Content")),
    createdAt,
    sender: emptyUser(senderId),
    receiver: emptyUser(receiverId),
  };
};

const mapChat = async (dto: BackendChat): Promise<ChatData> => {
  const id = normalizeNumber(prop(dto, "id", "Id"));
  const recruitmentId = normalizeNumber(prop(dto, "recruitmentId", "RecruitmentId"));
  const user1Id = normalizeNumber(prop(dto, "user1Id", "User1Id"));
  const user2Id = normalizeNumber(prop(dto, "user2Id", "User2Id"));
  const otherUserId = user1Id === currentUserId ? user2Id : user1Id;
  const otherUser = otherUserId ? await getUserById(otherUserId) : null;
  const lastMessage = prop<BackendMessage | null>(dto, "lastMessage", "LastMessage");
  const unreadCount =
    currentUserId === user1Id
      ? normalizeNumber(prop(dto, "newMsgsCntForUser1", "NewMsgsCntForUser1"))
      : normalizeNumber(prop(dto, "newMsgsCntForUser2", "NewMsgsCntForUser2"));

  return {
    id,
    recruitmentId,
    recruitmentTitle: "",
    otherUser: otherUser ?? emptyUser(otherUserId),
    lastMessage: lastMessage ? mapMessage(lastMessage, id, 0, currentUserId ?? 0) : null,
    unreadCount,
    chatStatus: mapChatStatus(prop(dto, "status", "Status")),
    newMessageAt: lastMessage ? normalizeText(prop(lastMessage, "sentAt", "SentAt")) : "",
    users: [
      { userId: user1Id, sentMessage: false },
      { userId: user2Id, sentMessage: false },
    ],
  };
};

const readPagedItems = <T,>(page: BackendPaged<T>) =>
  prop<T[]>(page, "items", "Items") ?? [];

export const login = async (
  username: string,
  password: string,
): Promise<UserInfo | null> => {
  clearAuthTokens();
  const tokens = await post<BackendToken>("/auth/login", { username, password });
  updateTokens(tokens);
  const user = await getUserById("me");
  if (user) currentUserId = user.id;
  return user;
};

export const register = async (
  username: string,
  password: string,
): Promise<UserInfo | null> => {
  clearAuthTokens();
  const tokens = await post<BackendToken>("/auth/register", { username, password });
  updateTokens(tokens);
  const user = await getUserById("me");
  if (user) currentUserId = user.id;
  return user;
};

export const getUserById = async (id: number | "me"): Promise<UserInfo | null> => {
  try {
    return mapUser(await get<BackendUser>(id === "me" ? "/users/me" : `/users/${id}`));
  } catch {
    return null;
  }
};

export const getUsers = (): Promise<UserInfo[]> => Promise.resolve([]);

export const updateUser = async (
  id: number,
  data: Record<string, unknown>,
): Promise<UserInfo | null> => {
  if (currentUserId !== id) return null;
  return mapUser(
    await patch<BackendUser>("/users/me", {
      nickname: data.nickname,
      signature: data.signature,
      gender: data.gender,
      tagIds: data.tagIds,
    }),
  );
};

export const getGames = async (query: string = ""): Promise<GameBrief[]> => {
  try {
    const games = await get<BackendGame[]>("/games", { Query: query || undefined });
    return games.map(mapGameBrief);
  } catch {
    return [];
  }
};

export const getGameById = async (id: number): Promise<GameInfo | null> => {
  try {
    return mapGame(await get<BackendGame>(`/games/${id}`));
  } catch {
    return null;
  }
};

export const getGameTags = async (): Promise<GameTag[]> => {
  try {
    const games = await get<BackendGame[]>("/games");
    return uniqueTags(
      games.flatMap((game) => (prop<BackendTag[]>(game, "tags", "Tags") ?? []).map(mapGameTag)),
    );
  } catch {
    return [];
  }
};

export const getRecruitmentTags = async (): Promise<RecruitmentTag[]> => {
  try {
    const page = await get<BackendPaged<BackendRecruitment>>("/recruitments", {
      PageSize: 100,
    });
    return uniqueTags(
      readPagedItems(page).flatMap((item) =>
        (prop<BackendTag[]>(item, "tags", "Tags") ?? []).map(mapTag),
      ),
    );
  } catch {
    return [];
  }
};

export const getRecruitments = async (
  gameName: string = "",
  gameTags: number[] = [],
  recruitmentTags: number[] = [],
): Promise<RecruitmentData[]> => {
  try {
    let gameId: number | undefined;
    if (gameName.trim()) {
      const games = await getGames(gameName.trim());
      gameId = games[0]?.id;
      if (!gameId) return [];
    }
    const page = await get<BackendPaged<BackendRecruitment>>("/recruitments", {
      GameId: gameId,
      GameTagIds: gameTags,
      RecruitmentTagIds: recruitmentTags,
      PageSize: 100,
    });
    return readPagedItems(page).map(mapRecruitment);
  } catch {
    return [];
  }
};

export const getRecruitmentsByGame = async (
  gameId: number,
): Promise<RecruitmentData[]> => {
  const page = await get<BackendPaged<BackendRecruitment>>("/recruitments", {
    GameId: gameId,
    PageSize: 100,
  });
  return readPagedItems(page).map(mapRecruitment);
};

export const getRecruitmentById = async (
  id: number,
): Promise<RecruitmentData | null> => {
  try {
    return mapRecruitment(await get<BackendRecruitment>(`/recruitments/${id}`));
  } catch {
    return null;
  }
};

export const getRecruitmentByChatId = async (
  chatId: number,
): Promise<RecruitmentData | null> => {
  const chat = await getChatById(chatId);
  return chat?.recruitmentId ? getRecruitmentById(chat.recruitmentId) : null;
};

export const getRecruitmentsByPublisherId = async (
  publisherId: number | null,
): Promise<RecruitmentData[]> => {
  if (publisherId === null) return getRecruitments();
  const page = await get<BackendPaged<BackendRecruitment>>(
    `/recruitments/recruiters/${publisherId}`,
    { PageSize: 100 },
  );
  return readPagedItems(page).map(mapRecruitment);
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
  if (data.id <= 0) return createRecruitment(data);
  return updateRecruitment(data.id, {
    title: data.title,
    description: data.description,
    status: data.status,
    expired_at: data.expiredAt,
    max_participants: data.maxParticipants,
    recruitment_tag_ids: data.tagsId,
  }).then((item) => {
    if (!item) throw new Error("Recruitment not found");
    return item;
  });
};

export const createRecruitment = async (data: {
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
  return mapRecruitment(
    await post<BackendRecruitment>("/recruitments", {
      gameId: data.gameId,
      title: data.title,
      description: data.description,
      maxParticipants: data.maxParticipants,
      expiresAt: data.expiredAt,
      recruitmentTagIds: data.tagsId,
    }),
  );
};

export const updateRecruitment = async (
  id: number,
  data: Partial<{
    title: string;
    description: string;
    status: string;
    expired_at: string;
    max_participants: number;
    current_participants: number;
    recruitment_tag_ids: number[];
  }>,
): Promise<RecruitmentData | null> => {
  try {
    return mapRecruitment(
      await patch<BackendRecruitment>(`/recruitments/${id}`, {
        title: data.title,
        description: data.description,
        status: toRecruitmentStatusValue(data.status),
        expiresAt: data.expired_at,
        maxParticipants: data.max_participants,
        recruitmentTagIds: data.recruitment_tag_ids,
      }),
    );
  } catch {
    return null;
  }
};

export const deleteRecruitment = async (id: number): Promise<boolean> => {
  return Boolean(await updateRecruitment(id, { status: "已删除" }));
};

export const getResponses = (
  _recruitmentId?: number,
): Promise<ResponseData[]> => Promise.resolve([]);

export const getResponsesByUserId = (
  _userId: number,
): Promise<ResponseData[]> => Promise.resolve([]);

export const createResponse = async (data: {
  recruitmentId: number;
  responserId: number;
}): Promise<ResponseData> => {
  return mapResponse(await post<BackendResponse>(`/recruitments/${data.recruitmentId}/responses`));
};

export const deleteResponse = async (id: number, _reason?: string): Promise<boolean> => {
  await post<BackendResponse>(`/responses/${id}/reject`);
  return true;
};

export const updateResponseStatus = async (
  id: number,
  responseStatus: ResponseStatus,
): Promise<ResponseData | null> => {
  try {
    const action = responseStatus === "已删除" ? "reject" : "accept";
    return mapResponse(await post<BackendResponse>(`/responses/${id}/${action}`));
  } catch {
    return null;
  }
};

export const getChats = async (_userId?: number): Promise<ChatBrief[]> => {
  try {
    const chats = await get<BackendChat[]>("/chats/me");
    const mapped = await Promise.all(chats.map(mapChat));
    return mapped.map((chat) => ({
      id: chat.id,
      otherUserAvatar: chat.otherUser.avatar,
      otherUserName: chat.otherUser.nickname || chat.otherUser.username,
      lastMessageContent: chat.lastMessage?.content ?? "",
      lastMessageAt: chat.lastMessage?.createdAt ?? "",
      createdAt: "",
    }));
  } catch {
    return [];
  }
};

export const getChatById = async (chatId: number): Promise<ChatData | null> => {
  try {
    return await mapChat(await get<BackendChat>(`/chats/${chatId}`));
  } catch {
    return null;
  }
};

export const getChatByUsers = async (userIds: number[]): Promise<ChatData | null> => {
  const otherUserId = userIds.find((id) => id !== currentUserId) ?? userIds[0];
  if (!otherUserId) return null;
  try {
    return await mapChat(await get<BackendChat>(`/chats/by-user/${otherUserId}`));
  } catch {
    return null;
  }
};

export const getChatsByRecruitmentId = async (
  recruitmentId: number,
): Promise<ChatData[]> => {
  try {
    const chats = await get<BackendChat[]>("/chats/me");
    const filtered = chats.filter(
      (chat) => normalizeNumber(prop(chat, "recruitmentId", "RecruitmentId")) === recruitmentId,
    );
    return Promise.all(filtered.map(mapChat));
  } catch {
    return [];
  }
};

export const createChat = async (data: {
  recruitmentId: number;
  user1Id: number;
  user2Id: number;
}): Promise<ChatData> => {
  await createResponse({ recruitmentId: data.recruitmentId, responserId: data.user1Id });
  const chat = await getChatByUsers([data.user1Id, data.user2Id]);
  if (!chat) throw new Error("Chat not found");
  return chat;
};

export const closeChat = (_id?: number): Promise<boolean> => Promise.resolve(false);

export const getMessagesByChatId = async (chatId: number): Promise<MessageData[]> => {
  try {
    const messages = await get<BackendMessage[]>(`/chats/${chatId}/messages`);
    return messages.map((message, index) => mapMessage(message, chatId, index));
  } catch {
    return [];
  }
};

export const sendMessage = (data: {
  chatId: number;
  senderId: number;
  receiverId: number;
  content: string;
}): Promise<MessageData> =>
  new Promise((resolve, reject) => {
    if (!accessToken) {
      reject(new Error("Authentication required"));
      return;
    }
    const url = buildUrl(`/chats/${data.chatId}/ws`)
      .replace(/^http:\/\//, "ws://")
      .replace(/^https:\/\//, "wss://");
    const socket = new (WebSocket as any)(url, undefined, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    let settled = false;

    socket.onopen = () => {
      socket.send(JSON.stringify({ content: data.content }));
    };
    socket.onmessage = (event: MessageEvent) => {
      if (settled) return;
      settled = true;
      socket.close();
      resolve(mapMessage(JSON.parse(String(event.data)), data.chatId, 0, data.receiverId));
    };
    socket.onerror = () => {
      if (settled) return;
      settled = true;
      reject(new Error("WebSocket message send failed"));
    };
    socket.onclose = () => {
      if (settled) return;
      settled = true;
      reject(new Error("WebSocket closed before message was sent"));
    };
  });

export const initTagCaches = async () => {
  await Promise.allSettled([getGameTags(), getRecruitmentTags()]);
};
