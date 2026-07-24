import dataSrc from './data.json';
import type {
  UserDto, GameDto, GameTagDto, RecruitmentTagDto,
  RecruitmentDto, RecruitmentDetailDto, ResponseDto,
  ChatDto, ChatBriefDto, MessageDto, ChatUserDto,
  ApiResponse, UserStatus, Gender, RecruitmentStatus, ChatStatus, ResponseStatus,
} from './dtos';

// Re-export DTOs for api.ts
export {
  UserDto, GameDto, GameTagDto, RecruitmentTagDto,
  RecruitmentDto, RecruitmentDetailDto, ResponseDto,
  ChatDto, ChatBriefDto, MessageDto, ChatUserDto,
  ApiResponse, UserStatus, Gender, RecruitmentStatus, ChatStatus, ResponseStatus,
};

// ==================== Entity Interfaces (Backend Schema) ====================

interface UserEntity {
  id: number;
  uid: number;
  username: string;
  phone_number: string;
  password: string;
  nickname: string;
  avatar: string;
  signature: string;
  gender: Gender;
  status: UserStatus;
  created_at: string;
  updated_at: string;
}

interface GameEntity {
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

interface GameTagEntity {
  id: number;
  name: string;
}

interface RecruitmentTagEntity {
  id: number;
  name: string;
}

interface RecruitmentEntity {
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

interface ResponseEntity {
  id: number;
  recruitment_id: number;
  responser_id: number;
  response_status: ResponseStatus;
  created_at: string;
  updated_at: string;
}

interface ChatUserEntity {
  user_id: number;
  sent_message: boolean;
}

interface ChatEntity {
  id: number;
  users: ChatUserEntity[];
  recruitment_id: number;
  chat_status: ChatStatus;
  created_at: string;
  updated_at: string;
  new_message_at: string;
}

interface MessageEntity {
  id: number;
  chat_id: number;
  sender_id: number;
  receiver_id: number;
  content: string;
  created_at: string;
}

interface ApiRequest {
  method: 'GET' | 'POST' | 'PUT' | 'DELETE';
  endpoint: string;
  payload?: any;
}

// ==================== BackendSim ====================

class BackendSim {
  private users: UserEntity[] = dataSrc.users as UserEntity[];
  private games: GameEntity[] = dataSrc.games as GameEntity[];
  private gameTags: GameTagEntity[] = dataSrc.gameTags as GameTagEntity[];
  private recruitmentTags: RecruitmentTagEntity[] = dataSrc.recruitmentTags as RecruitmentTagEntity[];
  private recruitments: RecruitmentEntity[] = dataSrc.recruitments as RecruitmentEntity[];
  private responses: ResponseEntity[] = dataSrc.responses as ResponseEntity[];
  private chats: ChatEntity[] = (dataSrc as any).chats as ChatEntity[] || [];
  private messages: MessageEntity[] = dataSrc.messages as MessageEntity[];
  private requestLog: ApiRequest[] = [];
  private apiLogs: { timestamp: string; request: { method: string; endpoint: string; payload: any }; response: { status: number; data: any; message: string } }[] = [];

  // 标签缓存（启动时加载，通过id快速查找）
  private gameTagCache: Map<number, GameTagEntity> = new Map();
  private recruitmentTagCache: Map<number, RecruitmentTagEntity> = new Map();

  constructor() {
    // 启动时抓取标签到缓存
    this.gameTags.forEach(t => this.gameTagCache.set(t.id, t));
    this.recruitmentTags.forEach(t => this.recruitmentTagCache.set(t.id, t));
  }

  private getGameTagById(id: number): GameTagEntity | undefined {
    return this.gameTagCache.get(id);
  }

  private getRecruitmentTagById(id: number): RecruitmentTagEntity | undefined {
    return this.recruitmentTagCache.get(id);
  }

  private getGameTagsByIds(ids: number[]): GameTagDto[] {
    return ids
      .map(id => this.getGameTagById(id))
      .filter((t): t is GameTagEntity => t !== undefined)
      .map(t => this.toGameTagDto(t));
  }

