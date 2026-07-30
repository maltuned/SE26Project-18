import { Tabs, useRouter } from "expo-router";
import { StyleSheet, Text } from "react-native";
import { useTheme } from "../../contexts/theme-context";

const iconStyle = { fontSize: 20 };

function HomeIcon() {
  return <Text style={iconStyle}>🏠</Text>;
}
function RecruitIcon() {
  return <Text style={iconStyle}>📋</Text>;
}
function PublishIcon() {
  return <Text style={iconStyle}>➕</Text>;
}
function ChatIcon() {
  return <Text style={iconStyle}>💬</Text>;
}
function ProfileIcon() {
  return <Text style={iconStyle}>👤</Text>;
}

export default function TabLayout() {
  const { colors } = useTheme();
  const router = useRouter();

  return (
    <Tabs
      screenOptions={{
        tabBarActiveTintColor: colors.tabBarActive,
        tabBarInactiveTintColor: colors.tabBarInactive,
        tabBarStyle: [
          styles.container,
          { backgroundColor: colors.card, borderTopColor: colors.border },
        ],
        tabBarLabelStyle: styles.label,
        headerShown: false,
      }}
    >
      <Tabs.Screen
        name="index"
        options={{
          tabBarLabel: "首页",
          tabBarIcon: HomeIcon,
        }}
      />
      <Tabs.Screen
        name="recruitment"
        options={{
          tabBarLabel: "招募",
          tabBarIcon: RecruitIcon,
        }}
      />
      <Tabs.Screen
        name="publish"
        listeners={{
          tabPress: (event) => {
            event.preventDefault();
            router.push("/recruitment-edit");
          },
        }}
        options={{
          tabBarLabel: "发布",
          tabBarIcon: PublishIcon,
        }}
      />
      <Tabs.Screen
        name="chat"
        options={{
          tabBarLabel: "聊天",
          tabBarIcon: ChatIcon,
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{
          tabBarLabel: "我的",
          tabBarIcon: ProfileIcon,
        }}
      />
    </Tabs>
  );
}

const styles = StyleSheet.create({
  container: {
    height: 60,
    paddingBottom: 8,
    borderTopWidth: 1,
  },
  label: {
    fontSize: 12,
  },
});
