import { useState } from 'react';
import {
  StyleSheet, Text, View, Modal, Pressable, TextInput, ScrollView, Alert, KeyboardAvoidingView, Platform,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { addReport, currentUserId } from '@/data/mock';
import { Button } from './Button';
import { X, Flag, AlertTriangle } from 'lucide-react-native';

const PRESETS = [
  '虚假信息 / 诈骗',
  '骚扰 / 不友善行为',
  '广告 / 垃圾信息',
  '冒充他人',
  '色情 / 违规内容',
  '其他原因',
];

type Props = {
  visible: boolean;
  onClose: () => void;
  target: string; // e.g. "帖子" or "用户：夜雨听风"
};

export function ReportModal({ visible, onClose, target }: Props) {
  const [selected, setSelected] = useState<string | null>(null);
  const [detail, setDetail] = useState('');

  function submit() {
    if (!selected && !detail.trim()) {
      Alert.alert('提示', '请选择举报原因或填写补充说明');
      return;
    }
    const reason = selected ?? '未指定';
    const msg = detail.trim() ? `${reason} — ${detail.trim()}` : reason;
    addReport({ target, reason, detail: detail.trim(), reporterId: currentUserId });
    Alert.alert('举报已提交', `针对 ${target} 的举报已收到，我们会尽快处理。\n\n举报原因：${msg}`);
    setSelected(null);
    setDetail('');
    onClose();
  }

  return (
    <Modal visible={visible} animationType="slide" presentationStyle="pageSheet">
      <View style={styles.screen}>
        <SafeAreaView edges={['top']} style={styles.navSafe}>
          <View style={styles.navBar}>
            <Pressable
              onPress={() => { setSelected(null); setDetail(''); onClose(); }}
              style={styles.navBack}
            >
              <X color={Colors.neutral[50]} size={24} />
            </Pressable>
            <Text style={styles.navTitle}>举报</Text>
            <View style={{ width: 40 }} />
          </View>
        </SafeAreaView>

        <KeyboardAvoidingView
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
          style={{ flex: 1 }}
        >
          <ScrollView
            showsVerticalScrollIndicator={false}
            contentContainerStyle={{ paddingBottom: 40 }}
            keyboardShouldPersistTaps="handled"
          >
            <View style={styles.section}>
              <View style={styles.targetRow}>
                <AlertTriangle color={Colors.accent[400]} size={18} />
                <Text style={styles.targetText}>
                  举报对象：{target}
                </Text>
              </View>
            </View>

            <View style={styles.section}>
              <Text style={styles.label}>选择举报原因</Text>
              <View style={styles.presetGrid}>
                {PRESETS.map((p) => {
                  const active = selected === p;
                  return (
                    <Pressable
                      key={p}
                      onPress={() => setSelected(p)}
                      style={[styles.presetItem, active && { backgroundColor: Colors.danger[400], borderColor: Colors.danger[400] }]}
                    >
                      <Text style={[styles.presetText, active && { color: Colors.ink[900], fontFamily: Typography.fontFamilyBodyBold }]}>
                        {p}
                      </Text>
                    </Pressable>
                  );
                })}
              </View>
            </View>

            <View style={styles.section}>
              <Text style={styles.label}>补充说明（可选）</Text>
              <TextInput
                value={detail}
                onChangeText={setDetail}
                placeholder="详细描述举报原因…"
                placeholderTextColor={Colors.neutral[400]}
                style={styles.input}
                multiline
                numberOfLines={4}
                maxLength={300}
                textAlignVertical="top"
              />
            </View>

            <View style={styles.section}>
              <Button
                label="提交举报"
                color={Colors.danger[400]}
                icon={<Flag color={Colors.ink[900]} size={18} />}
                size="lg"
                onPress={submit}
              />
              <Text style={styles.hint}>我们将在24小时内处理您的举报</Text>
            </View>
          </ScrollView>
        </KeyboardAvoidingView>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  navSafe: { backgroundColor: Colors.ink[900] },
  navBar: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingHorizontal: Spacing.lg, paddingVertical: Spacing.sm,
    borderBottomWidth: 1, borderBottomColor: Colors.border,
  },
  navBack: {
    width: 40, height: 40, borderRadius: 999, backgroundColor: Colors.ink[800],
    alignItems: 'center', justifyContent: 'center',
  },
  navTitle: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.lg },
  section: { paddingHorizontal: Spacing.xl, marginTop: Spacing.lg },
  targetRow: {
    flexDirection: 'row', alignItems: 'center', gap: Spacing.sm,
    backgroundColor: `${Colors.accent[400]}15`, padding: Spacing.md,
    borderRadius: Radius.md, borderWidth: 1, borderColor: `${Colors.accent[400]}33`,
  },
  targetText: { color: Colors.neutral[100], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBodyMedium, flex: 1 },
  label: {
    color: Colors.neutral[200], fontSize: Typography.sizes.sm,
    fontFamily: Typography.fontFamilyBodyMedium, marginBottom: Spacing.sm,
  },
  presetGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.sm },
  presetItem: {
    paddingHorizontal: Spacing.lg, paddingVertical: Spacing.sm,
    borderRadius: Radius.md, borderWidth: 1, borderColor: Colors.border,
    backgroundColor: Colors.ink[800],
  },
  presetText: { color: Colors.neutral[200], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody },
  input: {
    backgroundColor: Colors.ink[800], borderWidth: 1, borderColor: Colors.border,
    borderRadius: Radius.md, paddingHorizontal: Spacing.md, paddingVertical: Spacing.md,
    color: Colors.neutral[50], fontFamily: Typography.fontFamilyBody, fontSize: Typography.sizes.base,
    minHeight: 100,
  },
  hint: {
    textAlign: 'center', color: Colors.neutral[400], fontSize: Typography.sizes.xs,
    fontFamily: Typography.fontFamilyBody, marginTop: Spacing.md,
  },
});
