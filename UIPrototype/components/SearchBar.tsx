import { StyleSheet, View, TextInput } from 'react-native';
import { Search as SearchIcon } from 'lucide-react-native';
import { Colors, Radius, Spacing, Typography } from '@/constants/theme';

type Props = {
  value: string;
  onChangeText: (text: string) => void;
  placeholder?: string;
};

export function SearchBar({ value, onChangeText, placeholder = '搜索游戏、标签…' }: Props) {
  return (
    <View style={styles.wrap}>
      <SearchIcon color={Colors.neutral[300]} size={18} />
      <TextInput
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder}
        placeholderTextColor={Colors.neutral[400]}
        style={styles.input}
        returnKeyType="search"
      />
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.md,
    backgroundColor: Colors.ink[800],
    borderWidth: 1,
    borderColor: Colors.border,
    borderRadius: Radius.md,
    paddingHorizontal: Spacing.lg,
    height: 48,
  },
  input: {
    flex: 1,
    color: Colors.neutral[50],
    fontFamily: Typography.fontFamilyBody,
    fontSize: Typography.sizes.base,
    height: '100%',
  },
});
