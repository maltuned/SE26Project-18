export const LightTheme = {
  // === 基础色 ===
  background: '#fff',
  surface: '#f5f5f5',
  card: '#fff',

  // === 主色（按钮、标签、链接、Tab激活等统一用 primary） ===
  primary: '#007AFF',
  primaryText: '#fff',
  primaryLight: '#e8f0fe',

  // === 文字色 ===
  text: '#333',
  textSecondary: '#666',
  textTertiary: '#999',
  textQuaternary: '#aaa',

  // === 边框/分割线 ===
  border: '#f0f0f0',
  borderLight: '#eee',

  // === 输入框 ===
  inputBackground: '#f0f0f0',
  inputBackgroundAlt: '#f5f5f5',
  inputText: '#333',
  inputBorder: '#ddd',

  // === 标签（非激活态） ===
  tagBackground: '#f0f0f0',
  tagText: '#666',

  // === 功能色 ===
  danger: '#FF3B30',
  success: '#34C759',
  warning: '#eb9500',
  disabled: '#ccc',
  placeholder: '#ddd',

  // === 遮罩/阴影 ===
  overlay: 'rgba(0,0,0,0.5)',
  shadowColor: '#000',

  // === 搜索栏 ===
  searchBackground: '#f0f0f0',

  // === 弹窗 ===
  modalBackground: '#fff',

  // === 消息气泡 ===
  messageMy: '#007AFF',
  messageMyText: '#fff',
  messageOther: '#fff',
  messageOtherText: '#333',

  // === 输入栏 ===
  inputBarBackground: '#fff',
  textInputBackground: '#f9f9f9',

  // === 状态指示 ===
  statusRecruiting: '#34C759',
  statusRecruitingText: '#fff',
  statusClosed: '#f0f0f0',
  statusClosedText: '#999',

  // === 筛选器 ===
  filterInactive: '#f0f0f0',
  filterTextInactive: '#666',

  // === 导航栏 ===
  tabBarActive: '#007AFF',
  tabBarInactive: '#8E8E93',
  tabActiveBorder: '#007AFF',
  tabInactiveBorder: 'transparent',

  // === 杂项 ===
  statusBar: 'dark-content',
  headerBorder: '#eee',
  redDot: '#FF3B30',
  arrow: '#ccc',
  profileBackground: '#fff',
  bioText: '#999',
  menuBorder: '#f0f0f0',
  nicknameText: '#333',
  sectionTitle: '#333',
  descriptionText: '#555',
  gameModalBackground: '#fff',
};

export type ThemeColors = typeof LightTheme;