  private getRecruitmentTagsByIds(ids: number[]): RecruitmentTagDto[] {
    return ids
      .map(id => this.getRecruitmentTagById(id))
      .filter((t): t is RecruitmentTagEntity => t !== undefined)
      .map(t => this.toRecruitmentTagDto(t));
  }

  private appendLog(entry: { timestamp: string; request: { method: string; endpoint: string; payload: any }; response: { status: number; data: any; message: string } }) {
    this.apiLogs.push(entry);
    console.log(
      `\n[API Log] ${entry.timestamp}\n` +
      `→ ${entry.request.method} ${entry.request.endpoint}\n` +
      `  Payload: ${JSON.stringify(entry.request.payload, null, 2)}\n` +
      `← ${entry.response.status} ${entry.response.message}\n` +
      `  Data: ${JSON.stringify(entry.response.data, null, 2).substring(0, 500)}${JSON.stringify(entry.response.data).length > 500 ? '...(truncated)' : ''}`
    );
  }

  getApiLogs() {
    return [...this.apiLogs];
  }

  // --- Generic Request Handler ---

  post(endpoint: string, payload: any): ApiResponse {
    const request: ApiRequest = { method: 'POST', endpoint, payload };
    this.requestLog.push(request);

    try {
      let response: ApiResponse;
      switch (endpoint) {
        case '/recruitments':
          response = this.handleCreateRecruitment(payload);
          break;
        case '/recruitments/update':
          response = this.handleUpdateRecruitment(payload);
          break;
        case '/recruitments/delete':
          response = this.handleDeleteRecruitment(payload);
          break;
        case '/responses':
          response = this.handleCreateResponse(payload);
          break;
        case '/responses/status':
          response = this.handleUpdateResponseStatus(payload);
          break;
        case '/responses/delete':
          response = this.handleDeleteResponse(payload);
          break;
        case '/chats/create':
          response = this.handleCreateChat(payload);
          break;
        case '/messages':
          response = this.handleCreateMessage(payload);
          break;
        case '/users/login':
          response = this.handleLogin(payload);
          break;
        default:
          response = { status: 404, data: null, message: `Endpoint ${endpoint} not found` };
      }
      this.appendLog({
        timestamp: new Date().toISOString(),
        request: { method: 'POST', endpoint, payload },
        response: { status: response.status, data: response.data, message: response.message },
      });
      return response;
    } catch (error) {
      const errResponse: ApiResponse = { status: 500, data: null, message: 'Internal server error' };
      this.appendLog({
        timestamp: new Date().toISOString(),
        request: { method: 'POST', endpoint, payload },
        response: { status: 500, data: null, message: 'Internal server error' },
      });
      return errResponse;
    }
  }

