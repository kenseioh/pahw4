using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class CheckEntity : TableEntity
    {
        public CheckEntity()
        {
            this.PartitionKey = "partition";
            this.RowKey = "row";
        }

        public string url { get; set; }

        public int indexSize { get; set; }
        public int urlsCrawled { get; set; }
        public string last10Crawled { get; set; }
        public string last10Errors { get; set; }
        public int queueSize { get; set; }

        public string ram { get; set; }

        public string cpu { get; set; }
    }



}
