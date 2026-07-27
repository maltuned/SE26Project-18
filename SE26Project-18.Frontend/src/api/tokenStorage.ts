import * as SecureStore from "expo-secure-store";

const ACCESS_TOKEN_KEY = "access_token";
const REFRESH_TOKEN_KEY = "refresh_token";
const ACCESS_TOKEN_EXPIRES_KEY = "access_token_expires_at";
const REFRESH_TOKEN_EXPIRES_KEY = "refresh_token_expires_at";

export const tokenStorage = {
  async setTokens(
    accessToken: string,
    refreshToken: string,
    accessTokenExpiresAt: string,
    refreshTokenExpiresAt: string,
  ): Promise<void> {
    await Promise.all([
      SecureStore.setItemAsync(ACCESS_TOKEN_KEY, String(accessToken ?? "")),
      SecureStore.setItemAsync(REFRESH_TOKEN_KEY, String(refreshToken ?? "")),
      SecureStore.setItemAsync(ACCESS_TOKEN_EXPIRES_KEY, String(accessTokenExpiresAt ?? "")),
      SecureStore.setItemAsync(REFRESH_TOKEN_EXPIRES_KEY, String(refreshTokenExpiresAt ?? "")),
    ]);
  },

  async getAccessToken(): Promise<string | null> {
    return SecureStore.getItemAsync(ACCESS_TOKEN_KEY);
  },

  async getRefreshToken(): Promise<string | null> {
    return SecureStore.getItemAsync(REFRESH_TOKEN_KEY);
  },

  async getAccessTokenExpiresAt(): Promise<string | null> {
    return SecureStore.getItemAsync(ACCESS_TOKEN_EXPIRES_KEY);
  },

  async isAccessTokenExpired(): Promise<boolean> {
    const expiresAt = await SecureStore.getItemAsync(ACCESS_TOKEN_EXPIRES_KEY);
    if (!expiresAt) return true;
    return new Date(expiresAt).getTime() <= Date.now();
  },

  async clearTokens(): Promise<void> {
    await Promise.all([
      SecureStore.deleteItemAsync(ACCESS_TOKEN_KEY),
      SecureStore.deleteItemAsync(REFRESH_TOKEN_KEY),
      SecureStore.deleteItemAsync(ACCESS_TOKEN_EXPIRES_KEY),
      SecureStore.deleteItemAsync(REFRESH_TOKEN_EXPIRES_KEY),
    ]);
  },
};