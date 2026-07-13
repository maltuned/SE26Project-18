import type { Game, Tag, RecruitPost, ChatSession, ChatMessage, UserProfile, Report } from './types';

// ---- Helpers ----
const gameImg = (seed: number) =>
  `https://images.pexels.com/photos/${seed}/pexels-photo-${seed}.jpeg?auto=compress&cs=tinysrgb&w=800`;

export const tags: Tag[] = [
  { id: 1, name: 'MOBA', iconName: 'Shield', accentColor: '#22D3A0' },
  { id: 2, name: 'FPS', iconName: 'Crosshair', accentColor: '#F5A623' },
  { id: 3, name: 'RPG', iconName: 'Sparkles', accentColor: '#FF6B81' },
  { id: 4, name: '动作', iconName: 'Swords', accentColor: '#00C8F0' },
  { id: 5, name: '策略', iconName: 'Brain', accentColor: '#A78BFA' },
  { id: 6, name: '休闲', iconName: 'PartyPopper', accentColor: '#34D399' },
  { id: 7, name: '竞速', iconName: 'Car', accentColor: '#FBBF24' },
  { id: 8, name: '恐怖', iconName: 'Ghost', accentColor: '#F43F5E' },
  { id: 9, name: '上分', iconName: 'TrendingUp', accentColor: '#FFC54D' },
  { id: 10, name: '双排', iconName: 'Users', accentColor: '#00C8F0' },
  { id: 12, name: '新手友好', iconName: 'Smile', accentColor: '#34D399' },
];

export const games: Game[] = [
  { id: 1, name: '英雄联盟', coverUrl: gameImg(167964), tagline: '召唤师峡谷对决', memberCount: 58200, onlineCount: 5400, tagIds: [1, 9] },
  { id: 2, name: 'Valorant', coverUrl: gameImg(1660995), tagline: '战术射击5v5', memberCount: 41200, onlineCount: 3200, tagIds: [2, 9] },
  { id: 3, name: 'CS2', coverUrl: gameImg(1370750), tagline: '永恒经典反恐', memberCount: 36800, onlineCount: 2950, tagIds: [2] },
  { id: 4, name: 'Apex 英雄', coverUrl: gameImg(2387873), tagline: '英雄技能大逃杀', memberCount: 22450, onlineCount: 1610, tagIds: [2, 4] },
  { id: 5, name: '原神', coverUrl: gameImg(1693650), tagline: '提瓦特大陆冒险', memberCount: 39400, onlineCount: 2800, tagIds: [3] },
  { id: 6, name: '崩坏：星穹铁道', coverUrl: gameImg(9570632), tagline: '银河列车旅行', memberCount: 21300, onlineCount: 1450, tagIds: [3] },
  { id: 7, name: '艾尔登法环', coverUrl: gameImg(12932492), tagline: '交界地黄金树之旅', memberCount: 28430, onlineCount: 1820, tagIds: [4] },
  { id: 8, name: '怪物猎人', coverUrl: gameImg(20230), tagline: '狩猎巨兽的浪漫', memberCount: 19280, onlineCount: 940, tagIds: [4] },
  { id: 9, name: 'Dota 2', coverUrl: gameImg(7863442), tagline: '百位英雄千种打法', memberCount: 24300, onlineCount: 2100, tagIds: [1, 9] },
  { id: 10, name: '帝国时代', coverUrl: gameImg(259751), tagline: '文明兴衰史诗', memberCount: 9800, onlineCount: 320, tagIds: [5] },
  { id: 11, name: 'Among Us', coverUrl: gameImg(7651601), tagline: '太空船内鬼抓鬼', memberCount: 14800, onlineCount: 760, tagIds: [6] },
  { id: 12, name: '糖豆人', coverUrl: gameImg(3771118), tagline: '派对闯关综艺', memberCount: 11200, onlineCount: 540, tagIds: [6] },
  { id: 13, name: '马力欧赛车', coverUrl: gameImg(170811), tagline: '欢乐道具竞速', memberCount: 13400, onlineCount: 680, tagIds: [7] },
  { id: 14, name: '恐鬼症', coverUrl: gameImg(2690337), tagline: '四人灵异调查', memberCount: 8600, onlineCount: 410, tagIds: [8] },
  { id: 15, name: '求生之路', coverUrl: gameImg(336232), tagline: '丧尸末日合作', memberCount: 7400, onlineCount: 280, tagIds: [8] },
];

