
using ClassLibrary1;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for Admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class Admin : System.Web.Services.WebService
    {
        public static Dictionary<string, Tuple<DateTime, List<Tuple<Tuple<string, string, string>, int>>>> cache;
       
        /* 
       [WebMethod]
         [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
         public string urlTable(string url)
         {
             List<string> test = new List<string>();
             CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

             CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

             CloudTable table = tableClient.GetTableReference("table");

         TableQuery<WebEntity> query = new TableQuery<WebEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, CreateMD5(url)));
         foreach (WebEntity entity in table.ExecuteQuery(query))
         {
             return new JavaScriptSerializer().Serialize(entity.title);
         }
             return new JavaScriptSerializer().Serialize("title not found for: " + url);

         }
         */
        private PerformanceCounter process = new PerformanceCounter("Memory", "Available Mbytes");
        string filename = System.IO.Path.GetTempPath() + "\\wiki.txt";
        static Trie newTrie = new Trie();
        [WebMethod]
        public string DownloadWiki()
        {
            string tempname = "wiki.txt";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("info344");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(tempname);
            using (var fileStream = System.IO.File.OpenWrite(filename))

            {
                blockBlob.DownloadToStream(fileStream);
            }
            return "done";
        }

        //Searches through the built trie to find if the word that
        // a user searches exists.
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public String searchTrie(String input)
        {
            String building = "";
            input = input.ToLower();
            List<string> foundWords = new List<string>(); ;
            if (newTrie.startsWith(input) == true)
            {
                TrieNode tn = newTrie.searchNode(input);
                newTrie.wordsFinderTraversal(tn, 0);
                foundWords = newTrie.displayFoundWords();
            }
            foreach (string item in foundWords)
            {
                building += item;
            }

            building = new JavaScriptSerializer().Serialize(foundWords);
            return building;
        }

        //Building the trie after given an input of texts
        [WebMethod]
        public string BuildTRIE()
        {
            string lastWord = "";
            
            int count = 0;
            System.IO.StreamReader sr = new System.IO.StreamReader(filename);
            string trieItem = "";
            while (!sr.EndOfStream)
            {
                lastWord = sr.ReadLine();
                lastWord = lastWord.Replace('_', ' ');
                newTrie.insert(lastWord);
                if (count % 1000 == 0)
                {
                    if (process.NextValue() < 50)
                    {
                        break;
                    }
                }
                count++;
            }

            List<string> trieCount = new List<string>();
            trieCount.Add("Number of Wikis " + count + " Last link visited " + lastWord);
            trieItem = new JavaScriptSerializer().Serialize(trieCount);
            return trieItem;
            
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string titleTable(string sentence)
        {
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable storagetable = tableClient.GetTableReference("storagetable");
            if ((cache == null) || (cache.Count > 100))
            {
                cache = new Dictionary<string, Tuple<DateTime, List<Tuple<Tuple<string, string, string>, int>>>>();
            }
            string trimTitle = new string((from c in sentence
                                           where char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)
                                           select c).ToArray());
            string[] titleSplit = trimTitle.ToLower().Split(' ');
            var results = new List<Tuple<string, string, string>>();
            DateTime myTime = DateTime.Now;
            DateTime beforeTime = myTime.AddMinutes(-15);
            foreach (string titleWord in titleSplit)
            {
                if (cache.ContainsKey(sentence))
                {
                    Tuple<DateTime, List<Tuple<Tuple<string, string, string>, int>>> answer = cache[sentence];
                    
                    if (answer.Item1 < beforeTime)
                    {
                        cache.Add(titleWord,answer);
                        return new JavaScriptSerializer().Serialize(answer.Item2);
                    }
                    else {
                        return new JavaScriptSerializer().Serialize(answer.Item2);
                    }
                }
                else
                {
                    TableQuery<StorageEntity> query = new TableQuery<StorageEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, titleWord));
                    foreach (StorageEntity entity in storagetable.ExecuteQuery(query))
                    {
                        results.Add(new Tuple<string, string, string>(entity.url, entity.title, entity.date));
                    }
                    var ordered = results
                        .GroupBy(x => x)
                        .Select(x => new Tuple<Tuple<string, string, string>, int>(x.Key, x.Count()))
                        .OrderByDescending(x => x.Item2)
                        .Take(1000);
                    
                    cache.Add(sentence, new Tuple<DateTime,List<Tuple<Tuple<string, string, string>, int>>>(DateTime.Now,ordered.ToList()));
                    Tuple<DateTime, List<Tuple<Tuple<string, string, string>, int>>> answer = cache[sentence];
                    
                    return new JavaScriptSerializer().Serialize(answer.Item2);
                }                              
            }                        
                return new JavaScriptSerializer().Serialize("no url's found for: " + sentence);          
            
        }

        public object CloudConfigurationManager { get; private set; }

            [WebMethod]
            [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
            public string getCurrentCpuUsage()
            {
                List<string> listWord = new List<string>();
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable dashboardTable = tableClient.GetTableReference("dashboardtable");
                TableQuery<CheckEntity> query = new TableQuery<CheckEntity>()
                        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "partition"));
                foreach (CheckEntity entity in dashboardTable.ExecuteQuery(query))
                {
                    listWord.Add(entity.cpu);
                }

                return new JavaScriptSerializer().Serialize(listWord);
            }

            [WebMethod]
            [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
            public string getAvailableRAM()
            {
                List<string> listWord = new List<string>();
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable dashboardTable = tableClient.GetTableReference("dashboardtable");
                TableQuery<CheckEntity> query = new TableQuery<CheckEntity>()
                        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "partition"));
                foreach (CheckEntity entity in dashboardTable.ExecuteQuery(query))
                {
                    listWord.Add(entity.ram);
                }

                return new JavaScriptSerializer().Serialize(listWord);
            }


            [WebMethod]
            [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
            public string StartCrawling()
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudQueue stopqueue = queueClient.GetQueueReference("stopqueue");
                stopqueue.CreateIfNotExists();
                return "done";
            }
            [WebMethod]
            [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
            public string StopCrawling()
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudQueue stopqueue = queueClient.GetQueueReference("stopqueue");
                stopqueue.DeleteIfExists();
                return "done";
            }

            [WebMethod]
            [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
            public string ClearIndex()
            {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudQueue xmlqueue = queueClient.GetQueueReference("xmlstorage");
            CloudQueue urlqueue = queueClient.GetQueueReference("urlstorage");
            CloudTable tables = tableClient.GetTableReference("table");
            CloudTable dashboardTable = tableClient.GetTableReference("dashboardtable");
            CloudTable storageTable = tableClient.GetTableReference("storagetable");
            xmlqueue.DeleteIfExists();
            urlqueue.DeleteIfExists();
            tables.DeleteIfExists();
            dashboardTable.DeleteIfExists();
            storageTable.DeleteIfExists();


            return "";
            }

            [WebMethod]
            [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
            public string GetPageTitle()
            {
                List<string> listWord = new List<string>();
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable dashboardTable = tableClient.GetTableReference("dashboardtable");
                TableQuery<CheckEntity> query = new TableQuery<CheckEntity>()
                        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "partition"));
                foreach (CheckEntity entity in dashboardTable.ExecuteQuery(query))
                {
                    listWord.Add(entity.url);
                }

                return new JavaScriptSerializer().Serialize(listWord);

            }

            [WebMethod]
            [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
            public string GetQueueSize()
            {
                List<string> listWord = new List<string>();
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                var temp = queueClient.GetQueueReference("urlstorage");
                temp.FetchAttributes();
                listWord.Add(temp.ApproximateMessageCount.ToString());
                return new JavaScriptSerializer().Serialize(listWord);/*
             temp.ApproximateMessageCount.ToString();
            CloudTable dashboardTable = tableClient.GetTableReference("dashBoardTable");

            TableQuery<CheckEntity> query = new TableQuery<CheckEntity>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "partition"));
            foreach (CheckEntity entity in dashboardTable.ExecuteQuery(query))
            {
                listWord.Add(entity.queueSize);
            }

            return new JavaScriptSerializer().Serialize(listWord);
            */
            }

            [WebMethod]
            [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
            public string GetIndexSize()
            {
                List<int> listWord = new List<int>();
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient(); ;
                CloudTable dashboardTable = tableClient.GetTableReference("dashboardtable");
                TableQuery<CheckEntity> query = new TableQuery<CheckEntity>()
                        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "partition"));
                foreach (CheckEntity entity in dashboardTable.ExecuteQuery(query))
                {
                    listWord.Add(entity.indexSize);
                }

                return new JavaScriptSerializer().Serialize(listWord);

            }

        private static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
