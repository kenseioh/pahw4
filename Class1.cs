using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class WebEntity : TableEntity
    {
        public WebEntity(string url)
        {
            this.PartitionKey = url;
            this.RowKey = Guid.NewGuid().ToString();
        }

        public WebEntity() { }

        public string date { get; set; }

        public string title { get; set; }

        public string url { get; set; }
    }



}
