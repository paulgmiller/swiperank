namespace swiperank
{
    using Nancy;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using System.Security.Cryptography;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using System;

    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            Get["/"] = parameters =>
            {
                return View["index"];
            };

            //better if we take it an reject or return hash url
            Post["/ranking/{list}", runAsync: true] = async (param, token) =>
            {
                MD5 md5Hasher = MD5.Create();
                var hash = md5Hasher.ComputeHash(this.Request.Body);
                var relativeUrl = param.list + "/" + BitConverter.ToString(hash).ToString().Replace("-", "");
                var blob = Rankings().GetBlockBlobReference(relativeUrl);
                this.Request.Body.Seek(0, System.IO.SeekOrigin.Begin);
                await blob.UploadFromStreamAsync(this.Request.Body);
                return "ranking/" + relativeUrl;
            };

            //better if we take it an reject or return hash url
            Get["/ranking/{list}/{id}", runAsync: true] = async (param, token) =>
            {
                var blob = Rankings().GetBlockBlobReference(param.list + "/" + param.id);
                var json =  await blob.DownloadTextAsync();
                var ranking = JsonConvert.DeserializeObject<List<Entry>>(json);
                return View["ranking", ranking];
            };
        }

        private CloudBlobContainer Rankings()
        {
            var account = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=swiperankings;AccountKey=ReTB+/YWBrAeD7cC6//WrG2iRbG6D8ErOQRKI+Vcs5YJhXnQX/JFold6bsbW+Y5dFB9lGZUhoKpLat/o5b1gRA==");
            var client = account.CreateCloudBlobClient();
            return client.GetContainerReference("rankings");
        }

        public class Entry
        {
            public string name;
            public string img;
        }
    }
}