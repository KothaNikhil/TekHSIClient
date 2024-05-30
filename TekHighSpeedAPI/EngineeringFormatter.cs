using System;
using System.Linq;

namespace TekHighspeedAPI
{
    /// <summary>
    ///     Helps in parsing and formatting Engineering format numbers.
    /// </summary>
    /// <example>
    ///     <code>
    /// // Use as a formatter
    /// Console.WriteLine(string.Format(new EngineeringNotationFormatter(), "{0:eng}", 2.345e-13));
    /// 
    /// // Simple parse example
    /// double r = EngineeringNotationFormatter.Parse("1.73G");
    /// 
    /// // Direct formatting
    /// string rf = EngineeringNotationFormatter.Format(r);
    /// </code>
    /// </example>
    public class EngineeringNotationFormatter : ICustomFormatter, IFormatProvider
    {
        #region Custom Formatting

        #region ICustomFormatter Members

        /// <summary>
        ///     Used by IFormatProvider
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        public string Format(string format, object arg, IFormatProvider provider)
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
            // Format string
            const string token = "eng";

            if (arg == null || format == null) return "";

            // Check for pass though requests
            if (!format.Trim().StartsWith(token) || !(arg is double))
                return arg is IFormattable ? ((IFormattable)arg).ToString(format, provider) : arg.ToString();

            // Do custom formatting.
            if (format.Length > token.Length)
            {
                // IsSuppressed colons 
                var s = format.Substring(token.Length);
                while (s.Length > 0 && s[0] == ':')
                    s = s.Substring(1);

                // Request formatting w/user specialization.
                return Format((double)arg, s);
            }

            // Request formatting.
            return Format((double)arg);
        }

        #endregion

        #region IFormatProvider Members

