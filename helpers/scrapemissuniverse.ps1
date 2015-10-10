param($year)
$(wget http://missuniverse.com/members/contestants/superregion:missu/year:$year/category:GLA).content > contestants.txt
$profiles = gc contestants.txt | select-string  "members/profile"  | % { $_.line } | ? { $_ -notmatch "Click" }  | % { $_.split("`"")[3] } 
$profiles | % {
  $r = Invoke-webrequest ("http://missuniverse.com" + $_);
  $name = $r.AllElements | ? { $_.tagName -eq "title" }
  $img = $r.Images | ? { $_.src -match "photographer_uploaded" } 
  @{ name=$name.innerHTML;img=$img[2].src }
 } | convertto-json