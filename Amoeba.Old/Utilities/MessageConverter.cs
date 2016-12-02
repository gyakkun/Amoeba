using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Amoeba.Properties;
using Library;
using Library.Io;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba
{
    static class MessageConverter
    {
        public static string ToTagString(Tag tag)
        {
            if (tag.Name == null || tag.Id == null) return null;

            try
            {
                return tag.Name + " - " + NetworkConverter.ToBase64UrlString(tag.Id);
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(Tag tag)
        {
            try
            {
                var builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(tag.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Tag_Name, tag.Name));
                if (tag.Id != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Tag_Id, NetworkConverter.ToBase64UrlString(tag.Id)));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(Seed seed)
        {
            if (seed == null) throw new ArgumentNullException(nameof(seed));

            try
            {
                var keywords = seed.Keywords.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();

                var builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(seed.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Seed_Name, seed.Name));
                if (seed.Certificate != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Seed_Signature, seed.Certificate.ToString()));
                builder.AppendLine(string.Format("{0}: {1:#,0}", LanguagesManager.Instance.Seed_Length, seed.Length));
                if (keywords.Count != 0) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Seed_Keywords, String.Join(", ", keywords)));
                builder.AppendLine(string.Format("{0}: {1} UTC", LanguagesManager.Instance.Seed_CreationTime, seed.CreationTime.ToUniversalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(Box box)
        {
            if (box == null) throw new ArgumentNullException(nameof(box));

            try
            {
                var builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(box.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Box_Name, box.Name));
                builder.AppendLine(string.Format("{0}: {1} UTC", LanguagesManager.Instance.Box_CreationTime, BoxUtils.GetCreationTime(box).ToUniversalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }
    }
}
