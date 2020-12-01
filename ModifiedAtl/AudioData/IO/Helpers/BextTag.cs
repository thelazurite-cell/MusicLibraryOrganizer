﻿using Commons;
using System.Collections.Generic;
using System.IO;
using ATL.Logging;
using static ATL.AudioData.IO.MetaDataIO;
using System;

namespace ATL.AudioData.IO
{
    public static class BextTag
    {
        public const String CHUNK_BEXT = "bext";

        public static void FromStream(Stream source, MetaDataIO meta, ReadTagParams readTagParams)
        {
            String str;
            var data = new Byte[256];

            // Description
            source.Read(data, 0, 256);
            str = Utils.StripEndingZeroChars(Utils.Latin1Encoding.GetString(data).Trim());
            if (str.Length > 0) meta.SetMetaField("bext.description", str, readTagParams.ReadAllMetaFrames);

            // Originator
            source.Read(data, 0, 32);
            str = Utils.StripEndingZeroChars(Utils.Latin1Encoding.GetString(data, 0, 32).Trim());
            if (str.Length > 0) meta.SetMetaField("bext.originator", str, readTagParams.ReadAllMetaFrames);

            // OriginatorReference
            source.Read(data, 0, 32);
            str = Utils.StripEndingZeroChars(Utils.Latin1Encoding.GetString(data, 0, 32).Trim());
            if (str.Length > 0) meta.SetMetaField("bext.originatorReference", str, readTagParams.ReadAllMetaFrames);

            // OriginationDate
            source.Read(data, 0, 10);
            str = Utils.StripEndingZeroChars(Utils.Latin1Encoding.GetString(data, 0, 10).Trim());
            if (str.Length > 0) meta.SetMetaField("bext.originationDate", str, readTagParams.ReadAllMetaFrames);

            // OriginationTime
            source.Read(data, 0, 8);
            str = Utils.StripEndingZeroChars(Utils.Latin1Encoding.GetString(data, 0, 8).Trim());
            if (str.Length > 0) meta.SetMetaField("bext.originationTime", str, readTagParams.ReadAllMetaFrames);

            // TimeReference
            source.Read(data, 0, 8);
            var timeReference = StreamUtils.DecodeUInt64(data);
            meta.SetMetaField("bext.timeReference", timeReference.ToString(), readTagParams.ReadAllMetaFrames);

            // BEXT version
            source.Read(data, 0, 2);
            Int32 intData = StreamUtils.DecodeUInt16(data);
            meta.SetMetaField("bext.version", intData.ToString(), readTagParams.ReadAllMetaFrames);

            // UMID
            source.Read(data, 0, 64);
            str = "";

            var usefulLength = 32; // "basic" UMID
            if (data[12] > 19) usefulLength = 64; // data[12] gives the size of remaining UMID
            for (var i = 0; i < usefulLength; i++) str = str + data[i].ToString("X2");

            meta.SetMetaField("bext.UMID", str, readTagParams.ReadAllMetaFrames);

            // LoudnessValue
            source.Read(data, 0, 2);
            intData = StreamUtils.DecodeInt16(data);
            meta.SetMetaField("bext.loudnessValue", (intData / 100.0).ToString(), readTagParams.ReadAllMetaFrames);

            // LoudnessRange
            source.Read(data, 0, 2);
            intData = StreamUtils.DecodeInt16(data);
            meta.SetMetaField("bext.loudnessRange", (intData / 100.0).ToString(), readTagParams.ReadAllMetaFrames);

            // MaxTruePeakLevel
            source.Read(data, 0, 2);
            intData = StreamUtils.DecodeInt16(data);
            meta.SetMetaField("bext.maxTruePeakLevel", (intData / 100.0).ToString(), readTagParams.ReadAllMetaFrames);

            // MaxMomentaryLoudness
            source.Read(data, 0, 2);
            intData = StreamUtils.DecodeInt16(data);
            meta.SetMetaField("bext.maxMomentaryLoudness", (intData / 100.0).ToString(), readTagParams.ReadAllMetaFrames);

            // MaxShortTermLoudness
            source.Read(data, 0, 2);
            intData = StreamUtils.DecodeInt16(data);
            meta.SetMetaField("bext.maxShortTermLoudness", (intData / 100.0).ToString(), readTagParams.ReadAllMetaFrames);

            // Reserved
            source.Seek(180, SeekOrigin.Current);

            // CodingHistory
            var initialPos = source.Position;
            if (StreamUtils.FindSequence(source, new Byte[2] { 13, 10 } /* CR LF */ ))
            {
                var endPos = source.Position - 2;
                source.Seek(initialPos, SeekOrigin.Begin);

                if (data.Length < (Int32)(endPos - initialPos)) data = new Byte[(Int32)(endPos - initialPos)];
                source.Read(data, 0, (Int32)(endPos - initialPos));

                str = Utils.StripEndingZeroChars(Utils.Latin1Encoding.GetString(data, 0, (Int32)(endPos - initialPos)).Trim());
                if (str.Length > 0) meta.SetMetaField("bext.codingHistory", str, readTagParams.ReadAllMetaFrames);
            }
        }

        public static Boolean IsDataEligible(MetaDataIO meta)
        {
            if (meta.GeneralDescription.Length > 0) return true;

            foreach (var key in meta.AdditionalFields.Keys)
            {
                if (key.StartsWith("bext.")) return true;
            }

            return false;
        }

