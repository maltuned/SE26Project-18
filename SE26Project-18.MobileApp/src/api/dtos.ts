export enum GenderDto {
  Other = 0,
  Male = 1,
  Female = 2,
}

export enum UserStatusDto {
  Online = 0,
  Offline = 1,
  Suspended = 2,
}

export enum RecruitmentStatusDto {
  Open = 0,
  Closed = 1,
  Deleted = 2,
}

export enum ResponseTypeDto {
  Pending = 0,
  Accepted = 1,
  Rejected = 2,
}

export enum ChatStatusDto {
  Restricted = 0,
  Free = 1,
}

export interface TagResponse {
  id: number;
  name: string;
}

export interface UserResponse {
  id: number;
  username: string;
  nickname: string;
  signature: string;
  gender: GenderDto;
  status: UserStatusDto;
  isAdmin: boolean;
  tags: TagResponse[];
  avatarUrl: string;
}

export interface GameResponse {
  id: number;
  name: string;
  description: string;
  tags: TagResponse[];
  iconUrl: string;
  coverUrl: string;
}

export interface ResponseResponse {
  id: number;
  recruitmentId: number;
  responderId: number;
  type: ResponseTypeDto;
}

export interface RecruitmentResponse {
  id: number;
  game: GameResponse;
  recruiter: UserResponse;
  title: string;
  description: string;
  tags: TagResponse[];
  responses: ResponseResponse[];
  maxParticipants: number;
  currParticipants: number;
  status: RecruitmentStatusDto;
  expiresAt: string;
}

export interface MessageResponse {
  id: number;
  senderId: number;
  content: string;
  sentAt: string;
}

export interface ChatResponse {
  id: number;
  recruitmentId: number;
  user1Id: number;
  user2Id: number;
  status: ChatStatusDto;
  newMsgsCntForUser1: number;
  newMsgsCntForUser2: number;
  lastMessage: MessageResponse | null;
}

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CursorPagedResponse<T> {
  items: T[];
  nextCursor: string | null;
  hasMore: boolean;
}

export interface WebSocketTicketResponse {
  ticket: string;
  expiresAt: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}
