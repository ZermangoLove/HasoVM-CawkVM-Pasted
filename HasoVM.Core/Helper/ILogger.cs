namespace HasoVM.Core.Helper
{
    public interface ILogger
    {
       void Log(string m, string t);
       void Error(string m, string t);
    }
}
