// 前端数据结构（Data Patterns）
// 这些结构用于前端组件之间的数据传递

// ==================== Enum Types ====================

export type UserStatus = '正常' | '封禁' | '注销';
export type Gender = '男' | '女' | '其他';
export type RecruitmentStatus = '招募中' | '已关闭' | '已删除';
export type ChatStatus = '限制' | '开放' | '关闭';
export type ResponseStatus = '已回应' | '已删除';

// ==================== User ====================

export interface UserInfo {
  id: number;
  uid: number;
  username: string;
  nickname: string;
  avatar: string;
  signature: string;
  gender: Gender;
  status: UserStatus;
  createdAt: string;
  updatedAt: string;
}

// ==================== Game ====================

export interface GameBrief {
  id: number;
  name: string;
  icon: string;
}

export interface GameInfo {
  id: number;
  name: string;
  company: string;
  description: string;
  cover: string;
  icon: string;
  tags: string[];
  createdAt: string;
  updatedAt: string;
}

export interface GameTag {
  id: number;
  name: string;
}

// ==================== Recruitment ====================

export interface RecruitmentTag {
  id: number;
  name: string;
}

export interface RecruitmentData {
  id: number;
  publisherId: number;
  gameId: number;
  gameName: string;
  title: string;
  description: string;
  gameTags: string[];
  recruitmentTags: RecruitmentTag[];
  status: RecruitmentStatus;
  createdAt: string;
  updatedAt: string;
  expiredAt: string;
  maxParticipants: number;
  currentParticipants: number;
  publisher: UserInfo;
}

export interface RecruitmentInfo {
  id: number;
  gameId: number;
  gameName: string;
  title: string;
  description: string;
  gameTags: string[];
  recruitmentTags: RecruitmentTag[];
  status: RecruitmentStatus;
  createdAt: string;
}

// ==================== Response ====================

export interface ResponseData {
  id: number;
  recruitmentId: number;
  responserId: number;
  responseStatus: ResponseStatus;
  createdAt: string;
  updatedAt: string;
  responser: UserInfo;
}

// ==================== Chat ====================

export interface ChatBrief {
  id: number;
  otherUserAvatar: string;
  otherUserName: string;
  lastMessageContent: string;
  lastMessageAt: string;
  createdAt: string;
}

export interface ChatData {
  id: number;
  recruitmentId: number;
  recruitmentTitle: string;
  otherUser: UserInfo;
  lastMessage: MessageData | null;
  unreadCount: number;
  chatStatus: ChatStatus;
  newMessageAt: string;
  // For recruitment-based access (publisher/responser management)
  users?: { userId: number; sentMessage: boolean }[];
  recruitment?: RecruitmentData;
}

// ==================== Message ====================

export interface MessageData {
  id: number;
  chatId: number;
  senderId: number;
  receiverId: number;
  content: string;
  createdAt: string;
  sender: UserInfo;
  receiver: UserInfo;
}