$(document).ready(function(){

var imgsourcelist = shuffle([
    {
        "name":  "Miss Alabama ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Alabama-Meg-VcGuffin.jpg"
    },
    {
        "name":  "Miss Alaska ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Alaska-Zoey-Grenier.jpg"
    },
    {
        "name":  "Miss Arizona ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Arizona-Madi-Esteves.jpg"
    },
    {
        "name":  "Miss Arkansas ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Arkansas-Loren-Alyssa-McDaniel.jpg"
    },
    {
        "name":  "Miss California ",
        "img":  "http://www.missamerica.org/images/contestants/2016/California-Bree-Morse.jpg"
    },
    {
        "name":  "Miss Colorado ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Colorado-Kelley-Johnson.jpg"
    },
    {
        "name":  "Miss Connecticut ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Connecticut--Colleen-Ward.jpg"
    },
    {
        "name":  "Miss Delaware ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Delaware-Brooke-Mitchell.jpg"
    },
    {
        "name":  "Miss Florida ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Florida--Mary-Katherine-Fechtel.jpg"
    },
    {
        "name":  "Miss Georgia ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Georgia-Betty-Cantrell.jpg"
    },
    {
        "name":  "Miss Hawaii ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Hawaii--Jeanne-Kapela.jpg"
    },
    {
        "name":  "Miss Idaho ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Idaho-Kalie-Wright.jpg"
    },
    {
        "name":  "Miss Illinois ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Illinois-Crystal-davis.jpg"
    },
    {
        "name":  "Miss Indiana ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Indiana-Morgan-Jackson.jpg"
    },
    {
        "name":  "Miss Iowa ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Iowa-Taylor-Wiebers.jpg"
    },
    {
        "name":  "Miss Kansas ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Kansas-Hannah-Wagner.jpg"
    },
    {
        "name":  "Miss Kentucky ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Kentucky-Clark-Janell-Davis.jpg"
    },
    {
        "name":  "Miss Louisiana ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Louisiana-April-Nelson.jpg"
    },
    {
        "name":  "Miss Maine ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Maine-Kelsey-Earley.jpg"
    },
    {
        "name":  "Miss Maryland ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Maryland-Destiny-Clark.jpg"
    },
    {
        "name":  "Miss Massachusetts ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Massachusetts-Meagan-Fuller.jpg"
    },
    {
        "name":  "Miss Michigan ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Michigan-Emily-Kieliszewski.jpg"
    },
    {
        "name":  "Miss Minnesota ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Minnesota-Rachel-Latuff.jpg"
    },
    {
        "name":  "Miss Mississippi ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Mississippi-Hannah-Roberts.jpg"
    },
    {
        "name":  "Miss Missouri ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Missouri-McKensie-Garber.jpg"
    },
    {
        "name":  "Miss Montana ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Montana-Danielle-Wineman.jpg"
    },
    {
        "name":  "Miss Nebraska ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Nebraska-Alyssa-Howell.jpg"
    },
    {
        "name":  "Miss Nevada ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Nevada--Katherine-Kelley.jpg"
    },
    {
        "name":  "Miss New Hampshire ",
        "img":  "http://www.missamerica.org/images/contestants/2016/New-Hampshire-Holly-Blanchard.jpg"
    },
    {
        "name":  "Miss New Jersey ",
        "img":  "http://www.missamerica.org/images/contestants/2016/New-Jersey-Lindsey-Gianni.jpg"
    },
    {
        "name":  "Miss New Mexico ",
        "img":  "http://www.missamerica.org/images/contestants/2016/New-Mexico-Marissa-Livingston.jpg"
    },
    {
        "name":  "Miss New York ",
        "img":  "http://www.missamerica.org/images/contestants/2016/New-York-Jamie-Lynn-Macchia.jpg"
    },
    {
        "name":  "Miss North Carolina ",
        "img":  "http://www.missamerica.org/images/contestants/2016/North-Carolina-Kate-Peacock.jpg"
    },
    {
        "name":  "Miss North Dakota ",
        "img":  "http://www.missamerica.org/images/contestants/2016/North-Dakota-Delanie-Wiedrich.jpg"
    },
    {
        "name":  "Miss Ohio ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Ohio-Sarah-Hider.jpg"
    },
    {
        "name":  "Miss Oklahoma ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Oklahoma--Georgia-Frazier.jpg"
    },
    {
        "name":  "Miss Oregon ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Oregon-Ali-Wallace.jpg"
    },
    {
        "name":  "Miss Pennsylvania ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Pennsylvania-Ashley-Schmider.jpg"
    },
    {
        "name":  "Miss Rhode Island ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Rhode-Island-Allie-Curtis.jpg"
    },
    {
        "name":  "Miss South Carolina ",
        "img":  "http://www.missamerica.org/images/contestants/2016/South-Carolina-Daja-Dial.jpg"
    },
    {
        "name":  "Miss South Dakota ",
        "img":  "http://www.missamerica.org/images/contestants/2016/South-Dakota-Autumn-Simunek.jpg"
    },
    {
        "name":  "Miss Tennessee ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Tennessee--Hannah-Robison.jpg"
    },
    {
        "name":  "Miss Texas ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Texas--Shannon-Sanderfold.jpg"
    },
    {
        "name":  "Miss Utah ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Utah--Krissia-Beatty.jpg"
    },
    {
        "name":  "Miss Vermont ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Vermont-Alayna-Westcom.jpg"
    },
    {
        "name":  "Miss Virginia ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Virginia--Savannah-Lane.jpg"
    },
    {
        "name":  "Miss Washington ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Washington--Lizzi-Jackson.jpg"
    },
    {
        "name":  "Miss West Virginia ",
        "img":  "http://www.missamerica.org/images/contestants/2016/West-Virginia-Chelsea-Malone.jpg"
    },
    {
        "name":  "Miss Wisconsin ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Wisconsin-Rosalie-Smith.jpg"
    },
    {
        "name":  "Miss Wyoming ",
        "img":  "http://www.missamerica.org/images/contestants/2016/Wyoming-Mikaela-Shaw-.jpg"
    }
]);

var ranker = {
    ranking: [imgsourcelist.pop()],
    min : 0,
    max : 1,
    candidate: imgsourcelist.pop(),
    done : false,
    cap : 10,
  	
    doppel: function () {
        return Math.floor((this.max + this.min)/2);
    },
    worse:  function () {
	    if (this.done()) { return; }
	    this.max = this.doppel();
      this.check();
    },
    better: function () {
    	if (this.done()) { return; }
    	this.min = this.doppel()+1;
      this.check();
    },
    check: function () {
      if (this.min === this.max)
      {
        this.ranking.splice(this.min, 0, this.candidate);
        //if sourcelist.empty done.
      	this.candidate = imgsourcelist.pop();
        this.min = 0;
        if (this.ranking.length > this.cap)
        {
           this.ranking.pop();
        }  
        
        if (this.ranking.length >= this.cap)
        {  
           this.max = (this.cap*2-1);
        }
        else
        {
           this.max = this.ranking.length; //start in the middle while we are under the cap
        }
      }
      else if (this.min >= this.cap)
      {
        this.candidate = imgsourcelist.pop();
        this.min = 0;
        this.max = (this.cap*2-1);
      }
      
      paint();
    },
    consideration : function() { return this.ranking[this.doppel()]},
    done : function() { return this.candidate === undefined; }
};

function paint() {
  if (ranker.done())
  {
     $("#left").hide();
     $("#right").hide();
    
     for( var i in ranker.ranking)
     {
      	var item = $("<li></li>").text(ranker.ranking[i].name);
        $("ol").append(item);
     }
     return;
  }
  $("#right").text(ranker.candidate.name);
  $('#right').prepend($('<br>'));
  $('#right').prepend($('<img>',{src:ranker.candidate.img}))
  $("#left").text(ranker.consideration().name + " " + (ranker.doppel()+1) + "/" + ranker.ranking.length);
  $('#left').prepend($('<br>'));
  $('#left').prepend($('<img>',{src:ranker.consideration().img}))
}
paint();

//$("#left").click(function(ev) { ranker.better(); });
//$("#right").click(function(ev) { ranker.worse(); });
$("#all").on("swipeleft", function(ev) { 
  $( "#left" ).css("background-color", "green" );
  setTimeout(function () {
    $( "#left" ).css("background-color", "white" );
    ranker.better(); 
  }, 300);
});
$("#all").on("swiperight", function(ev) { 
  $( "#right" ).css("background-color", "green" );
  setTimeout(function () {
    $( "#right" ).css("background-color", "white" );
    ranker.worse(); 
  }, 300);
});


//+ Jonas Raoni Soares Silva
//@ http://jsfromhell.com/array/shuffle [v1.0]
function shuffle(o){ //v1.0
    for(var j, x, i = o.length; i; j = Math.floor(Math.random() * i), x = o[--i], o[i] = o[j], o[j] = x);
    return o;
};


});

$(document).on("pageshow", "[data-role='page']", function () {
 $('div.ui-loader').remove();
});
