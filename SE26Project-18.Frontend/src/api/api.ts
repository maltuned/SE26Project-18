import type {
  ApiResponse,
  ChatBriefDto,
  ChatDto,
  GameDto,
  GameTagDto,
  MessageDto,
  RecruitmentDetailDto,
  RecruitmentDto,
  RecruitmentTagDto,
  ResponseDto,
  UserDto
} from "./backend-sim";
import { backendSim } from "./backend-sim";
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
  UserInfo
} from "./data-patterns";

// Re-export frontend data patterns for convenience
export type {
  ChatBrief, ChatData, ChatStatus, GameBrief, GameInfo, GameTag, MessageData, RecruitmentData,
  RecruitmentInfo, RecruitmentTag, ResponseData,
  ResponseStatus, UserInfo
} from "./data-patterns";

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
});

// 标签缓存（启动时从后端抓取，通过id查找）
let gameTagCache: Map<number, GameTag> = new Map();
let recruitmentTagCache: Map<number, RecruitmentTag> = new Map();
let tagsInitialized = false;

const initTagCaches = () => {
  if (tagsInitialized) return;
  const gameTags = backendSim.findAllGameTags();
  gameTags.forEach((t) => gameTagCache.set(t.id, { id: t.id, name: t.name }));
  const recruitmentTags = backendSim.findAllRecruitmentTags();
  recruitmentTags.forEach((t) =>
    recruitmentTagCache.set(t.id, { id: t.id, name: t.name }),
  );
  tagsInitialized = true;
};

const getGameTagsByIds = (ids: number[]): GameTag[] => {
  initTagCaches();
  return ids
    .map((id) => gameTagCache.get(id))
    .filter((t): t is GameTag => t !== undefined);
};

const getRecruitmentTagsByIds = (ids: number[]): RecruitmentTag[] => {
  initTagCaches();
  return ids
    .map((id) => recruitmentTagCache.get(id))
    .filter((t): t is RecruitmentTag => t !== undefined);
};

const mapRecruitmentDto = (dto: RecruitmentDto): RecruitmentData => ({
  id: dto.id,
  publisherId: dto.publisher_id,
  gameId: dto.game_id,
  gameName: "",
  title: dto.title,
  description: dto.description,
  gameTags: [],
  recruitmentTags: [],
  status: dto.status,
  createdAt: dto.created_at,
  updatedAt: dto.updated_at,
  expiredAt: dto.expired_at,
  maxParticipants: dto.max_participants,
  currentParticipants: dto.current_participants,
  publisher: {} as UserInfo,
});

const mapRecruitmentDetailDto = (
  dto: RecruitmentDetailDto,
): RecruitmentData => ({
  id: dto.id,
  publisherId: dto.publisher_id,
  gameId: dto.game_id,
  gameName: dto.game.name,
  title: dto.title,
  description: dto.description,
  gameTags: getGameTagsByIds(dto.game.tags_id).map((t) => t.name),
  recruitmentTags: getRecruitmentTagsByIds(dto.tags_id),
  status: dto.status,
  createdAt: dto.created_at,
  updatedAt: dto.updated_at,
  expiredAt: dto.expired_at,
  maxParticipants: dto.max_participants,
  currentParticipants: dto.current_participants,
  publisher: mapUserDto(dto.publisher),
});

const mapResponseDto = (dto: ResponseDto): ResponseData => ({
  id: dto.id,
  recruitmentId: dto.recruitment_id,
  responserId: dto.responser_id,
  responseStatus: dto.response_status,
  createdAt: dto.created_at,
  updatedAt: dto.updated_at,
  responser: mapUserDto(dto.responser),
});

const mapGameBriefDto = (dto: GameDto): GameBrief => ({
  id: dto.id,
  name: dto.name,
  icon: dto.icon || "",
});

const mapGameDto = (dto: GameDto): GameInfo => ({
  id: dto.id,
  name: dto.name,
  company: dto.company,
  description: dto.description,
  cover: dto.cover || "",
  icon: dto.icon || "",
  tags: getGameTagsByIds(dto.tags_id).map((t) => t.name),
  createdAt: dto.created_at || "",
  updatedAt: dto.updated_at || "",
});

const mapMessageDto = (dto: MessageDto): MessageData => ({
  id: dto.id,
  chatId: dto.chat_id,
  senderId: dto.sender_id,
  receiverId: dto.receiver_id,
  content: dto.content,
  createdAt: dto.created_at,
  sender: mapUserDto(dto.sender),
  receiver: mapUserDto(dto.receiver),
});

