using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ZavaStatementService
{
    [DataContract]
    public class GeneratedStatementResponse
    {
        [DataMember] public int StatementId { get; set; }
        [DataMember] public int AccountId { get; set; }
        [DataMember] public int Month { get; set; }
        [DataMember] public int Year { get; set; }
        [DataMember] public DateTime GeneratedDateUtc { get; set; }
        [DataMember] public string HtmlContent { get; set; }
    }

    [DataContract]
    public class StatementHistoryItem
    {
        [DataMember] public int StatementId { get; set; }
        [DataMember] public int AccountId { get; set; }
        [DataMember] public int Month { get; set; }
        [DataMember] public int Year { get; set; }
        [DataMember] public DateTime GeneratedDateUtc { get; set; }
    }

    [DataContract]
    public class StatementHistoryResponse
    {
        [DataMember] public int AccountId { get; set; }
        [DataMember] public List<StatementHistoryItem> Statements { get; set; }
    }

    [DataContract]
    public class StatementDetailResponse
    {
        [DataMember] public int StatementId { get; set; }
        [DataMember] public int AccountId { get; set; }
        [DataMember] public int Month { get; set; }
        [DataMember] public int Year { get; set; }
        [DataMember] public DateTime GeneratedDateUtc { get; set; }
        [DataMember] public string HtmlContent { get; set; }
    }
}
