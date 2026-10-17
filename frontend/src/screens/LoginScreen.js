import React, { useState } from 'react';
import { KeyboardAvoidingView, Platform, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, ErrorBox } from '../components/UI';
import { api, configureApi } from '../api';
import { saveSession } from '../storage';
import { colors, font, radius } from '../theme';

export default function LoginScreen({ initialApiUrl, onLoggedIn }) {
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [apiUrl, setApiUrl] = useState(initialApiUrl);
  const [showServer, setShowServer] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const login = async () => {
    if (!userName.trim() || !password) {
      setError('Vui lòng nhập tên đăng nhập và mật khẩu.');
      return;
    }
    setLoading(true);
    setError('');
    try {
      configureApi({ baseUrl: apiUrl, token: null });
      const result = await api.login(userName.trim(), password);
      const session = { token: result.accessToken, user: userName.trim(), apiUrl };
      await saveSession(session);
      configureApi({ baseUrl: apiUrl, token: result.accessToken });
      onLoggedIn(session);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <KeyboardAvoidingView style={{ flex: 1 }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView contentContainerStyle={styles.container} keyboardShouldPersistTaps="handled">
        <View style={styles.hero}>
          <View style={styles.logo}><Text style={styles.logoText}>R</Text></View>
          <Text style={styles.title}>TOEIC Reading</Text>
          <Text style={styles.subtitle}>Luyện thi 100 câu · 75 phút</Text>
        </View>

        <Card>
          <Text style={styles.label}>Tên đăng nhập</Text>
          <TextInput
            style={styles.input}
            value={userName}
            onChangeText={setUserName}
            placeholder="vd: sv01"
            autoCapitalize="none"
            autoCorrect={false}
          />
          <Text style={styles.label}>Mật khẩu</Text>
          <TextInput
            style={styles.input}
            value={password}
            onChangeText={setPassword}
            placeholder="••••••"
            secureTextEntry
            onSubmitEditing={login}
          />
          <ErrorBox message={error} />
          <Button title="Đăng nhập" onPress={login} loading={loading} style={{ marginTop: 8 }} />

          <Pressable onPress={() => setShowServer(!showServer)} style={{ marginTop: 16, alignSelf: 'center' }}>
            <Text style={font.small}>⚙ Cài đặt máy chủ {showServer ? '▲' : '▼'}</Text>
          </Pressable>
          {showServer ? (
            <View style={{ marginTop: 8 }}>
              <Text style={styles.label}>Địa chỉ API (Web.Host)</Text>
              <TextInput style={styles.input} value={apiUrl} onChangeText={setApiUrl} autoCapitalize="none" autoCorrect={false} />
            </View>
          ) : null}
        </Card>

        <Text style={[font.small, { textAlign: 'center', marginTop: 24 }]}>SV2380601458 · Bài giữa kỳ</Text>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: { flexGrow: 1, justifyContent: 'center', padding: 20, backgroundColor: colors.bg },
  hero: { alignItems: 'center', marginBottom: 24 },
  logo: {
    width: 72, height: 72, borderRadius: 20, backgroundColor: colors.primary,
    alignItems: 'center', justifyContent: 'center', marginBottom: 12,
  },
  logoText: { color: colors.white, fontSize: 36, fontWeight: '800' },
  title: { ...font.h1 },
  subtitle: { ...font.small, marginTop: 4 },
  label: { fontSize: 14, fontWeight: '600', color: colors.text, marginBottom: 6, marginTop: 8 },
  input: {
    height: 48, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md,
    paddingHorizontal: 14, fontSize: 16, backgroundColor: colors.white, color: colors.text,
  },
});
