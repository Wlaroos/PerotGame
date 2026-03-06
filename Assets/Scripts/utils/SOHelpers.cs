using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class SOHelpers
{
    // Strip common asset prefixes used in your project
    public static string StripCommonPrefix(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        string[] prefixes = new[] { "E_", "R_", "M_", "C_" };
        foreach (var p in prefixes)
            if (name.StartsWith(p)) return name.Substring(p.Length);
        return name;
    }

    // Return base name (prefix removed + variant suffix removed)
    // Recognises '-', '_', ' ' and '(' as variant separators
    public static string GetBaseName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        string stripped = StripCommonPrefix(name);
        int idx = stripped.IndexOfAny(new[] { '-', '_', ' ', '(' });
        return (idx > 0) ? stripped.Substring(0, idx) : stripped;
    }

    public static string GetFullStrippedName(ScriptableObject so)
    {
        return so == null ? string.Empty : StripCommonPrefix(so.name);
    }

    // Unified symbol lookup (variant-aware + fallbacks)
    public static string GetSymbolForScriptableObject(ScriptableObject so)
    {
        if (so == null) return "?";

        string fullStripped = GetFullStrippedName(so); // e.g. "Silicate_4" or "Silicate-Clay"
        string baseName = GetBaseName(fullStripped);   // e.g. "Silicate"

        var variantMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Silicate_0", "SiO4" },
            { "Silicate_1", "SiO3" },
            { "Silicate_2", "Si3O9" },
            { "Silicate_3", "Si4O12" },
            { "Silicate_4", "Si6O18" }
        };

        if (variantMap.TryGetValue(fullStripped, out var variantSym))
            return variantSym;

        // Pattern fallback for silicates with numeric suffix
        if (!string.IsNullOrEmpty(baseName) && baseName.Equals("Silicate", StringComparison.OrdinalIgnoreCase))
        {
            int underscoreIdx = fullStripped.LastIndexOf('_');
            if (underscoreIdx >= 0 && underscoreIdx + 1 < fullStripped.Length)
            {
                string numPart = fullStripped.Substring(underscoreIdx + 1);
                if (int.TryParse(numPart, out int n))
                {
                    switch (n)
                    {
                        case 0: return "SiO4";
                        case 1: return "SiO3";
                        case 2: return "Si3O9";
                        case 3: return "Si4O12";
                        case 4: return "Si6O18";
                    }
                }
            }
            return "SiO4";
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"Hydrogen", "H"},
            {"Helium", "He"},
            {"Beryllium", "Be"},
            {"Carbon", "C"},
            {"Magnesium", "Mg"},
            {"Aluminum", "Al"},
            {"Silicon", "Si"},
            {"Phosphorus", "P"},
            {"Sulphur", "S"},
            {"Sulfur", "S"},
            {"Calcium", "Ca"},
            {"Titanium", "Ti"},
            {"Iron", "Fe"},
            {"Copper", "Cu"},
            {"Barium", "Ba"},
            {"Oxygen", "O"},
            {"Nitrogen", "N"},
            {"Chlorine", "Cl"},
            {"Sodium", "Na"},
            {"Potassium", "K"},
            {"Fluorine", "F"},
            {"Lithium", "Li"},
            {"Argon", "Ar"},
            {"Carbonate", "CO3"},
            {"Sulfate", "SO4"},
            {"Nitrate", "NO3"},
            {"Phosphate", "PO4"},
            {"Silicate", "SiO4"},
            {"Oxide", "O2-"},
            {"Heat", "Heat"},
            {"Slag", "Waste"},
        };

        if (!string.IsNullOrEmpty(baseName) && map.TryGetValue(baseName, out var sym)) return sym;

        if (string.IsNullOrEmpty(baseName)) return "?";
        if (baseName.Length == 1) return baseName.ToUpper();
        return char.ToUpper(baseName[0]) + baseName.Substring(1, Math.Min(1, baseName.Length - 1)).ToLower();
    }

    // Reflection helper: get field or property value (tries camel/pascal)
    public static object GetFieldOrPropertyValue(object obj, string name)
    {
        if (obj == null || string.IsNullOrEmpty(name)) return null;
        var type = obj.GetType();
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) return field.GetValue(obj);
        var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null) return prop.GetValue(obj, null);
        var pascal = char.ToUpperInvariant(name[0]) + name.Substring(1);
        field = type.GetField(pascal, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) return field.GetValue(obj);
        prop = type.GetProperty(pascal, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null) return prop.GetValue(obj, null);
        return null;
    }

    // Sprite/color/name extractors (use reflection to support multiple SO types)
    public static Sprite GetPrimarySpriteFromData(ScriptableObject so)
    {
        if (so == null) return null;
        var candidates = new[] { "mineralSprite", "elementSprite", "compoundSprite", "sprite", "icon" };
        foreach (var name in candidates)
        {
            var v = GetFieldOrPropertyValue(so, name);
            if (v is Sprite sp) return sp;
        }
        return null;
    }

    public static Sprite GetBigSpriteFromData(ScriptableObject so)
    {
        if (so == null) return null;
        var candidates = new[] { "mineralBigSprite", "bigSprite", "largeSprite" };
        foreach (var name in candidates)
        {
            var v = GetFieldOrPropertyValue(so, name);
            if (v is Sprite sp) return sp;
        }
        return null;
    }

    public static Color GetColorFromData(ScriptableObject so)
    {
        if (so == null) return Color.white;
        var v = GetFieldOrPropertyValue(so, "defaultColor");
        if (v is Color c) return c;
        if (v is Color32 c32) return (Color)c32;
        return Color.white;
    }

    public static string GetDisplayNameFromData(ScriptableObject so)
    {
        if (so == null) return string.Empty;
        var candidates = new[] { "mineralName", "elementName", "compoundName", "displayName", "title" };
        foreach (var name in candidates)
        {
            var v = GetFieldOrPropertyValue(so, name);
            if (v is string s && !string.IsNullOrEmpty(s)) return s;
        }
        return StripCommonPrefix(so.name);
    }

    public static string GetDescriptionFromData(ScriptableObject so)
    {
        if (so == null) return string.Empty;
        var candidates = new[] { "mineralDescription", "elementDescription", "compoundDescription", "description", "details" };
        foreach (var name in candidates)
        {
            var v = GetFieldOrPropertyValue(so, name);
            if (v is string s && !string.IsNullOrEmpty(s)) return s;
        }
        return string.Empty;
    }

    public static string GetFunFactFromData(ScriptableObject so)
    {
        if (so == null) return string.Empty;
        var candidates = new[] { "mineralFunFact", "elementFunFact", "compoundFunFact", "funFact" };
        foreach (var name in candidates)
        {
            var v = GetFieldOrPropertyValue(so, name);
            if (v is string s && !string.IsNullOrEmpty(s)) return s;
        }
        return string.Empty;
    }

    // TMP subscript helpers
    public static string MakeSubscriptTMP(string number) => $"<sub>{number}</sub>";

    public static string ToSubscript(int value)
    {
        if (value <= 0) return "";
        var s = value.ToString();
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            switch (ch)
            {
                case '0': sb.Append('\u2080'); break;
                case '1': sb.Append('\u2081'); break;
                case '2': sb.Append('\u2082'); break;
                case '3': sb.Append('\u2083'); break;
                case '4': sb.Append('\u2084'); break;
                case '5': sb.Append('\u2085'); break;
                case '6': sb.Append('\u2086'); break;
                case '7': sb.Append('\u2087'); break;
                case '8': sb.Append('\u2088'); break;
                case '9': sb.Append('\u2089'); break;
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }

    // NEW: Format a formula string so any digits inside become subscripts.
    // If useUnicodeSubscripts is true, digits are converted to Unicode subscript glyphs.
    // Otherwise contiguous digit runs are wrapped in TMP <sub>...</sub> tags.
    public static string FormatFormulaForDisplay(string formula, bool useUnicodeSubscripts)
    {
        if (string.IsNullOrEmpty(formula)) return formula;

        if (useUnicodeSubscripts)
        {
            var map = new Dictionary<char, char>
            {
                {'0', '\u2080'}, {'1', '\u2081'}, {'2', '\u2082'}, {'3', '\u2083'}, {'4', '\u2084'},
                {'5', '\u2085'}, {'6', '\u2086'}, {'7', '\u2087'}, {'8', '\u2088'}, {'9', '\u2089'}
            };
            var sb = new StringBuilder(formula.Length);
            foreach (var ch in formula)
            {
                if (map.TryGetValue(ch, out var sub)) sb.Append(sub);
                else sb.Append(ch);
            }
            return sb.ToString();
        }
        else
        {
            var sb = new StringBuilder(formula.Length + 8);
            int i = 0;
            while (i < formula.Length)
            {
                if (char.IsDigit(formula[i]))
                {
                    int j = i;
                    while (j < formula.Length && char.IsDigit(formula[j])) j++;
                    sb.Append("<sub>");
                    sb.Append(formula.Substring(i, j - i));
                    sb.Append("</sub>");
                    i = j;
                }
                else
                {
                    sb.Append(formula[i]);
                    i++;
                }
            }
            return sb.ToString();
        }
    }

    // Utility: find child component by name (case-insensitive substring)
    public static T FindChildComponentByName<T>(GameObject root, string childName) where T : Component
    {
        if (root == null) return null;
        var comps = root.GetComponentsInChildren<T>(true);
        foreach (var c in comps)
        {
            if (c == null || c.gameObject == null) continue;
            if (c.gameObject.name.IndexOf(childName, StringComparison.OrdinalIgnoreCase) >= 0) return c;
        }
        return null;
    }

    // Prefer active canvases (used by popup manager)
    public static Canvas GetAnyCanvas(Canvas overrideCanvas = null)
    {
        if (overrideCanvas != null) return overrideCanvas;
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>().Where(c => c != null && c.isActiveAndEnabled).ToArray();
        if (canvases != null && canvases.Length > 0)
        {
            var preferred = canvases.FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == Camera.main);
            if (preferred != null) return preferred;
            preferred = canvases.FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceCamera);
            if (preferred != null) return preferred;
            return canvases[0];
        }
        return null;
    }
}