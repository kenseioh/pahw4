using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;
using System.IO;
using ClassLibrary1;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Xml;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Text;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        PerformanceCounter cpuCounter;
        PerformanceCounter ramCounter;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        int tablecount = 0;
        int queuecount = 0;
        int urlsCrawled = 0;
        string RAM = "";
        string CPU = "";
        HtmlWeb web = new HtmlWeb();
        public static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
        public static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        CheckEntity updatedROW = new CheckEntity();
        public static CloudTable dashboardTable = tableClient.GetTableReference("dashboardtable");
        static HashSet<String> duplicates3 = new HashSet<string>();
        HashSet<String> duplicates = new HashSet<string>();
        HashSet<String> tableDuplicates = new HashSet<string>();
        HashSet<String> disallowSet = new HashSet<String>();
        List<string> disallow = new List<string> { "/editionssi", "/ads", "/aol", "/audio", "/beta", "/browsers", "/cl", "/cnews", "/cnn_adspaces", "/cnnbeta", "/cnnintl_adspaces",
                "/development", "/help/cnnx.html", "/NewsPass", "/NOKIA", "/partners", "/pipeline", "/pointroll", "/POLLSERVER", "/pr/", "/PV", "/quickcast", "/AAMSZ=160x600",
                "/AAMSZ=300x250", "/AAMSZ=728x90", "/account", "/accounts", "/activities", "/add_to_blacklist", "/login", "/long_poll", "/new_writer" ,"/newsletter_subscriptions",
                "/pages", "/signup", "/users/rankings", "/Quickcast", "/QUICKNEWS", "/test", "/virtual", "/WEB-INF", "/web.projects", "/search"};
        
        string cnn = "";
        
        //var client2 = new WebClient();
        public override void Run()
        {
            while (true)
            {
                disallowSet.Add(disallow.ToString());
                var client = new WebClient();
                dashboardTable.CreateIfNotExists();
                CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                CloudQueue stopqueue = queueClient.GetQueueReference("stopqueue");
                CloudTable tables = tableClient.GetTableReference("table");
                CloudTable storageTable = tableClient.GetTableReference("storagetable");
                CloudQueue xmlqueue = queueClient.GetQueueReference("xmlstorage");
                CloudQueue urlqueue = queueClient.GetQueueReference("urlstorage");
                tables.CreateIfNotExists();
                storageTable.CreateIfNotExists();
                xmlqueue.CreateIfNotExists();
                urlqueue.CreateIfNotExists();

                // HtmlWeb web = new HtmlWeb();



                Queue<string> dashboardWord = new Queue<string>();


                


                string stringMessage;

                Stream stream = client.OpenRead("http://www.cnn.com/robots.txt");
                //   Stream file = client2.OpenRead("http://bleacherreport.com/robots.txt");


                CheckEntity countpage = new CheckEntity();

                using (StreamReader sr = new StreamReader(stream))
                {
                    //using (StreamReader srr = new StreamReader(file))
                    //{
                    string line1;
                    string line2;
                    //List<string> lines = new List<string>();
                    RAM = getAvailableRAM();
                    CPU = getCurrentCpuUsage();
                    //Read and display lines from the file until the end of
                    // the file is reached.
                    if (!duplicates3.Contains("http://bleacherreport.com/sitemap/nba.xml"))
                    {
                        duplicates3.Add("http://bleacherreport.com/sitemap/nba.xml");
                        xmlqueue.AddMessage(new CloudQueueMessage("http://bleacherreport.com/sitemap/nba.xml"));
                    }
                    while ((line1 = sr.ReadLine()) != null)
                    {
                        if (line1.EndsWith(".xml"))
                        {
                            string[] word1 = line1.Split(' ');
                            CloudQueueMessage message = new CloudQueueMessage(word1[1]);
                            if (!duplicates3.Contains(message.AsString))
                            {
                                duplicates3.Add(message.AsString);
                                xmlqueue.AddMessage(message);
                            }
                        }
                    }


                    //         }
                    //     }
                    //  }
                    //CloudQueueMessage message = new CloudQueueMessage("hello");
                    //xmlqueue.AddMessage(message);
                    /*
                    while ((line2 = srr.ReadLine()) != null && line2.Contains(".xml"))
                    {
                        string[] word2 = line2.Split(' ');
                        CloudQueueMessage message = new CloudQueueMessage(word2[1]);
                        if (message.AsString.Contains("nba") && !duplicates3.Contains(message.AsString))
                        {
                            duplicates3.Add(message.AsString);
                            xmlqueue.AddMessageAsync(message);
                        }

                    }
             */
                }



                while (true & stopqueue.Exists())
                {

                    //TableOperation insertOperations = TableOperation.InsertOrReplace(countpage);
                    CloudQueueMessage getMessage = xmlqueue.GetMessage();

                    while (getMessage != null && stopqueue.Exists())
                    {

                        //xmlqueue.DeleteMessage(getMessage);
                        stringMessage = getMessage.AsString;
                        string sitemap = stringMessage;

                        XmlDocument xDoc = new XmlDocument();
                        Regex regex = new Regex(@"-(\d{4})-(\d{2}).xml");
                        //load up the xml from the location
                        xDoc.Load(sitemap);

                        // cycle through each child noed
                        foreach (XmlNode node in xDoc.DocumentElement.ChildNodes)
                        {
                           // RAM = getAvailableRAM();
                           // CPU = getCurrentCpuUsage();
                            // first node is the url ... have to go to nexted loc node
                            foreach (XmlNode locNode in node)
                            {
                                // thereare a couple child nodes here so only take data from node named loc
                                if (locNode.Name == "loc")
                                {
                                    // get the content of the loc node
                                    string loc = locNode.InnerText;
                                    if (loc.EndsWith(".xml"))
                                    {
                                        // write it to the console so you can see its working
                                        Console.WriteLine(loc + Environment.NewLine);


                                        if (!duplicates.Contains(loc))
                                        {
                                            CloudQueueMessage message = new CloudQueueMessage(loc);
                                            duplicates.Add(message.AsString);
                                            Match match = regex.Match(loc);
                                            if (match.Success)
                                            {
                                                int year = Int32.Parse(match.Groups[1].Value);
                                                int month = Int32.Parse(match.Groups[2].Value);
                                                if (month >= 3 && year >= 2016)
                                                {
                                                    if (xmlqueue.ApproximateMessageCount < 3)
                                                    {
                                                    if (!duplicates3.Contains(message.AsString) && message.AsString.Contains("cnn.com"))
                                                    {
                                                        duplicates3.Add(message.AsString);
                                                        xmlqueue.AddMessageAsync(message);
                                                    }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!duplicates.Contains(loc))
                                        {
                                            
                                            if (loc.Contains("cnn.com") || loc.Contains("bleacherreport"))
                                            {
                                                CloudQueueMessage message = new CloudQueueMessage(loc);
                                                duplicates.Add(message.AsString);
                                                urlqueue.AddMessageAsync(message);
                                                queuecount++;
                                                //countpage.queueSize = queuecount;


                                            }
                                        }
                                    }
                                }
                            }
                            //RAM = getAvailableRAM();
                            //CPU = getCurrentCpuUsage();
                            updateTable();
                            
                        }
                        xmlqueue.DeleteMessage(getMessage);
                        getMessage = xmlqueue.GetMessage();
                    }
                    CloudQueueMessage getMessage2 = urlqueue.GetMessage();
                    while (getMessage2 != null && stopqueue.Exists())
                    {
                        urlsCrawled++;
                        try
                        {
                            foreach (string disallows in disallowSet)
                            {
                                if (!getMessage2.AsString.Contains(disallows))
                                {
                                    if (getMessage2 != null)
                                    {
                                        getMessage2 = urlqueue.GetMessage();
                                    }
                                }
                            }
                            if (getMessage2.AsString.StartsWith("/"))
                            {
                                cnn = "http://cnn.com" + getMessage2.AsString;
                                getMessage2 = new CloudQueueMessage(cnn);
                            }
                            else if (getMessage2.AsString.StartsWith("//"))
                            {
                                cnn = "http:" + getMessage2.AsString;
                                getMessage2 = new CloudQueueMessage(cnn);
                            }
                            HtmlDocument document = web.Load(getMessage2.AsString);
                            HtmlNode[] nodes = document.DocumentNode.SelectNodes("//link[@href]").ToArray();
                            //urlqueue.DeleteMessage(getMessage2);
                            queuecount--;
                            //countpage.queueSize = queuecount;
                            // updateTable();

                            var titleNode = document.DocumentNode.SelectSingleNode("//title");
                            string title = titleNode.InnerText;

                            var dateNode = document.DocumentNode.SelectSingleNode("//meta[@name='pubdate']");
                            string date = "";
                            if (dateNode != null)
                            {
                                date = dateNode.Attributes["content"].Value;
                            }
                            foreach (HtmlNode link in nodes)
                            {
                                HtmlAttribute att = link.Attributes["href"];
                                if (att.Value.Contains(@"http://www.cnn.com") || att.Value.Contains(@"http://bleacherreport.com"))
                                {
                                    //if (!duplicates.Contains(att.Value.ToString()))
                                    //{

                                    CloudQueueMessage message2 = new CloudQueueMessage(att.Value.ToString());
                                    duplicates.Add(att.Value.ToString());
                                    urlqueue.AddMessageAsync(message2);
                                    queuecount++;
                                    //countpage.queueSize = queuecount;
                                    //insertOperations = TableOperation.InsertOrReplace(countpage);
                                    // dashboardTable.Execute(insertOperations);

                                    //}
                                }
                            }
                            if (!tableDuplicates.Contains(getMessage2.AsString))
                            {
                                tableDuplicates.Add(getMessage2.AsString);
                                WebEntity webpage = new WebEntity(CreateMD5(getMessage2.AsString));
                                webpage.date = date;
                                webpage.title = title;
                                webpage.url = getMessage2.AsString;
                                TableOperation insertOperations = TableOperation.Insert(webpage);
                                tables.Execute(insertOperations);
                                var str = title;
                                tablecount++;
                                string trimTitle = new string((from c in title
                                                               where char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)
                                                               select c).ToArray());
                                string[] titleSplit = trimTitle.ToLower().Split(' ');
                                foreach (string titleWord in titleSplit)
                                {
                                    if (!titleWord.Equals(""))
                                    {
                                        StorageEntity keyword = new StorageEntity(titleWord, CreateMD5(getMessage2.AsString));
                                        keyword.date = date;
                                        keyword.url = getMessage2.AsString;
                                        keyword.title = title;
                                        insertOperations = TableOperation.Insert(keyword);
                                        storageTable.Execute(insertOperations);

                                    }
                                }
                                    //}

                                
                                //countpage.indexSize = countpage.indexSize + 1;
                                //insertOperations = TableOperation.InsertOrReplace(countpage);
                                //dashboardTable.Execute(insertOperations);
                                dashboardWord.Enqueue(getMessage2.AsString);
                                if (dashboardWord.Count() == 10)
                                {
                                    string dashboardString = "";
                                    foreach (string word in dashboardWord.ToArray())
                                    {
                                        dashboardString = word + ", " + dashboardString;

                                    }

                                    //countpage.url = dashboardString;
                                    updatedROW.url = dashboardString;
                                    //countpage.queueSize = queuecount;
                                    //RAM = getAvailableRAM();
                                    //CPU = getCurrentCpuUsage();
                                    //insertOperations = TableOperation.InsertOrReplace(countpage);

                                    dashboardWord.Dequeue(); 
                                }
                                updateTable();
                                RAM = getAvailableRAM();
                                CPU = getCurrentCpuUsage();
                            }
                            urlqueue.DeleteMessage(getMessage2);
                        }
                        catch { getMessage2 = urlqueue.GetMessage(); }

                    }



                }
            }
        }




            public object CloudConfigurationManager { get; private set; }


        public string getCurrentCpuUsage()
        {
            List<string> testCPU = new List<string>();
            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            return (cpuCounter.NextValue() + "%");

        }


        public string getAvailableRAM()
        {
            List<string> testRAM = new List<string>();
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            return (ramCounter.NextValue() + "MB");

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
        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }


        private void updateTable()
        {

            updatedROW.indexSize = tablecount;
            updatedROW.urlsCrawled = urlsCrawled;
            updatedROW.cpu = CPU;
            updatedROW.ram = RAM;
            updatedROW.queueSize = queuecount;
            TableOperation replace = TableOperation.InsertOrReplace(updatedROW);
            TableResult result = dashboardTable.Execute(replace);

            //replace = TableOperation.InsertOrReplace(updatedROW);
            //result = dashboardTable.Execute(replace);


        }

    }

}

