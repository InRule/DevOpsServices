using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InRule.CICD.Helpers.Models
{
    public class Barium
    {
        public class Authenticate
        {
            public bool success { get; set; }
            public string ticket { get; set; }
            public string Ticket { get; set; }
            public string WebTicket { get; set; }
            public string Error { get; set; }
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
            public string success { get; set; }
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


    }
}
