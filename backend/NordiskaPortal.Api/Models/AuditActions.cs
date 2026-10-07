namespace NordiskaPortal.Api.Models
{
    // The audit action vocabulary. 
    // Plain strings rather than an enum so other features can add action without touching this file's callers.
    public static class AuditActions
    {
        public const string Login = "login";
        public const string LoginFailed = "login_failed";
        public const string BankIdLogin = "bankid_login";
        public const string Logout = "logout";

        public const string Deposit = "deposit";
        public const string Withdrawal = "withdrawal";

        public const string SavingsGoalCreated = "savings_goal_created";
        public const string SavingsGoalDeleted = "savings_goal_deleted";
        public const string TaxReportGenerated = "tax_report_generated";
        public const string NotificationSent = "notification_sent";
        public const string Transfer = "transfer";
        public const string UnauditedRequest = "unaudited_request";
        public const string AccountOpened = "account_opened";
        public const string AccountClosed = "account_closed";
    }
}