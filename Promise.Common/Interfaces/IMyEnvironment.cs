namespace Promise.Common;

public interface IMyEnvironment
{
    bool IsDevelopment();
    bool IsProduction();
    string GetEnvironment();
    bool IsNative();
    bool IsWeb();
    bool IsCutOff();
}
