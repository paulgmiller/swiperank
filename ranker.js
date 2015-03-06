$(document).ready(function(){

var sourcelist = shuffle(["bud", "coors", "miller", "rainer", "pabst", "jenny","stone"]);

var imgsourcelist = [
  {
    name: "budweiser",
    img: "http://www.budweiser.com/jcr:content/openGraphImage.img.jpg/Facebook-Budweiser-01.jpg",
  },
  {
    name: "coors",
    img: "http://www.soupleys.com/files_uploaded/COORS6PKBOTnXJvlRHcpO3_yOZ.jpg",
  },
  {
    name: "miller",
    img: "http://upload.wikimedia.org/wikipedia/en/thumb/4/45/Miller_Brewery_Logo.svg/1280px-Miller_Brewery_Logo.svg.png",
  },
  {
    name: "rainer",
    img: "http://brewbound-images.s3.amazonaws.com/wp-content/uploads/2013/10/rainer-beer.jpg",
  },
  {
    name: "pabst",
    img: "http://upload.wikimedia.org/wikipedia/en/thumb/c/cd/Pabst_Blue_Ribbon_logo.svg/911px-Pabst_Blue_Ribbon_logo.svg.png",
  },
  {
    name: "Genese",
    img: "http://www.artzberger.com/BeerCans/newcans/canpics/GeneseeBeer0511.jpg",
  },
  {
    name: "Yuengling",
    img: "https://aleheads.files.wordpress.com/2012/01/yuengling.jpg",
  },
];

var ranker = {
    ranking: [imgsourcelist.pop()],
    min : 0,
    max : 1,
    candidate: imgsourcelist.pop(),
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
      	this.candidate = imgsourcelist.pop();
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
      	var item = $("<li></li>").text(ranker.ranking[i].name);
        $("ol").append(item);
     }
     return;
  }
  $("#right").text(ranker.candidate.name);
  $('#right').prepend($('<img>',{src:ranker.candidate.img}))
  $("#left").text(ranker.consideration().name);
  $('#left').prepend($('<img>',{src:ranker.consideration().img}))
}
paint();

//$("#left").click(function(ev) { ranker.better(); });
//$("#right").click(function(ev) { ranker.worse(); });
$("#all").on("swipeleft", function(ev) { ranker.better(); });
$("#all").on("swiperight", function(ev) { ranker.worse(); });



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
