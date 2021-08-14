namespace Foreman
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Windows;
    using System.Windows.Media;
    using NLua;

    public static class LuaExtensions
    {
        public static LuaTable CreateTable(this Lua lua)
        {
            return (LuaTable)lua.DoString("return {}")[0];
        }

        public static float Float(this LuaTable table, string key)
        {
            if (table[key] == null)
                throw new MissingPrototypeValueException(table, key, "Key is missing");

            try {
                return Convert.ToSingle(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    $"Expected a float, but the value ('{table[key]}') isn't one");
            }
        }

        public static float FloatOrDefault(this LuaTable table, string key, float defaultValue = 0f)
        {
            if (table[key] == null)
                return defaultValue;

            try {
                return Convert.ToSingle(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    $"Expected a float, but the value ('{table[key]}') isn't one");
            }
        }

        public static double Double(this LuaTable table, string key)
        {
            if (table[key] == null)
                throw new MissingPrototypeValueException(table, key, "Key is missing");

            try {
                return Convert.ToSingle(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    $"Expected a float, but the value ('{table[key]}') isn't one");
            }
        }

        public static double DoubleOrDefault(this LuaTable table, string key, double defaultValue = 0.0)
        {
            if (table[key] == null)
                return defaultValue;

            try {
                return Convert.ToDouble(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    $"Expected a double, but the value ('{table[key]}') isn't one");
            }
        }

        public static int IntOrDefault(this LuaTable table, string key, int defaultValue = 0)
        {
            if (table[key] == null)
                return defaultValue;

            try {
                return Convert.ToInt32(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    $"Expected an Int32, but the value ('{table[key]}') isn't one");
            }
        }

        public static int? Int(this LuaTable table, string key)
        {
            if (table[key] == null)
                return null;

            try {
                return Convert.ToInt32(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    $"Expected an Int32, but the value ('{table[key]}') isn't one");
            }
        }

        public static string String(this LuaTable table, string key)
        {
            if (table[key] != null)
                return Convert.ToString(table[key])!;

            throw new MissingPrototypeValueException(table, key, "Key is missing");
        }

        [return: NotNullIfNotNull("defaultValue")]
        public static string? StringOrDefault(
            this LuaTable table, string key, string? defaultValue = null)
        {
            if (table[key] != null)
                return Convert.ToString(table[key]);
            return defaultValue;
        }

        public static bool BooleanOrDefault(this LuaTable table, string key, bool defaultValue = false)
        {
            if (table[key] == null)
                return defaultValue;

            try {
                return Convert.ToBoolean(table[key]);
            } catch (FormatException) {
                throw new MissingPrototypeValueException(table, key,
                    $"Expected a float, but the value ('{table[key]}') isn't one");
            }
        }

        public static LuaTable Table(this LuaTable table, string key)
        {
            if (table[key] != null)
                return (LuaTable)table[key];

            throw new MissingPrototypeValueException(table, key, "Key is missing");
        }

        public static LuaTable? TableOrDefault(
            this LuaTable table, string key, LuaTable? defaultValue = null)
        {
            if (table[key] != null)
                return table[key] as LuaTable;
            return defaultValue;
        }

        public static IEnumerable<KeyValuePair<object, object>> AsEnumerable(this LuaTable table)
        {
            foreach (KeyValuePair<object, object> entry in table)
                yield return entry;
        }


        [return: NotNullIfNotNull("defaultValue")]
        public static Color? ColorOrDefault(
            this LuaTable table, string key, Color? defaultValue = null)
        {
            if (table[key] is not LuaTable t)
                return defaultValue;

            double r = t.DoubleOrDefault("r");
            double g = t.DoubleOrDefault("g");
            double b = t.DoubleOrDefault("b");
            double a = t.DoubleOrDefault("a");
            return Color.FromArgb(
                (byte)(a * 255), (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        [return: NotNullIfNotNull("defaultValue")]
        public static Vector? VectorOrDefault(
            this LuaTable table, string key, Vector? defaultValue = null)
        {
            if (table[key] is not LuaTable t)
                return defaultValue;

            double x = (double)t[1];
            double y = (double)t[2];
            return new Vector(x, y);
        }

        public static string DumpToString(this LuaTable table, StringBuilder? buffer = null, int indent = 0)
        {
            var sb = buffer ?? new StringBuilder();

            var ind = new string(' ', indent * 2);
            foreach (KeyValuePair<object, object> entry in table) {
                sb.AppendFormat("{0}[{1}] = ", ind, entry.Key);
                if (entry.Value is LuaTable t) {
                    sb.AppendFormat("{{\n");
                    DumpToString(t, sb, indent + 1);
                    sb.AppendFormat("{0}}}\n", ind);
                } else {
                    sb.AppendFormat("{0}\n", entry.Value);
                }
            }

            return buffer == null ? sb.ToString() : "";
        }
    }
}
