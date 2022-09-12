using System;
using System.Collections.Generic;
using System.Text;

namespace MazeGenerator
{
    public class QuickFind<T>
    {
        private readonly int[] setIDs;
        private readonly Dictionary<T, int> map;

        public int SetCount { get; private set; }

        public QuickFind(IEnumerable<T> items)
        {
            int itemCount = 0;
            foreach(var item in items)
            {
                itemCount++;
            }

            SetCount = itemCount;
            setIDs = new int[itemCount];
            map = new Dictionary<T, int>();

            int setID = 0;
            int itemID = 0;
            foreach(var item in items)
            {
                map.Add(item, itemID);
                setIDs[itemID] = setID;
                
                setID++;
                itemID++;
            }
        }

        public int Find(T p) => setIDs[map[p]];
        public bool Union(T p, T q)
        {
            if (AreConnected(p, q)) return false;

            int pSetID = Find(p);
            int qSetID = Find(q);

            for(int itemID = 0; itemID < setIDs.Length; itemID++)
            {
                if(setIDs[itemID] == qSetID)
                {
                    setIDs[itemID] = pSetID;
                }
            }

            SetCount--;
            return true;
        }
        public bool AreConnected(T p, T q)
        {
            return Find(p) == Find(q);
        }
    }
}
