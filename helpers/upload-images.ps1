param($accountkey, $files, $listname)
$c = New-AzureStorageContext -connectionstring "DefaultEndpointsProtocol=https;AccountName=swiperankings;AccountKey=$accountkey"
$container = Get-AzureStorageContainer -n "images" -Context $c -ErrorAction Stop

$result = $files | % { 
  $hash = (get-filehash $_).hash; 
  $b = $container.CloudBlobContainer.GetBlockBlobReference($hash); 
  $b.UploadFromFile($_.fullname, [System.IO.FileMode]::Open); 
  @{name=$_.Name; img=$b.Uri.AbsoluteUri };
}

irm -method post http://swiperank.com/list/$listname -body ($result | convertto-json)