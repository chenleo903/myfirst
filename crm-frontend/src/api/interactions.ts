import { apiClient } from './client';
import type {
  ApiResponse,
  Interaction,
  CreateInteractionRequest,
  UpdateInteractionRequest,
} from '../types';

/**
 * Get all interactions for a customer (timeline)
 * Returns interactions sorted by happenedAt descending
 */
export async function getInteractions(
  customerId: string
): Promise<ApiResponse<Interaction[]>> {
  const response = await apiClient.get<ApiResponse<Interaction[]>>(
    `/customers/${customerId}/interactions`
  );
  return response.data;
}

/**
 * Get a single interaction by ID
 * Returns the interaction data and ETag for optimistic concurrency
 */
export async function getInteraction(
  id: string
): Promise<{ data: ApiResponse<Interaction>; etag?: string }> {
  const response = await apiClient.get<ApiResponse<Interaction>>(`/interactions/${id}`);
  return {
    data: response.data,
    etag: response.headers['etag'] as string | undefined,
  };
}

/**
 * Create a new interaction for a customer
 */
export async function createInteraction(
  customerId: string,
  request: CreateInteractionRequest
): Promise<{ data: ApiResponse<Interaction>; location?: string }> {
  const response = await apiClient.post<ApiResponse<Interaction>>(
    `/customers/${customerId}/interactions`,
    request
  );
  return {
    data: response.data,
    location: response.headers['location'] as string | undefined,
  };
}

/**
 * Update an existing interaction
 * @param id - Interaction ID
 * @param request - Update data
 * @param etag - Optional ETag for optimistic concurrency control
 */
export async function updateInteraction(
  id: string,
  request: UpdateInteractionRequest,
  etag?: string
): Promise<{ data: ApiResponse<Interaction>; etag?: string }> {
  const headers: Record<string, string> = {};
  if (etag) {
    headers['If-Match'] = etag;
  }
  
  const response = await apiClient.put<ApiResponse<Interaction>>(
    `/interactions/${id}`,
    request,
    { headers }
  );
  
  return {
    data: response.data,
    etag: response.headers['etag'] as string | undefined,
  };
}

/**
 * Delete an interaction (physical delete)
 * @param id - Interaction ID
 * @param etag - Optional ETag for optimistic concurrency control
 */
export async function deleteInteraction(id: string, etag?: string): Promise<void> {
  const headers: Record<string, string> = {};
  if (etag) {
    headers['If-Match'] = etag;
  }
  
  await apiClient.delete(`/interactions/${id}`, { headers });
}
