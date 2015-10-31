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

            Get["/cacheimages/{list}", runAsync: true] = async (param, token) =>
            {
                CloudBlockBlob blob = Lists().GetBlockBlobReference(param.list);
                var list = JsonConvert.DeserializeObject<IEnumerable<Entry>>(await blob.DownloadTextAsync());
                
                await CacheImages(list);
                var newlist = JsonConvert.SerializeObject(list);
                await blob.UploadTextAsync(newlist);
                return JsonConvert.SerializeObject(JsonConvert.SerializeObject(list.Select(e => e.cachedImg)));

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

            //better if we take it an reject or return hash url
            Get["/rename/{list}", runAsync: true] = async (param, token) =>
            {
                string to = this.Request.Query["to"];
                if (string.IsNullOrEmpty(to))
                    return HttpStatusCode.BadRequest;
                await RenameAll(param.list, to);

                return HttpStatusCode.OK;
            };


        }


        private async Task RenameAll(string from, string to)
        {
            await RenameAsync(Lists(), from, to);

            var rankings = await Rankings().ListBlobsSegmentedAsync(from, null);
            await Task.WhenAll(rankings.Results.OfType<CloudBlockBlob>().Select(r =>
            {
                var newName = r.Name.Replace(from, to);
                return RenameAsync(Rankings(), r.Name, newName);
            }));
        }

        private static async Task RenameAsync(CloudBlobContainer container, string oldName, string newName)
        {
            var source = await container.GetBlobReferenceFromServerAsync(oldName);
            var target = container.GetBlockBlobReference(newName);
            if (!await source.ExistsAsync())
                throw new ApplicationException("Rename failed does not exist : " + oldName);

            await target.StartCopyAsync(source.Uri);

            while (target.CopyState.Status == CopyStatus.Pending)
                await Task.Delay(100);

            if (target.CopyState.Status != CopyStatus.Success)
                throw new ApplicationException("Rename failed: " + target.CopyState.Status);

            await source.DeleteAsync();
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