  get(endpoint: string, params?: any): ApiResponse {
    const request: ApiRequest = { method: 'GET', endpoint, payload: params };
    this.requestLog.push(request);

    try {
      let response: ApiResponse;
      switch (endpoint) {
        case '/users':
          response = { status: 200, data: this.findAllUsers(), message: 'OK' };
          break;
        case '/users/by-id':
          response = { status: 200, data: this.findUserById(params.id), message: 'OK' };
          break;
        case '/games':
          response = { status: 200, data: this.findGames(params.query), message: 'OK' };
          break;
        case '/games/by-id':
          response = { status: 200, data: this.findGameById(params.id), message: 'OK' };
          break;
        case '/game-tags':
          response = { status: 200, data: this.findAllGameTags(), message: 'OK' };
          break;
        case '/recruitment-tags':
          response = { status: 200, data: this.findAllRecruitmentTags(), message: 'OK' };
          break;
        case '/recruitments':
          response = { status: 200, data: this.findRecruitments(params.gameName, params.gameTags, params.recruitmentTags), message: 'OK' };
          break;
        case '/recruitments/by-id':
          response = { status: 200, data: this.findRecruitmentById(params.id), message: 'OK' };
          break;
        case '/recruitments/by-publisher':
          response = { status: 200, data: this.findRecruitmentsByPublisherId(params.publisherId), message: 'OK' };
          break;
        case '/recruitments/by-game':
          response = { status: 200, data: this.findRecruitmentsByGameId(params.gameId), message: 'OK' };
          break;
        case '/responses/by-recruitment':
          response = { status: 200, data: this.findResponsesByRecruitmentId(params.recruitmentId), message: 'OK' };
          break;
        case '/responses/by-user':
          response = { status: 200, data: this.findResponsesByUserId(params.userId), message: 'OK' };
          break;
        case '/chats/by-user':
          response = { status: 200, data: this.findChatEntriesByUserId(params.userId), message: 'OK' };
          break;
        case '/chats/by-id':
          response = { status: 200, data: this.findChatById(params.chatId), message: 'OK' };
          break;
        case '/chats/by-recruitment':
          response = { status: 200, data: this.findChatsByRecruitmentId(params.recruitmentId), message: 'OK' };
          break;
        case '/messages/by-chat':
          response = { status: 200, data: this.findMessagesByChatId(params.chatId), message: 'OK' };
          break;
        default:
          response = { status: 404, data: null, message: `Endpoint ${endpoint} not found` };
      }
      this.appendLog({
        timestamp: new Date().toISOString(),
        request: { method: 'GET', endpoint, payload: params },
        response: { status: response.status, data: response.data, message: response.message },
      });
      return response;
    } catch (error) {
      const errResponse: ApiResponse = { status: 500, data: null, message: 'Internal server error' };
      this.appendLog({
        timestamp: new Date().toISOString(),
        request: { method: 'GET', endpoint, payload: params },
        response: { status: 500, data: null, message: 'Internal server error' },
      });
      return errResponse;
    }
  }

  getRequestLog(): ApiRequest[] {
    return [...this.requestLog];
  }

  clearRequestLog(): void {
    this.requestLog = [];
  }

  // --- POST Handlers ---

  private handleLogin(payload: { username: string; password: string }): ApiResponse {
    const user = this.users.find(u => u.username === payload.username && u.password === payload.password);
    if (!user) {
      return { status: 401, data: null, message: 'Invalid credentials' };
    }
    return { status: 200, data: this.toUserDto(user), message: 'Login successful' };
  }

  private handleCreateRecruitment(payload: Omit<RecruitmentEntity, 'id' | 'created_at' | 'updated_at'>): ApiResponse {
    const newId = Math.max(...this.recruitments.map(r => r.id), 0) + 1;
    const now = new Date().toISOString();
    const entity: RecruitmentEntity = {
      ...payload,
      id: newId,
      tags_id: payload.tags_id || [],
      created_at: now,
      updated_at: now,
    };
    this.recruitments.push(entity);
    return { status: 201, data: this.toRecruitmentDetailDto(entity), message: 'Recruitment created' };
  }

  private handleUpdateRecruitment(payload: { id: number; data: Partial<RecruitmentEntity> }): ApiResponse {
    const index = this.recruitments.findIndex(r => r.id === payload.id);
    if (index === -1) {
      return { status: 404, data: null, message: 'Recruitment not found' };
    }
    this.recruitments[index] = {
      ...this.recruitments[index],
      ...payload.data,
      updated_at: new Date().toISOString(),
    };
    return { status: 200, data: this.toRecruitmentDetailDto(this.recruitments[index]), message: 'Recruitment updated' };
  }

  private handleDeleteRecruitment(payload: { id: number }): ApiResponse {
    const recruitment = this.recruitments.find(r => r.id === payload.id);
    if (!recruitment) {
      return { status: 404, data: null, message: 'Recruitment not found' };
    }
    recruitment.status = '已删除';
    recruitment.updated_at = new Date().toISOString();
    return { status: 200, data: true, message: 'Recruitment deleted' };
  }

