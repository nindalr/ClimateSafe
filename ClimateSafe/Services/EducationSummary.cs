using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClimateSafe
{
    internal enum EducationState
    {
        Loading,
        Text,
        Empty,
        Error
    }

    // Ringkasan materi Edukasi untuk kartu bahaya (Beranda dan Pilih Bahaya).
    // Sumber data: API yang sama dengan halaman Edukasi. Satu-satunya salinan
    // aturan "kalimat pertama paragraf pertama" ada di kelas ini.
    internal static class EducationSummary
    {
        private sealed class Entry
        {
            public EducationState State;
            public string Text = "";
        }

        private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Entry> Cache =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<Action> Pending = new List<Action>();

        private static DateTime loadedAt = DateTime.MinValue;
        private static bool loading;
        private static bool listFailed;

        public static EducationState GetState(string hazardName, out string text)
        {
            lock (Gate)
            {
                Entry? entry;
                if (hazardName != null && Cache.TryGetValue(hazardName, out entry))
                {
                    text = entry.Text;
                    return entry.State;
                }

                text = "";
                if (loading || loadedAt == DateTime.MinValue) return EducationState.Loading;
                return listFailed ? EducationState.Error : EducationState.Empty;
            }
        }

        // Memuat cache bila kedaluwarsa. onLoaded dipanggil dari thread latar setelah
        // pemuatan selesai (termasuk bila pemuatan lain sedang berjalan).
        public static Task LoadAsync(Action? onLoaded = null, bool force = false)
        {
            lock (Gate)
            {
                if (loading)
                {
                    // Cegah dua pemuatan berjalan bersamaan; cukup ikut menunggu.
                    if (onLoaded != null) Pending.Add(onLoaded);
                    return Task.CompletedTask;
                }

                if (!force && DateTime.UtcNow - loadedAt < Ttl)
                {
                    if (onLoaded != null) onLoaded();
                    return Task.CompletedTask;
                }

                loading = true;
                if (onLoaded != null) Pending.Add(onLoaded);
            }

            return Task.Run(delegate ()
            {
                bool failed;
                Dictionary<string, Entry> fresh = Fetch(out failed);

                List<Action> callbacks;
                lock (Gate)
                {
                    foreach (KeyValuePair<string, Entry> pair in fresh) Cache[pair.Key] = pair.Value;
                    listFailed = failed;
                    loadedAt = DateTime.UtcNow;
                    loading = false;
                    callbacks = new List<Action>(Pending);
                    Pending.Clear();
                }

                foreach (Action callback in callbacks) callback();
            });
        }

        private static Dictionary<string, Entry> Fetch(out bool hazardListFailed)
        {
            Dictionary<string, Entry> map =
                new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            hazardListFailed = false;

            List<ApiHazard> hazards;
            try
            {
                hazards = ApiClient.Get<List<ApiHazard>>("api/hazards") ?? new List<ApiHazard>();
            }
            catch (Exception)
            {
                hazardListFailed = true;
                return map;
            }

            foreach (ApiHazard hazard in hazards)
            {
                Entry entry = new Entry();
                try
                {
                    List<ApiEducation> contents =
                        ApiClient.Get<List<ApiEducation>>("api/education/hazards/" + hazard.Id)
                        ?? new List<ApiEducation>();

                    if (contents.Count == 0)
                    {
                        entry.State = EducationState.Empty;
                    }
                    else
                    {
                        string sentence = FirstSentence(contents[0].Body);
                        entry.Text = sentence;
                        entry.State = sentence.Length == 0 ? EducationState.Empty : EducationState.Text;
                    }
                }
                catch (Exception)
                {
                    // Satu bahaya gagal tidak mempengaruhi bahaya lain.
                    entry.State = EducationState.Error;
                    entry.Text = "";
                }

                map[hazard.Name ?? ""] = entry;
            }

            return map;
        }

        // Kalimat pertama dari paragraf biasa pertama (baris berawalan "- " dilewati).
        public static string FirstSentence(string? body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "";

            string[] rawLines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string raw in rawLines)
            {
                string paragraph = raw.Trim();
                if (paragraph.Length == 0) continue;
                if (paragraph.StartsWith("- ", StringComparison.Ordinal)) continue;

                int stop = paragraph.IndexOf(". ", StringComparison.Ordinal);
                return stop >= 0 ? paragraph.Substring(0, stop + 1) : paragraph;
            }
            return "";
        }

        public static string StateText(EducationState state, string text)
        {
            if (state == EducationState.Text) return text;
            if (state == EducationState.Empty) return "Belum ada materi";
            if (state == EducationState.Error) return "Materi belum dapat dimuat";
            return "Memuat...";
        }
    }
}