function makeExpires(minutes: number): string {
  return new Date(Date.now() + minutes * 60_000).toISOString();
}

export let posts: RecruitPost[] = [
  {
    id: 101, gameId: 1, gameName: '英雄联盟', tagIds: [1, 9, 10],
    authorId: 1, authorName: '星河玩家', authorAvatar: "",
    title: '钻三打野 找中单双排上分',
    description: '主玩盲僧皇子 节奏型打野 钻三156级 晚上8-12点在线 找中单一起上大师 有麦心态好',
    needCount: 1, filledCount: 0, mode: 'ranked', voice: 'required', platform: 'PC',
    durationMinutes: 1440, expiresAt: makeExpires(1440), createdAt: '2分钟前', status: 'active', comments: 0,
  },
  {
    id: 102, gameId: 2, gameName: 'Valorant', tagIds: [2, 12],
    authorId: 1, authorName: '星河玩家', authorAvatar: "",
    title: '铂金决斗 找烟位/哨位休闲',
    description: '玩Jett/Raze 铂金水平 想找烟位或哨位队友 不打排位休闲为主 语音沟通 周末全天在',
    needCount: 1, filledCount: 0, mode: 'casual', voice: 'required', platform: 'PC',
    durationMinutes: 10080, expiresAt: makeExpires(10080), createdAt: '10分钟前', status: 'active', comments: 2,
  },
  {
    id: 103, gameId: 5, gameName: '原神', tagIds: [3, 6, 12],
    authorId: 1, authorName: '星河玩家', authorAvatar: "",
    title: '深渊11层互助 缺辅助和副C（已过期）',
    description: '之前发的招募 队伍已经找到了 放在这里作为过期样例',
    needCount: 2, filledCount: 2, mode: 'casual', voice: 'optional', platform: '全平台',
    durationMinutes: 30, expiresAt: makeExpires(-1), createdAt: '3天前', status: 'expired', comments: 5,
  },
  {
    id: 1, gameId: 1, gameName: '英雄联盟', tagIds: [1, 9, 10],
    authorId: 2, authorName: '夜雨听风', authorAvatar: "",
    title: '钻石中野双排 求稳的下路',
    description: '本人中单钻三 主玩辛德拉佐伊 野王钻二节奏型 想找下路双排组合 话多但配合好 今晚8点开打 目标大师',
    needCount: 2, filledCount: 0, mode: 'ranked', voice: 'required', platform: 'PC',
    durationMinutes: 1440, expiresAt: makeExpires(1440), createdAt: '8分钟前', status: 'active', comments: 14,
  },
  {
    id: 2, gameId: 2, gameName: 'Valorant', tagIds: [2, 9],
    authorId: 3, authorName: 'ZeroOne', authorAvatar: "",
    title: 'Immortal双排 找哨位/控场',
    description: '我玩决斗KDA稳定 找一个会读图的哨位或控场 晚9-12点固定 不喷队友 输了复盘不怪人',
    needCount: 1, filledCount: 0, mode: 'ranked', voice: 'required', platform: 'PC',
    durationMinutes: 30, expiresAt: makeExpires(30), createdAt: '15分钟前', status: 'active', comments: 9,
  },
  {
    id: 3, gameId: 1, gameName: '英雄联盟', tagIds: [1, 6, 12],
    authorId: 1, authorName: "星河玩家", authorAvatar: "",
    title: '黄金灵活五排缺一个辅助',
    description: '已组4人 都是黄金段位 缺软辅或硬开皆可 不压力 欢乐上分 最好有麦 周末连开',
    needCount: 1, filledCount: 0, mode: 'casual', voice: 'optional', platform: 'PC',
    durationMinutes: 10080, expiresAt: makeExpires(10080), createdAt: '24分钟前', status: 'active', comments: 6,
  },
  {
    id: 4, gameId: 5, gameName: '原神', tagIds: [3, 6, 12],
    authorId: 2, authorName: "夜雨听风", authorAvatar: "",
    title: '深渊12层互带 缺一个主C',
    description: '已有3人辅助配队 缺一个满练主C 周日轮换带新 不压力 战力截图私聊',
    needCount: 1, filledCount: 0, mode: 'casual', voice: 'optional', platform: '全平台',
    durationMinutes: 10080, expiresAt: makeExpires(10080), createdAt: '1小时前', status: 'active', comments: 8,
  },
  {
    id: 5, gameId: 3, gameName: 'CS2', tagIds: [2, 9],
    authorId: 3, authorName: "ZeroOne", authorAvatar: "",
    title: '5E 2000分 求突破手和指挥',
    description: '组3人 求突破手+指挥 有战术储备 火热练枪中 目标5E 2500 认真队 不混',
    needCount: 2, filledCount: 1, mode: 'ranked', voice: 'required', platform: 'PC',
    durationMinutes: 1440, expiresAt: makeExpires(1440), createdAt: '18分钟前', status: 'active', comments: 11,
  },
  {
    id: 6, gameId: 7, gameName: '艾尔登法环', tagIds: [4, 6],
    authorId: 1, authorName: "星河玩家", authorAvatar: "",
    title: '环世界联机 三人推图 缺一个',
    description: '已2人 平推路线 缺一个法师或战士 不刷等级 一起探索Boss 废弃地牢拉满 今晚9点',
    needCount: 1, filledCount: 0, mode: 'casual', voice: 'optional', platform: 'PC/PS5',
    durationMinutes: 1440, expiresAt: makeExpires(1440), createdAt: '32分钟前', status: 'active', comments: 5,
  },
  {
    id: 7, gameId: 11, gameName: 'Among Us', tagIds: [6, 12],
    authorId: 2, authorName: "夜雨听风", authorAvatar: "",
    title: '8人欢乐船 满人即开',
    description: '已6人 还差2人 喜剧抓内鬼 欢乐为主 不骂人 任何段位都欢迎 进群秒开',
    needCount: 2, filledCount: 0, mode: 'casual', voice: 'required', platform: '手机/PC',
    durationMinutes: 30, expiresAt: makeExpires(30), createdAt: '6分钟前', status: 'active', comments: 2,
  },
  {
    id: 8, gameId: 9, gameName: 'Dota 2', tagIds: [1, 9],
    authorId: 3, authorName: "ZeroOne", authorAvatar: "",
    title: '冠一/传奇 双排求1号位或5号位',
    description: '我4号位游走 冠一2600 求一个懂线优的1号位或5号位 双排上分 不压力 输了复盘',
    needCount: 1, filledCount: 0, mode: 'ranked', voice: 'required', platform: 'PC',
    durationMinutes: 1440, expiresAt: makeExpires(1440), createdAt: '50分钟前', status: 'active', comments: 7,
  },
  {
    id: 9, gameId: 14, gameName: '恐鬼症', tagIds: [8, 6, 12],
    authorId: 1, authorName: "星河玩家", authorAvatar: "",
    title: '新手三人 求1人陪玩 麦必',
    description: '第一次玩 很害怕但想体验 求一个老手带带 我们有麦笑点低 不嫌弃尖叫',
    needCount: 1, filledCount: 0, mode: 'casual', voice: 'required', platform: 'PC',
    durationMinutes: 10080, expiresAt: makeExpires(10080), createdAt: '3小时前', status: 'active', comments: 4,
  },
  {
    id: 10, gameId: 10, gameName: '帝国时代', tagIds: [5],
    authorId: 2, authorName: "夜雨听风", authorAvatar: "",
    title: '4v4团战 缺两个经济位',
    description: '已有2人主军事 找两个经济发育位 黑森林地图 语音沟通 周末下午开局',
    needCount: 2, filledCount: 0, mode: 'casual', voice: 'optional', platform: 'PC',
    durationMinutes: 10080, expiresAt: makeExpires(10080), createdAt: '2小时前', status: 'active', comments: 3,
  },
];

