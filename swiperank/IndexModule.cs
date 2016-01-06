namespace swiperank
{
    using Nancy;
    using Nancy.ModelBinding;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using System.Security.Cryptography;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using System;
    using System.Configuration;
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
                var r = new Random();
                var listing = await GetLists();
                var index = r.Next(0, listing.Lists.Count() + listing.Collections.Count());
                while (index >= listing.Lists.Count())
                {
                    listing = await GetLists(listing.Collections[index - listing.Lists.Count()]);
                    index = r.Next(0, listing.Lists.Count() + listing.Collections.Count());
                }
                return Response.AsRedirect("/rank?list=" + listing.Lists[index]);
            };

            Get["/rank"] = parameters =>
            {
                if (string.IsNullOrEmpty(this.Request.Query["list"]))
                    return Response.AsRedirect("/random");
                return View["rank"];
            };

            Get["/rank"] = parameters =>
            {
                if (string.IsNullOrEmpty(this.Request.Query["list"]))
                    return Response.AsRedirect("/random");
                return View["rank"];
            };

            Get["/", runAsync: true] = Get["/alllists", runAsync: true] = async (param, token) =>
            {
                var collection = this.Request.Query["collection"] ?? ""; //if nothing empty string is the root
                return View["alllists", await GetLists(collection)];
            };

            Get["/dedupe/{list*}", runAsync: true] = async (param, token) =>
            {
                CloudBlockBlob blob = Lists().GetBlockBlobReference(param.list);
                var list = JsonConvert.DeserializeObject<IEnumerable<Entry>>(await blob.DownloadTextAsync());

                var newlist = list.Distinct(new EntryComparer()).ToList();
                await blob.UploadTextAsync(JsonConvert.SerializeObject(newlist));
                return JsonConvert.SerializeObject(new { oldcount = list.Count(), newcount = newlist.Count() });

            };

            Post["/ranking/{list*}", runAsync: true] = async (param, token) =>
            {
                MD5 md5Hasher = MD5.Create();
                var hash = md5Hasher.ComputeHash(this.Request.Body);
                var relativeUrl = param.list + "/" + BitConverter.ToString(hash).ToString().Replace("-", "");
                var blob = Rankings().GetBlockBlobReference(relativeUrl);
                this.Request.Body.Seek(0, System.IO.SeekOrigin.Begin);
                await blob.UploadFromStreamAsync(this.Request.Body);
                return "ranking/" + relativeUrl;
            };

            Get["/aggregateranking/{list*}", runAsync: true] = async (param, token) =>
            {
                var aggregateranking = await Aggregate(param.list);
                return View["aggregateranking", aggregateranking];
                //return JsonConvert.SerializeObject(aggregateranking);
            };

            Get["/ranking/{list*}", runAsync: true] = async (param, token) =>
            {
                //greedy doesn't let us pull out the hash so do it manually
                string cobminedlistandhash = param.list;
                CloudBlockBlob blob = Rankings().GetBlockBlobReference(cobminedlistandhash);
                if (!await blob.ExistsAsync())
                    return HttpStatusCode.NotFound;

                var json = await blob.DownloadTextAsync();
                //need to save and pass back cap/max and seed.
                var ranking = JsonConvert.DeserializeObject<Ranking>(json);
                
                int sep = cobminedlistandhash.LastIndexOf('/');
                ranking.ListName = cobminedlistandhash.Substring(0, sep);
                ranking.Hash = cobminedlistandhash.Substring(sep+1); 
                return View["ranking", ranking];
            };

            Post["/list/{list*}", runAsync: true] = async (param, token) =>
            {
                using (var sr = new StreamReader(this.Request.Body))
                using (var jsonTextReader = new JsonTextReader(sr))
                {
                    var serializer = new JsonSerializer();
                    var list = serializer.Deserialize<IEnumerable<Entry>>(jsonTextReader);
                    return await Save(list, param.list);
                }
            };

            
            const string imgsearchurl = "https://api.datamarket.azure.com/Bing/Search/Image?Query=%27{0}%27&$format=json&Adult=%27{1}%27";
            Get["/createlist"] = _ => View["createlist"];

            Post["/createlist", runAsync: true] = async (param, token) =>
            {
                string key = ConfigurationManager.AppSettings["bingimagekey"];
                var http = new HttpClient();
                var base64 = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(key));
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);
                var input = this.Bind<NewList>();

                var tasklist = input.Lines.Select(async line => 
                {
                    var encoded = System.Web.HttpUtility.UrlEncode(line + " " + input.searchhelper);

                    var url = string.Format(imgsearchurl, encoded, input.safesearch);
                    var resp = await http.GetAsync(url);

                    var respstr = await resp.Content.ReadAsStringAsync();
                    var respjson = JsonConvert.DeserializeObject<ImageResponse>(respstr);

                    var image = respjson.d.results.FirstOrDefault(img =>
                    {
                        var req = new HttpRequestMessage(HttpMethod.Head, new Uri(img.mediaurl));
                        try
                        {
                            return http.SendAsync(req).Result.IsSuccessStatusCode;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    });

                    if (image == null)
                    {
                        throw new Exception("no results for " + line);
                    }

                    return new Entry
                    {
                        name = line,
                        img = image.mediaurl
                    };
                });
                var saved = await Save(await Task.WhenAll(tasklist), input.name);
                if (saved == HttpStatusCode.Created)
                {
                    return Response.AsRedirect("/Rank?list=" + System.Web.HttpUtility.UrlEncode(input.name));
                }
                return saved;

            };



            Get["/list/{list*}", runAsync: true] = async (param, token) =>
            {
                CloudBlockBlob blob = Lists().GetBlockBlobReference(param.list);
                if (!await blob.ExistsAsync())
                    return HttpStatusCode.NotFound;
                return await blob.DownloadTextAsync();
            };

            Delete["/list/{list*}", runAsync: true] = async (param, token) =>
            {
                if (!ConfigurationManager.AppSettings["deletepassword"].Equals(this.Request.Query["password"]))
                    return HttpStatusCode.Unauthorized;
                CloudBlockBlob blob = Lists().GetBlockBlobReference(param.list);
                if (!await blob.ExistsAsync())
                    return HttpStatusCode.NotFound;
                await blob.DeleteAsync();
                return HttpStatusCode.OK;
            };

            //better if we take it an reject or return hash url
            Get["/rename/{list*}", runAsync: true] = async (param, token) =>
            {
                string to = this.Request.Query["to"];
                if (string.IsNullOrEmpty(to))
                    return HttpStatusCode.BadRequest;
                await RenameAll(param.list, to);

                return HttpStatusCode.OK;
            };
        }

        private async Task<HttpStatusCode> Save(IEnumerable<Entry> list, string name)
        {
            CloudBlockBlob blob = Lists().GetBlockBlobReference(name.Trim());

            if (await blob.ExistsAsync())
                return HttpStatusCode.Conflict;

            list = list.Distinct(new EntryComparer());
            list = await CacheImages(list);
            await blob.UploadTextAsync(JsonConvert.SerializeObject(list));
            return HttpStatusCode.Created;
        }

        private async Task<AggregateRanking> Aggregate(string list)
        {
            var rankingnames = await Rankings().ListBlobsSegmentedAsync(list + "/", null);
            var rankings = await Task.WhenAll(rankingnames.Results.OfType<CloudBlockBlob>().Select(async r =>
            {
                var json = await r.DownloadTextAsync();
                //need to save and pass back cap/max and seed.
                return JsonConvert.DeserializeObject<Ranking>(json);
            }));
            var agg = new AggregateRanking() { ListName = list };
            foreach (var r in rankings)
            {
                agg.Add(r);
            }
            return agg;
        }


        private async Task RenameAll(string from, string to)
        {
            await RenameAsync(Lists(), from, to);

            var rankings = await Rankings().ListBlobsSegmentedAsync(from + "/", null);
            await Task.WhenAll(rankings.Results.OfType<CloudBlockBlob>().Select(r =>
            {
                var newName = r.Name.Replace(from, to);
                return RenameAsync(Rankings(), r.Name, newName);
            }));
        }

        private static async Task RenameAsync(CloudBlobContainer container, string oldName, string newName)
        {
            var source = container.GetBlockBlobReference(oldName);
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

        public class Listing 
        {
            public List<string> Lists;
            public List<string> Collections;
        }


        private async Task<Listing> GetLists(string prefix = "") //todo switch to async
        {
            var all = await Lists().ListBlobsSegmentedAsync(prefix,null);// "", false, BlobListingDetails.None, null, null, null, null);
            
            return new Listing()
            {
                Lists = FilterAndSort(all.Results.OfType<CloudBlockBlob>().Select(b => b.Name)),
                Collections = FilterAndSort(all.Results.OfType<CloudBlobDirectory>().Select(d => d.Prefix))
            };
        }

        private List<string> FilterAndSort(IEnumerable<string> list)
        {
            return list.Where(l => !l.ToLower().Contains("porn")).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private async Task<IEnumerable<Entry>> CacheImages(IEnumerable<Entry> entries)
        {
            MD5 md5Hasher = MD5.Create();
            var images = Client().GetContainerReference("images");
            var http = new HttpClient();

            var tasks = entries.Select(async e =>
            {
                var hash = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(e.img));
                var hashname = System.BitConverter.ToString(hash).Replace("-", "");
                var blob = images.GetBlockBlobReference(hashname);
                e.cachedImg = blob.Uri.ToString();
                if (!(await blob.ExistsAsync()))
                {
                    try
                    {
                        var img = await http.GetAsync(e.img);
                        await blob.UploadFromStreamAsync(await img.Content.ReadAsStreamAsync());
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
                return e;

            });
            
            return (await Task.WhenAll(tasks)).Where(e => e != null);

        }

       
        private CloudBlobClient Client()
        {
            var account = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["swiperankings"].ConnectionString);
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