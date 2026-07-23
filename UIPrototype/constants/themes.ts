// Three complete color themes for PlayMate.
// Each theme has the same token structure — swap the whole palette at runtime.

export interface ThemeColors {
  ink: { 900: string; 850: string; 800: string; 700: string; 600: string; 500: string };
  primary: { 50: string; 100: string; 200: string; 300: string; 400: string; 500: string; 600: string; 700: string };
  secondary: { 400: string; 500: string; 600: string };
  accent: { 400: string; 500: string; 600: string };
  danger: { 400: string; 500: string };
  neutral: { 50: string; 100: string; 200: string; 300: string; 400: string; 500: string };
  online: string;
  offline: string;
  border: string;
  borderStrong: string;
}

export type ThemeKey = 'arcade' | 'midnight' | 'light';

export interface ThemeMeta {
  key: ThemeKey;
  name: string;
  description: string;
  isDark: boolean;
  previewColors: string[]; // 4 swatch circles for the picker card
}

// ── Theme 1: 深色街机 Arcade (the original dark cyan theme) ──

const arcade: ThemeColors = {
  ink: {
    900: '#0A0E14',
    850: '#0E141C',
    800: '#131A24',
    700: '#1A2330',
    600: '#232E40',
    500: '#2E3A4F',
  },
  primary: {
    50: '#E6FBFF',
    100: '#C2F4FF',
    200: '#7EE8FF',
    300: '#33DAFF',
    400: '#00C8F0',
    500: '#00A6CC',
    600: '#0085A3',
    700: '#006680',
  },
  secondary: {
    400: '#22D3A0',
    500: '#10B981',
    600: '#059669',
  },
  accent: {
    400: '#FFC54D',
    500: '#F5A623',
    600: '#D98A0E',
  },
  danger: {
    400: '#FF6B81',
    500: '#F43F5E',
  },
  neutral: {
    50: '#F2F5F9',
    100: '#D7DEE8',
    200: '#A9B4C4',
    300: '#7A8699',
    400: '#5A6577',
    500: '#3D4659',
  },
  online: '#22D3A0',
  offline: '#5A6577',
  border: '#1F2937',
  borderStrong: '#2E3A4F',
};

// ── Theme 2: 午夜蓝 Midnight (deep indigo / violet dark) ──

const midnight: ThemeColors = {
  ink: {
    900: '#0A0F1E',
    850: '#0F1529',
    800: '#151D35',
    700: '#1C2642',
    600: '#263153',
    500: '#313D64',
  },
  primary: {
    50: '#EEF2FF',
    100: '#E0E7FF',
    200: '#C7D2FE',
    300: '#A5B4FC',
    400: '#818CF8',
    500: '#6366F1',
    600: '#4F46E5',
    700: '#4338CA',
  },
  secondary: {
    400: '#2DD4BF',
    500: '#14B8A6',
    600: '#0D9488',
  },
  accent: {
    400: '#FBBF24',
    500: '#F59E0B',
    600: '#D97706',
  },
  danger: {
    400: '#FF6B81',
    500: '#F43F5E',
  },
  neutral: {
    50: '#F2F5F9',
    100: '#D7DEE8',
    200: '#A9B4C4',
    300: '#7A8699',
    400: '#5A6577',
    500: '#3D4659',
  },
  online: '#2DD4BF',
  offline: '#5A6577',
  border: '#1E2A45',
  borderStrong: '#2D3B5A',
};

// ── Theme 3: 浅色模式 Light (clean white / gray) ──

const light: ThemeColors = {
  ink: {
    900: '#F8FAFC',
    850: '#F1F5F9',
    800: '#FFFFFF',
    700: '#E2E8F0',
    600: '#CBD5E1',
    500: '#94A3B8',
  },
  primary: {
    50: '#F0F9FF',
    100: '#E0F5FE',
    200: '#B9ECFE',
    300: '#7CDCFD',
    400: '#00A6CC',
    500: '#0085A3',
    600: '#006680',
    700: '#004D61',
  },
  secondary: {
    400: '#10B981',
    500: '#059669',
    600: '#047857',
  },
  accent: {
    400: '#F59E0B',
    500: '#D97706',
    600: '#B45309',
  },
  danger: {
    400: '#F43F5E',
    500: '#E11D48',
  },
  neutral: {
    50: '#0F172A',
    100: '#1E293B',
    200: '#334155',
    300: '#475569',
    400: '#64748B',
    500: '#94A3B8',
  },
  online: '#10B981',
  offline: '#94A3B8',
  border: '#E2E8F0',
  borderStrong: '#CBD5E1',
};

// ── Theme map & metadata ──

export const themes: Record<ThemeKey, ThemeColors> = {
  arcade,
  midnight,
  light,
};

export const themeMeta: Record<ThemeKey, ThemeMeta> = {
  arcade: {
    key: 'arcade',
    name: '深色街机',
    description: '电光青 × 深色背景',
    isDark: true,
    previewColors: ['#0A0E14', '#00C8F0', '#22D3A0', '#FFC54D'],
  },
  midnight: {
    key: 'midnight',
    name: '午夜蓝',
    description: '靛蓝紫 × 深邃夜空',
    isDark: true,
    previewColors: ['#0A0F1E', '#818CF8', '#2DD4BF', '#FBBF24'],
  },
  light: {
    key: 'light',
    name: '浅色模式',
    description: '清爽白 × 海洋青',
    isDark: false,
    previewColors: ['#FFFFFF', '#00A6CC', '#10B981', '#F59E0B'],
  },
};

// ── Default (for backward compat during migration) ──

export const Colors = arcade;
