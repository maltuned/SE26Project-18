import { useLocalSearchParams, useRouter } from "expo-router";
import * as ImagePicker from "expo-image-picker";
import { useEffect, useState } from "react";
import { ActivityIndicator, Alert, KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, TextInput, TouchableOpacity, View } from "react-native";
import { ApiError, createGame, deleteGameCover, deleteGameIcon, getAdminGameById, getGameTags, TagResponse, updateGame, uploadGameCover, uploadGameIcon } from "../../api/api";
import AdminScreen from "../../components/admin-screen";
import MediaImage from "../../components/media-image";
import { useTheme } from "../../contexts/theme-context";

type Asset = ImagePicker.ImagePickerAsset;

export default function AdminGameEditScreen() {
  const router = useRouter();
  const { id } = useLocalSearchParams<{ id?: string }>();
  const gameId = id ? Number(id) : null;
  const { colors } = useTheme();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [tags, setTags] = useState<TagResponse[]>([]);
  const [tagIds, setTagIds] = useState<number[]>([]);
  const [iconUrl, setIconUrl] = useState("");
  const [coverUrl, setCoverUrl] = useState("");
  const [icon, setIcon] = useState<Asset | null>(null);
  const [cover, setCover] = useState<Asset | null>(null);
  const [deleteIcon, setDeleteIcon] = useState(false);
  const [deleteCover, setDeleteCover] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    Promise.all([getGameTags(), gameId ? getAdminGameById(gameId) : Promise.resolve(null)])
      .then(([catalog, game]) => {
        setTags(catalog);
        if (game) {
          setName(game.name); setDescription(game.description); setTagIds(game.tags.map((tag) => tag.id));
          setIconUrl(game.iconUrl); setCoverUrl(game.coverUrl);
        }
      })
      .catch((reason) => Alert.alert("加载失败", reason instanceof Error ? reason.message : "请稍后重试"))
      .finally(() => setLoading(false));
  }, [gameId]);

  const pick = async (kind: "icon" | "cover") => {
    const result = await ImagePicker.launchImageLibraryAsync({ mediaTypes: ["images"], quality: 0.9 });
    if (result.canceled) return;
    if (kind === "icon") { setIcon(result.assets[0]); setDeleteIcon(false); }
    else { setCover(result.assets[0]); setDeleteCover(false); }
  };

  const save = async () => {
    const cleanName = name.trim();
    if (!cleanName) return Alert.alert("请填写名称", "游戏名称不能为空。");
    if (cleanName.length > 200) return Alert.alert("名称过长", "游戏名称最多 200 个字符。");
    if (description.length > 4000) return Alert.alert("简介过长", "游戏简介最多 4000 个字符。");
    setSaving(true);
    try {
      const game = gameId
        ? await updateGame(gameId, { name: cleanName, description, tagIds })
        : await createGame({ name: cleanName, description, tagIds });
      const mediaTasks: { label: string; run: () => Promise<void> }[] = [];
      if (icon) mediaTasks.push({ label: "图标上传", run: () => uploadGameIcon(game.id, icon) });
      else if (gameId && deleteIcon) mediaTasks.push({ label: "图标删除", run: () => deleteGameIcon(game.id) });
      if (cover) mediaTasks.push({ label: "封面上传", run: () => uploadGameCover(game.id, cover) });
      else if (gameId && deleteCover) mediaTasks.push({ label: "封面删除", run: () => deleteGameCover(game.id) });

      const results = await Promise.allSettled(mediaTasks.map((task) => task.run()));
      const failed = results.flatMap((result, index) => result.status === "rejected" ? [mediaTasks[index].label] : []);
      if (failed.length) {
        Alert.alert("资料已保存，部分媒体失败", `${failed.join("、")}失败。游戏文字资料已经保存，可重新进入编辑页面重试。`, [{ text: "知道了", onPress: () => router.replace("/admin/games" as any) }]);
      } else {
        router.replace("/admin/games" as any);
      }
    } catch (reason) {
      Alert.alert("保存失败", reason instanceof ApiError ? reason.message : reason instanceof Error ? reason.message : "请稍后重试");
    } finally { setSaving(false); }
  };

  if (loading) return <AdminScreen title={gameId ? "编辑游戏" : "新建游戏"}><ActivityIndicator style={styles.loader} color={colors.primary} /></AdminScreen>;
  const iconPreview = icon?.uri || (!deleteIcon ? iconUrl : "");
  const coverPreview = cover?.uri || (!deleteCover ? coverUrl : "");

  return <AdminScreen title={gameId ? "编辑游戏" : "新建游戏"}>
    <KeyboardAvoidingView style={styles.flex} behavior={Platform.OS === "ios" ? "padding" : undefined}>
      <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
        <Text style={[styles.label, { color: colors.text }]}>游戏名称 <Text style={{ color: colors.textTertiary }}>{name.length}/200</Text></Text>
        <TextInput value={name} onChangeText={setName} maxLength={200} style={[styles.input, { backgroundColor: colors.card, borderColor: colors.inputBorder, color: colors.text }]} />
        <Text style={[styles.label, { color: colors.text }]}>简介 <Text style={{ color: colors.textTertiary }}>{description.length}/4000</Text></Text>
        <TextInput value={description} onChangeText={setDescription} maxLength={4000} multiline style={[styles.input, styles.description, { backgroundColor: colors.card, borderColor: colors.inputBorder, color: colors.text }]} />
        <Text style={[styles.label, { color: colors.text }]}>游戏标签</Text>
        <View style={styles.tags}>{tags.map((tag) => { const selected = tagIds.includes(tag.id); return <TouchableOpacity key={tag.id} onPress={() => setTagIds((old) => selected ? old.filter((value) => value !== tag.id) : [...old, tag.id])} style={[styles.tag, { backgroundColor: selected ? colors.primary : colors.filterInactive }]}><Text style={{ color: selected ? colors.primaryText : colors.filterTextInactive }}>{tag.name}</Text></TouchableOpacity>; })}</View>
        <View style={styles.mediaRow}>
          <MediaEditor title="图标" preview={iconPreview} aspectStyle={styles.icon} onPick={() => pick("icon")} onDelete={() => { setIcon(null); setDeleteIcon(true); }} colors={colors} />
          <MediaEditor title="封面" preview={coverPreview} aspectStyle={styles.cover} onPick={() => pick("cover")} onDelete={() => { setCover(null); setDeleteCover(true); }} colors={colors} />
        </View>
        <Text style={{ color: colors.textSecondary, lineHeight: 20 }}>新建时会先保存游戏资料，再上传图标和封面。媒体失败不会撤销已创建的游戏。</Text>
        <TouchableOpacity disabled={saving} onPress={save} style={[styles.save, { backgroundColor: saving ? colors.disabled : colors.primary }]}><Text style={[styles.saveText, { color: colors.primaryText }]}>{saving ? "保存中..." : "保存游戏"}</Text></TouchableOpacity>
      </ScrollView>
    </KeyboardAvoidingView>
  </AdminScreen>;
}

