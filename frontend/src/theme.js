// Bảng màu & kích thước dùng chung cho toàn app (khớp với thiết kế Figma)
export const colors = {
  primary: '#1F4E79',
  primaryDark: '#163A5A',
  primarySoft: '#E8F0F8',
  accent: '#F2A900',
  accentSoft: '#FFF6DD',
  success: '#1E8E3E',
  successSoft: '#E6F4EA',
  danger: '#D93025',
  dangerSoft: '#FCE8E6',
  bg: '#F4F6FA',
  card: '#FFFFFF',
  text: '#1A1D23',
  muted: '#6B7280',
  border: '#E3E7EE',
  white: '#FFFFFF',
};

export const radius = { sm: 8, md: 12, lg: 16, pill: 999 };

export const shadow = {
  shadowColor: '#0F172A',
  shadowOpacity: 0.08,
  shadowRadius: 10,
  shadowOffset: { width: 0, height: 3 },
  elevation: 2,
};

export const font = {
  h1: { fontSize: 24, fontWeight: '700', color: colors.text },
  h2: { fontSize: 18, fontWeight: '700', color: colors.text },
  body: { fontSize: 15, color: colors.text, lineHeight: 22 },
  small: { fontSize: 13, color: colors.muted },
};
