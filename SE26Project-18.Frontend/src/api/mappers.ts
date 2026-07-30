import type {
  ChatBriefDto,
  ChatDto,
  GameBriefDto,
  GameDto,
  GameTagDto,
  MessageDto,
  NotificationDto,
  RecruitmentBriefDto,
  RecruitmentDetailDto,
  RecruitmentDto,
  RecruitmentTagDto,
  ResponseDto,
  ReviewDto,
  UserBriefDto,
  UserDto,
} from "./dtos";
import type {
  ChatBrief,
  ChatData,
  GameBrief,
  GameInfo,
  GameTag,
  MessageData,
  NotificationItem,
  RecruitmentBrief,
  RecruitmentData,
  RecruitmentTag,
  ResponseData,
  ReviewData,
  UserInfo,
} from "./data-patterns";

// ==================== Tag Cache ====================

let gameTagCache: Map<number, GameTag> = new Map();
let recruitmentTagCache: Map<number, RecruitmentTag> = new Map();
let tagsInitialized = false;

export const initTagCaches = async (): Promise<void> => {
  if (tagsInitialized) return;
  try {
    const { apiGet } = await import("./fetch");
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

// ==================== User Mappers ====================

export const mapUserDto = (dto: UserDto): UserInfo => ({
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
  settings: dto.settings
    ? {
        pushEnabled: dto.settings.push_enabled,
        profileVisible: dto.settings.profile_visible,
        darkMode: dto.settings.dark_mode,
      }
    : undefined,
});

export const mapUserBriefDto = (dto: UserBriefDto): UserInfo => ({
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

// ==================== Game Mappers ====================

export const mapGameBriefDto = (dto: GameBriefDto): GameBrief => ({
  id: dto.id,
  name: dto.name,
  nameEn: dto.name_en || "",
  icon: dto.icon || "",
});

export const mapGameDto = (dto: GameDto): GameInfo => ({
  id: dto.id,
  name: dto.name,
  nameEn: dto.name_en || "",
  aliases: dto.aliases || "",
  company: dto.company,
  description: dto.description,
  cover: dto.cover || "",
  icon: dto.icon || "",
  tags: getGameTagsByIds(dto.tags_id).map((t) => t.name),
  createdAt: dto.created_at || "",
  updatedAt: dto.updated_at || "",
});

// ==================== Recruitment Mappers ====================

export const mapRecruitmentDto = (dto: RecruitmentDto): RecruitmentData => ({
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

export const mapRecruitmentDetailDto = (dto: RecruitmentDetailDto): RecruitmentData => ({
  id: dto.id,
  publisherId: dto.publisher_id,
  gameId: dto.game_id ?? 0,
  gameName: dto.game?.name || dto.game_name || "",
  gameCover: dto.game?.cover || "",
  gameIcon: dto.game?.icon || "",
  title: dto.title,
  description: dto.description,
  gameTags: dto.gameTags.map((t) => t.name),
  recruitmentTags: dto.recruitmentTags.map((t) => ({ id: t.id, name: t.name })),
  status: dto.status,
  createdAt: dto.created_at,
  updatedAt: dto.updated_at,
  expiredAt: dto.expired_at,
  maxParticipants: dto.max_participants,
  currentParticipants: dto.current_participants,
  publisher: mapUserBriefDto(dto.publisher),
});

export const mapRecruitmentBriefDto = (dto: RecruitmentBriefDto): RecruitmentBrief => ({
  id: dto.id,
  title: dto.title,
  game: dto.game
    ? { id: dto.game.id, name: dto.game.name, nameEn: dto.game.name_en || "", icon: dto.game.icon }
    : { id: 0, name: dto.game_name || "(已删除)", nameEn: "", icon: "" },
});

// ==================== Response Mapper ====================

export const mapResponseDto = (dto: ResponseDto): ResponseData => ({
  id: dto.id,
  recruitmentId: dto.recruitment_id,
  responserId: dto.responser_id,
  responseStatus: dto.response_status,
  createdAt: dto.created_at,
  updatedAt: dto.updated_at,
  responser: mapUserBriefDto(dto.responser),
});

// ==================== Chat Mappers ====================

export const mapChatBriefDto = (dto: ChatBriefDto): ChatBrief => ({
  id: dto.id,
  otherUserAvatar: dto.other_user_avatar,
  otherUserName: dto.other_user_name,
  lastMessageContent: dto.last_message_content,
  lastMessageAt: dto.last_message_at,
  unreadCount: dto.unread_count,
  createdAt: dto.created_at,
});

export const mapChatDto = (dto: ChatDto): ChatData => ({
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

// ==================== Message Mapper ====================

export const mapMessageDto = (dto: MessageDto): MessageData => ({
  id: dto.id,
  chatId: dto.chat_id,
  senderId: dto.sender_id,
  receiverId: dto.receiver_id,
  content: dto.content,
  createdAt: dto.created_at,
  sender: mapUserBriefDto(dto.sender),
  receiver: mapUserBriefDto(dto.receiver),
});

// ==================== Notification Mapper ====================

export const mapNotificationDto = (dto: NotificationDto): NotificationItem => ({
  id: dto.id,
  title: dto.title,
  body: dto.body,
  isRead: dto.is_read,
  createdAt: dto.created_at,
});

// ==================== Review Mapper ====================

export const mapReviewDto = (dto: ReviewDto): ReviewData => ({
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