        public static Int32 ToStream(BinaryWriter w, Boolean isLittleEndian, MetaDataIO meta)
        {
            var additionalFields = meta.AdditionalFields;
            w.Write(Utils.Latin1Encoding.GetBytes(CHUNK_BEXT));

            var sizePos = w.BaseStream.Position;
            w.Write((Int32)0); // Placeholder for chunk size that will be rewritten at the end of the method

            // Text values
            var description = Utils.ProtectValue(meta.GeneralDescription);
            if (0 == description.Length && additionalFields.Keys.Contains("bext.description")) description = additionalFields["bext.description"];

            writeFixedTextValue(description, 256, w);
            writeFixedFieldTextValue("bext.originator", 32, additionalFields, w);
            writeFixedFieldTextValue("bext.originatorReference", 32, additionalFields, w);
            writeFixedFieldTextValue("bext.originationDate", 10, additionalFields, w);
            writeFixedFieldTextValue("bext.originationTime", 8, additionalFields, w);

            // Int values
            writeFieldIntValue("bext.timeReference", additionalFields, w, (UInt64)0);
            writeFieldIntValue("bext.version", additionalFields, w, (UInt16)0);

            // UMID
            if (additionalFields.Keys.Contains("bext.UMID"))
            {
                if (Utils.IsHex(additionalFields["bext.UMID"]))
                {
                    var usedValues = (Int32)Math.Floor(additionalFields["bext.UMID"].Length / 2.0);
                    for (var i = 0; i<usedValues; i++)
                    {
                        w.Write( Convert.ToByte(additionalFields["bext.UMID"].Substring(i*2, 2), 16) );
                    }
                    // Complete the field to 64 bytes
                    for (var i = 0; i < 64-usedValues; i++) w.Write((Byte)0);
                }
                else
                {
                    LogDelegator.GetLogDelegate()(Log.LV_WARNING, "'bext.UMID' : error writing field - hexadecimal notation required; " + additionalFields["bext.UMID"] + " found");
                    for (var i = 0; i < 64; i++) w.Write((Byte)0);
                }
            } else
            {
                for (var i = 0; i < 64; i++) w.Write((Byte)0);
            }


            // Float values
            writeField100DecimalValue("bext.loudnessValue", additionalFields, w, (Int16)0);
            writeField100DecimalValue("bext.loudnessRange", additionalFields, w, (Int16)0);
            writeField100DecimalValue("bext.maxTruePeakLevel", additionalFields, w, (Int16)0);
            writeField100DecimalValue("bext.maxMomentaryLoudness", additionalFields, w, (Int16)0);
            writeField100DecimalValue("bext.maxShortTermLoudness", additionalFields, w, (Int16)0);

            // Reserved
            for (var i = 0; i < 180; i++) w.Write((Byte)0);

            // CodingHistory
            var textData = new Byte[0];
            if (additionalFields.Keys.Contains("bext.codingHistory"))
            {
                textData = Utils.Latin1Encoding.GetBytes(additionalFields["bext.codingHistory"]);
                w.Write( textData );
            }
            w.Write(new Byte[2] { 13, 10 } /* CR LF */);

            // Emulation of the BWFMetaEdit padding behaviour (256 characters)
            for (var i = 0; i < 256 - ((textData.Length + 2) % 256); i++) w.Write((Byte)0);


            var finalPos = w.BaseStream.Position;
            w.BaseStream.Seek(sizePos, SeekOrigin.Begin);
            if (isLittleEndian)
            {
                w.Write((Int32)(finalPos - sizePos - 4));
            }
            else
            {
                w.Write(StreamUtils.EncodeBEInt32((Int32)(finalPos - sizePos - 4)));
            }

            return 14;
        }

        private static void writeFixedFieldTextValue(String field, Int32 length, IDictionary<String, String> additionalFields, BinaryWriter w, Byte paddingByte = 0)
        {
            if (additionalFields.Keys.Contains(field))
            {
                writeFixedTextValue(additionalFields[field], length, w, paddingByte);
            }
            else
            {
                writeFixedTextValue("", length, w, paddingByte);
            }
        }

        private static void writeFixedTextValue(String value, Int32 length, BinaryWriter w, Byte paddingByte = 0)
        {
            w.Write(Utils.BuildStrictLengthStringBytes(value, length, paddingByte, Utils.Latin1Encoding));
        }

        private static void writeFieldIntValue(String field, IDictionary<String, String> additionalFields, BinaryWriter w, Object defaultValue)
        {
            if (additionalFields.Keys.Contains(field))
            {
                if (Utils.IsNumeric(additionalFields[field], true))
                {
                    if (defaultValue is Int16) w.Write(Int16.Parse(additionalFields[field]));
                    else if (defaultValue is UInt64) w.Write(UInt64.Parse(additionalFields[field]));
                    else if (defaultValue is UInt16) w.Write(UInt16.Parse(additionalFields[field]));
                    return;
                }
                else
                {
                    LogDelegator.GetLogDelegate()(Log.LV_WARNING, "'" + field + "' : error writing field - integer required; " + additionalFields[field] + " found");
                }
            }

            if (defaultValue is Int16) w.Write((Int16)defaultValue);
            else if (defaultValue is UInt64) w.Write((UInt64)defaultValue);
            else if (defaultValue is UInt16) w.Write((UInt16)defaultValue);
        }

        private static void writeField100DecimalValue(String field, IDictionary<String, String> additionalFields, BinaryWriter w, Object defaultValue)
        {
            if (additionalFields.Keys.Contains(field))
            {
                if (Utils.IsNumeric(additionalFields[field]))
                {
                    var f = Single.Parse(additionalFields[field]) * 100;
                    if (defaultValue is Int16)  w.Write((Int16)Math.Round(f));
                    return;
                }
                else
                {
                    LogDelegator.GetLogDelegate()(Log.LV_WARNING, "'" + field + "' : error writing field - integer or decimal required; " + additionalFields[field] + " found");
                }
            }

            w.Write((Int16)defaultValue);
        }
    }
}
