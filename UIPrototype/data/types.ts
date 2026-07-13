// ---- Core entities ----

export interface Game {
  id: number;
  name: string;
  coverUrl: string;
  tagline: string;
  memberCount: number;
  onlineCount: number;
  tagIds: number[];
}

export interface Tag {
  id: number;
  name: string;
  iconName: string;
  accentColor: string;
}

export type PostStatus = 'active' | 'expired';

export interface RecruitPost {
  id: number;
  gameId: number;
  gameName: string;
  tagIds: number[];
  authorId: number;
  authorName: string;
  authorAvatar: string;
  title: string;
  description: string;
  needCount: number;
  filledCount: number;
  mode: 'casual' | 'ranked' | 'tournament';
  voice: 'required' | 'optional' | 'none';
  platform: string;
  durationMinutes: number; // 30 | 1440 | 10080
  expiresAt: string;
  createdAt: string;
  status: PostStatus;
  comments: number;
}

export interface ChatSession {
  id: number;
  participantName: string;
  participantAvatar: string;
  gameName: string;
  lastMessage: string;
  lastMessageTime: string;
  unreadCount: number;
  online: boolean;
}

export interface ChatMessage {
  id: string;
  authorName: string;
  authorAvatar: string;
  text: string;
  time: string;
  isMe?: boolean;
  isSystem?: boolean;
}

export interface Report {
  id: number;
  target: string;     // e.g. "帖子：xxx" or "用户：xxx"
  reason: string;
  detail: string;
  reporterId: number;
  createdAt: string;
  handled: boolean;
}

export interface UserProfile {
  id: number;
  name: string;
  handle: string;
  avatar: string;
  squads: number;
  posts: number;
  isAdmin?: boolean;
  recentGames: string[];
  bio: string;
}
