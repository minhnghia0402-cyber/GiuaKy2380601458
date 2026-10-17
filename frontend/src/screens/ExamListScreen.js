import React, { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, StyleSheet, Text, View } from 'react-native';
import { AppBar, Badge, Button, Card, ErrorBox } from '../components/UI';
import { api } from '../api';
import { colors, font, radius } from '../theme';

const formatDate = (s) => {
  if (!s) return '';
  const d = new Date(s);
  const pad = (n) => String(n).padStart(2, '0');
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

export default function ExamListScreen({ user, onOpenExam, onOpenResult, onLogout, onUnauthorized }) {
  const [tab, setTab] = useState('exams');
  const [exams, setExams] = useState([]);
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setError('');
    try {
      const [e, h] = await Promise.all([api.getPublishedExams(), api.getMyAttempts()]);
      setExams(e || []);
      setHistory(h || []);
    } catch (err) {
      if (err.unauthorized) return onUnauthorized();
      setError(err.message);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [onUnauthorized]);

  useEffect(() => { load(); }, [load]);

  const refresh = () => { setRefreshing(true); load(); };

  return (
    <View style={{ flex: 1, backgroundColor: colors.bg }}>
      <AppBar
        title="Đề thi TOEIC Reading"
        subtitle={`Xin chào, ${user}`}
        left={<Button title="⟳" variant="ghost" small onPress={refresh} style={{ width: 44 }} />}
        right={<Pressable onPress={onLogout}><Text style={styles.logout}>Đăng xuất</Text></Pressable>}
      />

      <View style={styles.tabs}>
        {[['exams', `Đề đang mở (${exams.length})`], ['history', `Lịch sử (${history.length})`]].map(([key, label]) => (
          <Pressable key={key} onPress={() => setTab(key)} style={[styles.tab, tab === key && styles.tabActive]}>
            <Text style={[styles.tabText, tab === key && styles.tabTextActive]}>{label}</Text>
          </Pressable>
        ))}
      </View>

      <ScrollView
        contentContainerStyle={{ padding: 16, paddingBottom: 40 }}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={refresh} />}
      >
        <ErrorBox message={error} />
        {loading ? <ActivityIndicator size="large" color={colors.primary} style={{ marginTop: 40 }} /> : null}

        {!loading && tab === 'exams' && exams.length === 0 ? (
          <Empty icon="📭" text="Hiện chưa có đề thi nào được mở. Vui lòng quay lại sau." />
        ) : null}

        {tab === 'exams' && exams.map((e) => (
          <Card key={e.id} style={{ marginBottom: 14 }}>
            <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: 8 }}>
              <Badge text="Reading" />
              {e.inProgressAttemptId ? <Badge text="Đang làm dở" color={colors.accent} bg={colors.accentSoft} style={{ marginLeft: 6 }} /> : null}
            </View>
            <Text style={font.h2}>{e.title}</Text>
            <View style={styles.metaRow}>
              <Meta icon="⏱" text={`${e.durationMinutes} phút`} />
              <Meta icon="📝" text={`${e.totalQuestions} câu`} />
              <Meta icon="🔁" text={`${e.myAttemptCount} lượt`} />
            </View>
            {e.myBestCorrect != null ? (
              <Text style={[font.small, { marginBottom: 10 }]}>
                Kết quả tốt nhất: <Text style={{ color: colors.success, fontWeight: '700' }}>{e.myBestCorrect}/{e.totalQuestions}</Text> câu đúng
              </Text>
            ) : null}
            <Button
              title={e.inProgressAttemptId ? 'Làm tiếp' : e.myAttemptCount > 0 ? 'Thi lại' : 'Vào thi'}
              variant={e.inProgressAttemptId ? 'success' : 'primary'}
              onPress={() => onOpenExam(e)}
            />
          </Card>
        ))}

        {!loading && tab === 'history' && history.length === 0 ? (
          <Empty icon="🗂" text="Bạn chưa có lượt thi nào." />
        ) : null}

        {tab === 'history' && history.map((h) => {
          const percent = h.totalQuestions ? Math.round((h.correctCount * 100) / h.totalQuestions) : 0;
          return (
            <Pressable key={h.attemptId} onPress={() => onOpenResult(h.attemptId)}>
              <Card style={{ marginBottom: 12, flexDirection: 'row', alignItems: 'center' }}>
                <View style={styles.scoreCircle}>
                  <Text style={styles.scoreText}>{h.correctCount}</Text>
                  <Text style={styles.scoreSub}>/{h.totalQuestions}</Text>
                </View>
                <View style={{ flex: 1, marginLeft: 14 }}>
                  <Text style={{ fontWeight: '700', color: colors.text }} numberOfLines={1}>{h.examTitle}</Text>
                  <Text style={font.small}>{formatDate(h.submitTime || h.startTime)} · {percent}%</Text>
                  {h.status === 2 ? <Text style={[font.small, { color: colors.accent }]}>Tự động nộp khi hết giờ</Text> : null}
                </View>
                <Text style={{ color: colors.primary, fontWeight: '700' }}>Xem ›</Text>
              </Card>
            </Pressable>
          );
        })}
      </ScrollView>
    </View>
  );
}

function Meta({ icon, text }) {
  return (
    <View style={styles.meta}>
      <Text style={{ fontSize: 13 }}>{icon}</Text>
      <Text style={styles.metaText}>{text}</Text>
    </View>
  );
}

function Empty({ icon, text }) {
  return (
    <View style={{ alignItems: 'center', marginTop: 48, paddingHorizontal: 24 }}>
      <Text style={{ fontSize: 44 }}>{icon}</Text>
      <Text style={[font.small, { textAlign: 'center', marginTop: 8 }]}>{text}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  logout: { color: colors.white, fontSize: 13, fontWeight: '600', paddingHorizontal: 4 },
  tabs: { flexDirection: 'row', backgroundColor: colors.white, borderBottomWidth: 1, borderBottomColor: colors.border },
  tab: { flex: 1, paddingVertical: 14, alignItems: 'center', borderBottomWidth: 3, borderBottomColor: 'transparent' },
  tabActive: { borderBottomColor: colors.primary },
  tabText: { color: colors.muted, fontWeight: '600' },
  tabTextActive: { color: colors.primary },
  metaRow: { flexDirection: 'row', marginVertical: 10, flexWrap: 'wrap' },
  meta: { flexDirection: 'row', alignItems: 'center', backgroundColor: colors.bg, borderRadius: radius.pill, paddingHorizontal: 10, paddingVertical: 4, marginRight: 8, marginBottom: 4 },
  metaText: { marginLeft: 4, fontSize: 13, color: colors.text },
  scoreCircle: { width: 58, height: 58, borderRadius: 29, backgroundColor: colors.primarySoft, alignItems: 'center', justifyContent: 'center', flexDirection: 'row' },
  scoreText: { fontSize: 18, fontWeight: '800', color: colors.primary },
  scoreSub: { fontSize: 11, color: colors.muted, marginTop: 6 },
});
