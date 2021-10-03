
using UnityEngine;
using System;
using System.Collections.Generic;

public class DebugUtility
{
    private static Dictionary<string, string> d = new Dictionary<string, string>();
    public static void Watch(string title, string v)
    {
        if (!d.ContainsKey(title)) d[title] = v;
        if (v != d[title]) Debug.Log("CHANGE " + title + ": " + d[title] + " --> " + v);
        d[title] = v;
    }
}
