﻿using ATL.AudioData.IO;
using Commons;
using System;
using System.Text.RegularExpressions;

namespace ATL.AudioData
{
    /// <summary>
    /// General utility class to manipulate values extracted from tracks metadata
    /// </summary>
    public static class TrackUtils
    {
        /// <summary>
        /// Extract the track number from the given string
        /// </summary>
        /// <param name="TrackString">Raw "track" field in string form</param>
        /// <returns>Track number, in integer form; 0 if no track number has been found</returns>
        public static UInt16 ExtractTrackNumber(String str)
        {
            // == Optimizations (Regex are too damn expensive to use them lazily)

            // Invalid inputs
            if (null == str) return 0;
            str = str.Trim();
            if (str.Length < 1) return 0;

            // Obvious case : string begins with a number
            var i = 0;
            while (Char.IsNumber(str[i]))
            {
                i++;
                if (str.Length == i) break;
            }

            if (i > 0)
            {
                var number = Int64.Parse(str.Substring(0, i));
                if (number > UInt16.MaxValue) number = 0;
                return (UInt16)number;
            }


            // == If everything above fails...

            // This case covers both single track numbers and (trk/total) formatting
            var regex = new Regex("\\d+");

            var match = regex.Match(str);
            // First match is directly returned
            if (match.Success)
            {
                var number = Int64.Parse(match.Value);
                if (number > UInt16.MaxValue) number = 0;
                return (UInt16)number;
            }
            return 0;
        }

        /// <summary>
        /// Extract rating level from the given string
        /// </summary>
        /// <param name="ratingString">Raw "rating" field in string form</param>
        /// <param name="convention">Tagging convention (see MetaDataIO.RC_XXX constants)</param>
        /// <returns>Rating level, in float form (0 = 0% to 1 = 100%)</returns>
        public static Single DecodePopularity(String ratingString, Int32 convention)
        {
            if ((null == ratingString) || (0 == ratingString.Trim().Length)) return 0;

            if (Utils.IsNumeric(ratingString))
            {
                ratingString = ratingString.Replace(',', '.');
                return DecodePopularity(Utils.ParseDouble(ratingString), convention);
            }

            // If the field is only one byte long, rating is evaluated numerically
            if (1 == ratingString.Length) return DecodePopularity((Byte)ratingString[0], convention);

            // Exceptional case : rating is stored in the form of stars
            // NB : Having just one star is embarassing, since it falls into the "one-byte-long field" case processed above
            // It won't be interpretated as a star rating, as those are very rare
            var regex = new Regex("\\*+");

            var match = regex.Match(ratingString.Trim());
            // First match is directly returned
            if (match.Success)
            {
                return (Single)(match.Value.Length / 5.0);
            }

            return 0;
        }

