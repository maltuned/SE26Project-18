import { useEffect, useState } from "react";
import { ActivityIndicator, Alert, ScrollView, StyleSheet, Text, TextInput, TouchableOpacity, View } from "react-native";
import { createGameTag, createRecruitmentTag, createUserTag, getGameTags, getRecruitmentTags, getUserTags, TagResponse } from "../../api/api";
import AdminScreen from "../../components/admin-screen";
import { useTheme } from "../../contexts/theme-context";

type CatalogKey = "game" | "user" | "recruitment";
const catalogInfo = {
  game: { title: "游戏标签", create: createGameTag },
  user: { title: "用户标签", create: createUserTag },
  recruitment: { title: "招募标签", create: createRecruitmentTag },
};

export default function AdminTagsScreen() {
  const { colors } = useTheme();
  const [catalogs, setCatalogs] = useState<Record<CatalogKey, TagResponse[]>>({ game: [], user: [], recruitment: [] });
  const [inputs, setInputs] = useState<Record<CatalogKey, string>>({ game: "", user: "", recruitment: "" });
  const [errors, setErrors] = useState<Partial<Record<CatalogKey, string>>>({});
  const [submitting, setSubmitting] = useState<CatalogKey | null>(null);
  const [loading, setLoading] = useState(true);

  const refresh = async () => {
    try {
      const [game, user, recruitment] = await Promise.all([getGameTags(), getUserTags(), getRecruitmentTags()]);
      setCatalogs({ game, user, recruitment });
    } finally { setLoading(false); }
  };
  useEffect(() => { refresh(); }, []);

  const create = async (key: CatalogKey) => {
    const name = inputs[key].trim();
    if (!name) return setErrors((old) => ({ ...old, [key]: "标签名称不能为空" }));
    if (name.length > 100) return setErrors((old) => ({ ...old, [key]: "标签名称最多 100 个字符" }));
    setSubmitting(key); setErrors((old) => ({ ...old, [key]: "" }));
    try {
      const created = await catalogInfo[key].create(name);
      setCatalogs((old) => ({
        ...old,
        [key]: [...old[key].filter((tag) => tag.id !== created.id), created]
          .sort((left, right) => left.name.localeCompare(right.name, "zh-CN")),
      }));
      setInputs((old) => ({ ...old, [key]: "" }));
      try {
        await refresh();
      } catch (reason) {
        Alert.alert("刷新失败", reason instanceof Error ? reason.message : "标签已创建，目录刷新失败");
      }
    } catch (reason) {
      setErrors((old) => ({ ...old, [key]: reason instanceof Error ? reason.message : "创建失败" }));
    } finally { setSubmitting(null); }
  };

  return <AdminScreen title="标签目录">
    {loading ? <ActivityIndicator style={styles.loader} color={colors.primary} /> : <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      {(Object.keys(catalogInfo) as CatalogKey[]).map((key) => <View key={key} style={[styles.section, { backgroundColor: colors.card, borderColor: colors.borderLight }]}>
        <Text style={[styles.title, { color: colors.text }]}>{catalogInfo[key].title}</Text>
        <Text style={[styles.count, { color: colors.textSecondary }]}>{catalogs[key].length} 个标签，只支持创建</Text>
        <View style={styles.tags}>{catalogs[key].map((tag) => <View key={tag.id} style={[styles.tag, { backgroundColor: colors.tagBackground }]}><Text style={{ color: colors.tagText }}>{tag.name}</Text></View>)}</View>
        <View style={styles.form}><TextInput value={inputs[key]} onChangeText={(value) => setInputs((old) => ({ ...old, [key]: value }))} onSubmitEditing={() => create(key)} maxLength={100} placeholder="新标签名称" placeholderTextColor={colors.textTertiary} style={[styles.input, { borderColor: colors.inputBorder, color: colors.text, backgroundColor: colors.inputBackgroundAlt }]} /><TouchableOpacity disabled={submitting === key} onPress={() => create(key)} style={[styles.button, { backgroundColor: colors.primary }]}><Text style={{ color: colors.primaryText }}>{submitting === key ? "创建中" : "创建"}</Text></TouchableOpacity></View>
        {!!errors[key] && <Text style={{ color: colors.danger }}>{errors[key]}</Text>}
      </View>)}
    </ScrollView>}
  </AdminScreen>;
}

const styles = StyleSheet.create({
  loader: { marginTop: 60 }, content: { padding: 16, gap: 14 }, section: { borderWidth: 1, borderRadius: 14, padding: 16 }, title: { fontSize: 19, fontWeight: "700" }, count: { marginTop: 3, marginBottom: 12 }, tags: { flexDirection: "row", flexWrap: "wrap", gap: 7, marginBottom: 14 }, tag: { paddingHorizontal: 10, paddingVertical: 6, borderRadius: 14 }, form: { flexDirection: "row", gap: 8 }, input: { flex: 1, height: 42, borderWidth: 1, borderRadius: 10, paddingHorizontal: 12 }, button: { borderRadius: 10, justifyContent: "center", paddingHorizontal: 16 },
});
