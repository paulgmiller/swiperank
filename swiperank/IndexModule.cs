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
    using System.Threading.Tasks;
    using System.Text;
    using System.Net.Http;
    using System.IO;

    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            Get["/random", runAsync: true] = async (param, token) =>
            {
                var all = (await AllLists()).ToList();
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

            Get["/", runAsync: true] = Get["/alllists", runAsync: true] = async (param, token) =>
            {
               return View["alllists", await AllLists()];
            };

            Get["/cacheimages", runAsync: true] = async (param, token) =>
            {
                var lists = await AllLists();
                await Task.WhenAll(lists.Select(async l =>
                {
                    CloudBlockBlob blob = Lists().GetBlockBlobReference(l);
                    var list = JsonConvert.DeserializeObject<IEnumerable<Entry>>(await blob.DownloadTextAsync());
                    await CacheImages(list);
                    blob.CreateSnapshot();
                    await blob.UploadTextAsync(JsonConvert.SerializeObject(list));
                }));
                return HttpStatusCode.OK;

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

            Get["/ranking/{list}/{hash}", runAsync: true] = async (param, token) =>
            {
                CloudBlockBlob blob = Rankings().GetBlockBlobReference(param.list + "/" + param.hash);
                if (!await blob.ExistsAsync())
                    return HttpStatusCode.NotFound;

                var json =  await blob.DownloadTextAsync();
                //need to save and pass back cap/max and seed.
                var ranking = JsonConvert.DeserializeObject<Ranking> (json);
                ranking.ListName = param.list;
                ranking.Hash = param.hash;
                return View["ranking", ranking];
            };

            Post["/list/{list}", runAsync: true] = async (param, token) =>
            {
                CloudBlockBlob blob = Lists().GetBlockBlobReference(param.list);
                
                if (await blob.ExistsAsync())
                    return HttpStatusCode.Conflict;
                //could cache images here
                using (var sr = new StreamReader(this.Request.Body))
                using (var jsonTextReader = new JsonTextReader(sr))
                {
                    var serializer = new JsonSerializer();
                    var list = serializer.Deserialize<IEnumerable<Entry>>(jsonTextReader);
                    await CacheImages(list);
                    await blob.UploadTextAsync(JsonConvert.SerializeObject(list));
                }

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

        private async Task<IEnumerable<string>> AllLists() //todo switch to async
        {
            var all = await Lists().ListBlobsSegmentedAsync(null); //todo figure out what todo when we have 5k + 
            var names = all.Results.Select(b => b.Uri.PathAndQuery.Split('/').Last());
            //limited black list 
            return names.Where(n => !n.ToLower().Contains("porn")).OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
        }

        private async Task CacheImages(IEnumerable<Entry> entries)
        {
            MD5 md5Hasher = MD5.Create();
            var images = Client().GetContainerReference("images");
            var http = new HttpClient();

            await Task.WhenAll(entries.Select(async e =>
            {
                var hash = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(e.img));
                var hashname = System.BitConverter.ToString(hash).Replace("-", "");
                var blob = images.GetBlockBlobReference(hashname);
                e.cachedImg = blob.Uri.ToString();
                if (!(await blob.ExistsAsync()))
                {
                    var img = await http.GetAsync(e.img);
                    await blob.UploadFromStreamAsync(await img.Content.ReadAsStreamAsync());
                }
            }));

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
        
    }
}