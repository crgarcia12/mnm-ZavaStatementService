using System.ServiceModel;

namespace ZavaStatementService
{
    [ServiceContract]
    public interface IStatementService
    {
        [OperationContract]
        GeneratedStatementResponse GenerateStatement(int accountId, int month, int year);

        [OperationContract]
        StatementHistoryResponse GetStatementHistory(int accountId);

        [OperationContract]
        StatementDetailResponse GetStatement(int statementId);
    }
}
