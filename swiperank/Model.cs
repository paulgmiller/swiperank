namespace swiperank
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Ranking : List<Entry>
    {
        public string ListName;
        public string Hash;
        public string PrettyListName { get { return ListName.Replace("_", " "); } }
    }

    public class Entry
    {
        public string name;
        public string img;
        public string cachedImg;
    }

    public class EntryWithScore : Entry
    {
        public int score;
    }

    public class ImageResponse
    {
        public ImageSchema d;
    }

    public class ImageSchema
    {
        public List<ImageResult> results;
    }

    public class ImageResult
    {
        public string Title;
        public string mediaurl;
    }

    public class NewList
    {
        public string name;
        public string searchquery;
        public string content;
        public IEnumerable<string> Lines { get { return content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries); } }
        public string safesearch;
    }


    public class AggregateRanking 
    {
        public string ListName;
        public string PrettyListName { get { return ListName.Replace("_", " "); } }
        public int TotalRankings;

        private Dictionary<string, EntryWithScore> ranked = new Dictionary<string, EntryWithScore>();

        public IEnumerable<EntryWithScore> OrderedRanking
        {
            get
            {
                return ranked.Values.OrderByDescending(e => e.score);
            }
        }
        public void Add(IReadOnlyCollection<Entry> ranking)
        {
            ++TotalRankings;
            var score = ranking.Count;
            foreach (var rank in ranking)
            {
                if (rank == null)
                {
                    continue;
                }
                EntryWithScore existing; 
                if (ranked.TryGetValue(rank.img, out existing))
                {

                    existing.score += score;
                }
                else
                {
                    ranked.Add(rank.img, new EntryWithScore {
                        name = rank.name,
                        img = rank.img,
                        cachedImg = rank.cachedImg ?? ListModule.CachedImg(rank.img).Uri.ToString(),
                        score = score,
                    });
                }
                --score;
            }
        }


       
        
    }

    public class EntryComparer: IEqualityComparer<Entry>
    {
       public bool Equals(Entry lhs, Entry rhs) { return lhs.img.Equals(rhs.img); }
       public int GetHashCode(Entry e) { return e.img.GetHashCode(); }
    }
}