function MediaEditor({ title, preview, aspectStyle, onPick, onDelete, colors }: { title: string; preview: string; aspectStyle: object; onPick: () => void; onDelete: () => void; colors: ReturnType<typeof useTheme>["colors"] }) {
  return <View style={styles.mediaBox}><Text style={[styles.label, { color: colors.text }]}>{title}</Text><MediaImage uri={preview} style={[styles.preview, aspectStyle, { backgroundColor: colors.placeholder }]} /><View style={styles.mediaActions}><TouchableOpacity onPress={onPick}><Text style={{ color: colors.primary }}>{preview ? "替换" : "选择"}</Text></TouchableOpacity>{!!preview && <TouchableOpacity onPress={onDelete}><Text style={{ color: colors.danger }}>删除</Text></TouchableOpacity>}</View></View>;
}

const styles = StyleSheet.create({
  flex: { flex: 1 }, loader: { marginTop: 60 }, content: { padding: 18, gap: 10 }, label: { fontWeight: "700", marginTop: 4 }, input: { minHeight: 44, borderWidth: 1, borderRadius: 10, paddingHorizontal: 12, paddingVertical: 10 }, description: { height: 130, textAlignVertical: "top" }, tags: { flexDirection: "row", flexWrap: "wrap", gap: 8 }, tag: { paddingHorizontal: 12, paddingVertical: 8, borderRadius: 18 }, mediaRow: { flexDirection: "row", flexWrap: "wrap", gap: 22, marginTop: 8 }, mediaBox: { minWidth: 160, flexGrow: 1 }, preview: { marginVertical: 8, borderRadius: 10 }, icon: { width: 110, height: 110 }, cover: { width: 180, height: 110 }, mediaActions: { flexDirection: "row", gap: 20 }, save: { marginTop: 16, borderRadius: 12, paddingVertical: 14, alignItems: "center" }, saveText: { fontWeight: "700", fontSize: 16 },
});