  private handleCreateResponse(payload: { recruitment_id: number; responser_id: number }): ApiResponse {
    // Check if response already exists
    const existing = this.responses.find(r =>
      r.recruitment_id === payload.recruitment_id && r.responser_id === payload.responser_id
    );
    if (existing) {
      return { status: 400, data: null, message: 'Response already exists' };
    }

    const newId = Math.max(...this.responses.map(r => r.id), 0) + 1;
    const now = new Date().toISOString();
    const entity: ResponseEntity = {
      id: newId,
      recruitment_id: payload.recruitment_id,
      responser_id: payload.responser_id,
      response_status: '已回应',
      created_at: now,
      updated_at: now,
    };
    this.responses.push(entity);
    return { status: 201, data: this.toResponseDto(entity), message: 'Response created' };
  }

  private handleUpdateResponseStatus(payload: { id: number; response_status: ResponseStatus }): ApiResponse {
    const index = this.responses.findIndex(r => r.id === payload.id);
    if (index === -1) {
      return { status: 404, data: null, message: 'Response not found' };
    }
    this.responses[index] = {
      ...this.responses[index],
      response_status: payload.response_status,
      updated_at: new Date().toISOString(),
    };
    return { status: 200, data: this.toResponseDto(this.responses[index]), message: 'Response status updated' };
  }

  private handleDeleteResponse(payload: { id: number; reason: string }): ApiResponse {
    const index = this.responses.findIndex(r => r.id === payload.id);
    if (index === -1) {
      return { status: 404, data: false, message: 'Response not found' };
    }

    const response = this.responses[index];
    const recruitment = this.recruitments.find(r => r.id === response.recruitment_id);
    if (!recruitment) {
      return { status: 404, data: false, message: 'Recruitment not found' };
    }

    const publisherId = recruitment.publisher_id;
    const responserId = response.responser_id;

    // Delete the response (soft delete: set status to '已删除')
    this.responses[index] = {
      ...this.responses[index],
      response_status: '已删除',
      updated_at: new Date().toISOString(),
    };

    // Find or create chat between publisher and responser
    let chat = this.chats.find(c =>
      c.recruitment_id === response.recruitment_id &&
      c.users.some(u => u.user_id === publisherId) &&
      c.users.some(u => u.user_id === responserId)
    );

    if (!chat) {
      const newChatId = this.chats.length > 0 ? Math.max(...this.chats.map(c => c.id)) + 1 : 1;
      const now = new Date().toISOString();
      chat = {
        id: newChatId,
        users: [
          { user_id: publisherId, sent_message: false },
          { user_id: responserId, sent_message: false },
        ],
        recruitment_id: response.recruitment_id,
        chat_status: '限制',
        created_at: now,
        updated_at: now,
        new_message_at: now,
      };
      this.chats.push(chat);
    }

    // Send rejection message from publisher to responser
    const messageContent = `回应已拒绝（原因：${payload.reason || '无'}）`;
    const newId = this.messages.length > 0 ? Math.max(...this.messages.map(m => m.id)) + 1 : 1;
    const messageEntity: MessageEntity = {
      id: newId,
      chat_id: chat.id,
      sender_id: publisherId,
      receiver_id: responserId,
      content: messageContent,
      created_at: new Date().toISOString(),
    };
    this.messages.push(messageEntity);

    // Update publisher's sent_message status
    const publisherChatUser = chat.users.find(u => u.user_id === publisherId);
    if (publisherChatUser) {
      publisherChatUser.sent_message = true;
    }

    // Check if all users have sent messages
    const allSent = chat.users.every(u => u.sent_message);
    if (allSent && chat.chat_status === '限制') {
      chat.chat_status = '开放';
    }

    chat.updated_at = new Date().toISOString();
    chat.new_message_at = new Date().toISOString();

    return { status: 200, data: true, message: 'Response deleted and rejection message sent' };
  }

