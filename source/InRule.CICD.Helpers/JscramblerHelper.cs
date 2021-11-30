using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Web.Hosting;
using System.Diagnostics;

namespace InRule.CICD.Helpers
{
    class JscramblerHelper
    {
        #region ConfigParams  
        private static readonly string moniker = "Jscrambler";
        public static string Prefix = "Jscrambler - ";


        #endregion
        public class JscramblerResponse
        {
            public Data data { get; set; }
        }
        public class Data
        {
            public CreateApplicationProtection createApplicationProtection { get; set; }
        }
        public class CreateApplicationProtection
        {
            public string _id { get; set; }
        }
        public class Get
        {
            public string GetRequest { get; set; }
        }
        public class GlobalVariableIndirection
        {
            public string name { get; set; }
        }
        private static string SignedParams(string method, string path, string host, Dictionary<String, String> keys, Dictionary<String, Object> parameters)
        {
            string query = "";
            foreach (String key in parameters.Select(x => x.Key).OrderBy(x => x))
            {
                if (query.Length > 0) query += "&";
                query = query + WebUtility.UrlEncode(key) + "=";
                if (parameters[key] is string) { query += WebUtility.UrlEncode(parameters[key].ToString()); }
                else { query += WebUtility.UrlEncode(JsonConvert.SerializeObject(parameters[key])); }
            }
            string signatureData = String.Format("{0};{1};{2};{3}", method.ToUpperInvariant(), host.ToLowerInvariant(), path,
                query.Replace("+", "%20").Replace("(", "%28").Replace(")", "%29").Replace("!", "%21").Replace("\'", "%27").Replace("*", "%2A").Replace("%7E", "~"));
            using HMACSHA256 hmac = new HMACSHA256(Encoding.ASCII.GetBytes(keys["secretKey"]));
            byte[] hashValue = hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureData));
            string hashValuebase64 = Convert.ToBase64String(hashValue);
            return hashValuebase64;
        }

        private static string GetRequest(string protectionID)
        {
            string getTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            string accessKey = SettingsManager.Get($"{moniker}.AccessKey");
            string secretKey = SettingsManager.Get($"{moniker}.SecretKey");
            string methodGET = "GET";
            string pathGET = "/application/download/" + protectionID;
            string hostGET = "api4.jscrambler.com";
            string GETURL = "https://api4.jscrambler.com/application/download/";

            string GETCalculatedSignature = SignedParams(methodGET, pathGET, hostGET,
                new Dictionary<String, String>() {
                        {"accessKey", accessKey},
                        {"secretKey", secretKey},
                    }, new Dictionary<String, Object>() {
                        {"access_key", accessKey},
                        {"timestamp", getTimestamp}
                });

            string getRequest = GETURL + protectionID + "?timestamp=" + WebUtility.UrlEncode(getTimestamp) + "&access_key=" + accessKey + "&signature=" + GETCalculatedSignature;
            return getRequest;
        }

        public static async Task CallJscramblerAPIAsync(string jsContent, string DestinationPath, string fileName)
        {
            string accessKey = SettingsManager.Get($"{moniker}.AccessKey");
            string secretKey = SettingsManager.Get($"{moniker}.SecretKey");
            string applicationId = SettingsManager.Get($"{moniker}.ApplicationId");
            string query = SettingsManager.Get($"{moniker}.Query");
            string timeOutString = SettingsManager.Get($"{moniker}.TimeOut");
            string obfuscation = SettingsManager.Get($"{moniker}.Obfuscation");
            string fileDownloadFolder = SettingsManager.Get($"{moniker}.FileDownloadFolder");

            int timeOut = Int32.Parse(timeOutString);

            if (accessKey.Length == 0 || secretKey.Length == 0 || applicationId.Length == 0 || query.Length == 0 || timeOutString.Length == 0)
                return;

            byte[] compressedBytes;
            string nameSubstring = fileName.Substring(0, (fileName.Length - 3));
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var tempFile = archive.CreateEntry(nameSubstring + ".js");
                    using var entryStream = tempFile.Open();
                    using var streamWriter = new StreamWriter(entryStream);
                    streamWriter.Write(jsContent);
                }
                using (var fileStream = new FileStream($"{DestinationPath}{nameSubstring}.zip", FileMode.Create))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);
                    compressedBytes = memoryStream.ToArray();
                }
            }

            string methodPOST = "POST";
            string pathPOST = "/application";
            string hostPOST = "api4.jscrambler.com";
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            string ZipFilebase64 = Convert.ToBase64String(compressedBytes);
            string ZipFilename = $"{nameSubstring}.zip";
            string ZipExtension = "zip";

            string POSTSignature = SignedParams(methodPOST, pathPOST, hostPOST,
                new Dictionary<String, String>() {
                {"accessKey", accessKey},
                {"secretKey", secretKey}
                    }, new Dictionary<String, Object>() {
                    {"query", query},
                    {"params", new Dictionary<String, object>() {
                        {"applicationId", applicationId},
                        {"data", new Dictionary<object, object>() {
                            { "areSubscribersOrdered", false},
                            { "bail", true},
                            { "debugMode", false},
                            { "profilingDataMode", "off"},
                            { "sourceMaps", false},
                            { "tolerateMinification", true},
                            { "useAppClassification", false},
                            { "useRecommendedOrder", true},
                            { "parameters", new GlobalVariableIndirection { name = "globalVariableIndirection" } },
                                { "source", new Dictionary<string, string>()
                                    {
                                        { "content", ZipFilebase64},
                                        { "extension", ZipExtension},
                                        { "filename", ZipFilename}
                                    }
                                }
                            }
                        }
                    }
                },
                {"access_key", accessKey },
                {"timestamp", timestamp }
            });
            // Delete original file
            //var filePathO = Path.Combine(DestinationPath, fileName);
            //File.Delete(filePathO);

            JObject POSTBody = JObject.FromObject(new JObject
            {
                ["params"] = new JObject
                {
                    ["applicationId"] = applicationId,
                    ["data"] = new JObject
                    {
                        ["areSubscribersOrdered"] = false,
                        ["bail"] = true,
                        ["debugMode"] = false,
                        ["profilingDataMode"] = "off",
                        ["sourceMaps"] = false,
                        ["tolerateMinification"] = true,
                        ["useAppClassification"] = false,
                        ["useRecommendedOrder"] = true,
                        ["parameters"] = new JObject
                        {
                            ["name"] = "globalVariableIndirection"
                        },
                        ["source"] = new JObject
                        {
                            ["content"] = ZipFilebase64,
                            ["extension"] = ZipExtension,
                            ["filename"] = ZipFilename,
                        }
                    }
                },
                ["query"] = query,
                ["access_key"] = accessKey,
                ["timestamp"] = timestamp,
                ["signature"] = POSTSignature
            });

            await NotificationHelper.NotifyAsync($"POSTBody {JsonConvert.SerializeObject(POSTBody)}", Prefix, "Debug");
            string protectionID = string.Empty;
            using (HttpClient jclient = new HttpClient())
            {
                jclient.BaseAddress = new Uri("https://api4.jscrambler.com/application");
                jclient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpContent hcontent = new StringContent(JsonConvert.SerializeObject(POSTBody), Encoding.ASCII, "application/json");
                hcontent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                using (HttpResponseMessage jresponse = await jclient.PostAsync(jclient.BaseAddress, hcontent))
                {
                    try
                    {
                        jresponse.EnsureSuccessStatusCode();
                        string postResponse = await jresponse.Content.ReadAsStringAsync();
                        JscramblerResponse jscramblerResponse = JsonConvert.DeserializeObject<JscramblerResponse>(postResponse);
                        protectionID = jscramblerResponse.data.createApplicationProtection._id;
                        await NotificationHelper.NotifyAsync($"Jscrambler Post Success. ID: {protectionID}", Prefix, "Debug");
                    }
                    catch (Exception ex)
                    {
                        await NotificationHelper.NotifyAsync($"Failed to Post: {ex.Message}", Prefix, "Debug");
                    }
                }
            }

            string jscramblerFileName = "jscrambler_" + nameSubstring + ".zip";
            string DestinationPath2 = DestinationPath + fileDownloadFolder + @"\";
            var filePath = Path.Combine(DestinationPath2, jscramblerFileName);
            var timeOutSeconds = timeOut * 60;
            System.Threading.Thread.Sleep(250);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            while (timer.Elapsed.TotalSeconds < timeOutSeconds)
            {
                string getRequest = GetRequest(protectionID);
                try
                {
                    using (WebClient gclient = new WebClient())
                    {
                        gclient.DownloadFile(new Uri(getRequest), filePath);
                        await NotificationHelper.NotifyAsync($"File successfully downloaded from Jscrambler.", Prefix, "Debug");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(250);
                    await NotificationHelper.NotifyAsync($"Failed to download file from Jscrambler. " + ex.Message, Prefix, "Debug");
                }
            }

            try
            {
                ZipFile.ExtractToDirectory(filePath, DestinationPath);
            }
            catch (Exception ex)
            {
                await NotificationHelper.NotifyAsync($"Failed to extract file. " + ex.Message, Prefix, "Debug");
            }
            return;
        }

    }
}