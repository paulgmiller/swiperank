param($listfile)
$list = gc $listfile
Add-Type -Assembly System.Web

function encode ($Query)
{
     return  '%27' + (($Query | %{ [Web.HttpUtility]::UrlEncode($_) }) -join '+') + '%27'
}

$key = "iXmLK5VWa0N0RdqJ4csrl6zDFw5DnFlwrPCbQcK4cqE="
$Base64KeyBytes = [byte[]] [Text.Encoding]::ASCII.GetBytes("ignored:$key")
$Base64Key = [Convert]::ToBase64String($Base64KeyBytes)

$list | % { 
 $encode = encode($_)
  
  $result = invoke-restmethod "https://api.datamarket.azure.com/Bing/Search/Image?Query=$encode&`$format=json" -headers @{ Authorization = "Basic $Base64Key" }
  @{ name =$_.ToString(); img= $result.d.results[0].mediaurl}
} | convertto-json
