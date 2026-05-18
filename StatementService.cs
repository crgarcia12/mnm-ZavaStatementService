using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;

namespace ZavaStatementService
{
    public class StatementService : IStatementService
    {
        public GeneratedStatementResponse GenerateStatement(int accountId, int month, int year)
        {
            ValidatePeriod(accountId, month, year);
            var periodStart = new DateTime(year, month, 1);
            var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

            using (var connection = new SqlConnection(DbConfig.GetConnectionString()))
            {
                connection.Open();
                EnsureGeneratedStatementsTableExists(connection);

                string accountNumber;
                decimal openingBalance;
                string customerName;

                using (var command = new SqlCommand(@"
SELECT TOP 1 a.AccountNumber, a.Balance, (c.FirstName + ' ' + c.LastName)
FROM Accounts a
INNER JOIN Customers c ON c.CustomerID = a.CustomerID
WHERE a.AccountID = @AccountID;", connection))
                {
                    command.Parameters.AddWithValue("@AccountID", accountId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            throw new FaultException("Account was not found.");
                        }

                        accountNumber = reader.GetString(0);
                        openingBalance = reader.GetDecimal(1);
                        customerName = reader.GetString(2);
                    }
                }

                var transactions = new List<string>();
                decimal totalCredits = 0m;
                decimal totalDebits = 0m;

                using (var command = new SqlCommand(@"
SELECT t.TransactionDate, tt.TypeCode, t.Description, t.Amount, t.BalanceAfter
FROM Transactions t
INNER JOIN TransactionTypes tt ON tt.TransactionTypeID = t.TransactionTypeID
WHERE t.AccountID = @AccountID
  AND t.TransactionDate >= @PeriodStart
  AND t.TransactionDate <= @PeriodEnd
ORDER BY t.TransactionDate ASC;", connection))
                {
                    command.Parameters.AddWithValue("@AccountID", accountId);
                    command.Parameters.AddWithValue("@PeriodStart", periodStart);
                    command.Parameters.AddWithValue("@PeriodEnd", periodEnd);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var txnDate = reader.GetDateTime(0);
                            var typeCode = reader.GetString(1);
                            var description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                            var amount = reader.GetDecimal(3);
                            var balanceAfter = reader.GetDecimal(4);

                            if (string.Equals(typeCode, "DEP", StringComparison.OrdinalIgnoreCase) || amount >= 0m)
                            {
                                totalCredits += amount;
                            }
                            else
                            {
                                totalDebits += Math.Abs(amount);
                            }

                            transactions.Add(string.Format(
                                "<tr><td>{0:yyyy-MM-dd}</td><td>{1}</td><td>{2}</td><td style='text-align:right'>{3:N2}</td><td style='text-align:right'>{4:N2}</td></tr>",
                                txnDate,
                                typeCode,
                                System.Security.SecurityElement.Escape(description),
                                amount,
                                balanceAfter));
                        }
                    }
                }

                var html = BuildHtmlStatement(accountId, accountNumber, customerName, periodStart, periodEnd, openingBalance, totalCredits, totalDebits, transactions);

                int requestId;
                using (var insertRequest = new SqlCommand(@"
INSERT INTO StatementRequests (CustomerID, AccountID, StatementType, PeriodStart, PeriodEnd, Format, Status, RequestedDate, CompletedDate)
SELECT TOP 1 CustomerID, @AccountID, 'Monthly', @PeriodStart, @PeriodEnd, 'HTML', 'Completed', GETDATE(), GETDATE()
FROM Accounts
WHERE AccountID = @AccountID;
SELECT CAST(SCOPE_IDENTITY() AS INT);", connection))
                {
                    insertRequest.Parameters.AddWithValue("@AccountID", accountId);
                    insertRequest.Parameters.AddWithValue("@PeriodStart", periodStart);
                    insertRequest.Parameters.AddWithValue("@PeriodEnd", periodEnd);
                    requestId = (int)insertRequest.ExecuteScalar();
                }

                int statementId;
                using (var insertGenerated = new SqlCommand(@"
INSERT INTO GeneratedStatements (RequestID, AccountID, StatementMonth, StatementYear, StatementHtml)
VALUES (@RequestID, @AccountID, @StatementMonth, @StatementYear, @StatementHtml);
SELECT CAST(SCOPE_IDENTITY() AS INT);", connection))
                {
                    insertGenerated.Parameters.AddWithValue("@RequestID", requestId);
                    insertGenerated.Parameters.AddWithValue("@AccountID", accountId);
                    insertGenerated.Parameters.AddWithValue("@StatementMonth", month);
                    insertGenerated.Parameters.AddWithValue("@StatementYear", year);
                    insertGenerated.Parameters.AddWithValue("@StatementHtml", html);
                    statementId = (int)insertGenerated.ExecuteScalar();
                }

                using (var insertArchive = new SqlCommand(@"
INSERT INTO StatementArchive (RequestID, AccountID, FilePath, FileSize, GeneratedDate, ExpiryDate, Checksum)
VALUES (@RequestID, @AccountID, @FilePath, @FileSize, GETDATE(), DATEADD(YEAR, 7, GETDATE()), @Checksum);", connection))
                {
                    insertArchive.Parameters.AddWithValue("@RequestID", requestId);
                    insertArchive.Parameters.AddWithValue("@AccountID", accountId);
                    insertArchive.Parameters.AddWithValue("@FilePath", "db://generated-statements/" + statementId);
                    insertArchive.Parameters.AddWithValue("@FileSize", html.Length);
                    insertArchive.Parameters.AddWithValue("@Checksum", ComputeChecksum(html));
                    insertArchive.ExecuteNonQuery();
                }

                return new GeneratedStatementResponse
                {
                    StatementId = statementId,
                    AccountId = accountId,
                    Month = month,
                    Year = year,
                    GeneratedDateUtc = DateTime.UtcNow,
                    HtmlContent = html
                };
            }
        }

        public StatementHistoryResponse GetStatementHistory(int accountId)
        {
            if (accountId <= 0)
            {
                throw new FaultException("accountId must be greater than 0.");
            }

            var history = new List<StatementHistoryItem>();
            using (var connection = new SqlConnection(DbConfig.GetConnectionString()))
            using (var command = new SqlCommand(@"
SELECT TOP 100 StatementID, AccountID, StatementMonth, StatementYear, GeneratedDate
FROM GeneratedStatements
WHERE AccountID = @AccountID
ORDER BY GeneratedDate DESC;", connection))
            {
                command.Parameters.AddWithValue("@AccountID", accountId);
                connection.Open();
                EnsureGeneratedStatementsTableExists(connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        history.Add(new StatementHistoryItem
                        {
                            StatementId = reader.GetInt32(0),
                            AccountId = reader.GetInt32(1),
                            Month = reader.GetInt32(2),
                            Year = reader.GetInt32(3),
                            GeneratedDateUtc = reader.GetDateTime(4).ToUniversalTime()
                        });
                    }
                }
            }

            return new StatementHistoryResponse
            {
                AccountId = accountId,
                Statements = history
            };
        }

        public StatementDetailResponse GetStatement(int statementId)
        {
            if (statementId <= 0)
            {
                throw new FaultException("statementId must be greater than 0.");
            }

            using (var connection = new SqlConnection(DbConfig.GetConnectionString()))
            using (var command = new SqlCommand(@"
SELECT StatementID, AccountID, StatementMonth, StatementYear, GeneratedDate, StatementHtml
FROM GeneratedStatements
WHERE StatementID = @StatementID;", connection))
            {
                command.Parameters.AddWithValue("@StatementID", statementId);
                connection.Open();
                EnsureGeneratedStatementsTableExists(connection);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new FaultException("Statement was not found.");
                    }

                    return new StatementDetailResponse
                    {
                        StatementId = reader.GetInt32(0),
                        AccountId = reader.GetInt32(1),
                        Month = reader.GetInt32(2),
                        Year = reader.GetInt32(3),
                        GeneratedDateUtc = reader.GetDateTime(4).ToUniversalTime(),
                        HtmlContent = reader.GetString(5)
                    };
                }
            }
        }

        private static void ValidatePeriod(int accountId, int month, int year)
        {
            if (accountId <= 0)
            {
                throw new FaultException("accountId must be greater than 0.");
            }

            if (month < 1 || month > 12)
            {
                throw new FaultException("month must be between 1 and 12.");
            }

            if (year < 2000 || year > 2100)
            {
                throw new FaultException("year is out of range.");
            }
        }

        private static void EnsureGeneratedStatementsTableExists(SqlConnection connection)
        {
            using (var command = new SqlCommand(@"
IF OBJECT_ID('dbo.GeneratedStatements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GeneratedStatements
    (
        StatementID INT IDENTITY(1,1) PRIMARY KEY,
        RequestID INT NOT NULL,
        AccountID INT NOT NULL,
        StatementMonth INT NOT NULL,
        StatementYear INT NOT NULL,
        StatementHtml NVARCHAR(MAX) NOT NULL,
        GeneratedDate DATETIME NOT NULL DEFAULT GETDATE()
    );
END", connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static string BuildHtmlStatement(
            int accountId,
            string accountNumber,
            string customerName,
            DateTime periodStart,
            DateTime periodEnd,
            decimal openingBalance,
            decimal totalCredits,
            decimal totalDebits,
            List<string> transactionRows)
        {
            var builder = new StringBuilder();
            builder.Append("<html><head><title>Zava Bank Statement</title>");
            builder.Append("<style>body{font-family:Arial;font-size:13px;}table{width:100%;border-collapse:collapse;}th,td{border:1px solid #cccccc;padding:6px;}th{background:#efefef;}</style>");
            builder.Append("</head><body>");
            builder.Append("<h2>Zava Bank Monthly Statement</h2>");
            builder.AppendFormat("<p><strong>Customer:</strong> {0}<br/>", System.Security.SecurityElement.Escape(customerName));
            builder.AppendFormat("<strong>Account ID:</strong> {0}<br/>", accountId);
            builder.AppendFormat("<strong>Account Number:</strong> {0}<br/>", System.Security.SecurityElement.Escape(accountNumber));
            builder.AppendFormat("<strong>Period:</strong> {0:yyyy-MM-dd} to {1:yyyy-MM-dd}</p>", periodStart, periodEnd);
            builder.AppendFormat("<p><strong>Opening Balance:</strong> {0:N2}<br/>", openingBalance);
            builder.AppendFormat("<strong>Total Credits:</strong> {0:N2}<br/>", totalCredits);
            builder.AppendFormat("<strong>Total Debits:</strong> {0:N2}<br/></p>", totalDebits);
            builder.Append("<table><thead><tr><th>Date</th><th>Type</th><th>Description</th><th>Amount</th><th>Balance After</th></tr></thead><tbody>");

            if (transactionRows.Count == 0)
            {
                builder.Append("<tr><td colspan='5'>No transactions in this period.</td></tr>");
            }
            else
            {
                foreach (var row in transactionRows)
                {
                    builder.Append(row);
                }
            }

            builder.Append("</tbody></table>");
            builder.AppendFormat("<p>Generated at {0:yyyy-MM-dd HH:mm:ss} UTC</p>", DateTime.UtcNow);
            builder.Append("</body></html>");
            return builder.ToString();
        }

        private static string ComputeChecksum(string content)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                var hash = sha256.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