  private handleCreateChat(payload: { recruitment_id: number; user1_id: number; user2_id: number }): ApiResponse {
    const recruitment = this.recruitments.find(r => r.id === payload.recruitment_id);
    if (!recruitment || recruitment.status === '已关闭' || recruitment.status === '已删除') {
      return { status: 400, data: null, message: 'Recruitment is not available' };
    }

    // Find existing chat between the two users (regardless of recruitment_id)
    let chat = this.chats.find(c =>
      c.users.some(u => u.user_id === payload.user1_id) &&
      c.users.some(u => u.user_id === payload.user2_id)
    );

    if (chat) {
      // Update recruitment_id
      chat.recruitment_id = payload.recruitment_id;
      chat.updated_at = new Date().toISOString();
    } else {
      const newChatId = this.chats.length > 0 ? Math.max(...this.chats.map(c => c.id)) + 1 : 1;
      const now = new Date().toISOString();
      chat = {
        id: newChatId,
        users: [
          { user_id: payload.user1_id, sent_message: false },
          { user_id: payload.user2_id, sent_message: false },
        ],
        recruitment_id: payload.recruitment_id,
        chat_status: '限制',
        created_at: now,
        updated_at: now,
        new_message_at: now,
      };
      this.chats.push(chat);
    }

    return { status: 200, data: this.toChatDto(chat), message: chat.recruitment_id === payload.recruitment_id ? 'Chat created' : 'Chat updated' };
  }

  private handleCreateMessage(payload: { chat_id: number; sender_id: number; receiver_id: number; content: string }): ApiResponse {
    const chat = this.chats.find(c => c.id === payload.chat_id);
    if (!chat) {
      return { status: 404, data: null, message: 'Chat not found' };
    }

    // Validate sender is part of this chat
    const senderChatUser = chat.users.find(u => u.user_id === payload.sender_id);
    if (!senderChatUser) {
      return { status: 403, data: null, message: 'Sender is not part of this chat' };
    }

    // If chat is closed, reject message
    if (chat.chat_status === '关闭') {
      return { status: 403, data: null, message: 'Cannot send message in a closed chat' };
    }

    // If chat is restricted and sender already sent a message, reject
    if (chat.chat_status === '限制' && senderChatUser.sent_message) {
      return { status: 403, data: null, message: 'Waiting for the other user to reply' };
    }

    // Create message
    const newId = this.messages.length > 0 ? Math.max(...this.messages.map(m => m.id)) + 1 : 1;
    const entity: MessageEntity = {
      id: newId,
      chat_id: chat.id,
      sender_id: payload.sender_id,
      receiver_id: payload.receiver_id,
      content: payload.content,
      created_at: new Date().toISOString(),
    };
    this.messages.push(entity);

    // Update sender's sent_message status
    senderChatUser.sent_message = true;

    // Check if all users have sent messages, if so, update chat_status to 开放
    const allSent = chat.users.every(u => u.sent_message);
    if (allSent && chat.chat_status === '限制') {
      chat.chat_status = '开放';
    }

    chat.new_message_at = entity.created_at;
    chat.updated_at = entity.created_at;

    return {
      status: 201,
      data: this.toMessageDto(entity),
      message: 'Message sent',
    };
  }

  private handleCreateGreeting(payload: { recruitment_id: number; publisher_id: number; responser_id: number; content: string }): ApiResponse {
    const recruitment = this.recruitments.find(r => r.id === payload.recruitment_id);
    if (!recruitment || recruitment.status === '已关闭' || recruitment.status === '已删除') {
      return { status: 400, data: null, message: 'Recruitment is not available' };
    }

    // Find or create chat for this recruitment between publisher and responser
    let chat = this.chats.find(c =>
      c.recruitment_id === payload.recruitment_id &&
      c.users.some(u => u.user_id === payload.publisher_id) &&
      c.users.some(u => u.user_id === payload.responser_id)
    );

    if (!chat) {
      // Create new chat
      const newChatId = this.chats.length > 0 ? Math.max(...this.chats.map(c => c.id)) + 1 : 1;
      const now = new Date().toISOString();
      const newChat: ChatEntity = {
        id: newChatId,
        users: [
          { user_id: payload.publisher_id, sent_message: false },
          { user_id: payload.responser_id, sent_message: false },
        ],
        recruitment_id: payload.recruitment_id,
        chat_status: '限制',
        created_at: now,
        updated_at: now,
        new_message_at: now,
      };
      this.chats.push(newChat);
      chat = newChat;
    }

    // Send the greeting message
    return this.handleCreateMessage({
      chat_id: chat.id,
      sender_id: payload.responser_id,
      receiver_id: payload.publisher_id,
      content: payload.content,
    });
  }

