import { useEffect, useState } from "react";
import { ActivityIndicator, StyleSheet, View } from "react-native";
import { router, useLocalSearchParams } from "expo-router";
import { getRecruitmentById } from "../../api/api";
import { useAuth } from "../../contexts/auth-context";
import { useTheme } from "../../contexts/theme-context";

export default function RecruitmentDetailIndex() {
  const params = useLocalSearchParams<{ recruitmentId?: string }>();
  const { userId } = useAuth();
  const { colors } = useTheme();
  const [redirecting, setRedirecting] = useState(false);

  useEffect(() => {
    const recruitmentId = params.recruitmentId;
    
    if (!recruitmentId || redirecting) {
      return;
    }

    setRedirecting(true);

    const checkAndRedirect = async () => {
      try {
        const recruitment = await getRecruitmentById(Number(recruitmentId));
        
        if (!recruitment) {
          router.replace("/(tabs)");
          return;
        }

        const isOwnRecruitment = userId === recruitment.publisherId;

        if (isOwnRecruitment) {
          router.replace(`/recruitment-detail/manage?recruitmentId=${recruitmentId}`);
        } else {
          router.replace(`/recruitment-detail/view?recruitmentId=${recruitmentId}`);
        }
      } catch {
        router.replace("/(tabs)");
      }
    };

    checkAndRedirect();
  }, [params.recruitmentId, userId, redirecting]);

  return (
    <View style={[styles.container, { backgroundColor: colors.surface }]}>
      <ActivityIndicator size="large" color={colors.primary} />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
  },
});
