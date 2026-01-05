// SqlText_River.cs
// Add this content to the existing SqlText.cs file

namespace Admin.Infrastructure.DataAccess
{
    public static partial class SqlText
    {
        // River
        public static string GetRiverById => Get("River.GetRiverById");
        public static string GetAllRivers => Get("River.GetAllRivers");
        public static string SearchRivers => Get("River.SearchRivers");
        public static string GetRiverList => Get("River.GetRiverList");
        public static string CreateRiver => Get("River.CreateRiver");
        public static string UpdateRiver => Get("River.UpdateRiver");
        public static string SetRiverActive => Get("River.SetRiverActive");
    }
}
