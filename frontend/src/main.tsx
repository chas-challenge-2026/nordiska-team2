import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from 'react-router-dom'
import './index.css'
import router  from './app/routes/router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AlertProvider } from './hooks/AlertProvider'

const queryClient = new QueryClient()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <AlertProvider>
        <RouterProvider router={router} />  
      </AlertProvider>
    </QueryClientProvider>
  </StrictMode>,
)


