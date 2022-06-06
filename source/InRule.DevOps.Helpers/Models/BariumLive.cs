using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InRule.DevOps.Helpers.Models
{
 public class BariumLive
    {
        public class Authenticate
        {
            public bool success { get; set; }
            public string ticket { get; set; }
            public string webTicket { get; set; }
            public string Error { get; set; }
        }
        public class BaseRequest
        {
            public string Host { get; set; }
            public string ApiVersion { get; set; }
            public string Ticket { get; set; }
        }
        public class PostFormsResponse
        {
            public bool success { get; set; }
            public List<string> updatedFields { get; set; }
            public List<object> ignoredFields { get; set; }
        }


        public class AppsGetAppID
        {
            public int TotalCount { get; set; }
            public List<DataList> Data { get; set; }
            public string Error { get; set; }
        }

        public class DataList
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
            public int InstancesCount { get; set; }
            public string ProcessId { get; set; }
            public string DomainObjectId { get; set; }
        }

        public class AppsStartInstance
        {
            public bool success { get; set; }
            public List<Item> Items { get; set; }
            public string Error { get; set; }
        }

        public class Item
        {
            public bool success { get; set; }
            public string ObjectClass { get; set; }
            public string Name { get; set; }
            public string Id { get; set; }
            public string ReferenceId { get; set; }
            public string resourceURI { get; set; }

        }

        public class AppGetProcessID
        {
            public bool success { get; set; }
            public string InstanceId { get; set; }
            public int errorCode { get; set; }
            public string errorMessage { get; set; }
            public string errorLogId { get; set; }
            public ErrorData errorData { get; set; }
        }
        public class ErrorData
        {
            public string ErrorId { get; set; }
        }
        public class Objects
        {
            public int TotalCount { get; set; }
            public List<Object> Data { get; set; }
        }
        public class Object
        {
            public string Id { get; set; }
            public object ReferenceId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
            public string ObjectClass { get; set; }
            public bool Container { get; set; }
            public bool ReadOnly { get; set; }
            public object TypeNamespace { get; set; }
            public string TemplateId { get; set; }
            public string FileType { get; set; }
            public object SortIndex { get; set; }
            public string State { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime UpdatedDate { get; set; }
            public string DataId { get; set; }
        }



    }
}