export const currentUserId = 1;

export const chatSessions: ChatSession[] = [
  { id: 1, participantName: '夜雨听风', participantAvatar: '', gameName: '英雄联盟', lastMessage: '没问题，晚上8点我拉你', lastMessageTime: '20:06', unreadCount: 2, online: true },
  { id: 2, participantName: 'ZeroOne', participantAvatar: '', gameName: 'Valorant', lastMessage: '我控场Sage 加入', lastMessageTime: '21:14', unreadCount: 0, online: true },
];

export const chatMessages: Record<number, ChatMessage[]> = {
  1: [
    { id: 'm1', authorName: '夜雨听风', authorAvatar: '', text: '看到你发的英雄联盟招募了，钻三打野是吧？', time: '19:55' },
    { id: 'm2', authorName: '你', authorAvatar: '', text: '对，主玩盲僧皇子，找中单双排', time: '19:58', isMe: true },
    { id: 'm3', authorName: '夜雨听风', authorAvatar: '', text: '我是中单，钻二，辛德拉佐伊都会', time: '20:00' },
    { id: 'm4', authorName: '你', authorAvatar: '', text: '那可以啊，晚上来两把试试', time: '20:02', isMe: true },
    { id: 'm5', authorName: '夜雨听风', authorAvatar: '', text: '没问题，晚上8点我拉你', time: '20:06' },
  ],
  2: [
    { id: 'm1', authorName: 'ZeroOne', authorAvatar: '', text: '你好，看到你的Valorant招募了', time: '21:08' },
    { id: 'm2', authorName: '你', authorAvatar: '', text: '哈喽，我铂金决斗，找烟位', time: '21:10', isMe: true },
    { id: 'm3', authorName: 'ZeroOne', authorAvatar: '', text: '我主玩烟位和哨位，可以配合', time: '21:12' },
    { id: 'm4', authorName: '你', authorAvatar: '', text: '太好了，我控场Sage 加入', time: '21:14', isMe: true },
  ],
};

