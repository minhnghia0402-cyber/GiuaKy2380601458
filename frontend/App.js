import React, { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, Platform, StyleSheet, View } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import { StatusBar } from 'expo-status-bar';

import LoginScreen from './src/screens/LoginScreen';
import ExamListScreen from './src/screens/ExamListScreen';
import ExamIntroScreen from './src/screens/ExamIntroScreen';
import TakeExamScreen from './src/screens/TakeExamScreen';
import ResultScreen from './src/screens/ResultScreen';
import { ErrorBox, Button } from './src/components/UI';
import { api, configureApi } from './src/api';
import { clearSession, DEFAULT_API_URL, getSession } from './src/storage';
import { colors } from './src/theme';

/**
 * Điều hướng đơn giản bằng state:
 * login -> list -> intro -> take -> result -> list
 */
export default function App() {
  const [booting, setBooting] = useState(true);
  const [session, setSession] = useState(null);
  const [apiUrl, setApiUrl] = useState(DEFAULT_API_URL);
  const [screen, setScreen] = useState({ name: 'login' });
  const [error, setError] = useState('');

  useEffect(() => {
    getSession().then((s) => {
      setApiUrl(s.apiUrl);
      if (s.token) {
        configureApi({ baseUrl: s.apiUrl, token: s.token });
        setSession(s);
        setScreen({ name: 'list' });
      }
      setBooting(false);
    });
  }, []);

  const logout = useCallback(async () => {
    await clearSession();
    configureApi({ baseUrl: apiUrl, token: null });
    setSession(null);
    setScreen({ name: 'login' });
  }, [apiUrl]);

  const openResult = async (attemptId) => {
    setError('');
    try {
      const result = await api.getResult(attemptId);
      setScreen({ name: 'result', result });
    } catch (e) {
      if (e.unauthorized) return logout();
      setError(e.message);
    }
  };

  let content;
  if (booting) {
    content = <ActivityIndicator size="large" color={colors.primary} style={{ flex: 1 }} />;
  } else if (!session || screen.name === 'login') {
    content = (
      <LoginScreen
        initialApiUrl={apiUrl}
        onLoggedIn={(s) => { setSession(s); setApiUrl(s.apiUrl); setScreen({ name: 'list' }); }}
      />
    );
  } else if (screen.name === 'list') {
    content = (
      <View style={{ flex: 1 }}>
        {error ? <View style={{ padding: 8 }}><ErrorBox message={error} /><Button title="Đóng" small variant="outline" onPress={() => setError('')} /></View> : null}
        <ExamListScreen
          key={screen.refreshKey}
          user={session.user}
          onOpenExam={(exam) => setScreen({ name: 'intro', exam })}
          onOpenResult={openResult}
          onLogout={logout}
          onUnauthorized={logout}
        />
      </View>
    );
  } else if (screen.name === 'intro') {
    content = (
      <ExamIntroScreen
        exam={screen.exam}
        onBack={() => setScreen({ name: 'list', refreshKey: Date.now() })}
        onStarted={(attempt) => setScreen({ name: 'take', attempt })}
        onUnauthorized={logout}
      />
    );
  } else if (screen.name === 'take') {
    content = (
      <TakeExamScreen
        attempt={screen.attempt}
        onFinished={(result) => setScreen({ name: 'result', result })}
        onExit={() => setScreen({ name: 'list', refreshKey: Date.now() })}
        onUnauthorized={logout}
      />
    );
  } else if (screen.name === 'result') {
    content = <ResultScreen result={screen.result} onBack={() => setScreen({ name: 'list', refreshKey: Date.now() })} />;
  }

  return (
    <SafeAreaProvider>
      <View style={styles.page}>
        {/* Trên máy tính (web): giới hạn khung rộng như điện thoại để test giao diện mobile */}
        <SafeAreaView style={styles.phone} edges={['top', 'bottom']}>
          <StatusBar style="light" />
          {content}
        </SafeAreaView>
      </View>
    </SafeAreaProvider>
  );
}

const styles = StyleSheet.create({
  page: { flex: 1, backgroundColor: Platform.OS === 'web' ? '#DDE3EC' : colors.bg },
  phone: {
    flex: 1,
    width: '100%',
    maxWidth: 480,
    alignSelf: 'center',
    backgroundColor: colors.bg,
    ...(Platform.OS === 'web' ? { boxShadow: '0 0 24px rgba(15,23,42,0.15)' } : null),
  },
});
