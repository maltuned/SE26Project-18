import { ApiResponse, TokenResponse } from "./dtos";
import { tokenStorage } from "./token-storage";
import { API_BASE } from "./config";
import { showToast } from "../utils/toast";

let onAuthExpired: (() => void) | null = null;

export const setAuthExpiredHandler = (handler: () => void) => {
  onAuthExpired = handler;
};

let logoutInProgress = false;

export const setLogoutInProgress = (value: boolean) => {
  logoutInProgress = value;
};

const buildUrl = (endpoint: string, params?: Record<string, any>) => {
  const url = new URL(API_BASE + endpoint);
  if (params) {
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== "") {
        if (Array.isArray(value)) {
          value.forEach((v) => url.searchParams.append(key, String(v)));
        } else {
          url.searchParams.append(key, String(value));
        }
      }
    });
  }
  return url.toString();
};

const getAuthHeaders = async (): Promise<Record<string, string>> => {
  const token = await tokenStorage.getAccessToken();
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }
  return headers;
};

let refreshPromise: Promise<boolean> | null = null;

const tryRefreshToken = async (): Promise<boolean> => {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    try {
      const refreshToken = await tokenStorage.getRefreshToken();
      if (!refreshToken) return false;

      const res = await fetch(buildUrl("/Auth/refresh"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refresh_token: refreshToken }),
      });

      if (!res.ok) return false;

      const data: ApiResponse<TokenResponse> = await res.json();
      if (data.status !== 200 || !data.data) return false;

      await tokenStorage.setTokens(
        data.data.access_token,
        data.data.refresh_token,
        data.data.access_token_expires_at,
        data.data.refresh_token_expires_at,
      );
      return true;
    } catch {
      return false;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
};

async function authFetch(
  method: "GET" | "POST" | "PUT",
  endpoint: string,
  params?: Record<string, any>,
  body?: any,
): Promise<ApiResponse<any>> {
  const headers = await getAuthHeaders();
  const res = await fetch(
    buildUrl(endpoint, method === "GET" ? params : undefined),
    {
      method,
      headers,
      body: body && method !== "GET" ? JSON.stringify(body) : undefined,
    },
  );

  if (res.status === 401) {
    if (logoutInProgress) {
      return { status: 401, data: null as any, message: "" };
    }
    const refreshed = await tryRefreshToken();
    if (refreshed) {
      const retryHeaders = await getAuthHeaders();
      const retryRes = await fetch(
        buildUrl(endpoint, method === "GET" ? params : undefined),
        {
          method,
          headers: retryHeaders,
          body: body && method !== "GET" ? JSON.stringify(body) : undefined,
        },
      );
      if (retryRes.ok) return retryRes.json();
    }
    await tokenStorage.clearTokens();
    onAuthExpired?.();
    throw new Error("认证已过期");
  }

  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
}

export const apiGet = <T>(endpoint: string, params?: Record<string, any>) =>
  authFetch("GET", endpoint, params) as Promise<ApiResponse<T>>;

export const apiPost = <T>(endpoint: string, body?: any) =>
  authFetch("POST", endpoint, undefined, body) as Promise<ApiResponse<T>>;

export const apiPut = <T>(endpoint: string, body?: any) =>
  authFetch("PUT", endpoint, undefined, body) as Promise<ApiResponse<T>>;

export const apiPostNoAuth = async <T>(endpoint: string, body?: any): Promise<ApiResponse<T>> => {
  const res = await fetch(buildUrl(endpoint), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  return res.json();
};

export const buildUrlExport = buildUrl;

export const handleResponse = async <T>(
  promise: Promise<ApiResponse<any>>,
  mapper: (data: any) => T,
): Promise<T | null> => {
  try {
    const res = await promise;
    if (res.status >= 200 && res.status < 300 && res.data !== undefined && res.data !== null) {
      return mapper(res.data);
    }
    return null;
  } catch (e) {
    showToast("网络连接失败，请检查网络");
    return null;
  }
};

export const handleResponseDirect = async <T>(
  promise: Promise<ApiResponse<T>>,
): Promise<T | null> => {
  try {
    const res = await promise;
    if (res.status >= 200 && res.status < 300 && res.data !== undefined && res.data !== null) {
      return res.data;
    }
    return null;
  } catch (e) {
    showToast("网络连接失败，请检查网络");
    return null;
  }
};

export const handleArrayResponse = async <T>(
  promise: Promise<ApiResponse<any>>,
  mapper: (data: any) => T[],
): Promise<T[]> => {
  try {
    const res = await promise;
    if (res.status >= 200 && res.status < 300 && Array.isArray(res.data)) {
      return mapper(res.data);
    }
    return [];
  } catch (e) {
    showToast("网络连接失败，请检查网络");
    return [];
  }
};

export const handlePostResponse = async <T>(
  promise: Promise<ApiResponse<any>>,
  mapper: (data: any) => T,
): Promise<T> => {
  const res = await promise;
  if (res.status >= 200 && res.status < 300 && res.data !== undefined && res.data !== null) {
    return mapper(res.data);
  }
  throw new Error(`API Error [${res.status}]: ${res.message}`);
};