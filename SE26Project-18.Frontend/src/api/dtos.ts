// 后端 DTO 结构（Data Transfer Objects）
// 这些结构用于后端与 api.ts 之间的数据传输

// ==================== Auth ====================

export interface TokenResponse {
  access_token: string;
  refresh_token: string;
  access_token_expires_at: string;
  refresh_token_expires_at: string;
}

// ==================== Enum Types ====================

export type UserStatus = '正常' | '封禁' | '注销';
export type Gender = '男' | '女' | '其他';
export type RecruitmentStatus = '招募中' | '已关闭' | '已删除';
export type ChatStatus = '限制' | '开放' | '关闭';
export type ResponseStatus = '已回应' | '已删除';

// ==================== User ====================

export interface UserBriefDto {
  id: number;
  nickname: string;
  username: string;
  avatar: string;
}

export interface UserDto {
  id: number;
  uid: number;
  username: string;
  nickname: string;
  avatar: string;
  signature: string;
  gender: Gender;
  status: UserStatus;
  created_at: string;
  updated_at: string;
}

// ==================== Game ====================

export interface GameBriefDto {
  id: number;
  name: string;
  cover: string;
  icon: string;
}

export interface GameDto {
  id: number;
  name: string;
  company: string;
  description: string;
  cover: string;
  icon: string;
  tags_id: number[];
  created_at: string;
  updated_at: string;
}

export interface GameTagDto {
  id: number;
  name: string;
}

// ==================== Recruitment ====================

export interface RecruitmentTagDto {
  id: number;
  name: string;
}

export interface RecruitmentBriefDto {
  id: number;
  title: string;
  game: GameBriefDto;
}

export interface RecruitmentDto {
  id: number;
  publisher_id: number;
  game_id: number;
  title: string;
  description: string;
  status: RecruitmentStatus;
  tags_id: number[];
  created_at: string;
  updated_at: string;
  expired_at: string;
  max_participants: number;
  current_participants: number;
}

export interface RecruitmentDetailDto extends RecruitmentDto {
  publisher: UserBriefDto;
  game: GameBriefDto;
  gameTags: GameTagDto[];
  recruitmentTags: RecruitmentTagDto[];
}

// ==================== Response ====================

export interface ResponseDto {
  id: number;
  recruitment_id: number;
  responser_id: number;
  response_status: ResponseStatus;
  created_at: string;
  updated_at: string;
  responser: UserBriefDto;
}

// ==================== Chat ====================

export interface ChatBriefDto {
  id: number;
  other_user_avatar: string;
  other_user_name: string;
  last_message_content: string;
  last_message_at: string;
  created_at: string;
}

export interface ChatUserDto {
  user_id: number;
  sent_message: boolean;
}

export interface ChatDto {
  id: number;
  recruitment_id: number;
  recruitment_title: string;
  other_user: UserBriefDto;
  last_message: MessageDto | null;
  unread_count: number;
  chat_status: ChatStatus;
  new_message_at: string;
  // For recruitment-based access
  users?: ChatUserDto[];
  recruitment?: RecruitmentBriefDto;
}

export interface MessageDto {
  id: number;
  chat_id: number;
  sender_id: number;
  receiver_id: number;
  content: string;
  created_at: string;
  sender: UserBriefDto;
  receiver: UserBriefDto;
}

// ==================== API Response ====================

export interface ApiResponse<T = any> {
  status: number;
  data: T;
  message: string;
}