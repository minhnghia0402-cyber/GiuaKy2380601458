import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Badge, Button, Card } from '../components/UI';
import ConfirmDialog from '../components/ConfirmDialog';
import Countdown from '../components/Countdown';
import PassageHtml from '../components/PassageHtml';
import { api } from '../api';
import { clearLocalAnswers, loadLocalAnswers, saveLocalAnswers } from '../storage';
import { colors, font, radius } from '../theme';

const PART5_PAGE_SIZE = 5; // Part 5 chia trang, mỗi trang 5 câu

/** Chia đề thành các "trang": Part 5 theo từng 5 câu, Part 6/7 mỗi bài đọc một trang */
function buildPages(parts) {
  const pages = [];
  for (const part of parts) {
    for (const group of part.groups) {
      if (group.passages.length === 0) {
        for (let i = 0; i < group.questions.length; i += PART5_PAGE_SIZE) {
          pages.push({ part, passages: [], questions: group.questions.slice(i, i + PART5_PAGE_SIZE) });
        }
      } else {
        pages.push({ part, passages: group.passages, questions: group.questions });
      }
    }
  }
  return pages;
}

const isTimeUpError = (e) => /hết giờ|đã được nộp|đã nộp/i.test(e?.message || '');

export default function TakeExamScreen({ attempt, onFinished, onExit, onUnauthorized }) {
  const attemptId = attempt.attemptId;
  const pages = useMemo(() => buildPages(attempt.parts), [attempt]);
  const allQuestions = useMemo(() => pages.flatMap((p, pageIndex) => p.questions.map((q) => ({ ...q, pageIndex }))), [pages]);
  const endAt = useMemo(() => Date.now() + attempt.remainingSeconds * 1000, [attempt]);

  const [pageIndex, setPageIndex] = useState(0);
  const [answers, setAnswers] = useState(() => {
    const init = {};
    (attempt.savedAnswers || []).forEach((a) => { init[a.questionId] = a.optionId; });
    return init;
  });
  const [saveState, setSaveState] = useState({}); // questionId -> 'saving' | 'saved' | 'error'
  const [flags, setFlags] = useState({});
  const [locked, setLocked] = useState(attempt.remainingSeconds <= 0);
  const [showNav, setShowNav] = useState(false);
  const [confirmSubmit, setConfirmSubmit] = useState(false);
  const [confirmExit, setConfirmExit] = useState(false);
  const [warning, setWarning] = useState(null);
  const [timeUp, setTimeUp] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState('');

  const versions = useRef({});      // chống ghi đè khi bấm đổi đáp án liên tục
  const answersRef = useRef(answers);
  const scrollRef = useRef(null);
  const questionY = useRef({});
  const submittedRef = useRef(false);
  answersRef.current = answers;

  // ---------- Khôi phục đáp án lưu trên máy (vd: mất mạng trước khi kịp gửi) ----------
  useEffect(() => {
    let alive = true;
    loadLocalAnswers(attemptId).then((local) => {
      if (!alive) return;
      const diff = Object.entries(local).filter(([qid, opt]) => answersRef.current[qid] !== opt);
      if (diff.length === 0) return;
      setAnswers((prev) => ({ ...prev, ...local }));
      diff.forEach(([qid, opt]) => sendAnswer(Number(qid), opt));
    });
    return () => { alive = false; };
  }, [attemptId]); // eslint-disable-line react-hooks/exhaustive-deps

  // ---------- Gửi 1 đáp án lên server ----------
  const sendAnswer = useCallback(async (questionId, optionId) => {
    const v = (versions.current[questionId] || 0) + 1;
    versions.current[questionId] = v;
    setSaveState((s) => ({ ...s, [questionId]: 'saving' }));
    try {
      await api.saveAnswer(attemptId, questionId, optionId);
      if (versions.current[questionId] === v) setSaveState((s) => ({ ...s, [questionId]: 'saved' }));
    } catch (e) {
      if (e.unauthorized) return onUnauthorized();
      if (isTimeUpError(e)) {
        handleExpire();
        return;
      }
      if (versions.current[questionId] === v) setSaveState((s) => ({ ...s, [questionId]: 'error' }));
    }
  }, [attemptId]); // eslint-disable-line react-hooks/exhaustive-deps

  // ---------- Chọn / đổi / huỷ đáp án ----------
  const choose = (questionId, optionId) => {
    if (locked) return;
    const next = answersRef.current[questionId] === optionId ? null : optionId; // bấm lại để huỷ chọn
    const updated = { ...answersRef.current, [questionId]: next };
    setAnswers(updated);
    saveLocalAnswers(attemptId, updated);
    sendAnswer(questionId, next);
  };

  const retryFailed = async () => {
    const failed = Object.entries(saveState).filter(([, st]) => st === 'error').map(([qid]) => Number(qid));
    await Promise.all(failed.map((qid) => sendAnswer(qid, answersRef.current[qid] ?? null)));
  };

  // ---------- Nộp bài ----------
  const submit = async (auto) => {
    if (submittedRef.current) return;
    submittedRef.current = true;
    setLocked(true);
    setSubmitting(true);
    setSubmitError('');
    try {
      if (!auto) await retryFailed();
      const result = await api.submit(attemptId);
      await clearLocalAnswers(attemptId);
      onFinished(result);
    } catch (e) {
      if (e.unauthorized) return onUnauthorized();
      try {
        // có thể server đã tự nộp khi hết giờ -> lấy kết quả
        const result = await api.getResult(attemptId);
        await clearLocalAnswers(attemptId);
        onFinished(result);
      } catch {
        submittedRef.current = false;
        setSubmitting(false);
        setSubmitError(e.message);
        if (!auto) setLocked(false);
      }
    }
  };

  // ---------- Hết giờ: khoá và tự nộp ----------
  const handleExpire = () => {
    if (submittedRef.current) return;
    setLocked(true);
    setConfirmSubmit(false);
    setWarning(null);
    setTimeUp(true);
    submit(true);
  };

  useEffect(() => {
    if (attempt.remainingSeconds <= 0) handleExpire();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // ---------- Điều hướng ----------
  const goToPage = (i, questionId) => {
    setPageIndex(i);
    setShowNav(false);
    requestAnimationFrame(() => {
      const y = questionId ? questionY.current[questionId] : 0;
      scrollRef.current?.scrollTo({ y: Math.max(0, (y || 0) - 8), animated: false });
    });
  };

  const answeredCount = Object.values(answers).filter((v) => v != null).length;
  const unanswered = allQuestions.filter((q) => answers[q.questionId] == null);
  const page = pages[pageIndex];
  const isLast = pageIndex === pages.length - 1;

  return (
    <View style={{ flex: 1, backgroundColor: colors.bg }}>
      {/* ===== Thanh trên: thoát · đồng hồ · nộp bài ===== */}
      <View style={styles.topBar}>
        <Pressable onPress={() => setConfirmExit(true)} style={styles.topBtn}>
          <Text style={styles.topBtnText}>✕</Text>
        </Pressable>
        <View style={{ flex: 1, alignItems: 'center' }}>
          <Countdown endAt={endAt} onWarn={(m) => setWarning(m)} onExpire={handleExpire} />
        </View>
        <Pressable onPress={() => setConfirmSubmit(true)} disabled={locked} style={[styles.submitPill, locked && { opacity: 0.5 }]}>
          <Text style={styles.submitPillText}>Nộp bài</Text>
        </Pressable>
      </View>

      {/* ===== Tiến độ ===== */}
      <View style={styles.progressWrap}>
        <View style={styles.progressBar}>
          <View style={[styles.progressFill, { width: `${(answeredCount * 100) / allQuestions.length}%` }]} />
        </View>
        <Text style={styles.progressText}>Đã làm {answeredCount}/{allQuestions.length}</Text>
      </View>

      {locked && !timeUp ? (
        <View style={styles.lockedBar}><Text style={styles.lockedText}>🔒 Đã khoá chọn đáp án</Text></View>
      ) : null}

      {/* ===== Nội dung trang ===== */}
      <ScrollView ref={scrollRef} contentContainerStyle={{ padding: 14, paddingBottom: 24 }}>
        <View style={styles.partHeader}>
          <Text style={styles.partTitle}>
            PART {page.part.partNumber} · Câu {page.questions[0]?.displayNumber}
            {page.questions.length > 1 ? `–${page.questions[page.questions.length - 1]?.displayNumber}` : ''}
          </Text>
          {page.part.directions ? <Text style={styles.partDir}>{page.part.directions}</Text> : null}
        </View>

        {page.passages.map((html, i) => (
          <Card key={i} style={{ marginBottom: 12, padding: 12 }}>
            {page.passages.length > 1 ? <Badge text={`Bài đọc ${i + 1}/${page.passages.length}`} style={{ marginBottom: 8 }} /> : null}
            <PassageHtml html={html} />
          </Card>
        ))}

        {page.questions.map((q) => (
          <View key={q.questionId} onLayout={(e) => { questionY.current[q.questionId] = e.nativeEvent.layout.y; }}>
            <QuestionCard
              q={q}
              selected={answers[q.questionId]}
              status={saveState[q.questionId]}
              flagged={!!flags[q.questionId]}
              locked={locked}
              onChoose={(optId) => choose(q.questionId, optId)}
              onFlag={() => setFlags((f) => ({ ...f, [q.questionId]: !f[q.questionId] }))}
              onRetry={() => sendAnswer(q.questionId, answers[q.questionId] ?? null)}
            />
          </View>
        ))}
      </ScrollView>

      {/* ===== Thanh dưới ===== */}
      <View style={styles.bottomBar}>
        <Button title="‹ Trước" variant="outline" small disabled={pageIndex === 0} onPress={() => goToPage(pageIndex - 1)} style={{ flex: 1 }} />
        <Button title={`☰ ${pageIndex + 1}/${pages.length}`} variant="ghost" small onPress={() => setShowNav(true)} style={{ flex: 1, marginHorizontal: 8 }} />
        {isLast ? (
          <Button title="Nộp bài" variant="success" small disabled={locked} onPress={() => setConfirmSubmit(true)} style={{ flex: 1 }} />
        ) : (
          <Button title="Tiếp ›" small onPress={() => goToPage(pageIndex + 1)} style={{ flex: 1 }} />
        )}
      </View>

      {/* ===== Bảng điều hướng câu hỏi ===== */}
      <Modal visible={showNav} transparent animationType="slide" onRequestClose={() => setShowNav(false)}>
        <View style={styles.sheetOverlay}>
          <View style={styles.sheet}>
            <View style={styles.sheetHeader}>
              <Text style={font.h2}>Danh sách câu hỏi</Text>
              <Pressable onPress={() => setShowNav(false)}><Text style={{ fontSize: 22, color: colors.muted }}>✕</Text></Pressable>
            </View>
            <View style={styles.legend}>
              <Legend color={colors.primary} text="Đã làm" />
              <Legend color={colors.white} border text="Chưa làm" />
              <Legend color={colors.accent} text="Đánh dấu" />
            </View>
            <ScrollView>
              {attempt.parts.map((part) => {
                const qs = allQuestions.filter((q) => q.partNumber === part.partNumber);
                return (
                  <View key={part.partNumber} style={{ marginBottom: 12 }}>
                    <Text style={styles.navPart}>Part {part.partNumber}</Text>
                    <View style={styles.grid}>
                      {qs.map((q) => {
                        const done = answers[q.questionId] != null;
                        const flagged = flags[q.questionId];
                        const current = q.pageIndex === pageIndex;
                        return (
                          <Pressable
                            key={q.questionId}
                            onPress={() => goToPage(q.pageIndex, q.questionId)}
                            style={[
                              styles.cell,
                              done && { backgroundColor: colors.primary, borderColor: colors.primary },
                              flagged && { backgroundColor: colors.accent, borderColor: colors.accent },
                              current && { borderWidth: 2, borderColor: colors.text },
                            ]}
                          >
                            <Text style={[styles.cellText, (done || flagged) && { color: colors.white }]}>{q.displayNumber}</Text>
                          </Pressable>
                        );
                      })}
                    </View>
                  </View>
                );
              })}
            </ScrollView>
          </View>
        </View>
      </Modal>

      {/* ===== Xác nhận nộp bài ===== */}
      <ConfirmDialog
        visible={confirmSubmit}
        title="Nộp bài thi?"
        message={
          <View>
            <Text style={font.body}>
              Bạn đã trả lời <Text style={{ fontWeight: '700' }}>{answeredCount}/{allQuestions.length}</Text> câu.
            </Text>
            {unanswered.length > 0 ? (
              <View style={styles.warnBox}>
                <Text style={{ color: colors.danger, fontWeight: '700' }}>⚠ Còn {unanswered.length} câu chưa trả lời</Text>
                <Text style={[font.small, { marginTop: 4 }]} numberOfLines={3}>
                  Câu: {unanswered.slice(0, 20).map((q) => q.displayNumber).join(', ')}{unanswered.length > 20 ? '…' : ''}
                </Text>
              </View>
            ) : (
              <Text style={[font.small, { marginTop: 6, color: colors.success }]}>✓ Bạn đã trả lời tất cả câu hỏi.</Text>
            )}
            {Object.values(saveState).includes('error') ? (
              <Text style={[font.small, { marginTop: 6, color: colors.accent }]}>Một số đáp án chưa lưu được, hệ thống sẽ gửi lại trước khi nộp.</Text>
            ) : null}
            {submitError ? <Text style={{ color: colors.danger, marginTop: 8 }}>{submitError}</Text> : null}
            <Text style={[font.small, { marginTop: 8 }]}>Sau khi nộp, bạn không thể thay đổi đáp án.</Text>
          </View>
        }
        confirmText="Nộp bài"
        confirmVariant="success"
        loading={submitting}
        onCancel={() => setConfirmSubmit(false)}
        onConfirm={() => submit(false)}
      />

      {/* ===== Cảnh báo sắp hết giờ ===== */}
      <ConfirmDialog
        visible={warning != null && !timeUp}
        title={`⏰ Còn ${warning} phút!`}
        message={`Thời gian làm bài sắp hết. Bạn còn ${unanswered.length} câu chưa trả lời. Hết giờ hệ thống sẽ tự động nộp bài.`}
        confirmText="Tiếp tục làm bài"
        onConfirm={() => setWarning(null)}
      />

      {/* ===== Hết giờ ===== */}
      <ConfirmDialog
        visible={timeUp}
        title="⌛ Hết giờ làm bài"
        message={submitError ? `Không nộp được bài: ${submitError}` : 'Đã khoá chọn đáp án. Hệ thống đang tự động nộp bài...'}
        confirmText={submitError ? 'Thử nộp lại' : undefined}
        loading={submitting}
        onConfirm={submitError ? () => submit(true) : undefined}
      />

      {/* ===== Thoát giữa chừng ===== */}
      <ConfirmDialog
        visible={confirmExit}
        title="Tạm rời bài thi?"
        message="Các đáp án đã được lưu. Bạn có thể vào lại để làm tiếp, nhưng đồng hồ vẫn tiếp tục chạy."
        confirmText="Rời bài thi"
        confirmVariant="danger"
        onCancel={() => setConfirmExit(false)}
        onConfirm={() => { setConfirmExit(false); onExit(); }}
      />
    </View>
  );
}

// ================= Một câu hỏi =================
const QuestionCard = React.memo(function QuestionCard({ q, selected, status, flagged, locked, onChoose, onFlag, onRetry }) {
  return (
    <Card style={[styles.qCard, flagged && { borderColor: colors.accent, borderWidth: 1.5 }]}>
      <View style={styles.qHeader}>
        <View style={styles.qNumber}><Text style={styles.qNumberText}>{q.displayNumber}</Text></View>
        <View style={{ flex: 1 }}>
          {q.content ? (
            <Text style={font.body}>{q.content}</Text>
          ) : (
            <Text style={[font.small, { fontStyle: 'italic', marginTop: 6 }]}>Chọn đáp án cho chỗ trống ({q.displayNumber}) trong bài đọc</Text>
          )}
        </View>
        <Pressable onPress={onFlag} hitSlop={8} style={{ paddingLeft: 8 }}>
          <Text style={{ fontSize: 18, opacity: flagged ? 1 : 0.35 }}>🚩</Text>
        </Pressable>
      </View>

      {q.options.map((o) => {
        const active = selected === o.optionId;
        return (
          <Pressable
            key={o.optionId}
            disabled={locked}
            onPress={() => onChoose(o.optionId)}
            style={({ pressed }) => [styles.option, active && styles.optionActive, pressed && !locked && { opacity: 0.8 }]}
          >
            <View style={[styles.optLabel, active && styles.optLabelActive]}>
              <Text style={[styles.optLabelText, active && { color: colors.white }]}>{o.label}</Text>
            </View>
            <Text style={[font.body, { flex: 1 }, active && { color: colors.primary, fontWeight: '600' }]}>{o.content}</Text>
          </Pressable>
        );
      })}

      <View style={styles.qFooter}>
        {selected != null && !locked ? <Text style={font.small}>Bấm lại đáp án đã chọn để huỷ</Text> : <View />}
        {status === 'saving' ? <Text style={styles.status}>Đang lưu…</Text> : null}
        {status === 'saved' ? <Text style={[styles.status, { color: colors.success }]}>✓ Đã lưu</Text> : null}
        {status === 'error' ? (
          <Pressable onPress={onRetry}><Text style={[styles.status, { color: colors.danger }]}>⚠ Chưa lưu · Thử lại</Text></Pressable>
        ) : null}
      </View>
    </Card>
  );
});

function Legend({ color, text, border }) {
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', marginRight: 16 }}>
      <View style={{ width: 14, height: 14, borderRadius: 4, backgroundColor: color, borderWidth: border ? 1 : 0, borderColor: colors.border, marginRight: 6 }} />
      <Text style={font.small}>{text}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  topBar: { height: 56, backgroundColor: colors.primary, flexDirection: 'row', alignItems: 'center', paddingHorizontal: 10 },
  topBtn: { width: 40, height: 40, alignItems: 'center', justifyContent: 'center' },
  topBtnText: { color: colors.white, fontSize: 20 },
  submitPill: { backgroundColor: colors.white, borderRadius: radius.pill, paddingHorizontal: 14, paddingVertical: 7 },
  submitPillText: { color: colors.primary, fontWeight: '700' },
  progressWrap: { backgroundColor: colors.white, paddingHorizontal: 14, paddingVertical: 8, flexDirection: 'row', alignItems: 'center', borderBottomWidth: 1, borderBottomColor: colors.border },
  progressBar: { flex: 1, height: 8, backgroundColor: colors.bg, borderRadius: 4, overflow: 'hidden', marginRight: 10 },
  progressFill: { height: 8, backgroundColor: colors.success, borderRadius: 4 },
  progressText: { fontSize: 13, color: colors.muted, fontWeight: '600' },
  lockedBar: { backgroundColor: colors.dangerSoft, padding: 8, alignItems: 'center' },
  lockedText: { color: colors.danger, fontWeight: '600' },
  partHeader: { backgroundColor: colors.primarySoft, borderRadius: radius.md, padding: 12, marginBottom: 12 },
  partTitle: { color: colors.primary, fontWeight: '800', fontSize: 15 },
  partDir: { fontSize: 12, color: colors.muted, marginTop: 4, lineHeight: 17 },
  qCard: { marginBottom: 12, padding: 14, borderWidth: 1, borderColor: 'transparent' },
  qHeader: { flexDirection: 'row', alignItems: 'flex-start', marginBottom: 10 },
  qNumber: { minWidth: 40, height: 32, borderRadius: 16, backgroundColor: colors.primary, alignItems: 'center', justifyContent: 'center', marginRight: 10, paddingHorizontal: 6 },
  qNumberText: { color: colors.white, fontWeight: '800', fontSize: 14 },
  option: { flexDirection: 'row', alignItems: 'center', borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, padding: 10, marginBottom: 8, backgroundColor: colors.white },
  optionActive: { borderColor: colors.primary, backgroundColor: colors.primarySoft },
  optLabel: { width: 30, height: 30, borderRadius: 15, borderWidth: 1.5, borderColor: colors.border, alignItems: 'center', justifyContent: 'center', marginRight: 10 },
  optLabelActive: { backgroundColor: colors.primary, borderColor: colors.primary },
  optLabelText: { fontWeight: '700', color: colors.muted },
  qFooter: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginTop: 2 },
  status: { fontSize: 12, color: colors.muted, fontWeight: '600' },
  bottomBar: { flexDirection: 'row', padding: 10, backgroundColor: colors.white, borderTopWidth: 1, borderTopColor: colors.border },
  sheetOverlay: { flex: 1, backgroundColor: 'rgba(15,23,42,0.45)', justifyContent: 'flex-end' },
  sheet: { backgroundColor: colors.white, borderTopLeftRadius: 20, borderTopRightRadius: 20, padding: 16, maxHeight: '80%', width: '100%', maxWidth: 480, alignSelf: 'center' },
  sheetHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 },
  legend: { flexDirection: 'row', marginBottom: 12, flexWrap: 'wrap' },
  navPart: { fontWeight: '700', color: colors.primary, marginBottom: 6 },
  grid: { flexDirection: 'row', flexWrap: 'wrap' },
  cell: { width: 46, height: 40, borderRadius: 8, borderWidth: 1, borderColor: colors.border, alignItems: 'center', justifyContent: 'center', margin: 3, backgroundColor: colors.white },
  cellText: { fontWeight: '700', color: colors.text, fontSize: 13 },
  warnBox: { backgroundColor: colors.dangerSoft, borderRadius: radius.sm, padding: 10, marginTop: 10 },
});