        /// <summary>
        /// Extract rating level from the given byte
        /// </summary>
        /// <param name="rating">Raw "rating" field in byte form</param>
        /// <param name="convention">Tagging convention (see MetaDataIO.RC_XXX constants)</param>
        /// <returns>Rating level, in float form (0 = 0% to 1 = 100%)</returns>
        public static Single DecodePopularity(Double rating, Int32 convention)
        {
            switch (convention)
            {
                case MetaDataIO.RC_ASF:

                    if (rating < 1) return 0;
                    else if (rating < 25) return (Single)0.2;
                    else if (rating < 50) return (Single)0.4;
                    else if (rating < 75) return (Single)0.6;
                    else if (rating < 99) return (Single)0.8;
                    else return 1;

                case MetaDataIO.RC_APE:

                    if (rating < 5.1) return (Single)rating/5; // Stored as float
                    else if (rating < 10) return 0;           // Stored as scale of 0..100
                    else if (rating < 20) return (Single)0.1;
                    else if (rating < 30) return (Single)0.2;
                    else if (rating < 40) return (Single)0.3;
                    else if (rating < 50) return (Single)0.4;
                    else if (rating < 60) return (Single)0.5;
                    else if (rating < 70) return (Single)0.6;
                    else if (rating < 80) return (Single)0.7;
                    else if (rating < 90) return (Single)0.8;
                    else if (rating < 100) return (Single)0.9;
                    else return 1;

                default:                // ID3v2 convention
                    if (rating > 10)
                    {
                        // De facto conventions (windows explorer, mediaMonkey, musicBee)
                        if (rating < 54) return (Single)0.1;
                        // 0.2 is value "1"; handled in two blocks
                        else if (rating < 64) return (Single)0.3;
                        else if (rating < 118) return (Single)0.4;
                        else if (rating < 128) return (Single)0.5;
                        else if (rating < 186) return (Single)0.6;
                        else if (rating < 196) return (Single)0.7;
                        else if (rating < 242) return (Single)0.8;
                        else if (rating < 255) return (Single)0.9;
                        else return 1;
                    } else if (rating > 5) // Between 5 and 10
                    {
                        return (Single)(rating / 10.0);
                    }
                    else // Between 1 and 5
                    {
                        return (Single)(rating / 5.0);
                    }
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public static Int32 EncodePopularity(String ratingStr, Int32 convention)
        {
            var rating = Utils.ParseDouble(ratingStr);
            switch (convention)
            {
                case MetaDataIO.RC_ASF:

                    if (rating < 1) return 0;
                    else if (rating < 2) return 1;
                    else if (rating < 3) return 25;
                    else if (rating < 4) return 50;
                    else if (rating < 5) return 75;
                    else return 99;

                case MetaDataIO.RC_APE:

                    if (rating < 0.5) return 0;           // Stored as scale of 0..100
                    else if (rating < 1) return 10;
                    else if (rating < 1.5) return 20;
                    else if (rating < 2) return 30;
                    else if (rating < 2.5) return 40;
                    else if (rating < 3) return 50;
                    else if (rating < 3.5) return 60;
                    else if (rating < 4) return 70;
                    else if (rating < 4.5) return 80;
                    else if (rating < 5) return 90;
                    else return 100;

                default:                // ID3v2 convention
                    if (rating < 0.5) return 0;
                    else if (rating < 1) return 13;
                    else if (rating < 1.5) return 1;
                    else if (rating < 2) return 54;
                    else if (rating < 2.5) return 64;
                    else if (rating < 3) return 118;
                    else if (rating < 3.5) return 128;
                    else if (rating < 4) return 186;
                    else if (rating < 4.5) return 196;
                    else if (rating < 5) return 242;
                    else return 255;
            }
        }

        /// <summary>
        /// Finds a year (4 consecutive numeric chars) in a string
        /// </summary>
        /// <param name="str">String to search the year into</param>
        /// <returns>Found year in integer form; 0 if no year has been found</returns>
   		public static Int32 ExtractIntYear(String str)
		{
            var resStr = ExtractStrYear(str);
            if (0 == resStr.Length) return 0; else return Int32.Parse(resStr);
        }

        /// <summary>
		/// Finds a year (4 consecutive numeric chars) in a string
		/// </summary>
		/// <param name="str">String to search the year into</param>
		/// <returns>Found year in string form; "" if no year has been found</returns>
		public static String ExtractStrYear(String str)
        {
            // == Optimizations (Regex are too damn expensive to use them lazily)

            // Invalid inputs
            if (null == str) return "";
            str = str.Trim();
            if (str.Length < 4) return "";

            // Obvious plain year
            if (str.Length > 3)
            {
                // Begins with 4 numeric chars
                if (Char.IsNumber(str[0]) && Char.IsNumber(str[1]) && Char.IsNumber(str[2]) && Char.IsNumber(str[3]))
                {
                    return str.Substring(0, 4);
                }
                // Ends with 4 numeric chars
                if (Char.IsNumber(str[str.Length - 1]) && Char.IsNumber(str[str.Length - 2]) && Char.IsNumber(str[str.Length - 3]) && Char.IsNumber(str[str.Length - 4]))
                {
                    return str.Substring(str.Length - 4, 4);
                }
            }

            // == If everything above fails...
            var regex = new Regex("\\d{4}");

            var match = regex.Match(str.Trim());
            // First match is directly returned
            if (match.Success)
            {
                return match.Value;
            }
            return "";
		}


    }
}
