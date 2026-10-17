import React, { useMemo, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { AppBar, Badge, Button, Card, Stat } from '../components/UI';
import { formatTime } from '../components/Countdown';
import { colors, font, radius } from '../theme';

export default function ResultScreen({ result, onBack }) {
  const [filter, setFilter] = useState('all');
  const [openHints, setOpenHints] = useState({});

  const percent = Math.round(result.scorePercent);
  const ringColor = percent >= 70 ? colors.success : percent >= 50 ? colors.accent : colors.danger;
  const review = useMemo(() => result.review.filter((r) =>
    filter === 'all' ? true : filter === 'wrong' ? !r.isCorrect && !r.isSkipped : filter === 'skipped' ? r.isSkipped : r.isCorrect
  ), [result, filter]);

  return (
    <View style={{ flex: 1, backgroundColor: colors.bg }}>
      <AppBar title="Kết quả bài thi" subtitle={result.title} />
      <ScrollView contentContainerStyle={{ padding: 16, paddingBottom: 40 }}>
        {/* ===== Điểm tổng ===== */}
        <Card style={{ alignItems: 'center', marginBottom: 14 }}>
          {result.autoSubmitted ? (
            <Badge text="Tự động nộp khi hết giờ" color={colors.accent} bg={colors.accentSoft} style={{ alignSelf: 'center', marginBottom: 10 }} />
          ) : null}
          <View style={[styles.ring, { borderColor: ringColor }]}>
            <Text style={[styles.ringValue, { color: ringColor }]}>{result.correctCount}</Text>
            <Text style={styles.ringSub}>/ {result.totalQuestions} câu đúng</Text>
          </View>
          <Text style={[font.h2, { marginTop: 12 }]}>{percent}%</Text>
          <Text style={font.small}>Thời gian làm bài: {formatTime(result.durationUsedSeconds)}</Text>
          <View style={styles.statsRow}>
            <Stat label="Đúng" value={result.correctCount} color={colors.success} bg={colors.successSoft} />
            <View style={{ width: 8 }} />
            <Stat label="Sai" value={result.wrongCount} color={colors.danger} bg={colors.dangerSoft} />
            <View style={{ width: 8 }} />
            <Stat label="Bỏ qua" value={result.skippedCount} color={colors.muted} bg={colors.bg} />
          </View>
        </Card>

        {/* ===== Theo từng Part ===== */}
        <Card style={{ marginBottom: 14 }}>
          <Text style={[font.h2, { fontSize: 16, marginBottom: 10 }]}>Kết quả theo phần</Text>
          {result.parts.map((p) => {
            const rate = p.total ? (p.correct * 100) / p.total : 0;
            return (
              <View key={p.partNumber} style={{ marginBottom: 12 }}>
                <View style={{ flexDirection: 'row', justifyContent: 'space-between', marginBottom: 4 }}>
                  <Text style={{ fontWeight: '700', color: colors.text }}>Part {p.partNumber}</Text>
                  <Text style={font.small}>{p.correct}/{p.total} đúng · {p.wrong} sai · {p.skipped} bỏ qua</Text>
                </View>
                <View style={styles.bar}><View style={[styles.barFill, { width: `${rate}%` }]} /></View>
              </View>
            );
          })}
        </Card>

        {/* ===== Xem lại từng câu ===== */}
        <Text style={[font.h2, { fontSize: 16, marginBottom: 8 }]}>Xem lại bài làm</Text>
        <View style={styles.filters}>
          {[
            ['all', `Tất cả (${result.review.length})`],
            ['wrong', `Sai (${result.wrongCount})`],
            ['skipped', `Bỏ qua (${result.skippedCount})`],
            ['correct', `Đúng (${result.correctCount})`],
          ].map(([key, label]) => (
            <Pressable key={key} onPress={() => setFilter(key)} style={[styles.chip, filter === key && styles.chipActive]}>
              <Text style={[styles.chipText, filter === key && { color: colors.white }]}>{label}</Text>
            </Pressable>
          ))}
        </View>

        {review.map((r) => {
          const tag = r.isSkipped
            ? { text: 'Bỏ qua', color: colors.muted, bg: colors.bg }
            : r.isCorrect
              ? { text: 'Đúng', color: colors.success, bg: colors.successSoft }
              : { text: 'Sai', color: colors.danger, bg: colors.dangerSoft };
          return (
            <Card key={r.questionId} style={{ marginBottom: 10, padding: 14 }}>
              <View style={{ flexDirection: 'row', alignItems: 'center', marginBottom: 8 }}>
                <Text style={styles.num}>Câu {r.displayNumber}</Text>
                <Badge text={tag.text} color={tag.color} bg={tag.bg} />
                <Text style={[font.small, { marginLeft: 'auto' }]}>Part {r.partNumber}</Text>
              </View>
              {r.content ? <Text style={[font.body, { marginBottom: 8 }]}>{r.content}</Text> : null}
              {r.options.map((o) => {
                const style = o.isCorrect ? styles.optCorrect : o.isSelected ? styles.optWrong : null;
                return (
                  <View key={o.label} style={[styles.opt, style]}>
                    <Text style={[styles.optText, o.isCorrect && { color: colors.success, fontWeight: '700' }, o.isSelected && !o.isCorrect && { color: colors.danger }]}>
                      ({o.label}) {o.content}
                      {o.isCorrect ? '  ✓' : ''}{o.isSelected && !o.isCorrect ? '  ✗ (bạn chọn)' : ''}
                    </Text>
                  </View>
                );
              })}
              <Pressable onPress={() => setOpenHints((h) => ({ ...h, [r.questionId]: !h[r.questionId] }))}>
                <Text style={styles.hintToggle}>{openHints[r.questionId] ? '▲ Ẩn gợi ý' : '💡 Xem gợi ý'}</Text>
              </Pressable>
              {openHints[r.questionId] ? <View style={styles.hint}><Text style={font.body}>{r.hint}</Text></View> : null}
            </Card>
          );
        })}

        <Button title="Về danh sách đề" onPress={onBack} style={{ marginTop: 10 }} />
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  ring: { width: 150, height: 150, borderRadius: 75, borderWidth: 10, alignItems: 'center', justifyContent: 'center' },
  ringValue: { fontSize: 44, fontWeight: '800' },
  ringSub: { fontSize: 12, color: colors.muted },
  statsRow: { flexDirection: 'row', marginTop: 16, alignSelf: 'stretch' },
  bar: { height: 10, backgroundColor: colors.bg, borderRadius: 5, overflow: 'hidden' },
  barFill: { height: 10, backgroundColor: colors.success, borderRadius: 5 },
  filters: { flexDirection: 'row', flexWrap: 'wrap', marginBottom: 10 },
  chip: { paddingHorizontal: 12, paddingVertical: 7, borderRadius: radius.pill, backgroundColor: colors.white, borderWidth: 1, borderColor: colors.border, marginRight: 6, marginBottom: 6 },
  chipActive: { backgroundColor: colors.primary, borderColor: colors.primary },
  chipText: { fontSize: 13, fontWeight: '600', color: colors.text },
  num: { fontWeight: '800', color: colors.primary, marginRight: 8 },
  opt: { borderRadius: radius.sm, paddingVertical: 6, paddingHorizontal: 10, marginBottom: 4 },
  optCorrect: { backgroundColor: colors.successSoft },
  optWrong: { backgroundColor: colors.dangerSoft },
  optText: { fontSize: 14, color: colors.text },
  hintToggle: { color: colors.primary, fontWeight: '600', marginTop: 8 },
  hint: { backgroundColor: colors.accentSoft, borderRadius: radius.sm, padding: 10, marginTop: 6 },
});
