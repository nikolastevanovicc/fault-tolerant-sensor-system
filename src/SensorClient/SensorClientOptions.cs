using System.Globalization;

namespace SensorClient;

internal sealed record SensorClientOptions(
    Uri BaseAddress,
    bool MaliciousDemoEnabled,
    string MaliciousSensorId,
    double MaliciousOffset)
{
    private const string DefaultBaseAddress = "http://localhost:5095";
    private const double DefaultMaliciousOffset = 60.0;

    public static SensorClientOptions Parse(string[] args, string defaultMaliciousSensorId)
    {
        var baseAddress = DefaultBaseAddress;
        var baseAddressSpecified = false;
        var maliciousDemoEnabled = false;
        var maliciousSensorId = defaultMaliciousSensorId;
        var maliciousOffset = DefaultMaliciousOffset;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--malicious-demo":
                    maliciousDemoEnabled = true;
                    break;
                case "--malicious-sensor":
                    maliciousSensorId = GetOptionValue(args, ref index, "--malicious-sensor");
                    break;
                case "--malicious-offset":
                    var offsetValue = GetOptionValue(args, ref index, "--malicious-offset");
                    if (!double.TryParse(offsetValue, NumberStyles.Float, CultureInfo.InvariantCulture, out maliciousOffset))
                    {
                        throw new ArgumentException($"Invalid malicious offset '{offsetValue}'.");
                    }

                    break;
                default:
                    if (args[index].StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown option '{args[index]}'.");
                    }

                    if (baseAddressSpecified)
                    {
                        throw new ArgumentException("Only one server base address can be specified.");
                    }

                    baseAddress = args[index];
                    baseAddressSpecified = true;
                    break;
            }
        }

        return new SensorClientOptions(
            new Uri(baseAddress, UriKind.Absolute),
            maliciousDemoEnabled,
            maliciousSensorId,
            maliciousOffset);
    }

    private static string GetOptionValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Option '{optionName}' requires a value.");
        }

        index++;
        return args[index];
    }
}
