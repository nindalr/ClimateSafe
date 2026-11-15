using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateSafe
{
    internal class Recommendation
    {
        private int _recommendationId;
        private string _description;
        private string _priority;

        private readonly List<ChecklistItem> _checklistItems;

        public Recommendation(
            int recommendationId,
            string description,
            string priority)
        {
            _recommendationId = recommendationId;
            _description = description;
            _priority = priority;

            _checklistItems = new List<ChecklistItem>();
        }

        public int RecommendationId
        {
            get { return _recommendationId; }
            set { _recommendationId = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public string Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        public void AddChecklistItem(ChecklistItem item)
        {
            if (item == null)
            {
                return;
            }

            if (!_checklistItems.Contains(item))
            {
                _checklistItems.Add(item);
            }
        }

        public IReadOnlyList<ChecklistItem> GetChecklistItems()
        {
            return _checklistItems.AsReadOnly();
        }
    }
}