import React, { useEffect, useRef, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { colors, radius } from '../theme';

export function formatTime(totalSeconds) {
  const s = Math.max(0, Math.floor(totalSeconds));
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const sec = s % 60;
  const pad = (n) => String(n).padStart(2, '0');
  return h > 0 ? `${h}:${pad(m)}:${pad(sec)}` : `${pad(m)}:${pad(sec)}`;
}

/**
 * Đồng hồ đếm ngược. Tách thành component riêng để mỗi giây chỉ vẽ lại đồng hồ,
 * không vẽ lại cả trang đề (bài đọc có ảnh rất nặng).
 * - endAt: thời điểm hết giờ (ms) tính từ số giây server trả về
 * - onWarn(minutesLeft): gọi đúng 1 lần ở mốc 5 phút và 1 phút
 * - onExpire(): gọi 1 lần khi về 0
 */
export default function Countdown({ endAt, onWarn, onExpire }) {
  const [left, setLeft] = useState(() => Math.max(0, (endAt - Date.now()) / 1000));
  const fired = useRef({ 5: false, 1: false, expire: false });

  useEffect(() => {
    const tick = () => {
      const remain = Math.max(0, (endAt - Date.now()) / 1000);
      setLeft(remain);
      for (const m of [5, 1]) {
        if (!fired.current[m] && remain <= m * 60 && remain > 0) {
          fired.current[m] = true;
          onWarn && onWarn(m);
        }
      }
      if (!fired.current.expire && remain <= 0) {
        fired.current.expire = true;
        onExpire && onExpire();
      }
    };
    tick();
    const id = setInterval(tick, 1000);
    return () => clearInterval(id);
  }, [endAt]); // eslint-disable-line react-hooks/exhaustive-deps

  const danger = left <= 60;
  const warn = left <= 300;
  const bg = danger ? colors.danger : warn ? colors.accent : 'rgba(255,255,255,0.18)';
  return (
    <View style={[styles.pill, { backgroundColor: bg }]}>
      <Text style={styles.text}>⏱ {formatTime(left)}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  pill: { paddingHorizontal: 12, paddingVertical: 6, borderRadius: radius.pill },
  text: { color: colors.white, fontWeight: '800', fontSize: 15, fontVariant: ['tabular-nums'] },
});
