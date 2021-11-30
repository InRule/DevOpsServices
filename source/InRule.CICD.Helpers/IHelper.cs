namespace InRule.CICD.Helpers
{
    public interface IHelper
    {
        public enum InRuleEventHelperType
        {
            Slack,
            Teams,
            Email,
            TestSuite,
            ServiceBus,
            EventGrid,
            Java,
            JavaScript,
            AppInsights,
            Sql,
            RuleAppReport,
            RuleAppDiffReport,
            DevOps,
            EventLog,
            ApprovalFlow,
            GitHub,
            Box
        }

        InRuleEventHelperType EventType { get; set; }

        string Moniker { get; set; }

        //List<InRuleEventHelperType> NotificationChannels { get; set; }
    }
}
