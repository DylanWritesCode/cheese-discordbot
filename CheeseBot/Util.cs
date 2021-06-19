using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CheeseBot
{
    public class Util
    {
        public static bool StringHasSpecialChars(string stString)
        {
            if (stString.Any(ch => !Char.IsLetterOrDigit(ch)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
