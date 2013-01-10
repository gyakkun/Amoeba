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
using Library.Io;
using Library.Net.Amoeba;
using Library.Security;

namespace Amoeba
{
    static class MessageConverter
    {
        public static string ToSignatureString(DigitalSignature digitalSignature)
        {
            if (digitalSignature == null || digitalSignature.Nickname == null || digitalSignature.PublicKey == null) return null;

            try
            {
                if (digitalSignature.DigitalSignatureAlgorithm == DigitalSignatureAlgorithm.ECDsaP521_Sha512
                    || digitalSignature.DigitalSignatureAlgorithm == DigitalSignatureAlgorithm.Rsa2048_Sha512)
                {
                    using (var sha512 = new SHA512Managed())
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            var nicknameBuffer = new UTF8Encoding(false).GetBytes(digitalSignature.Nickname);

                            memoryStream.Write(nicknameBuffer, 0, nicknameBuffer.Length);
                            memoryStream.Write(digitalSignature.PublicKey, 0, digitalSignature.PublicKey.Length);
                            memoryStream.Seek(0, SeekOrigin.Begin);

                            return digitalSignature.Nickname.Replace("@", "_") + "@" + Convert.ToBase64String(sha512.ComputeHash(memoryStream).ToArray())
                                .Replace('+', '-').Replace('/', '_').TrimEnd('=').Substring(0, 64);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return null;
            }

            return null;
        }

        public static string ToSignatureString(Certificate certificate)
        {
            if (certificate == null || certificate.Nickname == null || certificate.PublicKey == null) return null;

            try
            {
                if (certificate.DigitalSignatureAlgorithm == DigitalSignatureAlgorithm.ECDsaP521_Sha512
                    || certificate.DigitalSignatureAlgorithm == DigitalSignatureAlgorithm.Rsa2048_Sha512)
                {
                    using (var sha512 = new SHA512Managed())
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            var nicknameBuffer = new UTF8Encoding(false).GetBytes(certificate.Nickname);

                            memoryStream.Write(nicknameBuffer, 0, nicknameBuffer.Length);
                            memoryStream.Write(certificate.PublicKey, 0, certificate.PublicKey.Length);
                            memoryStream.Seek(0, SeekOrigin.Begin);

                            return certificate.Nickname.Replace("@", "_") + "@" + Convert.ToBase64String(sha512.ComputeHash(memoryStream).ToArray())
                                .Replace('+', '-').Replace('/', '_').TrimEnd('=').Substring(0, 64);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return null;
            }

            return null;
        }

        public static string ToInfoMessage(Library.Net.Amoeba.Seed seed)
        {
            try
            {
                var keywords = seed.Keywords.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();

                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(seed.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Seed_Name, seed.Name));
                if (seed.Certificate != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Seed_Signature, MessageConverter.ToSignatureString(seed.Certificate)));
                builder.AppendLine(string.Format("{0}: {1:#,0}", LanguagesManager.Instance.Seed_Length, seed.Length));
                if (keywords.Count != 0) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Seed_Keywords, String.Join(", ", keywords)));
                builder.AppendLine(string.Format("{0}: {1} UTC", LanguagesManager.Instance.Seed_CreationTime, seed.CreationTime.ToUniversalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                if (!string.IsNullOrWhiteSpace(seed.Comment)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Seed_Comment, seed.Comment));

                if (builder.Length != 0) return builder.ToString().Remove(builder.Length - 2);
                else return null;
            }
            catch (Exception e)
            {
                throw new ArgumentException("ArgumentException", e);
            }
        }

        public static string ToInfoMessage(Library.Net.Amoeba.Box box)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(box.Name)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Box_Name, box.Name));
                if (box.Certificate != null) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Box_Signature, MessageConverter.ToSignatureString(box.Certificate)));
                builder.AppendLine(string.Format("{0}: {1} UTC", LanguagesManager.Instance.Box_CreationTime, box.CreationTime.ToUniversalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo)));
                if (!string.IsNullOrWhiteSpace(box.Comment)) builder.AppendLine(string.Format("{0}: {1}", LanguagesManager.Instance.Box_Comment, box.Comment));

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
