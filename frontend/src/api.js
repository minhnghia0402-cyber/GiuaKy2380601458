// Gọi API của backend ASP.NET Boilerplate (Web.Host)
// ABP bọc kết quả dạng { result, success, error: { message, details } }

export class ApiError extends Error {
  constructor(message, status) {
    super(message);
    this.status = status;
    this.unauthorized = status === 401;
  }
}

let config = { baseUrl: '', token: null };

export function configureApi({ baseUrl, token }) {
  config = { baseUrl: (baseUrl || '').replace(/\/+$/, ''), token };
}

async function request(path, { method = 'GET', body, auth = true } = {}) {
  const headers = { 'Content-Type': 'application/json', Accept: 'application/json' };
  if (auth && config.token) headers.Authorization = `Bearer ${config.token}`;

  let response;
  try {
    response = await fetch(config.baseUrl + path, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch (e) {
    throw new ApiError(
      `Không kết nối được máy chủ ${config.baseUrl}. Kiểm tra Web.Host đã chạy (dotnet run) và địa chỉ API.`,
      0
    );
  }

  let data = null;
  try {
    data = await response.json();
  } catch {
    // phản hồi không phải JSON
  }

  if (!response.ok || (data && data.success === false)) {
    const msg = data?.error?.message
      ? data.error.message + (data.error.details ? ` ${data.error.details}` : '')
      : response.status === 401
        ? 'Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.'
        : `Lỗi máy chủ (HTTP ${response.status}).`;
    throw new ApiError(msg, response.status);
  }
  return data ? data.result : null;
}

const svc = (name) => `/api/services/app/StudentExam/${name}`;

export const api = {
  login: (userName, password) =>
    request('/api/TokenAuth/Authenticate', {
      method: 'POST',
      auth: false,
      body: { userNameOrEmailAddress: userName, password, rememberClient: true },
    }),
  getPublishedExams: () => request(svc('GetPublishedExams')),
  startAttempt: (examId) => request(svc('StartAttempt'), { method: 'POST', body: { id: examId } }),
  saveAnswer: (attemptId, questionId, optionId) =>
    request(svc('SaveAnswer'), { method: 'POST', body: { attemptId, questionId, optionId } }),
  submit: (attemptId) => request(svc('Submit'), { method: 'POST', body: { id: attemptId } }),
  getResult: (attemptId) => request(svc(`GetResult?Id=${attemptId}`)),
  getMyAttempts: () => request(svc('GetMyAttempts')),
};
