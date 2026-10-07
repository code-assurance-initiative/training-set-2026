namespace Warehouse.Stock.Application.Common;

/// <summary>The category of an expected failure, which the API maps to an HTTP status.</summary>
public enum ErrorKind
{
    NotFound,
    Conflict,
    Invalid,
}
