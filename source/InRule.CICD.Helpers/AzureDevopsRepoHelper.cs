using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace InRule.CICD.Helpers
{
    public static class AzureDevopsRepoHelper
    {
        private static readonly string moniker = "DevOps";
        private static readonly bool IsCloudBased = bool.Parse(SettingsManager.Get("IsCloudBased"));
        public static string Prefix = "DEVOPS REPOSITORY";

        public class Refs
        {
            public string name { get; set; }

            //public string objectId { get; set; }
            public string oldObjectId { get; set; }
            //public Creator creator { get; set; }
            //public string url { get; set; }
        }

        public class CommitToAdd
        {
            public string comment { get; set; }
            public ChangeToAdd[] changes { get; set; }
        }

        public class ChangeToAdd
        {
            public string changeType { get; set; }
            public ItemBase item { get; set; }
            public Newcontent newContent { get; set; }
        }

        public class ItemBase
        {
            public string path { get; set; }
        }

        public class Newcontent
        {
            public string content { get; set; }
            public string contentType { get; set; }
        }

        public static async Task<string> UploadFileToRepo(string fileContent, string fileName)
        {
            var DevOpsOrganization = SettingsManager.Get($"{moniker}.DevOpsOrganization");
            var DevOpsProjectName = SettingsManager.Get($"{moniker}.DevOpsProjectName");
            //var DevOpsRepositoryId = SettingsManager.Get($"{moniker}.DevOpsRepositoryId");
            var DevOpsBranch = SettingsManager.Get($"{moniker}.DevOpsBranch");
            var DevOpsPath = SettingsManager.Get($"{moniker}.DevOpsPath");
            var DevOpsPersonalAccessToken = SettingsManager.Get($"{moniker}.DevOpsPersonalAccessToken");
            var DevOpsRefName = "refs/heads/" + DevOpsBranch;
            var commitId = string.Empty;

            using (var client = new HttpClient())
            {
                var authorizationToken = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", DevOpsPersonalAccessToken)));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authorizationToken);
                var uri = $"https://dev.azure.com/{DevOpsOrganization}/{DevOpsProjectName}/_apis/git/repositories/{DevOpsProjectName}/commits?api-version=6.0";
                var response = await client.GetAsync(uri);

                if (!response.IsSuccessStatusCode)
                {
                    await NotificationHelper.NotifyAsync( $"Failed to get latest commit ID in order to push report to {DevOpsProjectName}. Status code: {response.StatusCode} Error: {response}", Prefix, "Debug");
                    return string.Empty;
                }
                dynamic responseObject = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                commitId = responseObject.value[0].commitId;
            }

            var refs = new List<Refs>() {new Refs {oldObjectId = commitId, name = DevOpsRefName}};
            try
            {
                var changes = new List<ChangeToAdd>();
                var commit = new CommitToAdd();
                commit.comment = "Saved by InRule CI/CD.";
                var changeJson = new ChangeToAdd()
                {
                    changeType = "add",
                    item = new ItemBase() {path = DevOpsPath + fileName},
                    newContent = new Newcontent()
                    {
                        contentType = "rawtext",
                        content = fileContent
                    }
                };
                changes.Add(changeJson);
                commit.changes = changes.ToArray();
                var content = new List<CommitToAdd>() {commit};
                var request = new
                {
                    refUpdates = refs,
                    commits = content
                };

                using (var client = new HttpClient())
                {
                    var authorizationToken = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", DevOpsPersonalAccessToken)));
                    var uri = $"https://dev.azure.com/{DevOpsOrganization}/{DevOpsProjectName}/_apis/git/repositories/{DevOpsProjectName}/pushes?api-version=6.0";
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authorizationToken);
                    var requestJson = JsonConvert.SerializeObject(request);
                    var httpContent = new StringContent(requestJson, Encoding.ASCII, "application/json");
                    var response = await client.PostAsync(uri, httpContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        await NotificationHelper.NotifyAsync($"Failed to upload file {DevOpsProjectName}. Status code: {response.StatusCode} Error: {response}", Prefix, "Debug");
                        return string.Empty;
                    }
                }
                var downloadLink = $"https://dev.azure.com/{DevOpsOrganization}/{DevOpsProjectName}/_apis/git/repositories/{DevOpsProjectName}/items?scopePath=/{DevOpsPath + fileName}?api-version=6.0&download=true&api-version=6.0&versionType=Branch&version={DevOpsBranch}"; ;
                return downloadLink;

            }
            catch (Exception e)
            {
                await NotificationHelper.NotifyAsync($"Failed to upload file to Azure repo: {DevOpsProjectName} " + e.Message, Prefix, "Debug");
                return string.Empty;
            }
        }
    }
}
