import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../client'

export function useApiTest() {
  return useQuery({
    queryKey: ['apiTest'],
    queryFn: async () => {
      const response = await apiClient.get('/health')
      return response.data
    },
  })
}