using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateSafe
{
    internal class ChecklistItem
    {
        private int _checklistItemId;
        private string _description;
        private bool _isCompleted;

        public ChecklistItem(
            int checklistItemId,
            string description)
        {
            _checklistItemId = checklistItemId;
            _description = description;
            _isCompleted = false;
        }

        public int ChecklistItemId
        {
            get { return _checklistItemId; }
            set { _checklistItemId = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public bool IsCompleted
        {
            get { return _isCompleted; }
        }

        public void MarkCompleted()
        {
            _isCompleted = true;
        }
    }
}