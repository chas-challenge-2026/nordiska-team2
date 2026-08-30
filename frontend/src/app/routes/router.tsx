import { createBrowserRouter } from 'react-router-dom';
// import authRoute from './authRoutes';
// import dashboardRoute from './dashboardRoutes';
// import transactionRoute from './transactionRoutes';
// import taxReportRoute from './taxReportRoutes';
// import faqRoute from './faqRoutes';
// import { create } from 'domain';

import LoginPage from '../../features/auth/LoginPage';
import AuthenticatedLayout from '../layout/AuthenticatedLayout';
import DashboardPage from '../../features/dashboard/DashboardPage';
import TransactionPage from '../../features/transactions/TransactionPage';
import TaxReportsPage from '../../features/Taxreports/TaxReportsPage';
import FaqPage from '../../features/FAQ/FaqPage';


// const router = createBrowserRouter([
//   authRoute,
//   dashboardRoute,
//   transactionRoute,
//   taxReportRoute,
//   faqRoute,
// ]);

const router = createBrowserRouter([
   {
    path: "/login",
    element: <LoginPage />,
  },
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
    ],
  },
])

export default router;