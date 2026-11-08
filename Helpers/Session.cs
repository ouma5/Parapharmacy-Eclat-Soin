using EclatEtSoin.Models;

namespace EclatEtSoin.Helpers
{
    public static class Session
    {
        public static User Current { get; set; }
        public static bool IsAdmin => Current != null && Current.Role == "Administrateur";
        public static int UserId => Current == null ? 0 : Current.Id;
        public static string DisplayName => Current == null ? "" : Current.FullName;
    }
}
