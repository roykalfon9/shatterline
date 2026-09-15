using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    [System.Serializable]
    public class BrickRow
    {
        [Tooltip("HP per column in this row. 0 = no brick, 1 = standard, 2 = tough.")]
        public int[] hp = new int[8];
    }

    [CreateAssetMenu(fileName = "LevelData", menuName = "Shatterline/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Tooltip("Level number shown in the HUD.")]
        public int levelNumber = 1;

        [Tooltip("Rows are authored top-to-bottom, matching how the grid reads on screen. " +
                 "Row 0 in this list is the topmost row.")]
        public List<BrickRow> rows = new List<BrickRow>();

        public int RowCount => rows.Count;

        public int ColumnCount
        {
            get
            {
                int max = 0;
                foreach (var row in rows)
                {
                    if (row.hp != null && row.hp.Length > max)
                        max = row.hp.Length;
                }
                return max;
            }
        }

        /// <summary>
        /// Scoring row index for a brick at the given list row (0 = top).
        /// Counts from the bottom row upward, so the topmost (toughest) rows
        /// score highest per the "10 x row index" rule.
        /// </summary>
        public int ScoringRowIndex(int listRowIndex)
        {
            return RowCount - listRowIndex;
        }
    }
}
