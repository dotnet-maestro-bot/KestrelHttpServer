// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal static class HttpCharacters
    {
        public static readonly int TableSize = 128;
        public static readonly bool[] AlphaNumeric;
        public static readonly bool[] Authority;
        public static readonly bool[] Token;
        public static readonly bool[] Host;
        public static readonly bool[] FieldValue;

        static HttpCharacters()
        {
            // ALPHA and DIGIT https://tools.ietf.org/html/rfc5234#appendix-B.1
            AlphaNumeric = new bool[TableSize];
            for (var c = '0'; c <= '9'; c++)
            {
                AlphaNumeric[c] = true;
            }
            for (var c = 'A'; c <= 'Z'; c++)
            {
                AlphaNumeric[c] = true;
            }
            for (var c = 'a'; c <= 'z'; c++)
            {
                AlphaNumeric[c] = true;
            }

            // Authority https://tools.ietf.org/html/rfc3986#section-3.2
            // Examples:
            // microsoft.com
            // hostname:8080
            // [::]:8080
            // [fe80::]
            // 127.0.0.1
            // user@host.com
            // user:password@host.com
            Authority = new bool[TableSize];
            Array.Copy(AlphaNumeric, Authority, TableSize);
            Authority[':'] = true;
            Authority['.'] = true;
            Authority['['] = true;
            Authority[']'] = true;
            Authority['@'] = true;

            // Matches Http.Sys
            // Matches RFC 3986 except "*" / "+" / "," / ";" / "=" and "%" HEXDIG HEXDIG which are not allowed by Http.Sys
            Host = new bool[TableSize];
            Array.Copy(AlphaNumeric, Host, TableSize);
            Host['!'] = true;
            Host['$'] = true;
            Host['&'] = true;
            Host['\''] = true;
            Host['('] = true;
            Host[')'] = true;
            Host['-'] = true;
            Host['.'] = true;
            Host['_'] = true;
            Host['~'] = true;

            // tchar https://tools.ietf.org/html/rfc7230#appendix-B
            Token = new bool[TableSize];
            Array.Copy(AlphaNumeric, Token, TableSize);
            Token['!'] = true;
            Token['#'] = true;
            Token['$'] = true;
            Token['%'] = true;
            Token['&'] = true;
            Token['\''] = true;
            Token['*'] = true;
            Token['+'] = true;
            Token['-'] = true;
            Token['.'] = true;
            Token['^'] = true;
            Token['_'] = true;
            Token['`'] = true;
            Token['|'] = true;
            Token['~'] = true;

            // field-value https://tools.ietf.org/html/rfc7230#section-3.2
            FieldValue = new bool[TableSize];
            for (var c = 0x20; c <= 0x7e; c++) // VCHAR and SP
            {
                FieldValue[c] = true;
            }
        }

        public static bool IsAuthority(byte c)
            => Authority[c];

        public static bool IsToken(byte c)
            => Token[c];

        public static bool IsToken(char c)
            => c <= 0x7f && Token[c];

        public static bool IsFieldValue(char c)
            => c <= 0x7f && FieldValue[c];
    }
}
