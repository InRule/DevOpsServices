using InRule.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using System.Xml;

namespace InRule.CICD
{
    [ServiceContract]
    public interface IService
    {

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "GetRuleAppReport",
        RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml,
        BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GetRuleAppReport(Stream data);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "GetRuleAppReportToGitHub",
        RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml,
        BodyStyle = WebMessageBodyStyle.Bare)]
        string GetRuleAppReportToGitHub(Stream data);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "GetRuleAppDiffReport",
        RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml,
        BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GetRuleAppDiffReport(Stream diffReportRequest);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "GetRuleAppDiffReportToGitHub",
        RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml,
        BodyStyle = WebMessageBodyStyle.Bare)]
        string GetRuleAppDiffReportToGitHub(Stream data);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "ApproveRuleAppPromotion?data={data}",
        RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml,
        BodyStyle = WebMessageBodyStyle.Bare)]
        string ApproveRuleAppPromotion(string data);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "ProcessInRuleEvent",
        RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json,
        BodyStyle = WebMessageBodyStyle.Bare)]
        Stream ProcessInRuleEvent(Stream data);

        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "ProcessInRuleEventI?data={data}",
        RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml,
        BodyStyle = WebMessageBodyStyle.Bare)]
        string ProcessInRuleEventI(string data);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "RunTestsInGitHubForRuleapp",
        RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml,
        BodyStyle = WebMessageBodyStyle.Bare)]
        Stream RunTestsInGitHubForRuleapp(Stream data);
    }

    [DataContract(Namespace = "")]
    public class PostData
    {
        [DataMember]
        public string FromRuleAppXml { get; set; }

        [DataMember]
        public string ToRuleAppXml { get; set; }
    }
}
