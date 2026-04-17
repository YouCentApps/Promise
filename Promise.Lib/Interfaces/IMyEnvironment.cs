namespace Promise.Lib;

public interface IMyEnvironment
{
    bool IsDevelopment();
    bool IsProduction();
    string GetEnvironment();
    bool IsNative();
    bool IsWeb();
    bool IsCutOff();
}
