namespace swiperank
{
    using System.Collections.Generic;

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
}
