using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Support
{
    /// <summary>
    /// The permissions for this application.
    /// </summary>
    public enum PermissionCategories
    {
        BILLING,
        SCHEDULING
    }

    /// <summary>
    /// A static class that defines which users can have which permissions.
    /// 
    /// Current users:
    /// Admin
    /// Physician
    /// Receptionist
    /// 
    /// </summary>
    public static class UserPermissions
    {
        private static Dictionary<string, HashSet<PermissionCategories>> UserCategories =
            new Dictionary<string, HashSet<PermissionCategories>>();

        static UserPermissions()
        {
            HashSet<PermissionCategories> AdminPerms = new HashSet<PermissionCategories>();

            AdminPerms.Add(PermissionCategories.SCHEDULING);

            HashSet<PermissionCategories> PhysicianPerms = new HashSet<PermissionCategories>();

            PhysicianPerms.Add(PermissionCategories.BILLING);
            PhysicianPerms.Add(PermissionCategories.SCHEDULING);

            HashSet<PermissionCategories> ReceptionPerms = new HashSet<PermissionCategories>(AdminPerms);

            UserCategories["Admin"] = AdminPerms;
            UserCategories["Physician"] = PhysicianPerms;
            UserCategories["Receptionist"] = ReceptionPerms;
        }

        /// <summary>
        /// Checks if the given user has the given permission.
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="permission">The permission</param>
        /// <returns></returns>
        public static bool HasPermission(string user, PermissionCategories permission)
        {
            if(UserCategories.ContainsKey(user))
            {
                return UserCategories[user].Contains(permission);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("WARNING: Invalid user '{0}'", user);
                return false;
            }
        }
    }
}
