using System.Text;

namespace ReportDesk.Api.People;

/// <summary>Escapes a value for use inside an LDAP search filter (RFC 4515, section 3).</summary>
public static class LdapFilter
{
    public static string Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var escaped = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            escaped.Append(c switch
            {
                '\\' => @"\5c",
                '*' => @"\2a",
                '(' => @"\28",
                ')' => @"\29",
                '\0' => @"\00",
                _ => c.ToString(),
            });
        }

        return escaped.ToString();
    }
}
