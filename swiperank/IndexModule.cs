namespace swiperank
{
    using Nancy;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage;
    using System.Security.Cryptography;
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
                var account = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=swiperankings;AccountKey=ReTB+/YWBrAeD7cC6//WrG2iRbG6D8ErOQRKI+Vcs5YJhXnQX/JFold6bsbW+Y5dFB9lGZUhoKpLat/o5b1gRA==");
                var client = account.CreateCloudBlobClient();

                var rankings = client.GetContainerReference("rankings");
                await rankings.CreateIfNotExistsAsync();
                MD5 md5Hasher = MD5.Create();
                var hash = md5Hasher.ComputeHash(this.Request.Body);
                var blob = rankings.GetBlockBlobReference(param.list + "/" + BitConverter.ToString(hash).ToString());


                await blob.UploadFromStreamAsync(this.Request.Body);
                return HttpStatusCode.OK;
            };
        }
    }
}