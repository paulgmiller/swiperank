param($year, $index = 1)
$(wget http://www.missuniverse.com/missusa/members/contestants/superregion:missusa/year:$year/category:GLA).content > contestants.txt
$profiles = gc contestants.txt | select-string  "members/profile"  | % { $_.line } | ? { $_ -notmatch "Click" }  | % { $_.split("`"")[3] } 
$profiles | % {
  $r = Invoke-webrequest ("http://missuniverse.com" + $_);
  $name = $r.AllElements | ? { $_.tagName -eq "title" }
  $i= $r.Images | ? { $_.src -match "photographer" }
  $img = $i[$index]
  @{ name=$name.innerHTML;img=$img.src }
 } | convertto-json