const mapChatBriefDto = (dto: ChatBriefDto): ChatBrief => ({
  id: dto.id,
  otherUserAvatar: dto.other_user_avatar,
  otherUserName: dto.other_user_name,
  lastMessageContent: dto.last_message_content,
  lastMessageAt: dto.last_message_at,
  createdAt: dto.created_at,
});

const mapChatDto = (dto: ChatDto): ChatData => ({
  id: dto.id,
  recruitmentId: dto.recruitment_id,
  recruitmentTitle: dto.recruitment_title,
  otherUser: mapUserDto(dto.other_user),
  lastMessage: dto.last_message ? mapMessageDto(dto.last_message) : null,
  unreadCount: dto.unread_count,
  chatStatus: dto.chat_status,
  newMessageAt: dto.new_message_at,
  users: dto.users?.map((u) => ({
    userId: u.user_id,
    sentMessage: u.sent_message,
  })),
  recruitment: dto.recruitment ? mapRecruitmentDto(dto.recruitment) : undefined,
});

// ==================== Simulated Async API Calls ====================

const simulateAsync = <T>(data: T): Promise<T> => {
  return new Promise((resolve) => resolve(data));
};

const handleResponse = <T>(
  response: ApiResponse,
  mapper?: (data: any) => T,
): Promise<T | null> => {
  if (response.status >= 200 && response.status < 300) {
    return simulateAsync(mapper ? mapper(response.data) : response.data);
  }
  console.warn(`API Error [${response.status}]: ${response.message}`);
  return simulateAsync(null);
};

const handleArrayResponse = <T>(
  response: ApiResponse,
  mapper: (data: any) => T[],
): Promise<T[]> => {
  if (response.status >= 200 && response.status < 300) {
    return simulateAsync(mapper(response.data));
  }
  console.warn(`API Error [${response.status}]: ${response.message}`);
  return simulateAsync([]);
};

const handlePostResponse = <T>(
  response: ApiResponse,
  mapper: (data: any) => T,
): Promise<T> => {
  if (response.status >= 200 && response.status < 300) {
    return simulateAsync(mapper(response.data));
  }
  throw new Error(`API Error [${response.status}]: ${response.message}`);
};

// ==================== User API ====================

export const login = (
  username: string,
  password: string,
): Promise<UserInfo | null> => {
  const response = backendSim.post("/users/login", { username, password });
  return handleResponse<UserInfo>(response, mapUserDto);
};

export const getUserById = (id: number): Promise<UserInfo | null> => {
  const response = backendSim.get("/users/by-id", { id });
  return handleResponse<UserInfo>(response, mapUserDto);
};

export const getUsers = (): Promise<UserInfo[]> => {
  const response = backendSim.get("/users");
  return handleArrayResponse<UserInfo>(response, (data: UserDto[]) =>
    data.map(mapUserDto),
  );
};

// ==================== Game API ====================

export const getGames = (query: string = ""): Promise<GameBrief[]> => {
  const response = backendSim.get("/games", { query });
  return handleArrayResponse<GameBrief>(response, (data: GameDto[]) =>
    data.map(mapGameBriefDto),
  );
};

export const getGameById = (id: number): Promise<GameInfo | null> => {
  const response = backendSim.get("/games/by-id", { id });
  return handleResponse<GameInfo>(response, (dto: GameDto) => mapGameDto(dto));
};

// ==================== Tag API ====================

export const getGameTags = (): Promise<GameTag[]> => {
  const response = backendSim.get("/game-tags");
  return handleArrayResponse<GameTag>(response, (data: GameTagDto[]) => data);
};