        /// <summary>
        ///     Used by ICustomFormatter
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        public object GetFormat(Type type) => type == typeof(ICustomFormatter) ? this : null;
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).

        #endregion

        #endregion

        #region Parse/Format

        /// <summary>
        ///     This function parses binary and hex numbers such as
        ///     0b100101 or 0x43ac
        /// </summary>
        /// <param name="s">The string to parse</param>
        /// <param name="bitcount">The number of bits in the number</param>
        /// <returns>The UInt64 result</returns>
        public static ulong ParseUInt64(string s, out int bitcount)
        {
            var retval = 0UL;
            bitcount = 0;
            if (s.Length == 0) return retval;

            // Handle binary
            if (s[0] == '0' && (s[1] == 'b' || s[1] == 'B'))
            {
                for (var i = 2; i < s.Length; i++)
                {
                    retval *= 2;
                    switch (s[i])
                    {
                        case '0':
                            bitcount++;
                            break;
                        case '1':
                            retval += 1UL;
                            bitcount++;
                            break;
                        default:
                            return retval;
                    }
                }

                return retval;
            }

            // Handle Hex
            if (s[0] == '0' && (s[1] == 'x' || s[1] == 'X'))
            {
                for (var i = 2; i < s.Length; i++)
                {
                    retval *= 16;
                    switch (s[i])
                    {
                        case '0':
                            bitcount += 4;
                            break;
                        case '1':
                            retval += 1;
                            bitcount += 4;
                            break;
                        case '2':
                            retval += 2;
                            bitcount += 4;
                            break;
                        case '3':
                            retval += 3;
                            bitcount += 4;
                            break;
                        case '4':
                            retval += 4;
                            bitcount += 4;
                            break;
                        case '5':
                            retval += 5;
                            bitcount += 4;
                            break;
                        case '6':
                            retval += 6;
                            bitcount += 4;
                            break;
                        case '7':
                            retval += 7;
                            bitcount += 4;
                            break;
                        case '8':
                            retval += 8;
                            bitcount += 4;
                            break;
                        case '9':
                            retval += 9;
                            bitcount += 4;
                            break;
                        case 'a':
                        case 'A':
                            retval += 10;
                            bitcount += 4;
                            break;
                        case 'b':
                        case 'B':
                            retval += 11;
                            bitcount += 4;
                            break;
                        case 'c':
                        case 'C':
                            retval += 12;
                            bitcount += 4;
                            break;
                        case 'd':
                        case 'D':
                            retval += 13;
                            bitcount += 4;
                            break;
                        case 'e':
                        case 'E':
                            retval += 14;
                            bitcount += 4;
                            break;
                        case 'f':
                        case 'F':
                            retval += 15;
                            bitcount += 4;
                            break;
                        default:
                            return retval;
                    }
                }

                return retval;
            }

            return ulong.Parse(s);
        }

        /// <summary>
        /// </summary>
        /// <param name="s"></param>
        /// <param name="retval"></param>
        /// <param name="units"></param>
        /// <returns></returns>
        public static bool TryParse(string s, out double retval, out string units)
        {
            char[] suffixes = { 'f', 'p', 'n', 'u', 'µ', 'μ', 'k', 'K', 'm', 'M', 'G', '%' };
            units = "";
            retval = double.NaN;

            if (s.Length == 0) return false;
            if (s.Length == 1) return double.TryParse(s, out retval);

            retval = 0.0;

            // Handle binary
            if (s.Length >= 3 && s[0] == '0' && (s[1] == 'b' || s[1] == 'B') && s[2] != 'l' && s[2] != 'L')
            {
                retval = 0.0;

                for (var i = 2; i < s.Length; i++)
                {
                    retval *= 2.0;
                    switch (s[i])
                    {
                        case '0':
                            break;
                        case '1':
                            retval += 1.0;
                            break;
                        case 'M':
                        case 'm':
                            break;
                        default:
                            return true;
                    }
                }

                return true;
            }

            if (s.Length >= 3 && s[0] == '0' && (s[1] == 'b' || s[1] == 'B') && (s[2] == 'l' || s[2] == 'l'))
            {
                retval = 0.0;

                for (var i = s.Length - 1; i >= 3; i--)
                {
                    retval *= 2.0;
                    switch (s[i])
                    {
                        case '0':
                            break;
                        case '1':
                            retval += 1.0;
                            break;
                        default:
                            return true;
                    }
                }

                return true;
            }

            // Handle Hex
            if (s[0] == '0' && (s[1] == 'x' || s[1] == 'X'))
            {
                retval = 0.0;

                for (var i = 2; i < s.Length; i++)
                {
                    retval *= 16.0;
                    switch (s[i])
                    {
                        case '0':
                            break;
                        case '1':
                            retval += 1.0;
                            break;
                        case '2':
                            retval += 2.0;
                            break;
                        case '3':
                            retval += 3.0;
                            break;
                        case '4':
                            retval += 4.0;
                            break;
                        case '5':
                            retval += 5.0;
                            break;
                        case '6':
                            retval += 6.0;
                            break;
                        case '7':
                            retval += 7.0;
                            break;
                        case '8':
                            retval += 8.0;
                            break;
                        case '9':
                            retval += 9.0;
                            break;
                        case 'a':
                        case 'A':
                            retval += 10.0;
                            break;
                        case 'b':
                        case 'B':
                            retval += 11.0;
                            break;
                        case 'c':
                        case 'C':
                            retval += 12.0;
                            break;
                        case 'd':
                        case 'D':
                            retval += 13.0;
                            break;
                        case 'e':
                        case 'E':
                            retval += 14.0;
                            break;
                        case 'f':
                        case 'F':
                            retval += 15.0;
                            break;
                        default:
                            return true;
                    }
                }

                return true;
            }

            var substring = "";
            var index = 0;

            // Parse Engineering Format
            while (index < s.Length && char.IsWhiteSpace(s[index]))
                index++;

            if (index < s.Length && (s[index] == '+' || s[index] == '-')) substring += s[index++];

            while (index < s.Length && char.IsDigit(s[index]))
                substring += s[index++];

            if (index < s.Length && s[index] == '.')
            {
                substring += s[index++];
                while (index < s.Length && char.IsDigit(s[index]))
                    substring += s[index++];
            }

            if (index < s.Length && (s[index] == '+' || s[index] == '-')) substring += s[index++];

            if (index < s.Length && (s[index] == 'e' || s[index] == 'E')) substring += s[index++];

            while (index < s.Length && char.IsDigit(s[index]))
                substring += s[index++];

            while (index < s.Length && char.IsWhiteSpace(s[index]))
                index++;

            var last = '\0';
            if (index < s.Length && suffixes.Any(c => c == s[index])) last = s[index++];

            while (index < s.Length && char.IsWhiteSpace(s[index]))
                index++;

            while (index < s.Length && !char.IsWhiteSpace(s[index]))
                units += s[index++];


            bool status;
            switch (last)
            {
                case 'f':
                    status = double.TryParse(substring, out retval);
                    retval *= 1.0e-15;
                    return status;
                case 'p':
                    status = double.TryParse(substring, out retval);
                    retval *= 1.0e-12;
                    return status;
                case 'n':
                    status = double.TryParse(substring, out retval);
                    retval *= 1.0e-9;
                    return status;
                case 'u':
                case 'µ':
                case 'μ':
                    status = double.TryParse(substring, out retval);
                    retval *= 1.0e-6;
                    return status;
                case 'm':
                    status = double.TryParse(substring, out retval);
                    retval *= 1.0e-3;
                    return status;
                case 'k':
                case 'K':
                    status = double.TryParse(substring, out retval);
                    retval *= 1.0e+3;
                    return status;
                case 'M':
                    status = double.TryParse(substring, out retval);
                    retval *= 1.0e+6;
                    return status;
                case 'G':
                    status = double.TryParse(substring, out retval);
                    retval *= 1.0e+9;
                    return status;
                case '%':
                    status = double.TryParse(substring, out retval);
                    retval /= 100.0;
                    return status;
            }

            // DPOJET returns 9.91e-37 when it doesn't produce a result.
            // This attempts to convert that into NaN.
            if (retval > 9.0e37)
            {
                retval = double.NaN;
                return true;
            }


            if (last == '%')
            {
                status = double.TryParse(s, out retval);
                retval /= 100.0;
                return status;
            }

            return double.TryParse(s, out retval);
        }

        /// <summary>
        ///     Attempts to parse the specified input.
        /// </summary>
        /// <example>
        ///     <code>
        /// double d = 0.0;
        /// string s = "2.34p";
        /// if (EngineeringNotationFormatter.TryParse(s, out d))
        /// {
        ///     Console.WriteLine("{0}", d);
        /// }
        /// else
        /// {
        ///     Console.WriteLine("parse failed");
        /// }
        /// </code>
        /// </example>
        /// <param name="s">The string to parse.</param>
        /// <param name="retval">The value returned on a successful parse.</param>
        /// <returns>Returns true if the parse succeeded, if parsing fails then false is returned</returns>
        public static bool TryParse(string s, out double retval)
        {
            string units;
            return TryParse(s, out retval, out units);
        }

        /// <summary>
        ///     Converts a string in engineering notation, into a double.
        /// </summary>
        /// <example>
        ///     <code>
        /// double v = EngineeringNotationFormatter.Parse("2.3n");
        /// </code>
        /// </example>
        /// <param name="s">string to convert</param>
        /// <returns>converted value</returns>
        public static double Parse(string s)
        {
            double retval;
            return TryParse(s, out retval) ? retval : 0.0;
        }

        /// <summary>
        ///     Formats a double as a string in engineering notation.
        /// </summary>
        /// <example>
        ///     <code>
        /// string s = EngineeringNotationFormatter.Format(2.3e-9);
        /// </code>
        /// </example>
        /// <param name="v">double value</param>
        /// <returns>resultant string</returns>
        public static string Format(double v)
        {
            return Format(v, null);
        }

        /// <summary>
        ///     Formats a double as a string in engineering notation.
        /// </summary>
        /// <example>
        ///     <code>
        /// Console.WriteLine("{0}", EngineeringNotationFormatter.Format(2.34567e-12, "#.###"));
        /// </code>
        /// </example>
        /// <param name="v">Double value to format</param>
        /// <param name="format">Formatting string like the one used by ToString</param>
        /// <returns>Formatted string</returns>
        public static string Format(double v, string format)
        {
            const double fudge = 0.1;

            if (double.IsNaN(v) || v.ToString("0.000") == "NaN")
                return "N/A";

            if (double.IsNegativeInfinity(v))
                return "-inf";

            if (double.IsPositiveInfinity(v) || double.IsInfinity(v))
                return "+inf";

            if (v == Math.Floor(v) && Math.Abs(v) < 1000)
            {
                var l = (long)v;
                return l.ToString();
            }

            var isNeg = v < 0.0;
            if (isNeg) v = -v;
            var log10 = Math.Log10(v);
            var suffix = "";

            if (log10 < 24.0 && log10 + fudge >= 21.0)
            {
                suffix = "Z";
                v = v * 1.0e-21;
            }
            else if (log10 < 21.0 && log10 + fudge >= 18.0)
            {
                suffix = "E";
                v = v * 1.0e-18;
            }
            else if (log10 < 18.0 && log10 + fudge >= 15.0)
            {
                suffix = "P";
                v = v * 1.0e-15;
            }
            else if (log10 < 15.0 && log10 + fudge >= 12.0)
            {
                suffix = "T";
                v = v * 1.0e-12;
            }
            else if (log10 < 12.0 && log10 + fudge >= 9.0)
            {
                suffix = "G";
                v = v * 1.0e-9;
            }
            else if (log10 < 9.0 && log10 + fudge >= 6.0)
            {
                suffix = "M";
                v = v * 1.0e-6;
            }
            else if (log10 < 6.0 && log10 + fudge >= 3.0)
            {
                suffix = "k";
                v = v * 1.0e-3;
            }
            else if (log10 < 0.0 && log10 + fudge >= -3.0)
            {
                suffix = "m";
                v = v * 1.0e3;
            }
            else if (log10 < -3.0 && log10 + fudge >= -6.0)
            {
                suffix = "u";
                v = v * 1.0e6;
            }
            else if (log10 < -6.0 && log10 + fudge >= -9.0)
            {
                suffix = "n";
                v = v * 1.0e9;
            }
            else if (log10 < -9.0 && log10 + fudge >= -12.0)
            {
                suffix = "p";
                v = v * 1.0e12;
            }
            else if (log10 < -12.0 && log10 + fudge >= -15.0)
            {
                suffix = "f";
                v = v * 1.0e15;
            }
            else if (log10 < -15.0 && log10 + fudge >= -18.0)
            {
                suffix = "a";
                v = v * 1.0e18;
            }
            else if (log10 < -18.0 && log10 + fudge >= -21.0)
            {
                suffix = "z";
                v = v * 1.0e21;
            }
            else if (log10 < -21.0 && log10 + fudge >= -24.0)
            {
                suffix = "y";
                v = v * 1.0e24;
            }

            string numeric;

            if (format == null)
            {
                if (v >= 100.0)
                    numeric = v.ToString("0.000");
                else if (v >= 10.0)
                    numeric = v.ToString("0.0000");
                else
                    numeric = v.ToString("0.00000");
            }
            else
            {
                numeric = v.ToString(format);
            }

            if (format == null && numeric.IndexOf(".") >= 0)
            {
                if (v >= 100.0)
                {
                    while (numeric[numeric.Length - 1] == '0') numeric = numeric.Substring(0, numeric.Length - 1);

                    if (numeric[numeric.Length - 1] == '.') numeric = numeric.Substring(0, numeric.Length - 1);
                }
                else
                {
                    while (numeric.Length > 2 && numeric[numeric.Length - 1] == '0' &&
                           numeric[numeric.Length - 2] != '.')
                        numeric = numeric.Substring(0, numeric.Length - 1);
                }
            }

            return (isNeg ? "-" : "") + numeric + suffix;
        }

        #endregion
    }
}