import React from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, radius, shadow } from '../theme';

export function Button({ title, onPress, variant = 'primary', disabled, loading, style, small, icon }) {
  const v = variants[variant] || variants.primary;
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled || loading}
      style={({ pressed }) => [
        styles.btn,
        small && styles.btnSmall,
        { backgroundColor: v.bg, borderColor: v.border },
        (disabled || loading) && { opacity: 0.5 },
        pressed && { opacity: 0.85 },
        style,
      ]}
    >
      {loading ? (
        <ActivityIndicator color={v.text} />
      ) : (
        <Text style={[styles.btnText, small && { fontSize: 14 }, { color: v.text }]}>
          {icon ? `${icon}  ` : ''}
          {title}
        </Text>
      )}
    </Pressable>
  );
}

const variants = {
  primary: { bg: colors.primary, border: colors.primary, text: colors.white },
  success: { bg: colors.success, border: colors.success, text: colors.white },
  danger: { bg: colors.danger, border: colors.danger, text: colors.white },
  outline: { bg: colors.white, border: colors.border, text: colors.primary },
  ghost: { bg: 'transparent', border: 'transparent', text: colors.primary },
};

export function Card({ children, style }) {
  return <View style={[styles.card, style]}>{children}</View>;
}

export function Badge({ text, color = colors.primary, bg = colors.primarySoft, style }) {
  return (
    <View style={[styles.badge, { backgroundColor: bg }, style]}>
      <Text style={[styles.badgeText, { color }]}>{text}</Text>
    </View>
  );
}

export function AppBar({ title, subtitle, left, right }) {
  return (
    <View style={styles.appBar}>
      <View style={{ width: 64 }}>{left}</View>
      <View style={{ flex: 1, alignItems: 'center' }}>
        <Text style={styles.appBarTitle} numberOfLines={1}>{title}</Text>
        {subtitle ? <Text style={styles.appBarSub} numberOfLines={1}>{subtitle}</Text> : null}
      </View>
      <View style={{ width: 64, alignItems: 'flex-end' }}>{right}</View>
    </View>
  );
}

export function Stat({ label, value, color = colors.text, bg = colors.bg }) {
  return (
    <View style={[styles.stat, { backgroundColor: bg }]}>
      <Text style={[styles.statValue, { color }]}>{value}</Text>
      <Text style={styles.statLabel}>{label}</Text>
    </View>
  );
}

export function ErrorBox({ message }) {
  if (!message) return null;
  return (
    <View style={styles.error}>
      <Text style={{ color: colors.danger, fontSize: 14 }}>{message}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  btn: {
    height: 48,
    borderRadius: radius.md,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 18,
    borderWidth: 1,
  },
  btnSmall: { height: 38, paddingHorizontal: 12 },
  btnText: { fontSize: 16, fontWeight: '600' },
  card: { backgroundColor: colors.card, borderRadius: radius.lg, padding: 16, ...shadow },
  badge: { paddingHorizontal: 10, paddingVertical: 4, borderRadius: radius.pill, alignSelf: 'flex-start' },
  badgeText: { fontSize: 12, fontWeight: '700' },
  appBar: {
    height: 56,
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    backgroundColor: colors.primary,
  },
  appBarTitle: { color: colors.white, fontSize: 17, fontWeight: '700' },
  appBarSub: { color: '#D6E4F0', fontSize: 12 },
  stat: { flex: 1, borderRadius: radius.md, paddingVertical: 12, alignItems: 'center' },
  statValue: { fontSize: 22, fontWeight: '800' },
  statLabel: { fontSize: 12, color: colors.muted, marginTop: 2 },
  error: { backgroundColor: colors.dangerSoft, borderRadius: radius.sm, padding: 12, marginVertical: 8 },
});
