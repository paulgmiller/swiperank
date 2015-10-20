namespace swiperank
{
    using Nancy;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using System.Security.Cryptography;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using System;
    using System.Linq;

    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            Get["/random"] = parameters =>
            {
                var all = AllLists().ToList();
                var r = new Random();
                var pick = all[r.Next(0, all.Count())];
                return Response.AsRedirect("/rank?list=" + pick);
            };

            Get["/rank"] = parameters =>
            {
                if (string.IsNullOrEmpty(this.Request.Query["list"]))
                    return Response.AsRedirect("/random");
                return View["index"];
            };

            Get["/"] = Get["/alllists"] = parameters =>
            {
               return View["alllists", AllLists()];
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
                //could cache images here
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

        private IEnumerable<string> AllLists() //todo switch to async
        {
            IEnumerable<IListBlobItem> all = Lists().ListBlobs();
            var names = all.Select(b => b.Uri.PathAndQuery.Split('/').Last());
            //limited black list 
            return names.Where(n => !n.ToLower().Contains("porn"));
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