export const getRecruitmentTags = (): Promise<RecruitmentTag[]> => {
  const response = backendSim.get("/recruitment-tags");
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
  const response = backendSim.get("/recruitments", {
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
  const response = backendSim.get("/recruitments/by-game", { gameId });
  return handleArrayResponse<RecruitmentData>(
    response,
    (data: RecruitmentDetailDto[]) => data.map(mapRecruitmentDetailDto),
  );
};

export const getRecruitmentById = (
  id: number,
): Promise<RecruitmentData | null> => {
  const response = backendSim.get("/recruitments/by-id", { id });
  return handleResponse<RecruitmentData>(response, mapRecruitmentDetailDto);
};

export const getRecruitmentsByPublisherId = (
  publisherId: number | null,
): Promise<RecruitmentData[]> => {
  if (publisherId === null) return getRecruitments();
  const response = backendSim.get("/recruitments/by-publisher", {
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
  const endpoint = isNew ? "/recruitments" : "/recruitments/update";
  const payload = isNew
    ? {
        publisher_id: data.publisherId,
        game_id: data.gameId,
        title: data.title,
        description: data.description,
        status: data.status,
        expired_at: data.expiredAt,
        max_participants: data.maxParticipants,
        current_participants: data.currentParticipants,
        tags_id: data.tagsId,
      }
    : {
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
  const response = backendSim.post(endpoint, payload);
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
  const response = backendSim.post("/recruitments", payload);
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
  const response = backendSim.post("/recruitments/update", { id, data });
  return handleResponse<RecruitmentData>(response, mapRecruitmentDetailDto);
};

export const deleteRecruitment = (id: number): Promise<boolean> => {
  const response = backendSim.post("/recruitments/delete", { id });
  return handlePostResponse(response, (data: boolean) => data);
};

// ==================== Response API ====================

export const getResponses = (
  recruitmentId?: number,
): Promise<ResponseData[]> => {
  if (recruitmentId === undefined) {
    const allResponses: ResponseData[] = [];
    const recruitments = backendSim.findRecruitments();
    for (const r of recruitments) {
      const responses = backendSim.findResponsesByRecruitmentId(r.id);
      allResponses.push(...responses.map(mapResponseDto));
    }
    return simulateAsync(allResponses);
  }
  const response = backendSim.get("/responses/by-recruitment", {
    recruitmentId,
  });
  return handleArrayResponse<ResponseData>(response, (data: ResponseDto[]) =>
    data.map(mapResponseDto),
  );
};

export const getResponsesByUserId = (
  userId: number,
): Promise<ResponseData[]> => {
  const response = backendSim.get("/responses/by-user", { userId });
  return handleArrayResponse<ResponseData>(response, (data: ResponseDto[]) =>
    data.map(mapResponseDto),
  );
};

export const createResponse = (data: {
  recruitmentId: number;
  responserId: number;
}): Promise<ResponseData> => {
  const response = backendSim.post("/responses", {
    recruitment_id: data.recruitmentId,
    responser_id: data.responserId,
  });
  return handlePostResponse(response, mapResponseDto);
};

export const deleteResponse = (
  id: number,
  reason: string,
): Promise<boolean> => {
  const response = backendSim.post("/responses/delete", { id, reason });
  return handlePostResponse(response, (data: boolean) => data);
};

export const updateResponseStatus = (
  id: number,
  responseStatus: ResponseStatus,
): Promise<ResponseData | null> => {
  const response = backendSim.post("/responses/status", {
    id,
    response_status: responseStatus,
  });
  return handleResponse<ResponseData>(response, mapResponseDto);
};

// ==================== Chat API ====================

export const getChats = (userId: number): Promise<ChatBrief[]> => {
  const response = backendSim.get("/chats/by-user", { userId });
  return handleArrayResponse<ChatBrief>(response, (data: ChatBriefDto[]) =>
    data.map(mapChatBriefDto),
  );
};

export const getChatById = (chatId: number): Promise<ChatData | null> => {
  const response = backendSim.get("/chats/by-id", { chatId });
  return handleResponse<ChatData>(response, mapChatDto);
};

export const getChatsByRecruitmentId = (
  recruitmentId: number,
): Promise<ChatData[]> => {
  const response = backendSim.get("/chats/by-recruitment", { recruitmentId });
  return handleArrayResponse<ChatData>(response, (data: ChatDto[]) =>
    data.map(mapChatDto),
  );
};

export const createChat = (data: {
  recruitmentId: number;
  user1Id: number;
  user2Id: number;
}): Promise<ChatData> => {
  const response = backendSim.post("/chats/create", {
    recruitment_id: data.recruitmentId,
    user1_id: data.user1Id,
    user2_id: data.user2Id,
  });
  return handlePostResponse(response, mapChatDto);
};

// ==================== Message API ====================

export const getMessagesByChatId = (chatId: number): Promise<MessageData[]> => {
  const response = backendSim.get("/messages/by-chat", { chatId });
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
  const response = backendSim.post("/messages", {
    chat_id: data.chatId,
    sender_id: data.senderId,
    receiver_id: data.receiverId,
    content: data.content,
  });
  return handlePostResponse(response, mapMessageDto);
};