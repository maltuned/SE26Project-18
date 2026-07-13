import { View, StyleSheet, Platform } from 'react-native';
import { Tabs, usePathname, useRouter } from 'expo-router';
import { Colors, Spacing, Typography } from '@/constants/theme';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { FAB } from '@/components/FAB';
import { Search, MessageCircle, FileText, Wrench, User } from 'lucide-react-native';

const TAB_ICON_MAP = {
  Search,
  MessageCircle,
  FileText,
  Wrench,
  User,
} as const;

function TabIcon({ icon, color, focused }: { icon: keyof typeof TAB_ICON_MAP; color: string; focused: boolean }) {
  const Icon = TAB_ICON_MAP[icon];
  return (
    <View style={styles.iconWrap}>
      {focused && <View style={[styles.glow, { backgroundColor: color }]} />}
      <Icon color={color} size={22} strokeWidth={focused ? 2.4 : 2} />
    </View>
  );
}

export default function TabLayout() {
  const insets = useSafeAreaInsets();
  const pathname = usePathname();
  const router = useRouter();

  const showFAB = pathname === '/' || pathname === '/posts';

  return (
    <View style={styles.container}>
      <Tabs
        screenOptions={{
          headerShown: false,
          tabBarStyle: {
            position: 'absolute',
            backgroundColor: Platform.select({ web: 'rgba(10,14,20,0.92)', default: Colors.ink[850] }),
            borderTopColor: Colors.border,
            borderTopWidth: 1,
            height: 64 + (insets.bottom || 0),
            paddingBottom: insets.bottom || 0,
            paddingHorizontal: Spacing.xl,
            elevation: 0,
          },
          tabBarActiveTintColor: Colors.primary[400],
          tabBarInactiveTintColor: Colors.neutral[400],
          tabBarLabelStyle: {
            fontFamily: Typography.fontFamilyBodyMedium,
            fontSize: Typography.sizes.xs,
            marginTop: 2,
          },
          tabBarIconStyle: { marginBottom: 2 },
        }}
      >
        <Tabs.Screen
          name="index"
          options={{
            title: '首页',
            tabBarIcon: ({ color, focused }) => <TabIcon icon="Search" color={color} focused={focused} />,
          }}
        />
        <Tabs.Screen
          name="chat"
          options={{
            title: '聊天',
            tabBarIcon: ({ color, focused }) => <TabIcon icon="MessageCircle" color={color} focused={focused} />,
          }}
        />
        <Tabs.Screen
          name="posts"
          options={{
            title: '发布',
            tabBarIcon: ({ color, focused }) => <TabIcon icon="FileText" color={color} focused={focused} />,
          }}
        />
        <Tabs.Screen
          name="tools"
          options={{
            title: '工具',
            tabBarIcon: ({ color, focused }) => <TabIcon icon="Wrench" color={color} focused={focused} />,
          }}
        />
        <Tabs.Screen
          name="profile"
          options={{
            title: '我的',
            tabBarIcon: ({ color, focused }) => <TabIcon icon="User" color={color} focused={focused} />,
          }}
        />
      </Tabs>

      {showFAB && (
        <FAB
          onPress={() => router.push('/create-post')}
          bottomOffset={80 + (insets.bottom || 0)}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  iconWrap: { alignItems: 'center', justifyContent: 'center', width: 44, height: 28 },
  glow: {
    position: 'absolute',
    top: -6,
    width: 32,
    height: 4,
    borderRadius: 999,
    opacity: 0.9,
  },
});
