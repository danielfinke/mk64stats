using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mk64stats
{
    class Types
    {
        private static readonly string[] CHARACTERS = { "Mario", "Luigi", "Peach", "Toad", "Yoshi", "D.K.", "Wario", "Bowser" };
        private static readonly string[] CUPS = { "Mushroom", "Flower", "Star", "Special" };
        private static readonly string[,] COURSES = { { "Luigi Raceway", "Moo Moo Farm", "Koopa Troopa Beach", "Kalimari Desert" },
                                                      { "Toad's Turnpike", "Frappe Snowland", "Choco Mountain", "Mario Raceway" },
                                                      { "Wario Stadium", "Sherbet Land", "Royal Raceway", "Bowser's Castle" },
                                                      { "D.K.'s Jungle Parkway", "Yoshi Valley", "Banshee Boardwalk", "Rainbow Road" } };

        public static string CharacterName(int index)
        {
            return CHARACTERS[index - 1];
        }

        public static string CupName(int index)
        {
            return CUPS[index];
        }

        public static string CourseName(int cup, int index)
        {
            return COURSES[cup, index];
        }
    }
}
