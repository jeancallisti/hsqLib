using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CryoDataLib.TextLib
{

    public static class CharSets
    {
        public static string BasicJsonData = @"
                [
                    {
                        Culture : ""en-US"",
                        Redirects : [] //English doesn't need any redirect
                    },
                    {
                        Culture : ""fr-FR"",
                        Redirects : [
		                    { Key: 91, Value:   ""â"" }, // '['
		                    { Key: 92, Value:   ""ê"" }, // '\\'
		                    { Key: 93, Value:   ""î"" }, // ']'
		                    { Key: 95, Value:   ""û"" }, // '_'
		                    { Key: 123, Value:  ""à"" }, // '{'
		                    { Key: 124, Value:  ""é"" }, // '|'
		                    { Key: 125, Value:  ""è"" }, // '}'
		                    { Key: 126, Value:  ""ù"" }, // '~'
		                    { Key: 127, Value:  ""ç"" }, // DEL
                            //TODO: ô
                        ]
                    },
                ]
            ";
    }

    public class CharsetRedirectTable
    {
        public string Culture { get; init; }
        public IEnumerable<KeyValuePair<byte, char>> Redirects { get; init; }
    }

    //public class CharSetMatch
    //{
    //    public char Key { get; int; }
    //}
}