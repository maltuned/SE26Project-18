// Design system for the gaming teammate-finder app.
// Dark "neon arcade" aesthetic — cyan/emerald accents on deep ink backgrounds.

export const Colors = {
  // Background ramps
  ink: {
    900: '#0A0E14', // app background
    850: '#0E141C',
    800: '#131A24', // cards / surfaces
    700: '#1A2330', // elevated surfaces
    600: '#232E40', // hover / pressed
    500: '#2E3A4F',
  },
  // Primary — electric cyan
  primary: {
    50: '#E6FBFF',
    100: '#C2F4FF',
    200: '#7EE8FF',
    300: '#33DAFF',
    400: '#00C8F0',
    500: '#00A6CC', // main accent
    600: '#0085A3',
    700: '#006680',
  },
  // Secondary — emerald (success / online)
  secondary: {
    400: '#22D3A0',
    500: '#10B981',
    600: '#059669',
  },
  // Accent — amber (highlights / XP)
  accent: {
    400: '#FFC54D',
    500: '#F5A623',
    600: '#D98A0E',
  },
  // Danger — rose
  danger: {
    400: '#FF6B81',
    500: '#F43F5E',
  },
  // Neutrals
  neutral: {
    50: '#F2F5F9',
    100: '#D7DEE8',
    200: '#A9B4C4',
    300: '#7A8699',
    400: '#5A6577',
    500: '#3D4659',
  },
  // Functional
  online: '#22D3A0',
  offline: '#5A6577',
  border: '#1F2937',
  borderStrong: '#2E3A4F',
} as const;

export const Spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  xxl: 24,
  xxxl: 32,
  huge: 40,
} as const;

export const Radius = {
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  pill: 999,
} as const;

export const Typography = {
  fontFamilyDisplay: 'SpaceGrotesk-Bold',
  fontFamilyDisplayMedium: 'SpaceGrotesk-Medium',
  fontFamilyBody: 'Inter-Regular',
  fontFamilyBodyMedium: 'Inter-Medium',
  fontFamilyBodyBold: 'Inter-SemiBold',
  sizes: {
    xs: 11,
    sm: 13,
    base: 15,
    lg: 17,
    xl: 21,
    xxl: 27,
    huge: 34,
  } as const,
} as const;

export const Shadow = {
  card: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.35,
    shadowRadius: 12,
    elevation: 6,
  },
  glow: {
    shadowColor: Colors.primary[400],
    shadowOffset: { width: 0, height: 0 },
    shadowOpacity: 0.45,
    shadowRadius: 16,
    elevation: 0,
  },
} as const;

// Per-category accent color used across category tiles + post tags.
export const CategoryAccent: Record<string, string> = {
  action: '#00C8F0',
  fps: '#F5A623',
  moba: '#22D3A0',
  rpg: '#FF6B81',
  strategy: '#A78BFA',
  casual: '#34D399',
  racing: '#FBBF24',
  horror: '#F43F5E',
};
