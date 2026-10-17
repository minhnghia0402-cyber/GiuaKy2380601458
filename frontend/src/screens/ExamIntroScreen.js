import React, { useState } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import { AppBar, Button, Card, ErrorBox } from '../components/UI';
import { api } from '../api';
import { colors, font } from '../theme';

export default function ExamIntroScreen({ exam, onBack, onStarted, onUnauthorized }) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const resume = !!exam.inProgressAttemptId;

  const start = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await api.startAttempt(exam.id);
      onStarted(data);
    } catch (e) {
      if (e.unauthorized) return onUnauthorized();
      setError(e.message);
      setLoading(false);
    }
  };

  const rules = [
    ['⏱', `Thời gian làm bài ${exam.durationMinutes} phút, đồng hồ đếm ngược theo giờ của máy chủ.`],
    ['💾', 'Mỗi đáp án được lưu tự động. Có thể đổi hoặc huỷ lựa chọn trước khi nộp.'],
    ['🔔', 'Hệ thống cảnh báo khi còn 5 phút và 1 phút.'],
    ['🔒', 'Hết giờ, bài thi tự động được nộp và khoá quyền chọn đáp án.'],
    ['🔀', 'Thứ tự câu hỏi và đáp án có thể được hoán vị cho từng lượt thi.'],
  ];

  return (
    <View style={{ flex: 1, backgroundColor: colors.bg }}>
      <AppBar title="Thông tin đề thi" left={<Button title="‹ Quay lại" variant="ghost" small onPress={onBack} style={{ paddingHorizontal: 0 }} />} />
      <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 40 }}>
        <Card style={{ marginBottom: 16 }}>
          <Text style={font.h2}>{exam.title}</Text>
          <View style={styles.statsRow}>
            <Box value={exam.totalQuestions} label="câu hỏi" />
            <Box value={exam.durationMinutes} label="phút" />
            <Box value="3" label="phần (5·6·7)" />
          </View>
        </Card>

        <Card style={{ marginBottom: 16 }}>
          <Text style={[font.h2, { fontSize: 16, marginBottom: 8 }]}>Cấu trúc đề</Text>
          <Row title="Part 5" desc="Hoàn thành câu (câu hỏi độc lập)" />
          <Row title="Part 6" desc="Hoàn thành đoạn văn (chọn từ / câu cho chỗ trống)" />
          <Row title="Part 7" desc="Đọc hiểu (đơn, đôi, ba bài đọc)" last />
        </Card>

        <Card style={{ marginBottom: 16 }}>
          <Text style={[font.h2, { fontSize: 16, marginBottom: 8 }]}>Quy định</Text>
          {rules.map(([icon, text]) => (
            <View key={text} style={styles.rule}>
              <Text style={{ width: 26 }}>{icon}</Text>
              <Text style={[font.body, { flex: 1, fontSize: 14 }]}>{text}</Text>
            </View>
          ))}
        </Card>

        <ErrorBox message={error} />
        <Button
          title={resume ? 'Làm tiếp bài thi' : 'Bắt đầu làm bài'}
          variant={resume ? 'success' : 'primary'}
          onPress={start}
          loading={loading}
        />
        {loading ? <Text style={[font.small, { textAlign: 'center', marginTop: 8 }]}>Đang tải đề (kèm hình ảnh bài đọc)...</Text> : null}
      </ScrollView>
    </View>
  );
}

function Box({ value, label }) {
  return (
    <View style={styles.box}>
      <Text style={styles.boxValue}>{value}</Text>
      <Text style={font.small}>{label}</Text>
    </View>
  );
}

function Row({ title, desc, last }) {
  return (
    <View style={[styles.partRow, !last && { borderBottomWidth: 1, borderBottomColor: colors.border }]}>
      <Text style={styles.partTitle}>{title}</Text>
      <Text style={[font.small, { flex: 1 }]}>{desc}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  statsRow: { flexDirection: 'row', marginTop: 14 },
  box: { flex: 1, alignItems: 'center', backgroundColor: colors.primarySoft, borderRadius: 12, paddingVertical: 12, marginHorizontal: 4 },
  boxValue: { fontSize: 22, fontWeight: '800', color: colors.primary },
  partRow: { flexDirection: 'row', alignItems: 'center', paddingVertical: 10 },
  partTitle: { width: 64, fontWeight: '700', color: colors.primary },
  rule: { flexDirection: 'row', paddingVertical: 6 },
});
