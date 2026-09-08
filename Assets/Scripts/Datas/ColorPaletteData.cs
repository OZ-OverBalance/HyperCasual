using System.Collections.Generic;
using UnityEngine;

public static class ColorPaletteData
{
    public readonly struct Data
    {
        public readonly int Index;
        public readonly string Name;
        public readonly Color Color;

        public Data(int index, string name, Color color)
        {
            Index = index;
            Name = name;
            Color = color;
        }
    }

    // 하이어라키/인스펙터 등록 순서 (0: Black ~ 9: Yellow)
    private static readonly Data[] Palettes = new Data[]
    {
        new Data(0, "Black",  new Color(0.12f, 0.12f, 0.12f)),
        new Data(1, "Blue",   new Color(0.12f, 0.40f, 0.95f)),
        new Data(2, "Gray",   new Color(0.55f, 0.55f, 0.55f)),
        new Data(3, "Green",  new Color(0.20f, 0.70f, 0.25f)),
        new Data(4, "Orange", new Color(0.95f, 0.45f, 0.10f)),
        new Data(5, "Pink",   new Color(0.95f, 0.35f, 0.65f)),
        new Data(6, "Purple", new Color(0.55f, 0.15f, 0.70f)),
        new Data(7, "Red",    new Color(0.90f, 0.15f, 0.15f)),
        new Data(8, "Sky",    new Color(0.25f, 0.75f, 0.95f)),
        new Data(9, "Yellow", new Color(0.98f, 0.80f, 0.10f))
    };

    private static readonly Dictionary<int, Data> PaletteDict;

    static ColorPaletteData()
    {
        PaletteDict = new Dictionary<int, Data>(Palettes.Length);
        for (int i = 0; i < Palettes.Length; i++)
        {
            PaletteDict[Palettes[i].Index] = Palettes[i];
        }
    }

    public static int Count => Palettes.Length;

    public static bool TryGet(int index, out Data data)
    {
        return PaletteDict.TryGetValue(index, out data);
    }

    public static Data Get(int index)
    {
        if (PaletteDict.TryGetValue(index, out Data data))
        {
            return data;
        }
        return Palettes[0];
    }
}