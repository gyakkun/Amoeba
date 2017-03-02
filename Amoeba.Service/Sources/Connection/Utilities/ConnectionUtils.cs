using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Amoeba.Service
{
    static class ConnectionUtils
    {
        private readonly static Regex _regex = new Regex(@"(.*?):(.*):(\d*)", RegexOptions.Compiled);
        private readonly static Regex _regex2 = new Regex(@"(.*?):(.*)", RegexOptions.Compiled);

        public static bool ParseUri(string uri, out string scheme, out string address, ref int port)
        {
            {
                var match = _regex.Match(uri);

                if (match.Success)
                {
                    scheme = match.Groups[1].Value;
                    address = match.Groups[2].Value;
                    port = int.Parse(match.Groups[3].Value);

                    return true;
                }
            }

            {
                var match = _regex2.Match(uri);

                if (match.Success)
                {
                    scheme = match.Groups[1].Value;
                    address = match.Groups[2].Value;

                    return true;
                }
            }

            {
                scheme = null;
                address = null;

                return false;
            }
        }

        public static bool ParseUri(string uri, out string scheme, out string host)
        {
            {
                var match = _regex2.Match(uri);

                if (match.Success)
                {
                    scheme = match.Groups[1].Value;
                    host = match.Groups[2].Value;

                    return true;
                }
            }

            {
                scheme = null;
                host = null;

                return false;
            }
        }
    }
}
