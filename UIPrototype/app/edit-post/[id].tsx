import { useState } from 'react';
import { useLocalSearchParams, useRouter } from 'expo-router';
import {
  StyleSheet, Text, View, ScrollView, Pressable, TextInput,
  KeyboardAvoidingView, Platform, Alert,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Colors, Spacing, Radius, Typography } from '@/constants/theme';
import { posts, tags as allTags } from '@/data/mock';
import { Button } from '@/components/Button';
import { Chip } from '@/components/Chip';
import { DurationPicker } from '@/components/DurationPicker';
import { ChevronLeft, Save, X } from 'lucide-react-native';
import type { RecruitPost } from '@/data/types';

const GAME_TAGS = allTags.filter((t) => [1, 2, 3, 4, 5, 6, 7, 8].includes(t.id));
const EXTRA_TAGS = allTags.filter((t) => [9, 10, 12].includes(t.id));

const VOICE_OPTIONS = [
  { value: 'required' as const, label: '语音必开' },
  { value: 'optional' as const, label: '语音可选' },
  { value: 'none' as const, label: '无需语音' },
];
const MODE_OPTIONS = [
  { value: 'casual' as const, label: '休闲' },
  { value: 'ranked' as const, label: '排位' },
  { value: 'tournament' as const, label: '比赛' },
];

