using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClimateSafe
{
    internal static class RecentAccounts
    {
        private const int MaxItems = 5;

        private static string FilePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ClimateSafe");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "recent_accounts.txt");
            }
        }

        public static List<string> Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new List<string>();
                return File.ReadAllLines(FilePath)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .Take(MaxItems)
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public static void Add(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            email = email.Trim();

            try
            {
                List<string> items = Load();
                items.RemoveAll(x => string.Equals(x, email, StringComparison.OrdinalIgnoreCase));
                items.Insert(0, email);
                File.WriteAllLines(FilePath, items.Take(MaxItems));
            }
            catch (Exception)
            {
            }
        }

        public static void Remove(string email)
        {
            try
            {
                List<string> items = Load();
                items.RemoveAll(x => string.Equals(x, email, StringComparison.OrdinalIgnoreCase));
                File.WriteAllLines(FilePath, items);
            }
            catch (Exception)
            {
            }
        }
    }
}