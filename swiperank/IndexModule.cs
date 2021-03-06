﻿namespace swiperank
{
    using Nancy;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using System.Security.Cryptography;
    using System.Collections.Generic;
    using System.Configuration;
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Text;
    using System.Web;
    using Loggr;

    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            Get["/random", runAsync: true] = async (param, token) =>
            {
                var r = new Random();
                var listing = await GetLists();
                var index = r.Next(0, listing.Count());
                return Response.AsRedirect("/rank?list=" + listing.ElementAt(index).Name);
            };

            Get["/rank", runAsync: true] = async (param, tokens) =>
            {
                var listname = this.Request.Query["list"];
                if (string.IsNullOrEmpty(listname))
                    return Response.AsRedirect("/random");
                var listjson = await ListModule.GetList(listname);
                var rankembryo = new RankingEmbryo
                {
                    ListName = listname,
                    Json = listjson
                };
               
                //debating whether to just embed the list in the page rather than making an ajax call
                return View["rank", rankembryo];
            };

            Get["/", runAsync: true] = 
            Get["/alllists", runAsync: true] = async (param, token) =>
            {
                var collection = this.Request.Query["collection"] ?? ""; //if nothing empty string is the root
                IEnumerable<CloudBlockBlob> lists = await GetLists(collection);
                //lists.Select(l => new { nam = l.Name, rankings = RankCount(l) }) (show rank counts?} 
                return View["alllists", await SortLists(lists)];
            };
      
            Post["/ranking/{list*}", runAsync: true] = async (param, token) =>
            {
                MD5 md5Hasher = MD5.Create();
                var hash = md5Hasher.ComputeHash(this.Request.Body);
                var relativeUrl = param.list + "/" + BitConverter.ToString(hash).ToString().Replace("-", "");
                var ranking = Rankings().GetBlockBlobReference(relativeUrl);
                if (await ranking.ExistsAsync())
                {
                    int rankingcount = await RankCount(ranking, "1");
                    await SetRankCount(ranking, ++rankingcount);
                }
                else
                {
                    this.Request.Body.Seek(0, System.IO.SeekOrigin.Begin);
                    await ranking.UploadFromStreamAsync(this.Request.Body);
                }
                string name = param.list;
                CloudBlockBlob list = Lists().GetBlockBlobReference(name);
                int r = await RankCount(list);
                await SetRankCount(list, ++r);

                Loggr.Events.Create()
                    .Text("Ranking created: {0}", "ranking/" + relativeUrl)
                    .Link("ranking /" + relativeUrl)
                    .Source(this.Context.Request.UserHostAddress ?? "unknown")
                    .Post();
                return "ranking/" + relativeUrl;
            };

            Get["/aggregateranking/{list*}", runAsync:true] = async (param, token) =>
            {
                var aggregateranking = await Aggregate(param.list);
                return View["aggregateranking", aggregateranking];
            };

            Get["/updatemetadata", runAsync: true] = async (param, token) =>
            {
                var lists = await GetLists();
                var tasks = lists.Select(async list =>
                {
                    var rankings = await Rankings().ListBlobsSegmentedAsync(list.Name + "/", null);
                    await SetRankCount(list, rankings.Results.Count());
                });
                await Task.WhenAll(tasks);
                return HttpStatusCode.OK;
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

            
            Get["/createlist"] = _ => View["createlist"];
            Get["/createlistfromquery"] = _ => View["createlistfromquery"];
            Get["/createlistfromfiles"] = _ => View["createlistfromfiles"];
            Get["/google8c897581c514bf87.html"] = _ => "google-site-verification: google8c897581c514bf87.html";
            Get["/sitemap.txt", runAsync: true] = async (param, tokens) =>
            {
                IEnumerable<CloudBlockBlob> lists = await GetLists();
                StringBuilder sitemap = new StringBuilder();
                var scheme =  this.Request.Url.Scheme;
                var host = this.Request.Url.HostName;
                foreach (var list in lists)
                {
                    var name = HttpUtility.UrlPathEncode(list.Name);
                    sitemap.AppendLine($"{scheme}://{host}/aggregateranking/{name}");
                }
                return sitemap.ToString();
            };

        }

        //so ideally we want to bias by rankcount and freshness (last ranking and creation?)
        private async Task<IEnumerable<ListStub>> SortLists(IEnumerable<CloudBlockBlob> blobs)
        {
            var rand = new Random();
            var top = blobs.OrderBy(b => rand.Next())
                        .Where(b => RankCount(b).Result > 1)
                        .Take(10);
            var stubs = top.Select(async b =>
                        new ListStub {
                            name = b.Name,
                            rankings = await RankCount(b),
                            thumbnail = await Thumbnail(b)
                        });
            return await Task.WhenAll(stubs);
        }

       
        private async Task<string> Thumbnail(CloudBlockBlob b)
        {
            var rand = new Random();
            var list = JsonConvert.DeserializeObject<IEnumerable<Entry>>(await b.DownloadTextAsync());
            return list.OrderBy(e => rand.Next()).First().cachedImg;
        }


        private async Task<AggregateRanking> Aggregate(string list)
        {
            var rankingnames = await Rankings().ListBlobsSegmentedAsync(list + "/", null);
            var rankings = await Task.WhenAll(rankingnames.Results.OfType<CloudBlockBlob>().Select(async r =>
            {
                var json = await r.DownloadTextAsync();
                //need to save and pass back cap/max and seed.
                var ranking = JsonConvert.DeserializeObject<Ranking>(json);
                ranking.Multiplier  = await RankCount(r, "1");
                return ranking;
            }));
            var agg = new AggregateRanking() { ListName = list };
            
            foreach (var r in rankings)
            {
                if (r == null) continue;
                agg.Add(r);
            }
            return agg;
        }

        private async Task<int> RankCount(CloudBlockBlob b, string defaultcount = "0")
        {
            if (b.Metadata == null)
                await b.FetchAttributesAsync();
            try
            {
                return int.Parse(b.Metadata["rankcount"] ?? defaultcount);
            }
            catch (KeyNotFoundException)
            {
                return int.Parse(defaultcount);
            }

        }

        //move to blob extentions
        private async Task SetRankCount(CloudBlockBlob b, int newcount)
        {
            b.Metadata["rankcount"] = newcount.ToString();
            await b.SetMetadataAsync();
        }
        
        private async Task<IEnumerable<CloudBlockBlob>> GetLists(string prefix = "") //todo switch to async
        {
            var blobs = await Lists().ListBlobsSegmentedAsync(prefix, true, 
                BlobListingDetails.Metadata, null, null, null, null);// "", false, BlobListingDetails.None, null, null, null, null)
            return Filter(blobs.Results.OfType<CloudBlockBlob>());
        }

        private IEnumerable<CloudBlockBlob> Filter(IEnumerable<CloudBlockBlob> list)
        {
            return list.Where(l => !l.Name.ToLower().Contains("porn"));
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