$(document).ready(function(){

var url = URI(document.URL);
var query = url.query(true); 
var pick = query["list"] || "no list specified";
var ppick = new String(pick).replace("_", " ");
$("#listname").text(ppick);

var data = JSON.parse(document.getElementById('listdata').innerHTML);

var max = query["max"] || Math.min(32, data.length);
var imgsourcelist = shuffle(data).slice(0, max);

var ranker = {
    ranking: [imgsourcelist.pop()],
    min : 0,
    max : 1,
    candidate: imgsourcelist.pop(),
    cap :  query["cap"] || 8,
  	
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
        $('#rightimg').hide();
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
        $('#rightimg').hide();
        this.min = 0;
        this.max = (this.cap*2-1);
      }
      $('#leftimg').hide();

      paint();
    },
    consideration : function() { return this.ranking[this.doppel()]},
    done : function() { return this.candidate === undefined; }
};

function paint() {
  if (ranker.done())
  {
     finish();
     return;
  }
  $("#righttxt").text(ranker.candidate.name);
  var ri = $('#rightimg');
  ri.attr("src", ranker.candidate.cachedImg || ranker.candidate.img);
  ri.show();
  $("#lefttxt").text(ranker.consideration().name + " " + (ranker.doppel()+1) + "/" + ranker.ranking.length);
  var li = $('#leftimg');
  li.attr("src", ranker.consideration().cachedImg || ranker.consideration().img);
  li.show(); 
}

function finish()
{
    var saveurl = "ranking/" + pick;
    $.post(saveurl, JSON.stringify(ranker.ranking), function (data) {
        window.location = "http://" + url.host() + "/" + data;
    }).fail(function (err) {
        alert("coudn't save " + JSON.stringify(err));
    });
}

paint();

document.onkeydown = function (event)
{
    if ( event.which ==  37 ) //left
    {
        leftwins();
    }
    else if ( event.which ==  39 ) //right
    {
        rightwins();
    }
}

var leftwins = function (ev) {
    $("#left").css("background-color", "green");
    setTimeout(function () {
        $("#left").css("background-color", "white");
        ranker.better();
    }, 300);
};

var rightwins = function(ev) { 
    $( "#right" ).css("background-color", "green" );
    setTimeout(function () {
        $( "#right" ).css("background-color", "white" );
        ranker.worse(); 
    }, 300);
};

$("#all").on("swipeleft", leftwins);

$("#all").on("swiperight", rightwins);

$("#done").click(function () {
    finish();
});

//precache the images now that we've loaded first two.
for (index = 2; index < imgsourcelist.length; ++index) {
    var im = new Image();
    im.src = imgsourcelist[index].cachedImg;
}

});

//+ Jonas Raoni Soares Silva
//@ http://jsfromhell.com/array/shuffle [v1.0]
function shuffle(o){ //v1.0
    for(var j, x, i = o.length; i; j = Math.floor(Math.random() * i), x = o[--i], o[i] = o[j], o[j] = x);
    return o;
};


$(document).on("pageshow", "[data-role='page']", function () {
 $('div.ui-loader').remove();
});
