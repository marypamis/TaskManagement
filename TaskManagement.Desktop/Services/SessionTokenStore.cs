namespace TaskManagement.Desktop.Services;

// marypamis: Storing JWT token in memory for current session authentication
public class SessionTokenStore
{
    private string? _token;

    public string? Token => _token;

    public bool HasToken => !string.IsNullOrWhiteSpace(_token);

    public void SetToken(string token)
    {
        _token = token;
    }

    public void Clear()
    {
        _token = null;
    }
}
