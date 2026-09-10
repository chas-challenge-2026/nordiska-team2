import { createBrowserRouter } from 'react-router-dom';

import LoginPage from '../../features/auth/LoginPage';
import AuthenticatedLayout from '../layout/AuthenticatedLayout';
import DashboardPage from '../../features/dashboard/DashboardPage';
import TransactionPage from '../../features/transactions/TransactionPage';
import TaxReportsPage from '../../features/Taxreports/TaxReportsPage';
import FaqPage from '../../features/FAQ/FaqPage';
import ProtectedRoute from './ProtectedRoute';
import SettingsPage from '../../features/settings/SettingsPage';



const router = createBrowserRouter([
   {
    path: "/login",
    element: <LoginPage />,
  },

  {
    element: <ProtectedRoute />,
    children: [
  {
    path: "/",
    element: <AuthenticatedLayout />,
    children: [
      {
        path: "dashboard",
        element: <DashboardPage />,
      },
      {
        path: "transactions",
        element: <TransactionPage />,
      },
      {
        path: "taxreport",
        element: <TaxReportsPage />,
      },
      {
        path: "faq",
        element: <FaqPage />,
      },
      {
        path: "settings",
        element: <SettingsPage />
      }
    ],
  },
  ],
  },
])

export default router;