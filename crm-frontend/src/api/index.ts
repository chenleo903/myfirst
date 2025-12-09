export { apiClient } from './client';

// Customer API
export {
  getCustomers,
  getCustomer,
  createCustomer,
  updateCustomer,
  deleteCustomer,
} from './customers';

// Interaction API
export {
  getInteractions,
  getInteraction,
  createInteraction,
  updateInteraction,
  deleteInteraction,
} from './interactions';

// Auth API
export {
  login,
  logout,
  getToken,
  isAuthenticated,
} from './auth';
