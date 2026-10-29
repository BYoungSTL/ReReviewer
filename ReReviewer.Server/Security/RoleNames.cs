namespace ReReviewer.Security;

// Названия ролей для [Authorize(Roles = ...)]. Должны совпадать с Title ролей в таблице Roles.
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Moderator = "Moderator";

    // Любая из ролей: правка каталога и модерация отзывов.
    public const string Staff = Admin + "," + Moderator;
}