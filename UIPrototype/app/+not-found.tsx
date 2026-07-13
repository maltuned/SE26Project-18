import { StyleSheet, Text, View } from 'react-native';
import { Link } from 'expo-router';
import { Colors } from '@/constants/theme';

export default function NotFound() {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>页面未找到</Text>
      <Link href="/" style={styles.link}>
        <Text style={styles.linkText}>返回首页</Text>
      </Link>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: Colors.ink[900] },
  title: { color: Colors.neutral[50], fontSize: 20, fontFamily: 'Inter-SemiBold' },
  link: { marginTop: 16 },
  linkText: { color: Colors.primary[400], fontSize: 16 },
});
