$year = 2016
$states = gc states.txt 
foreach ($state in $states )
{
  $stateenc = $state.Trim().replace(" ", "+") 
  $sc = wget http://www.missamerica.org/competition-info/national-contestants.aspx?state=$stateenc`&year=$year
  $image = $sc.Content.split("`"")  | select-string "images/contestant"
  "Miss $state, http://www.missamerica.org$image"
}
#Import-Csv .\missamerica2016.json | ConvertTo-Json | clip