import {
  useQuery,
  useMutation,
  useQueryClient,
  type UseQueryOptions,
  type UseMutationOptions,
} from '@tanstack/react-query';
import {
  getInteractions,
  getInteraction,
  createInteraction,
  updateInteraction,
  deleteInteraction,
} from '../api';
import { customerKeys } from './useCustomers';
import type {
  ApiResponse,
  Interaction,
  CreateInteractionRequest,
  UpdateInteractionRequest,
} from '../types';

// Query keys for cache management
export const interactionKeys = {
  all: ['interactions'] as const,
  lists: () => [...interactionKeys.all, 'list'] as const,
  list: (customerId: string) => [...interactionKeys.lists(), customerId] as const,
  details: () => [...interactionKeys.all, 'detail'] as const,
  detail: (id: string) => [...interactionKeys.details(), id] as const,
};

/**
 * Hook to fetch interactions for a customer (timeline)
 */
export function useInteractions(
  customerId: string,
  options?: Omit<UseQueryOptions<ApiResponse<Interaction[]>>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: interactionKeys.list(customerId),
    queryFn: () => getInteractions(customerId),
    enabled: !!customerId,
    staleTime: 30 * 1000, // 30 seconds
    ...options,
  });
}

/**
 * Hook to fetch a single interaction by ID
 */
export function useInteraction(
  id: string,
  options?: Omit<UseQueryOptions<{ data: ApiResponse<Interaction>; etag?: string }>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: interactionKeys.detail(id),
    queryFn: () => getInteraction(id),
    enabled: !!id,
    staleTime: 30 * 1000,
    ...options,
  });
}


/**
 * Hook to create a new interaction
 */
export function useCreateInteraction(
  options?: UseMutationOptions<
    { data: ApiResponse<Interaction>; location?: string },
    Error,
    { customerId: string; request: CreateInteractionRequest }
  >
) {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ customerId, request }) => createInteraction(customerId, request),
    onSuccess: (_, variables) => {
      // Invalidate interaction list for this customer
      queryClient.invalidateQueries({ queryKey: interactionKeys.list(variables.customerId) });
      // Invalidate customer detail (LastInteractionAt may have changed)
      queryClient.invalidateQueries({ queryKey: customerKeys.detail(variables.customerId) });
      // Invalidate customer lists (LastInteractionAt affects sorting)
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() });
    },
    ...options,
  });
}

/**
 * Hook to update an existing interaction
 */
export function useUpdateInteraction(
  options?: UseMutationOptions<
    { data: ApiResponse<Interaction>; etag?: string },
    Error,
    { id: string; customerId: string; request: UpdateInteractionRequest; etag?: string }
  >
) {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, request, etag }) => updateInteraction(id, request, etag),
    onSuccess: (result, variables) => {
      // Update the cache with new data
      queryClient.setQueryData(interactionKeys.detail(variables.id), result);
      // Invalidate interaction list for this customer
      queryClient.invalidateQueries({ queryKey: interactionKeys.list(variables.customerId) });
    },
    ...options,
  });
}

/**
 * Hook to delete an interaction (physical delete)
 */
export function useDeleteInteraction(
  options?: UseMutationOptions<void, Error, { id: string; customerId: string; etag?: string }>
) {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, etag }) => deleteInteraction(id, etag),
    onSuccess: (_, variables) => {
      // Remove from cache
      queryClient.removeQueries({ queryKey: interactionKeys.detail(variables.id) });
      // Invalidate interaction list for this customer
      queryClient.invalidateQueries({ queryKey: interactionKeys.list(variables.customerId) });
      // Invalidate customer detail (LastInteractionAt may have changed)
      queryClient.invalidateQueries({ queryKey: customerKeys.detail(variables.customerId) });
      // Invalidate customer lists (LastInteractionAt affects sorting)
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() });
    },
    ...options,
  });
}
