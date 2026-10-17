import React from 'react';
import { Modal, StyleSheet, Text, View } from 'react-native';
import { Button } from './UI';
import { colors, radius } from '../theme';

// Hộp thoại xác nhận tự làm (Alert.alert không hoạt động trên bản web)
export default function ConfirmDialog({
  visible,
  title,
  message,
  confirmText = 'Đồng ý',
  cancelText = 'Huỷ',
  confirmVariant = 'primary',
  onConfirm,
  onCancel,
  loading,
}) {
  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onCancel}>
      <View style={styles.overlay}>
        <View style={styles.box}>
          <Text style={styles.title}>{title}</Text>
          {typeof message === 'string' ? <Text style={styles.message}>{message}</Text> : message}
          <View style={styles.row}>
            {onCancel ? (
              <Button title={cancelText} variant="outline" onPress={onCancel} style={{ flex: 1 }} disabled={loading} />
            ) : null}
            {onConfirm ? (
              <Button
                title={confirmText}
                variant={confirmVariant}
                onPress={onConfirm}
                style={{ flex: 1, marginLeft: onCancel ? 10 : 0 }}
                loading={loading}
              />
            ) : null}
          </View>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: { flex: 1, backgroundColor: 'rgba(15,23,42,0.5)', alignItems: 'center', justifyContent: 'center', padding: 24 },
  box: { width: '100%', maxWidth: 400, backgroundColor: colors.white, borderRadius: radius.lg, padding: 20 },
  title: { fontSize: 18, fontWeight: '700', color: colors.text, marginBottom: 8 },
  message: { fontSize: 15, color: colors.text, lineHeight: 22 },
  row: { flexDirection: 'row', marginTop: 20 },
});
