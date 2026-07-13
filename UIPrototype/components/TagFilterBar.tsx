import { ScrollView, Pressable } from 'react-native';
import { Chip } from './Chip';
import { Spacing } from '@/constants/theme';
import type { Tag } from '@/data/types';

type Props = {
  tags: Tag[];
  selectedIds: number[];
  onToggle: (tagId: number) => void;
};

export function TagFilterBar({ tags, selectedIds, onToggle }: Props) {
  return (
    <ScrollView
      horizontal
      showsHorizontalScrollIndicator={false}
      contentContainerStyle={{ paddingRight: Spacing.xl, gap: Spacing.sm }}
    >
      {tags.map((tag) => {
        const active = selectedIds.includes(tag.id);
        return (
          <Pressable key={tag.id} onPress={() => onToggle(tag.id)}>
            <Chip
              label={tag.name}
              color={tag.accentColor}
              solid={active}
              size="sm"
            />
          </Pressable>
        );
      })}
    </ScrollView>
  );
}
