using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class StorageEntity : TableEntity
    {
        public StorageEntity(string word, string url)
        {
            this.PartitionKey = word;
            this.RowKey = url;
        }

        public StorageEntity() { }

        public string date { get; set; }

        public string title { get; set; }

        public string url { get; set; }
    }



}
