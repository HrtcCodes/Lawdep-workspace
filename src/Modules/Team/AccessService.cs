using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using WorkSpace.Core;

namespace WorkSpace.Modules.Team
{
    public class AccessService
    {
        private readonly NetworkStorage _storage;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        public const string FileName = "team.json";

        public AccessService(NetworkStorage storage)
        {
            _storage = storage;
            EnsureFirstAdmin();
            RefreshTeamKey();
        }

        private void EnsureFirstAdmin()
        {
            if (File.Exists(_storage.GetFilePath(FileName))) return;

            var initialMembers = new List<TeamMember>
            {
                new TeamMember
                {
                    Login = AppConfig.CurrentUser,
                    FullName = AppConfig.CurrentUser + " (Создатель)",
                    Role = Roles.Admin
                }
            };

            SaveTeamMembers(initialMembers);
        }

        public void SaveTeamMembers(List<TeamMember> members)
        {
            var logins = members
                .Select(m => m.Login.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            byte[] newKey = CryptoService.DeriveKeyFromLogins(logins);
            string plainJson = _serializer.Serialize(new TeamDatabase { Members = members });
            string encryptedBlock = CryptoService.EncryptWithKey(plainJson, newKey);

            _storage.Update<TeamFileContainer>(FileName, container =>
            {
                container.AllowedLogins = logins;
                container.EncryptedData = encryptedBlock;
            });

            CryptoService.TeamKey = newKey;
        }

        public void RefreshTeamKey()
        {
            var container = _storage.Read<TeamFileContainer>(FileName);
            if (container.AllowedLogins == null || container.AllowedLogins.Count == 0)
            {
                CryptoService.TeamKey = null;
                return;
            }

            bool isAllowed = container.AllowedLogins.Any(l =>
                l.Equals(AppConfig.CurrentUser, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                CryptoService.TeamKey = null;
                return;
            }

            CryptoService.TeamKey = CryptoService.DeriveKeyFromLogins(container.AllowedLogins);
        }

        public List<TeamMember> GetAllMembers()
        {
            if (CryptoService.TeamKey == null) return new List<TeamMember>();
            try
            {
                var container = _storage.Read<TeamFileContainer>(FileName);
                if (string.IsNullOrEmpty(container.EncryptedData)) return new List<TeamMember>();

                string plainJson = CryptoService.DecryptWithKey(container.EncryptedData, CryptoService.TeamKey);
                var db = _serializer.Deserialize<TeamDatabase>(plainJson);
                return db != null && db.Members != null ? db.Members : new List<TeamMember>();
            }
            catch
            {
                return new List<TeamMember>();
            }
        }

        public TeamMember GetCurrentMember()
        {
            return GetAllMembers().FirstOrDefault(m =>
                m.Login.Equals(AppConfig.CurrentUser, StringComparison.OrdinalIgnoreCase));
        }

        public string GetCurrentRole()
        {
            var member = GetCurrentMember();
            return member != null ? member.Role : "Нет доступа";
        }

        public bool IsAdmin() { return GetCurrentRole() == Roles.Admin; }

        public bool CanEditContent()
        {
            string role = GetCurrentRole();
            return role == Roles.Admin || role == Roles.Editor;
        }
    }
}