export default function EditPostScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const router = useRouter();
  const post = posts.find((p) => p.id === Number(id));

  if (!post) {
    return (
      <View style={styles.center}>
        <Text style={styles.notFound}>招募未找到</Text>
        <Pressable onPress={() => router.back()}><Text style={styles.backLink}>返回</Text></Pressable>
      </View>
    );
  }

  // Pre-seed all form state from the existing post
  const [selectedGameTag, setSelectedGameTag] = useState<number | null>(
    post.tagIds.find((tid) => GAME_TAGS.some((gt) => gt.id === tid)) ?? null,
  );
  const [selectedAuxTags, setSelectedAuxTags] = useState<number[]>(
    post.tagIds.filter((tid) => EXTRA_TAGS.some((et) => et.id === tid)),
  );
  const [title, setTitle] = useState(post.title);
  const [description, setDescription] = useState(post.description);
  const [needCount, setNeedCount] = useState(String(post.needCount));
  const [mode, setMode] = useState<RecruitPost['mode']>(post.mode);
  const [voice, setVoice] = useState<RecruitPost['voice']>(post.voice);
  const [platform, setPlatform] = useState(post.platform);
  const [durationMinutes, setDurationMinutes] = useState(post.durationMinutes);

  function toggleAuxTag(tagId: number) {
    setSelectedAuxTags((prev) =>
      prev.includes(tagId) ? prev.filter((id) => id !== tagId) : [...prev, tagId],
    );
  }

  function submit() {
    if (!selectedGameTag) {
      Alert.alert('提示', '请选择游戏标签');
      return;
    }
    if (!title.trim()) {
      Alert.alert('提示', '请输入招募标题');
      return;
    }

    const idx = posts.findIndex((p) => p.id === post!.id);
    if (idx === -1) return;

    const gameTag = allTags.find((t) => t.id === selectedGameTag)!;
    posts[idx] = {
      ...posts[idx],
      gameName: gameTag.name,
      tagIds: [selectedGameTag, ...selectedAuxTags],
      title: title.trim(),
      description: description.trim() || '一起开黑，欢乐无压力。',
      needCount: Math.max(1, Math.min(8, parseInt(needCount) || 2)),
      mode,
      voice,
      platform,
      durationMinutes,
      expiresAt: new Date(Date.now() + durationMinutes * 60_000).toISOString(),
    };

    Alert.alert('修改成功', '招募信息已更新');
    router.back();
  }

  const tagAccent = selectedGameTag
    ? allTags.find((t) => t.id === selectedGameTag)?.accentColor ?? Colors.primary[400]
    : Colors.primary[400];

  return (
    <View style={styles.screen}>
      <SafeAreaView edges={['top']} style={styles.navSafe}>
        <View style={styles.navBar}>
          <Pressable onPress={() => router.back()} style={styles.backBtn}>
            <X color={Colors.neutral[50]} size={24} />
          </Pressable>
          <Text style={styles.navTitle}>修改招募</Text>
          <View style={{ width: 40 }} />
        </View>
      </SafeAreaView>

      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        style={{ flex: 1 }}
      >
        <ScrollView
          showsVerticalScrollIndicator={false}
          contentContainerStyle={{ paddingBottom: 120 }}
          keyboardShouldPersistTaps="handled"
        >
          {/* Game tag selection */}
          <View style={styles.section}>
            <Text style={styles.label}>游戏类型（必选）</Text>
            <View style={styles.tagGrid}>
              {GAME_TAGS.map((tag) => {
                const active = selectedGameTag === tag.id;
                return (
                  <Pressable key={tag.id} onPress={() => setSelectedGameTag(tag.id)}>
                    <Chip label={tag.name} color={tag.accentColor} solid={active} size="md" />
                  </Pressable>
                );
              })}
            </View>
          </View>

          {/* Auxiliary tags */}
          <View style={styles.section}>
            <Text style={styles.label}>额外标签（可选）</Text>
            <View style={styles.tagGrid}>
              {EXTRA_TAGS.map((tag) => {
                const active = selectedAuxTags.includes(tag.id);
                return (
                  <Pressable key={tag.id} onPress={() => toggleAuxTag(tag.id)}>
                    <Chip label={tag.name} color={tag.accentColor} solid={active} size="md" />
                  </Pressable>
                );
              })}
            </View>
          </View>

          {/* Title */}
          <View style={styles.section}>
            <Text style={styles.label}>招募标题</Text>
            <TextInput
              value={title}
              onChangeText={setTitle}
              placeholder="一句话说明你想找什么样的搭子"
              placeholderTextColor={Colors.neutral[400]}
              style={styles.input}
              maxLength={40}
            />
          </View>

          {/* Description */}
          <View style={styles.section}>
            <Text style={styles.label}>详细说明</Text>
            <TextInput
              value={description}
              onChangeText={setDescription}
              placeholder="段位、时间、要求、玩法风格…"
              placeholderTextColor={Colors.neutral[400]}
              style={[styles.input, styles.textarea]}
              multiline
              numberOfLines={4}
              maxLength={300}
              textAlignVertical="top"
            />
          </View>

          {/* Need count */}
          <View style={styles.section}>
            <Text style={styles.label}>需要人数</Text>
            <View style={styles.stepper}>
              <Pressable
                style={styles.stepBtn}
                onPress={() => setNeedCount(String(Math.max(1, (parseInt(needCount) || 2) - 1)))}
              >
                <Text style={styles.stepBtnText}>−</Text>
              </Pressable>
              <Text style={styles.stepVal}>{needCount}</Text>
              <Pressable
                style={styles.stepBtn}
                onPress={() => setNeedCount(String(Math.min(8, (parseInt(needCount) || 2) + 1)))}
              >
                <Text style={styles.stepBtnText}>+</Text>
              </Pressable>
            </View>
          </View>

          {/* Mode */}
          <View style={styles.section}>
            <Text style={styles.label}>模式</Text>
            <View style={styles.segRow}>
              {MODE_OPTIONS.map((o) => (
                <Pressable
                  key={o.value}
                  onPress={() => setMode(o.value)}
                  style={[styles.segItem, mode === o.value && { backgroundColor: tagAccent, borderColor: tagAccent }]}
                >
                  <Text style={[styles.segText, mode === o.value && { color: Colors.ink[900], fontFamily: Typography.fontFamilyBodyBold }]}>
                    {o.label}
                  </Text>
                </Pressable>
              ))}
            </View>
          </View>

          {/* Voice */}
          <View style={styles.section}>
            <Text style={styles.label}>语音</Text>
            <View style={styles.segRow}>
              {VOICE_OPTIONS.map((o) => (
                <Pressable
                  key={o.value}
                  onPress={() => setVoice(o.value)}
                  style={[styles.segItem, voice === o.value && { backgroundColor: tagAccent, borderColor: tagAccent }]}
                >
                  <Text style={[styles.segText, voice === o.value && { color: Colors.ink[900], fontFamily: Typography.fontFamilyBodyBold }]}>
                    {o.label}
                  </Text>
                </Pressable>
              ))}
            </View>
          </View>

          {/* Platform */}
          <View style={styles.section}>
            <Text style={styles.label}>平台</Text>
            <View style={styles.segRow}>
              {['PC', '手机', '主机', '全平台'].map((p) => (
                <Pressable
                  key={p}
                  onPress={() => setPlatform(p)}
                  style={[styles.segItem, platform === p && { backgroundColor: tagAccent, borderColor: tagAccent }]}
                >
                  <Text style={[styles.segText, platform === p && { color: Colors.ink[900], fontFamily: Typography.fontFamilyBodyBold }]}>
                    {p}
                  </Text>
                </Pressable>
              ))}
            </View>
          </View>

          {/* Duration picker */}
          <View style={styles.section}>
            <Text style={styles.label}>招募持续时间</Text>
            <DurationPicker value={durationMinutes} onChange={setDurationMinutes} accent={tagAccent} />
          </View>

          {/* Submit */}
          <View style={styles.section}>
            <Button
              label="保存修改"
              color={tagAccent}
              icon={<Save color={Colors.ink[900]} size={18} />}
              size="lg"
              onPress={submit}
              disabled={!selectedGameTag || !title.trim()}
            />
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: Colors.ink[900] },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: Colors.ink[900], gap: Spacing.md },
  notFound: { color: Colors.neutral[100], fontSize: Typography.sizes.lg, fontFamily: Typography.fontFamilyDisplay },
  backLink: { color: Colors.primary[400], fontFamily: Typography.fontFamilyBodyBold },
  navSafe: { backgroundColor: Colors.ink[900] },
  navBar: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    paddingHorizontal: Spacing.lg, paddingVertical: Spacing.md,
    borderBottomWidth: 1, borderBottomColor: Colors.border,
  },
  backBtn: {
    width: 40, height: 40, borderRadius: 999, backgroundColor: Colors.ink[800],
    alignItems: 'center', justifyContent: 'center',
  },
  navTitle: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.lg },
  section: { paddingHorizontal: Spacing.xl, marginBottom: Spacing.lg },
  label: {
    color: Colors.neutral[200], fontSize: Typography.sizes.sm,
    fontFamily: Typography.fontFamilyBodyMedium, marginBottom: Spacing.sm,
  },
  tagGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.sm },
  input: {
    backgroundColor: Colors.ink[800], borderWidth: 1, borderColor: Colors.border,
    borderRadius: Radius.md, paddingHorizontal: Spacing.md, paddingVertical: Spacing.md,
    color: Colors.neutral[50], fontFamily: Typography.fontFamilyBody, fontSize: Typography.sizes.base,
  },
  textarea: { minHeight: 96 },
  stepper: {
    flexDirection: 'row', alignItems: 'center', gap: Spacing.md,
    backgroundColor: Colors.ink[800], borderWidth: 1, borderColor: Colors.border,
    borderRadius: Radius.md, paddingHorizontal: Spacing.sm, paddingVertical: Spacing.sm,
    alignSelf: 'flex-start',
  },
  stepBtn: {
    width: 40, height: 40, borderRadius: Radius.sm, backgroundColor: Colors.ink[700],
    alignItems: 'center', justifyContent: 'center',
  },
  stepBtnText: { color: Colors.neutral[50], fontSize: 22, fontFamily: Typography.fontFamilyDisplay, lineHeight: 24 },
  stepVal: { color: Colors.neutral[50], fontFamily: Typography.fontFamilyDisplay, fontSize: Typography.sizes.xl, minWidth: 30, textAlign: 'center' },
  segRow: { flexDirection: 'row', gap: Spacing.sm, flexWrap: 'wrap' },
  segItem: {
    paddingHorizontal: Spacing.lg, paddingVertical: Spacing.sm, borderRadius: Radius.md,
    borderWidth: 1, borderColor: Colors.border, backgroundColor: Colors.ink[800],
  },
  segText: { color: Colors.neutral[200], fontSize: Typography.sizes.sm, fontFamily: Typography.fontFamilyBody },
});
