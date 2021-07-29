namespace RoseLibLS.LanguageServer
{
    public class CommandResponse
    {
        public object Value { get; set; }
        public string Message { get; set; }
        public bool Error { get; set; }

        public CommandResponse(object value, string message, bool error)
        {
            Value = value;
            Message = message;
            Error = error;
        }

        public CommandResponse(string message, bool error)
        {
            Message = message;
            Error = error;
        }
    }
}