const defaultMessages: ChatMessage[] = [
  { id: 'd1', authorName: '系统', authorAvatar: '', text: '欢迎来到聊天室，文明开黑，拒绝压力。', time: '00:00', isSystem: true },
];

export function getChat(sessionId: number): ChatMessage[] {
  return chatMessages[sessionId] ?? defaultMessages;
}

export const currentUser: UserProfile = {
  id: 1,
  name: '星河玩家',
  handle: '@galaxy_gg',
  avatar: "",
  squads: 3,
  posts: 12,
  isAdmin: true,
  recentGames: ['英雄联盟', 'Valorant', '原神'],
  bio: '钻三打野主玩盲僧皇子，找个中单双排上大师。晚上8-12点在线，有麦心态好。',
};

export function getUserPosts(): RecruitPost[] {
  return posts.filter((p) => p.authorId === currentUserId);
}

// ---- Mock user profiles for post detail / user page ----

export const mockUsers: Record<number, UserProfile> = {
  1: currentUser,
  2: {
    id: 2, name: '夜雨听风', handle: '@night_rain', avatar: "",
    squads: 5, posts: 48, recentGames: ['英雄联盟', 'Dota 2', 'CS2'],
    bio: '英雄联盟大师打野，Dota老玩家。话多不压力，求稳的上分队友。',
  },
  3: {
    id: 3, name: 'ZeroOne', handle: '@zero_one', avatar: "",
    squads: 4, posts: 32, recentGames: ['Valorant', 'Apex 英雄'],
    bio: 'Valorant铂金决斗，主玩Jett/Raze。休闲为主不打排位。',
  },
};

export function getUserById(id: number): UserProfile | undefined {
  return mockUsers[id];
}

export function recordRecentGame(userId: number, gameName: string) {
  const user = mockUsers[userId];
  if (!user || !gameName) return;
  user.recentGames = [gameName, ...user.recentGames.filter((g) => g !== gameName)].slice(0, 5);
}

export function updateBio(userId: number, bio: string) {
  const user = mockUsers[userId];
  if (user) user.bio = bio;
}

// ---- Admin data ----

export let reports: Report[] = [];
export let bannedUserIds: number[] = [];

export function addReport(report: Omit<Report, 'id' | 'createdAt' | 'handled'>) {
  reports.unshift({
    ...report,
    id: Date.now(),
    createdAt: '刚刚',
    handled: false,
  });
}

export function deletePost(postId: number) {
  const idx = posts.findIndex((p) => p.id === postId);
  if (idx !== -1) posts.splice(idx, 1);
}

export function banUser(userId: number) {
  if (!bannedUserIds.includes(userId)) {
    bannedUserIds.push(userId);
  }
}

export function unbanUser(userId: number) {
  bannedUserIds = bannedUserIds.filter((id) => id !== userId);
}

