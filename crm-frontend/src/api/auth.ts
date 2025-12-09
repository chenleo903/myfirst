import { apiClient } from './client';
import type { ApiResponse, LoginRequest, LoginResponse } from '../types';

const TOKEN_KEY = 'token';
const TOKEN_EXPIRY_KEY = 'tokenExpiresAt';

/**
 * Login with username and password
 * Stores the token in localStorage on success
 */
export async function login(request: LoginRequest): Promise<ApiResponse<LoginResponse>> {
  const response = await apiClient.post<ApiResponse<LoginResponse>>('/api/auth/login', request);
  
  if (response.data.success && response.data.data) {
    localStorage.setItem(TOKEN_KEY, response.data.data.token);
    localStorage.setItem(TOKEN_EXPIRY_KEY, response.data.data.expiresAt);
  }
  
  return response.data;
}

/**
 * Logout - clears the stored token
 */
export function logout(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(TOKEN_EXPIRY_KEY);
}

/**
 * Get the current auth token
 */
export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

/**
 * Check if user is authenticated (has a non-expired token)
 */
export function isAuthenticated(): boolean {
  const token = localStorage.getItem(TOKEN_KEY);
  const expiresAt = localStorage.getItem(TOKEN_EXPIRY_KEY);
  
  if (!token || !expiresAt) {
    return false;
  }
  
  const expiryDate = new Date(expiresAt);
  return expiryDate > new Date();
}
