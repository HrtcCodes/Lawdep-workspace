using System;
using System.Collections.Generic;

namespace WorkSpace.Modules.Team
{
    public static class Roles
    {
        public const string Admin = "Администратор";
        public const string Editor = "Редактор";
        public const string Viewer = "Наблюдатель";
    }

    public class TeamMember
    {
        public string Login { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string AddedAt { get; set; }

        public TeamMember()
        {
            Role = Roles.Editor;
            AddedAt = DateTime.Now.ToString("dd.MM.yyyy");
        }
    }

    public class TeamDatabase
    {
        public List<TeamMember> Members { get; set; }

        public TeamDatabase()
        {
            Members = new List<TeamMember>();
        }
    }

    public class TeamFileContainer
    {
        public List<string> AllowedLogins { get; set; }
        public string EncryptedData { get; set; }

        public TeamFileContainer()
        {
            AllowedLogins = new List<string>();
        }
    }
}
