import AsyncStorage from '@react-native-async-storage/async-storage';

// Địa chỉ API mặc định: Web.Host chạy trên máy tính (dotnet run)
export const DEFAULT_API_URL = 'https://localhost:44311';

const KEYS = {
  token: 'toeic.token',
  user: 'toeic.user',
  apiUrl: 'toeic.apiUrl',
};

export async function getSession() {
  const [token, user, apiUrl] = await Promise.all([
    AsyncStorage.getItem(KEYS.token),
    AsyncStorage.getItem(KEYS.user),
    AsyncStorage.getItem(KEYS.apiUrl),
  ]);
  return { token, user, apiUrl: apiUrl || DEFAULT_API_URL };
}

export async function saveSession({ token, user, apiUrl }) {
  await AsyncStorage.multiSet([
    [KEYS.token, token],
    [KEYS.user, user],
    [KEYS.apiUrl, apiUrl],
  ]);
}

export async function clearSession() {
  await AsyncStorage.multiRemove([KEYS.token, KEYS.user]);
}

// ----- Lưu tạm đáp án trên máy (phòng khi mất mạng / tải lại trang) -----

const answersKey = (attemptId) => `toeic.answers.${attemptId}`;

export async function loadLocalAnswers(attemptId) {
  try {
    const raw = await AsyncStorage.getItem(answersKey(attemptId));
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

export async function saveLocalAnswers(attemptId, answers) {
  try {
    await AsyncStorage.setItem(answersKey(attemptId), JSON.stringify(answers));
  } catch {
    // bộ nhớ đầy hoặc bị chặn: bỏ qua, server vẫn lưu
  }
}

export async function clearLocalAnswers(attemptId) {
  await AsyncStorage.removeItem(answersKey(attemptId));
}
