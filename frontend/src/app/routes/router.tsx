import { createBrowserRouter } from 'react-router-dom';
import authRoute from './authRoutes';
import dashboardRoute from './dashboardRoutes';
import transactionRoute from './transactionRoutes';
import taxReportRoute from './taxReportRoutes';
import faqRoute from './faqRoutes';

const router = createBrowserRouter([
  authRoute,
  dashboardRoute,
  transactionRoute,
  taxReportRoute,
  faqRoute,
]);

export default router;