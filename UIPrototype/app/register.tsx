import { useState } from 'react';
import { useRouter } from 'expo-router';
import {
  StyleSheet, Text, View, TextInput, Pressable, KeyboardAvoidingView, Platform,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { LinearGradient } from 'expo-linear-gradient';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { Button } from '@/components/Button';
import { UserPlus, LogIn, Eye, EyeOff, ChevronLeft } from 'lucide-react-native';

export default function RegisterScreen() {
  const router = useRouter();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [showPw, setShowPw] = useState(false);

  const canSubmit = username.trim() && password.trim() && password === confirm;

  function handleRegister() {
    if (!canSubmit) return;
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
            {/* Header */}
            <Pressable onPress={() => router.back()} style={styles.backBtn}>
              <ChevronLeft color={Colors.neutral[50]} size={24} />
            </Pressable>

            <Text style={styles.title}>创建账号</Text>
            <Text style={styles.subtitle}>加入 PlayMate，找到你的搭子</Text>

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

              <Text style={styles.label}>确认密码</Text>
              <TextInput
                value={confirm}
                onChangeText={setConfirm}
                placeholder="请再次输入密码"
                placeholderTextColor={Colors.neutral[400]}
                style={[styles.input, confirm && password !== confirm && styles.inputError]}
                secureTextEntry
              />
              {confirm.length > 0 && password !== confirm && (
                <Text style={styles.errorText}>两次输入的密码不一致</Text>
              )}

              <Button
                label="注册"
                color={Colors.secondary[400]}
                icon={<UserPlus color={Colors.ink[900]} size={18} />}
                size="lg"
                onPress={handleRegister}
                disabled={!canSubmit}
              />

              <Pressable onPress={() => router.back()} style={styles.switchRow}>
                <Text style={styles.switchText}>已有账号？</Text>
                <Text style={styles.switchLink}>立即登录</Text>
                <LogIn color={Colors.primary[400]} size={14} />
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
  backBtn: {
    width: 40, height: 40, borderRadius: 999, backgroundColor: Colors.ink[800],
    alignItems: 'center', justifyContent: 'center', marginBottom: Spacing.lg,
  },
  title: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.xxl },
  subtitle: { color: Colors.neutral[300], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody, marginTop: 4, marginBottom: Spacing.xxl },
  form: { gap: Spacing.sm },
  label: { color: Colors.neutral[200], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBodyMedium, marginTop: Spacing.sm },
  input: {
    backgroundColor: Colors.ink[800], borderWidth: 1, borderColor: Colors.border,
    borderRadius: Radius.md, paddingHorizontal: Spacing.md, paddingVertical: Spacing.md,
    color: Colors.neutral[50], fontFamily: Typography.fontFamilyBody, fontSize: Typography.sizes.base,
    marginBottom: Spacing.xs,
  },
  inputError: { borderColor: Colors.danger[400] },
  errorText: { color: Colors.danger[400], fontSize: Typography.sizes.xs, fontFamily: Typography.fontFamilyBody, marginTop: -4 },
  pwWrap: {
    flexDirection: 'row', alignItems: 'center', backgroundColor: Colors.ink[800],
    borderWidth: 1, borderColor: Colors.border, borderRadius: Radius.md, marginBottom: Spacing.xs,
  },
  eyeBtn: { paddingHorizontal: Spacing.md },
  switchRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: Spacing.xs, marginTop: Spacing.lg },
  switchText: { color: Colors.neutral[300], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody },
  switchLink: { color: Colors.primary[400], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBodyBold },
});
