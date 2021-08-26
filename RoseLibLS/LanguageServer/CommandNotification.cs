namespace RoseLibLS.LanguageServer
{
    public class CommandNotification
    {
        public string Type { get; set; }
        public object Value { get; set; }
        public string Message { get; set; }

        public CommandNotification(string type, object value, string message)
        {
            Type = type;
            Value = value;
            Message = message;
        }
    }
}
