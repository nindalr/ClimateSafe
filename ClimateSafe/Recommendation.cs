using System;
using System.Collections.Generic;

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

        public List<ChecklistItem> ChecklistItems
        {
            get { return _checklistItems; }
        }
    }
}