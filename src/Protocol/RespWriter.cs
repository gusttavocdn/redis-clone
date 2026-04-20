namespace codecrafters_redis;

public static class RespWriter
{
    public static string BulkString(string value) => $"${value.Length}\r\n{value}\r\n";
    public static string NullBulk()               => "$-1\r\n";
    public static string SimpleString(string s)   => $"+{s}\r\n";
    public static string Error(string msg)         => $"-{msg}\r\n";
    public static string Integer(long n)           => $":{n}\r\n";
}
