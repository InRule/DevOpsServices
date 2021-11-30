using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.IO;
using System.Threading.Tasks;

namespace CheckinRequestListener
{
    public class AzureOperationHelper
    {
        public string srcPath { get; set; }
        public string destinationPath { get; set; }
        public string storageAccountName { get; set; }
        public string containerName { get; set; }
        public string storageEndPoint { get; set; }
        public string blobName { get; set; }
    }

    public static class AzureStorageHelper
    {
        #region ConfigParams  
        public static string tenantId;
        public static string applicationId;
        public static string clientSecret;
        #endregion
        public static void UploadFile(AzureOperationHelper azureOperationHelper)
        {
            CloudBlobContainer blobContainer = CreateCloudBlobContainer(tenantId, applicationId, clientSecret, azureOperationHelper.storageAccountName, azureOperationHelper.containerName, azureOperationHelper.storageEndPoint);
            blobContainer.CreateIfNotExists();
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(azureOperationHelper.blobName);
            blob.UploadFromFile(azureOperationHelper.srcPath);
        }
        public static void DownloadFile(AzureOperationHelper azureOperationHelper)
        {
            CloudBlobContainer blobContainer = CreateCloudBlobContainer(tenantId, applicationId, clientSecret, azureOperationHelper.storageAccountName, azureOperationHelper.containerName, azureOperationHelper.storageEndPoint);
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(azureOperationHelper.blobName);
            blob.DownloadToFile(azureOperationHelper.destinationPath, FileMode.OpenOrCreate);
        }
        private static CloudBlobContainer CreateCloudBlobContainer(string tenantId, string applicationId, string clientSecret, string storageAccountName, string containerName, string storageEndPoint)
        {
            string accessToken = GetUserOAuthToken(tenantId, applicationId, clientSecret);
            TokenCredential tokenCredential = new TokenCredential(accessToken);
            StorageCredentials storageCredentials = new StorageCredentials(tokenCredential);
            CloudStorageAccount cloudStorageAccount = new CloudStorageAccount(storageCredentials, storageAccountName, storageEndPoint, useHttps: true);
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
            return blobContainer;
        }
        static string GetUserOAuthToken(string tenantId, string applicationId, string clientSecret)
        {
            const string ResourceId = "https://storage.azure.com/";
            const string AuthInstance = "https://login.microsoftonline.com/{0}/";
            string authority = string.Format(CultureInfo.InvariantCulture, AuthInstance, tenantId);
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext authContext = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(authority);
            var clientCred = new ClientCredential(applicationId, clientSecret);
            AuthenticationResult result = authContext.AcquireTokenAsync(ResourceId, clientCred).Result;
            return result.AccessToken;
        }
        //static string GetUserOAuthToken(string tenantId, string applicationId, string clientSecret)
        //{
        //    const string ResourceId = "https://storage.azure.com/";
        //    const string AuthInstance = "https://login.microsoftonline.com/{0}/";

        //    string authority = string.Format(CultureInfo.InvariantCulture, AuthInstance, tenantId);
        //    Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext authContext = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(authority);

        //    var clientCred = new ClientCredential(applicationId, clientSecret);
        //    AuthenticationResult result = authContext.AcquireTokenAsync(
        //                                        ResourceId,
        //                                        clientCred
        //                                        ).Result;
        //    return result.AccessToken;
        //}
    }
}
