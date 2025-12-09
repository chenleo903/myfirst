import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
  type UseMutationOptions,
} from '@tanstack/react-query';
import {
  getCustomers,
  getCustomer,
  createCustomer,
  updateCustomer,
  deleteCustomer,
} from '../api';
import type {
  ApiResponse,
  Customer,
  PagedResponse,
  CreateCustomerRequest,
  UpdateCustomerRequest,
  CustomerSearchRequest,
} from '../types';

// Query keys for cache management
export const customerKeys = {
  all: ['customers'] as const,
  lists: () => [...customerKeys.all, 'list'] as const,
  list: (params: CustomerSearchRequest) => [...customerKeys.lists(), params] as const,
  details: () => [...customerKeys.all, 'detail'] as const,
  detail: (id: string) => [...customerKeys.details(), id] as const,
};

/**
 * Hook to fetch paginated list of customers
 */
export function useCustomers(
  params: CustomerSearchRequest = {},
  options?: Omit<UseQueryOptions<ApiResponse<PagedResponse<Customer>>>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: customerKeys.list(params),
    queryFn: () => getCustomers(params),
    staleTime: 30 * 1000, // 30 seconds
    ...options,
  });
}

/**
 * Hook to fetch a single customer by ID
 */
export function useCustomer(
  id: string,
  options?: Omit<UseQueryOptions<{ data: ApiResponse<Customer>; etag?: string }>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: customerKeys.detail(id),
    queryFn: () => getCustomer(id),
    enabled: !!id,
    staleTime: 30 * 1000,
    ...options,
  });
}


/**
 * Hook to create a new customer
 */
export function useCreateCustomer(
  options?: UseMutationOptions<
    { data: ApiResponse<Customer>; location?: string },
    Error,
    CreateCustomerRequest
  >
) {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: createCustomer,
    onSuccess: () => {
      // Invalidate customer lists to refetch
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() });
    },
    ...options,
  });
}

/**
 * Hook to update an existing customer
 */
export function useUpdateCustomer(
  options?: UseMutationOptions<
    { data: ApiResponse<Customer>; etag?: string },
    Error,
    { id: string; request: UpdateCustomerRequest; etag?: string }
  >
) {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, request, etag }) => updateCustomer(id, request, etag),
    onSuccess: (result, variables) => {
      // Update the cache with new data
      queryClient.setQueryData(customerKeys.detail(variables.id), result);
      // Invalidate lists to refetch
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() });
    },
    ...options,
  });
}

/**
 * Hook to delete a customer (soft delete)
 */
export function useDeleteCustomer(
  options?: UseMutationOptions<void, Error, { id: string; etag?: string }>
) {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, etag }) => deleteCustomer(id, etag),
    onSuccess: (_, variables) => {
      // Remove from cache
      queryClient.removeQueries({ queryKey: customerKeys.detail(variables.id) });
      // Invalidate lists to refetch
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() });
    },
    ...options,
  });
}
