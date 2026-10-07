namespace NEXUS.Common.Constants;

/**
 * Identifier shapes taken straight from the SRS:
 *   Order ID    - one prefix letter + 10 digit serial
 *   Account ID  - one type letter + 3 digit city code + 12 digit serial
 * The prefix letter carries the connection type, so it can be read back
 * without a join.
 */
public static class IdFormats
{
    public const int OrderSerialLength = 10;
    public const int AccountSerialLength = 12;
    public const int CityCodeLength = 3;
    public const int AccountIdLength = 1 + CityCodeLength + AccountSerialLength;

    public static string OrderSerial(int value) =>
        value.ToString().PadLeft(OrderSerialLength, '0');

    public static string AccountSerial(long value) =>
        value.ToString().PadLeft(AccountSerialLength, '0');

    public static string CityCode(int value) =>
        value.ToString().PadLeft(CityCodeLength, '0');

    public static string BuildOrderId(char prefix, int serial) => prefix + OrderSerial(serial);

    public static string BuildAccountId(char typeLetter, int cityCode, long serial) =>
        typeLetter + CityCode(cityCode) + AccountSerial(serial);

    public static bool IsWellFormedAccountId(string? id) =>
        id is { Length: AccountIdLength } && id.All(char.IsLetterOrDigit);

    public static bool IsWellFormedOrderId(string? id) =>
        id is { Length: 1 + OrderSerialLength } && char.IsLetter(id[0]) && id[1..].All(char.IsDigit);
}
