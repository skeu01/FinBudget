namespace Application.Auth;

public static class Permissions
{
    public const string ProfileRead = "profile.read";
    public const string HouseholdsRead = "households.read";
    public const string HouseholdsManage = "households.manage";
    public const string TransactionsRead = "transactions.read";
    public const string TransactionsManage = "transactions.manage";
    public const string ReportsExport = "reports.export";
    public const string UsersManage = "users.manage";

    public static IReadOnlyCollection<string> ForRole(string role) =>
        role switch
        {
            "Admin" =>
            [
                ProfileRead,
                HouseholdsRead,
                HouseholdsManage,
                TransactionsRead,
                TransactionsManage,
                ReportsExport,
                UsersManage
            ],
            "User" =>
            [
                ProfileRead,
                HouseholdsRead,
                HouseholdsManage,
                TransactionsRead,
                TransactionsManage,
                ReportsExport
            ],
            _ => []
        };
}
