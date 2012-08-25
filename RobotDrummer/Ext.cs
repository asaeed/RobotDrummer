using System;
using Microsoft.SPOT;
using System.Collections;

// Required for NETMF to recognized extension methods
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class ExtensionAttribute : Attribute { }
}

namespace RobotDrummer
{
    public static class Ext
    {
        public static ArrayList SortInts(this ArrayList arrayList)
        {
            ArrayList sortedList = new ArrayList();

            while (arrayList.Count > 0)
            {
                int minInt = arrayList.GetMinInt();
                sortedList.Add(minInt);
                arrayList.Remove(minInt);
            }

            return sortedList;
        }

        public static int GetMinInt(this ArrayList arrayList)
        {
            int min = (int)arrayList[0];
            foreach (int val in arrayList)
                if (val < min) min = val;

            return min;
        }

        public static int GetMedianInt(this ArrayList arrayList)
        {
            // even # of elements, take avg of middle 2
            // odd # of elements, take middle
            int median;
            if (arrayList.Count % 2 == 0)
                median = ((int)arrayList[arrayList.Count / 2] + (int)arrayList[arrayList.Count / 2 - 1]) / 2;
            else
                median = (int)arrayList[(int)System.Math.Floor(arrayList.Count / 2)];

            return median;
        }

        public static ArrayList RemoveOutlierInts(this ArrayList arrayList)
        {
            int median = arrayList.GetMedianInt();
            int upperLimit = median + (int)(median * .2);
            int lowerLimit = median - (int)(median * .2);

            for (int i = 0; i < arrayList.Count; i++)
            {
                int val = (int)arrayList[i];
                if ((val > upperLimit) || (val < lowerLimit))
                    arrayList.RemoveAt(i);
            }

            return arrayList;
        }

        public static int GetAvgInt(this ArrayList arrayList)
        {
            int sum = 0;
            foreach (int val in arrayList)
                sum += val;
            return sum / arrayList.Count;
        }
    }
}
