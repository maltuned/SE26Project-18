import { useState } from 'react';
import { useRouter } from 'expo-router';
import {
  StyleSheet, Text, View, TextInput, Pressable, KeyboardAvoidingView, Platform,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { LinearGradient } from 'expo-linear-gradient';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { Button } from '@/components/Button';
import { LogIn, UserPlus, Eye, EyeOff } from 'lucide-react-native';

export default function LoginScreen() {
  const router = useRouter();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPw, setShowPw] = useState(false);

  function handleLogin() {
    if (!username.trim() || !password.trim()) return;
    router.replace('/');
  }

  return (
    <View style={styles.screen}>
      <LinearGradient colors={[`${Colors.primary[400]}10`, Colors.ink[900]]} locations={[0, 0.4]} style={StyleSheet.absoluteFill} />

      <SafeAreaView edges={['top']} style={{ flex: 1 }}>
        <KeyboardAvoidingView
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
          style={{ flex: 1 }}
        >
          <View style={styles.inner}>
            {/* Logo area */}
            <View style={styles.logoArea}>
              <View style={styles.logoCircle}>
                <Text style={styles.logoText}>PM</Text>
              </View>
              <Text style={styles.appName}>PlayMate</Text>
              <Text style={styles.tagline}>找到你的游戏搭子</Text>
            </View>

            {/* Form */}
            <View style={styles.form}>
              <Text style={styles.label}>用户名</Text>
              <TextInput
                value={username}
                onChangeText={setUsername}
                placeholder="请输入用户名"
                placeholderTextColor={Colors.neutral[400]}
                style={styles.input}
                autoCapitalize="none"
              />

              <Text style={styles.label}>密码</Text>
              <View style={styles.pwWrap}>
                <TextInput
                  value={password}
                  onChangeText={setPassword}
                  placeholder="请输入密码"
                  placeholderTextColor={Colors.neutral[400]}
                  style={[styles.input, { flex: 1, borderWidth: 0, marginBottom: 0 }]}
                  secureTextEntry={!showPw}
                />
                <Pressable onPress={() => setShowPw(!showPw)} style={styles.eyeBtn}>
                  {showPw ? <EyeOff color={Colors.neutral[400]} size={18} /> : <Eye color={Colors.neutral[400]} size={18} />}
                </Pressable>
              </View>

              <Button
                label="登录"
                color={Colors.primary[400]}
                icon={<LogIn color={Colors.ink[900]} size={18} />}
                size="lg"
                onPress={handleLogin}
                disabled={!username.trim() || !password.trim()}
              />

              <Pressable onPress={() => router.push('/register')} style={styles.switchRow}>
                <Text style={styles.switchText}>还没有账号？</Text>
                <Text style={styles.switchLink}>立即注册</Text>
                <UserPlus color={Colors.primary[400]} size={14} />
              </Pressable>
            </View>
          </View>
        </KeyboardAvoidingView>
      </SafeAreaView>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  inner: { flex: 1, justifyContent: 'center', paddingHorizontal: Spacing.xl },
  logoArea: { alignItems: 'center', marginBottom: Spacing.xxl },
  logoCircle: {
    width: 88, height: 88, borderRadius: 44, backgroundColor: Colors.primary[400],
    alignItems: 'center', justifyContent: 'center', marginBottom: Spacing.md,
  },
  logoText: { color: Colors.ink[900], fontFamily: Typography.fontFamilyDisplay, fontSize: 32 },
  appName: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.xxl },
  tagline: { color: Colors.neutral[300], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody, marginTop: 4 },
  form: { gap: Spacing.sm },
  label: { color: Colors.neutral[200], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBodyMedium, marginTop: Spacing.sm },
  input: {
    backgroundColor: Colors.ink[800], borderWidth: 1, borderColor: Colors.border,
    borderRadius: Radius.md, paddingHorizontal: Spacing.md, paddingVertical: Spacing.md,
    color: Colors.neutral[50], fontFamily: Typography.fontFamilyBody, fontSize: Typography.sizes.base,
    marginBottom: Spacing.xs,
  },
  pwWrap: {
    flexDirection: 'row', alignItems: 'center', backgroundColor: Colors.ink[800],
    borderWidth: 1, borderColor: Colors.border, borderRadius: Radius.md, marginBottom: Spacing.lg,
  },
  eyeBtn: { paddingHorizontal: Spacing.md },
  switchRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: Spacing.xs, marginTop: Spacing.lg },
  switchText: { color: Colors.neutral[300], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody },
  switchLink: { color: Colors.primary[400], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBodyBold },
});
