$(document).ready(function(){

var sourcelist = shuffle(["bud", "coors", "miller", "rainer", "pabst", "jenny","stone"]);

var ranker = {
    ranking: [sourcelist.pop()],
    min : 0,
    max : 1,
    candidate: sourcelist.pop(),
    done : false,
  	
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
      	this.candidate = sourcelist.pop();
        this.min = 0;
        this.max = this.ranking.length;
      }
      paint();
    },
    consideration : function() { return this.ranking[this.doppel()]},
    done : function() { return this.candidate === undefined; }
};

function paint() {
  if (ranker.done())
  {
     for( var i in ranker.ranking)
     {
      	var item = $("<li></li>").text(ranker.ranking[i]);
	$("ol").append(item);
     }
     return;
  }
  $("#right").text(ranker.candidate);
  $("#left").text(ranker.consideration());

}
paint();

//$("#left").click(function(ev) { ranker.better(); });
//$("#right").click(function(ev) { ranker.worse(); });
$("#all").on("swipeleft", function(ev) { ranker.better(); });
$("#all").on("swiperight", function(ev) { ranker.worse(); });

//mcl.on("tap press", function(ev) { ranker.better(); });
//mcr.on("tap press", function(ev) { ranker.worse(); });


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
