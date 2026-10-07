namespace HarbourLane.Bookings.Web.Security;

public static class AuthorizationPolicies
{
    /// <summary>Signed-in visitors who book rooms.</summary>
    public const string Member = "member";

    /// <summary>Centre staff who edit rooms and the site notice.</summary>
    public const string FacilitiesStaff = "facilities-staff";

    public const string StaffRole = "facilities-staff";
}
