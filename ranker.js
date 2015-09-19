$(document).ready(function(){

var possible = [
  //""miss_america_2001.json
  //""miss_america_2002.json
  //"miss_america_2003.json
  //"miss_america_2004.json
  //"miss_america_2005.json",
  //"miss_america_2006.json",
  //"miss_america_2007.json",
  //"miss_america_2008.json",
  //"miss_america_2009.json",
  "miss_america_2010.json",
  "miss_america_2011.json",
  "miss_america_2012.json",
  "miss_america_2013.json",
  "miss_america_2014.json",
  "miss_america_2015.json",
  "miss_america_2016.json",
]
var pick = shuffle(possible).pop();
document.title = pick;

$.getJSON( pick).fail(function(err) {
    alert( "couldn't load " + pick );
 }
).done(function( data ) {

var imgsourcelist = shuffle(data);

var ranker = {
    ranking: [imgsourcelist.pop()],
    min : 0,
    max : 1,
    candidate: imgsourcelist.pop(),
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



})});

//+ Jonas Raoni Soares Silva
//@ http://jsfromhell.com/array/shuffle [v1.0]
function shuffle(o){ //v1.0
    for(var j, x, i = o.length; i; j = Math.floor(Math.random() * i), x = o[--i], o[i] = o[j], o[j] = x);
    return o;
};

$(document).on("pageshow", "[data-role='page']", function () {
 $('div.ui-loader').remove();
});
