export type UserStatus = "正常" | "离线" | "封禁";
export type Gender = "其他" | "男" | "女";
export type RecruitmentStatus = "招募中" | "已关闭" | "已删除";
export type ChatStatus = "限制" | "开放";
export type ResponseStatus = "待处理" | "已接受" | "已拒绝";

export interface UserInfo {
  id: number;
  username: string;
  nickname: string;
  avatar: string;
  signature: string;
  gender: Gender;
  status: UserStatus;
  isAdmin: boolean;
  tags: { id: number; name: string }[];
}

export interface GameBrief {
  id: number;
  name: string;
  icon: string;
}

export interface GameInfo extends GameBrief {
  description: string;
  cover: string;
  tags: string[];
}

export interface GameTag {
  id: number;
  name: string;
}

export interface RecruitmentTag {
  id: number;
  name: string;
}

export interface ResponseData {
  id: number;
  recruitmentId: number;
  responserId: number;
  responseStatus: ResponseStatus;
  responser?: UserInfo;
}

export interface RecruitmentData {
  id: number;
  publisherId: number;
  gameId: number;
  gameName: string;
  gameIcon: string;
  gameCover: string;
  title: string;
  description: string;
  gameTags: string[];
  recruitmentTags: RecruitmentTag[];
  responses: ResponseData[];
  status: RecruitmentStatus;
  expiredAt: string;
  maxParticipants: number;
  currentParticipants: number;
  publisher: UserInfo;
}

export interface MessageData {
  id: number;
  senderId: number;
  content: string;
  createdAt: string;
}

export interface ChatBrief {
  id: number;
  recruitmentId: number;
  otherUserId: number;
  otherUserAvatar: string;
  otherUserName: string;
  lastMessageContent: string;
  lastMessageAt: string;
  unreadCount: number;
}

export interface ChatData extends ChatBrief {
  otherUser: UserInfo;
  lastMessage: MessageData | null;
  chatStatus: ChatStatus;
}
