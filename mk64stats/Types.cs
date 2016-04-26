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
        private static readonly string[] CHARACTER_IMGS =
        {
            "question_mark.png",
            "mario.png",
            "luigi.png",
            "peach.png",
            "toad.png",
            "yoshi.png",
            "dk.png",
            "wario.png",
            "bowser.png"
        };
        private static readonly string CHARACTER_IMGS_PREFIX = "img/";

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

        public static string CharacterImg(int index)
        {
            return CHARACTER_IMGS_PREFIX + CHARACTER_IMGS[index];
        }
    }
}
