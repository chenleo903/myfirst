import { apiClient } from './client';
import type {
  ApiResponse,
  Customer,
  PagedResponse,
  CreateCustomerRequest,
  UpdateCustomerRequest,
  CustomerSearchRequest,
} from '../types';

/**
 * Get paginated list of customers with optional filters
 */
export async function getCustomers(
  params: CustomerSearchRequest = {}
): Promise<ApiResponse<PagedResponse<Customer>>> {
  const response = await apiClient.get<ApiResponse<PagedResponse<Customer>>>('/customers', {
    params,
  });
  return response.data;
}

/**
 * Get a single customer by ID
 * Returns the customer data and ETag for optimistic concurrency
 */
export async function getCustomer(
  id: string
): Promise<{ data: ApiResponse<Customer>; etag?: string }> {
  const response = await apiClient.get<ApiResponse<Customer>>(`/customers/${id}`);
  return {
    data: response.data,
    etag: response.headers['etag'] as string | undefined,
  };
}

/**
 * Create a new customer
 */
export async function createCustomer(
  request: CreateCustomerRequest
): Promise<{ data: ApiResponse<Customer>; location?: string }> {
  const response = await apiClient.post<ApiResponse<Customer>>('/customers', request);
  return {
    data: response.data,
    location: response.headers['location'] as string | undefined,
  };
}

/**
 * Update an existing customer
 * @param id - Customer ID
 * @param request - Update data
 * @param etag - Optional ETag for optimistic concurrency control
 */
export async function updateCustomer(
  id: string,
  request: UpdateCustomerRequest,
  etag?: string
): Promise<{ data: ApiResponse<Customer>; etag?: string }> {
  const headers: Record<string, string> = {};
  if (etag) {
    headers['If-Match'] = etag;
  }
  
  const response = await apiClient.put<ApiResponse<Customer>>(
    `/customers/${id}`,
    request,
    { headers }
  );
  
  return {
    data: response.data,
    etag: response.headers['etag'] as string | undefined,
  };
}

/**
 * Soft delete a customer
 * @param id - Customer ID
 * @param etag - Optional ETag for optimistic concurrency control
 */
export async function deleteCustomer(id: string, etag?: string): Promise<void> {
  const headers: Record<string, string> = {};
  if (etag) {
    headers['If-Match'] = etag;
  }
  
  await apiClient.delete(`/customers/${id}`, { headers });
}
