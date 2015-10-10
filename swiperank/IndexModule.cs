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

            Get["/ranking/{list}/{id}", runAsync: true] = async (param, token) =>
            {
                CloudBlockBlob blob = Rankings().GetBlockBlobReference(param.list + "/" + param.id);
                if (!await blob.ExistsAsync())
                    return HttpStatusCode.NotFound;

                var json =  await blob.DownloadTextAsync();
                //need to save and pass back cap/max and seed.
                var ranking = JsonConvert.DeserializeObject<Ranking> (json);
                ranking.ListName = param.list;
                return View["ranking", ranking];
            };

            Post["/list/{list}", runAsync: true] = async (param, token) =>
            {
                CloudBlockBlob blob = Lists().GetBlockBlobReference(param.list);
                if (await blob.ExistsAsync())
                    return HttpStatusCode.Conflict;
                await blob.UploadFromStreamAsync(this.Request.Body);
                return HttpStatusCode.Created;
            };

            //better if we take it an reject or return hash url
            Post["/images/{img}", runAsync: true] = async (param, token) =>
            {
                var images = Client().GetContainerReference("images");
                await images.CreateIfNotExistsAsync();
                CloudBlockBlob blob = images.GetBlockBlobReference(param.img);
                if (await blob.ExistsAsync())
                    return HttpStatusCode.Conflict;
                await blob.UploadFromStreamAsync(this.Request.Body);
                return HttpStatusCode.Created;
            };

            //better if we take it an reject or return hash url
            Get["/list/{list}", runAsync: true] = async (param, token) =>
            {
                CloudBlockBlob blob = Lists().GetBlockBlobReference(param.list);
                if (!await blob.ExistsAsync())
                    return HttpStatusCode.NotFound;
                return await  blob.DownloadTextAsync();
            };
        }

        private CloudBlobClient Client()
        {
            var account = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=swiperankings;AccountKey=ReTB+/YWBrAeD7cC6//WrG2iRbG6D8ErOQRKI+Vcs5YJhXnQX/JFold6bsbW+Y5dFB9lGZUhoKpLat/o5b1gRA==");
            return account.CreateCloudBlobClient();
        }


        private CloudBlobContainer Rankings()
        {
            return Client().GetContainerReference("rankings");
        }

        private CloudBlobContainer Lists()
        {
            return Client().GetContainerReference("lists");
        }

        public class Ranking : List<Entry>
        {
            public string ListName;
        }

        public class Entry
        {
            public string name;
            public string img;
        }
    }
}