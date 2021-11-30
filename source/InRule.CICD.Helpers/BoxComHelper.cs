using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using Box.V2.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InRule.CICD.Helpers
{
    public static class BoxComHelper
    {
        #region ConfigParams  
        private static readonly string moniker = "Box";
        public static string Prefix = "BOX.COM - ";
        #endregion

        public static async Task<string> UploadFile(string fileName, string filePath)
        {
            return await UploadFile(fileName, filePath, moniker);
        }
        public static async Task<string> UploadFile(string fileName, string filePath, string moniker)
        {
            string ClientId = SettingsManager.Get($"{moniker}.BoxClientId");
            string ClientSecret = SettingsManager.Get($"{moniker}.BoxClientSecret");
            string DeveloperToken = SettingsManager.Get($"{moniker}.BoxDeveloperToken");
            string UploadFolderID = SettingsManager.Get($"{moniker}.BoxUploadFolderID");

            if (ClientId.Length == 0 || ClientSecret.Length == 0 || DeveloperToken.Length == 0)
                return string.Empty;

            var config = new BoxConfig(ClientId, ClientSecret, new Uri("http://localhost"));
            var session = new OAuthSession(DeveloperToken, "NOT_NEEDED", 3600, "bearer");
            var client = new BoxClient(config, session);
            //await client.Auth.AuthenticateAsync(session.AccessToken);

            // Upload file
            BoxFile file;
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                BoxFileRequest req = new BoxFileRequest()
                {
                    Name = fileName,
                    Parent = new BoxRequestEntity() { Id = UploadFolderID },
                    Type = BoxType.file
                };
                file = await client.FilesManager.UploadAsync(req, fs);

                var sharedLinkParams = new BoxSharedLinkRequest()
                {
                    Access = BoxSharedLinkAccessType.open,
                    Permissions = new BoxPermissionsRequest()
                    {
                        Download = true
                    }
                };
                file = await client.FilesManager.CreateSharedLinkAsync(file.Id, sharedLinkParams);
                return file.SharedLink.Url;

                //SlackHelper.PostSimpleMessage(file.SharedLink.DownloadUrl);
            }
        }
    }
}