  // --- User ---

  findAllUsers(): UserDto[] {
    return this.users.map(u => this.toUserDto(u));
  }

  findUserById(id: number): UserDto | null {
    const user = this.users.find(u => u.id === id);
    return user ? this.toUserDto(user) : null;
  }

  // --- Game ---

  findGames(query?: string): GameDto[] {
    if (!query || query.trim() === '') {
      return this.games.slice(0, 5).map(g => this.toGameDto(g));
    }
    return this.games
      .filter(g => g.name.toLowerCase().includes(query.toLowerCase()))
      .slice(0, 5)
      .map(g => this.toGameDto(g));
  }

  findGameById(id: number): GameDto | null {
    const game = this.games.find(g => g.id === id);
    return game ? this.toGameDto(game) : null;
  }

  searchGames(query: string): GameDto[] {
    return this.games
      .filter(g => g.name.toLowerCase().includes(query.toLowerCase()))
      .slice(0, 5)
      .map(g => this.toGameDto(g));
  }

  // --- Game Tag ---

  findAllGameTags(): GameTagDto[] {
    return this.gameTags.map(t => this.toGameTagDto(t));
  }

  findGameTagsByGameId(gameId: number): GameTagDto[] {
    const game = this.games.find(g => g.id === gameId);
    if (!game) return [];
    return this.getGameTagsByIds(game.tags_id);
  }

  // --- Recruitment Tag ---

  findAllRecruitmentTags(): RecruitmentTagDto[] {
    return this.recruitmentTags.map(t => this.toRecruitmentTagDto(t));
  }

  findRecruitmentTagsByRecruitmentId(recruitmentId: number): RecruitmentTagDto[] {
    const recruitment = this.recruitments.find(r => r.id === recruitmentId);
    if (!recruitment) return [];
    return this.getRecruitmentTagsByIds(recruitment.tags_id);
  }

  // --- Recruitment ---

  findRecruitments(
    gameName: string = '',
    gameTags: number[] = [],
    recruitmentTags: number[] = []
  ): RecruitmentDetailDto[] {
    // Step 1: 由游戏名和游戏标签筛选出游戏ID
    let filteredGames = this.games.filter(g => !gameName || g.name === gameName);
    filteredGames = filteredGames.filter(g => gameTags.length === 0 || gameTags.every(tagId => g.tags_id.includes(tagId)));
    let gameIds = filteredGames.map(g => g.id);

    // Step 2: 查找这些游戏ID对应的所有招募（招募中状态）
    let result = this.recruitments.filter(r =>
      r.status === '招募中' && gameIds.includes(r.game_id)
    );

    // Step 3: 筛选包含所有招募标签的招募
    if (recruitmentTags.length > 0) {
      result = result.filter(r =>
        recruitmentTags.every(tagId => r.tags_id.includes(tagId))
      );
    }

    return result
      .sort((a, b) => new Date(b.created_at).getTime() - new Date(a.created_at).getTime())
      .map(r => this.toRecruitmentDetailDto(r));
  }

  findRecruitmentById(id: number): RecruitmentDetailDto | null {
    const r = this.recruitments.find(rec => rec.id === id);
    return r ? this.toRecruitmentDetailDto(r) : null;
  }

  findRecruitmentsByGameId(gameId: number): RecruitmentDetailDto[] {
    return this.recruitments
      .filter(r => r.game_id === gameId && r.status !== '已删除')
      .map(r => this.toRecruitmentDetailDto(r));
  }

  findRecruitmentsByPublisherId(publisherId: number): RecruitmentDetailDto[] {
    return this.recruitments
      .filter(r => r.publisher_id === publisherId && r.status !== '已删除')
      .map(r => this.toRecruitmentDetailDto(r));
  }

  // --- Response ---

  findResponsesByRecruitmentId(recruitmentId: number): ResponseDto[] {
    return this.responses
      .filter(r => r.recruitment_id === recruitmentId)
      .map(r => this.toResponseDto(r));
  }

