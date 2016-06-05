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

    public class ListModule : NancyModule
    {
        public ListModule() : base("/list")
        {
         
            Get["/dedupe/{list*}", runAsync: true] = async (param, token) =>
            {
                CloudBlockBlob blob = Lists().GetBlockBlobReference(param.list);
                var list = JsonConvert.DeserializeObject<IEnumerable<Entry>>(await blob.DownloadTextAsync());

                var newlist = list.Distinct(new EntryComparer()).ToList();
                await blob.UploadTextAsync(JsonConvert.SerializeObject(newlist));
                return JsonConvert.SerializeObject(new { oldcount = list.Count(), newcount = newlist.Count() });

            };

            Post["/{list*}", runAsync: true] = async (param, token) =>
            {
                using (var sr = new StreamReader(this.Request.Body))
                using (var jsonTextReader = new JsonTextReader(sr))
                {
                    var serializer = new JsonSerializer();
                    var list = serializer.Deserialize<IEnumerable<Entry>>(jsonTextReader);
                    return await Save(list, param.list);
                }
            };

            
            Post["/", runAsync: true] = async (param, token) =>
            {
                var input = this.Bind<NewList>();

                var tasklist = input.Lines.Select(async line =>
                {
                    var image = (await GetValidResults(line + " " +  input.searchquery, input.safesearch)).FirstOrDefault();
                    if (image == null)
                    {
                        throw new Exception("no results for " + line);
                    }
                    image.name = line;
                    return image;
                });

                var saved = await Save(await Task.WhenAll(tasklist), input.name);
                if (saved == HttpStatusCode.Created)
                {
                    return Response.AsRedirect("/rank?list=" + System.Web.HttpUtility.UrlEncode(input.name));
                }
                return saved;

            };

            Post["/fromquery", runAsync: true] = async (param, token) =>
            {
                var input = this.Bind<NewList>();
                
                var entries = (await GetValidResults(input.searchquery, input.safesearch)).Take(32);
                if (!entries.Any())
                {
                    throw new Exception("no results for " + input.searchquery);
                }
                var saved = await Save(entries, input.name);
                if (saved == HttpStatusCode.Created)
                {
                    return Response.AsRedirect("/rank?list=" + System.Web.HttpUtility.UrlEncode(input.name));
                }
                return saved;
            };



            Get["/{list*}", runAsync: true] = async (param, token) =>
            {
                CloudBlockBlob blob = Lists().GetBlockBlobReference(param.list);
                if (!await blob.ExistsAsync())
                    return HttpStatusCode.NotFound;
                return await blob.DownloadTextAsync();
            };

            Delete["/{list*}", runAsync: true] = async (param, token) =>
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
            Get["/{list*}/rename", runAsync: true] = async (param, token) =>
            {
                string to = this.Request.Query["to"];
                if (string.IsNullOrEmpty(to))
                    return HttpStatusCode.BadRequest;
                await RenameAll(param.list, to);

                return HttpStatusCode.OK;
            };
        }

        const string imgsearchurl = "https://api.datamarket.azure.com/Bing/Search/Image?Query=%27{0}%27&$format=json&Adult=%27{1}%27";

        private async Task<IEnumerable<Entry>> GetValidResults(string query, string safesearch)
        {
            string key = ConfigurationManager.AppSettings["bingimagekey"];
            var http = new HttpClient();
            var base64 = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(key));
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64);


            var encoded = System.Web.HttpUtility.UrlEncode(query);

            var url = string.Format(imgsearchurl, encoded, safesearch);
            var resp = await http.GetAsync(url);

            var respstr = await resp.Content.ReadAsStringAsync();
            var respjson = JsonConvert.DeserializeObject<ImageResponse>(respstr);

            if (!respjson.d.results.Any())
                throw new Exception("no results from" + url);

            return respjson.d.results.Where(img =>
            {
                var req = new HttpRequestMessage(HttpMethod.Head, new Uri(img.mediaurl));
                try
                {
                    var imgresp = http.SendAsync(req).Result;
                    return imgresp.IsSuccessStatusCode; // &&
                           //imgresp.Headers.GetValues("Content-Type").Any(type => type.ToLower().StartsWith("image"));
                }
                catch (Exception)
                {
                    return false;
                }
            }).Select(image => new Entry
            {
                name = image.Title,
                img = image.mediaurl
            });
        }

        //somewhere else?
        private async Task SetRankCount(CloudBlockBlob b, int newcount)
        {
            b.Metadata["rankcount"] = newcount.ToString();
            await b.SetMetadataAsync();
        }

        private async Task<HttpStatusCode> Save(IEnumerable<Entry> list, string name)
        {
            CloudBlockBlob blob = Lists().GetBlockBlobReference(name.Trim());

            if (await blob.ExistsAsync())
                return HttpStatusCode.Conflict;

            list = list.Distinct(new EntryComparer());
            list = await CacheImages(list);
            await blob.UploadTextAsync(JsonConvert.SerializeObject(list));
            await SetRankCount(blob, 0);
            return HttpStatusCode.Created;
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

        private async Task<IEnumerable<Entry>> CacheImages(IEnumerable<Entry> entries)
        {
            var http = new HttpClient();

            var tasks = entries.Select(async e =>
            {
                var blob = CachedImg(e.img);
                e.cachedImg = blob.Uri.ToString();
                if (!!(await blob.ExistsAsync()))
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

        internal static CloudBlockBlob CachedImg(string imageurl)
        {
            var images = Client().GetContainerReference("images");
            if (imageurl.StartsWith(images.Uri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
            {
                //something we already uploaded
                return new CloudBlockBlob(new Uri(imageurl)); 
            }

            MD5 md5Hasher = MD5.Create();
            var hash = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(imageurl));
            var hashname = System.BitConverter.ToString(hash).Replace("-", "");
            return images.GetBlockBlobReference(hashname);
        }

       
        private static CloudBlobClient Client()
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