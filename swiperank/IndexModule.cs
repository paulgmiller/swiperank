namespace swiperank
{
    using Nancy;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage;

    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            Get["/"] = parameters =>
            {
                return View["index"];
            };

            //better if we take it an reject or return hash url
            Post["/ranking/{list}/{id}", runAsync: true] = async (param, token) =>
            {
                var account = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=swiperankings;AccountKey=ReTB+/YWBrAeD7cC6//WrG2iRbG6D8ErOQRKI+Vcs5YJhXnQX/JFold6bsbW+Y5dFB9lGZUhoKpLat/o5b1gRA==;");
                var client = account.CreateCloudBlobClient();

                var rankings = client.GetContainerReference("rankings");
                //await rankings.CreateIfNotExistsAsync();
                var blob = rankings.GetBlockBlobReference(param.list + "/" + param.id);

                await blob.UploadTextAsync(this.Request.Body);
                return "ok";
            };
        }
    }
}