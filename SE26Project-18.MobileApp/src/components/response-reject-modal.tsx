import {
  Modal,
  Pressable,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { useTheme } from "../contexts/theme-context";

interface ResponseRejectModalProps {
  visible: boolean;
  onClose: () => void;
  onSubmit: () => void;
}

function ResponseRejectModal({
  visible,
  onClose,
  onSubmit: onReject,
}: ResponseRejectModalProps) {
  const { colors } = useTheme();
  const handleReject = () => {
    onReject();
    onClose();
  };

  const handleCancel = () => {
    onClose();
  };

  return (
    <Modal
      visible={visible}
      transparent
      animationType="fade"
      onRequestClose={onClose}
    >
      <View style={styles.avoidingView}>
        <Pressable
          style={[styles.overlay, { backgroundColor: colors.overlay }]}
          onPress={handleCancel}
        >
          <View style={[styles.container, { backgroundColor: colors.card }]}>
            <Text style={[styles.title, { color: colors.text }]}>确认拒绝</Text>
            <Text style={[styles.confirmText, { color: colors.textSecondary }]}>确定拒绝该回应吗？</Text>
            <View style={styles.buttonRow}>
              <TouchableOpacity
                style={[styles.button, { backgroundColor: colors.border }]}
                onPress={handleCancel}
              >
                <Text style={[styles.buttonText, { color: colors.text }]}>
                  取消
                </Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.button, { backgroundColor: colors.danger }]}
                onPress={handleReject}
              >
                <Text
                  style={[styles.buttonText, { color: colors.primaryText }]}
                >
                  拒绝
                </Text>
              </TouchableOpacity>
            </View>
          </View>
        </Pressable>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  avoidingView: {
    flex: 1,
  },
  overlay: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
  },
  container: {
    width: "80%",
    borderRadius: 12,
    padding: 20,
    elevation: 4,
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.15,
    shadowRadius: 8,
  },
  title: {
    fontSize: 18,
    fontWeight: "600",
    marginBottom: 16,
    textAlign: "center",
  },
  confirmText: { fontSize: 15, textAlign: "center", marginBottom: 20 },
  buttonRow: {
    flexDirection: "row",
    gap: 12,
  },
  button: {
    flex: 1,
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: "center",
  },
  buttonText: {
    fontSize: 16,
    fontWeight: "600",
  },
});

export default ResponseRejectModal;
