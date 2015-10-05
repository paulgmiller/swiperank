param($year)
$states = gc states.txt 
$contestants = $states | % {
  $state = $_;
  $stateenc = $state.Trim().replace(" ", "+") 
  $sc = wget http://www.missamerica.org/competition-info/national-contestants.aspx?state=$stateenc`&year=$year
  $image = $sc.Content.split("`"")  | select-string "images/contestant"
  @{ name="Miss $state"; img="http://www.missamerica.org$image"}
}
$contestants | ConvertTo-Json 