  findResponsesByUserId(userId: number): ResponseDto[] {
    return this.responses
      .filter(r => r.responser_id === userId && r.response_status !== '已删除')
      .map(r => this.toResponseDto(r));
  }

  // --- Chat ---

  findChatsByRecruitmentId(recruitmentId: number): ChatDto[] {
    return this.chats
      .filter(c => c.recruitment_id === recruitmentId)
      .map(c => this.toChatDto(c));
  }

  findChatById(chatId: number): ChatDto | null {
    const chat = this.chats.find(c => c.id === chatId);
    return chat ? this.toChatDto(chat) : null;
  }

  findChatEntriesByUserId(userId: number): ChatBriefDto[] {
    const userChats = this.chats.filter(c => c.users.some(u => u.user_id === userId));
    return userChats.map(chat => {
      const otherChatUser = chat.users.find(u => u.user_id !== userId)!;
      const otherUserId = otherChatUser.user_id;
      const otherUser = this.users.find(u => u.id === otherUserId)!;
      const chatMessages = this.messages.filter(msg => msg.chat_id === chat.id);
      const lastMessage = chatMessages.length > 0 ? chatMessages[chatMessages.length - 1] : null;

      return {
        id: chat.id,
        other_user_avatar: otherUser.avatar || '',
        other_user_name: otherUser.nickname || otherUser.username,
        last_message_content: lastMessage ? lastMessage.content : '',
        last_message_at: lastMessage ? lastMessage.created_at : chat.created_at,
        created_at: chat.created_at,
      };
    });
  }

  // --- Message ---

  findMessagesByChatId(chatId: number): MessageDto[] {
    return this.messages
      .filter(m => m.chat_id === chatId)
      .sort((a, b) => a.created_at.localeCompare(b.created_at))
      .map(m => this.toMessageDto(m));
  }

  // --- DTO Mappers ---

  private toUserDto(entity: UserEntity): UserDto {
    const { password, phone_number, ...rest } = entity;
    return rest;
  }

  private toGameDto(entity: GameEntity): GameDto {
    return { ...entity };
  }

  private toGameTagDto(entity: GameTagEntity): GameTagDto {
    return { ...entity };
  }

  private toRecruitmentTagDto(entity: RecruitmentTagEntity): RecruitmentTagDto {
    return { ...entity };
  }

  private toRecruitmentDto(entity: RecruitmentEntity): RecruitmentDto {
    return { ...entity };
  }

  private toRecruitmentDetailDto(entity: RecruitmentEntity): RecruitmentDetailDto {
    return {
      ...this.toRecruitmentDto(entity),
      publisher: this.toUserDto(this.users.find(u => u.id === entity.publisher_id)!),
      game: this.toGameDto(this.games.find(g => g.id === entity.game_id)!),
      gameTags: this.findGameTagsByGameId(entity.game_id),
      recruitmentTags: this.findRecruitmentTagsByRecruitmentId(entity.id),
    };
  }

  private toResponseDto(entity: ResponseEntity): ResponseDto {
    return {
      ...entity,
      responser: this.toUserDto(this.users.find(u => u.id === entity.responser_id)!),
    };
  }

  private toChatDto(entity: ChatEntity): ChatDto {
    const recruitment = this.recruitments.find(r => r.id === entity.recruitment_id)!;
    return {
      id: entity.id,
      recruitment_id: entity.recruitment_id,
      recruitment_title: recruitment.title,
      other_user: this.toUserDto(this.users.find(u => u.id === entity.users[0].user_id)!),
      last_message: null,
      unread_count: 0,
      chat_status: entity.chat_status,
      new_message_at: entity.new_message_at,
      users: entity.users.map(u => ({
        user_id: u.user_id,
        sent_message: u.sent_message,
      })),
      recruitment: this.toRecruitmentDto(recruitment),
    };
  }

  private toMessageDto(entity: MessageEntity): MessageDto {
    return {
      ...entity,
      sender: this.toUserDto(this.users.find(u => u.id === entity.sender_id)!),
      receiver: this.toUserDto(this.users.find(u => u.id === entity.receiver_id)!),
    };
  }
}

export const backendSim = new BackendSim();