using System;
using System.Collections.Generic;
using System.Linq;

namespace CommandLineUtil
{
    public class SwitchSetting
    {
        public string Key { get; init; }
        public bool IsOptional { get; init; }
        public IEnumerable<string> AcceptedValues { get; init; }
        public bool IsFollowedByValue { get; init; }
        public string FallbackValue { get; init; }

    }

    public static class ArgumentsParsing
    {
        public static bool TryParseArguments(string[] args, List<SwitchSetting> availableSwitches, out Dictionary<string, string> switches)
        {
            var result = new Dictionary<string, string>();

            var argsUpperCase = args.Select(a => a.ToUpperInvariant()).ToArray();
            if (argsUpperCase.Contains("-H") || argsUpperCase.Contains("--H") || argsUpperCase.Contains("-HELP") || argsUpperCase.Contains("--HELP"))
            {
                switches = result;
                return false;
            }

            //Transform args to make their position available at any time in the future.
            var argsWithPosition = Enumerable.Range(0, argsUpperCase.Length).ToArray()
                                            //Filter the switches from the other arguments
                                            .Where(i => argsUpperCase[i].StartsWith("-"))
                                            .Select(i => new { Position = i, Arg = argsUpperCase[i] });

            try
            {
                availableSwitches.ForEach(s =>
                {
                    var textToRecognize = s.AcceptedValues.Select(v => $"-{v.ToUpperInvariant()}");

                    var matches = argsWithPosition.Where(m => textToRecognize.Contains(m.Arg));

                    if (matches.Count() > 1)
                    {
                        throw new Exception($"You can have only one of those simultaneously : {string.Join(",", matches.Select(v => $"'{v}'"))} ");
                    }

                    if (matches.Count() == 0)
                    {
                        if (!s.IsOptional)
                        {
                            throw new Exception($"A mandatory switch is missing. Expected : {string.Join(",", s.AcceptedValues.Select(v => $"'{v}'"))} ");
                        }

                        //Use fallback value
                        result.Add(s.Key, s.FallbackValue);
                    }
                    else
                    {
                        var match = matches.First();

                        if (!s.IsFollowedByValue)
                        {
                            result.Add(s.Key, match.Arg);
                        }
                        else
                        {
                            var paramPosition = match.Position + 1;

                            if (paramPosition >= args.Length)
                            {
                                throw new Exception($"Switch '{match.Arg}' needs to be followed by a value.");
                            }

                            //Read the arg just after this switch.
                            var param = args[paramPosition];

                            if (param.StartsWith("-"))
                            {
                                throw new Exception($"Switch '{match.Arg}' needs to be followed by a value. Found this instead : '{param}'");
                            }

                            result.Add(s.Key, param);
                        }

                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                switches = result;
                return false;
            }

            //+DEBUG
            //result.Keys.ToList().ForEach(s => Console.WriteLine($"{s} : {result[s]}"));
            //-DEBUG

            switches = result;
            return true;
        }
    }
}
