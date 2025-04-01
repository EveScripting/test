/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 12.12.2016
 * Time: 16:30
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;
using System.Collections.Generic;

namespace EVESharpCore.Framework
{
    public static class DirectCache
    {
        #region Constructors

        static DirectCache()
        {
        }

        #endregion Constructors

        public static void ClearPerSystemCache()
        {
            BracketNameDictionary = new Dictionary<int, string>();
            BracketTexturePathDictionary = new Dictionary<int, string>();
            BracketTypeDictionary = new Dictionary<int, BracketType>();
            DictionaryCachedBasePrices = new Dictionary<int, double>();
        }

        public static void ClearPerPocketCache()
        {
        }

        public static Dictionary<int, string> BracketNameDictionary = new Dictionary<int, string>();

        public static Dictionary<int, string> BracketTexturePathDictionary = new Dictionary<int, string>();
        public static Dictionary<int, BracketType> BracketTypeDictionary = new Dictionary<int, BracketType>();

        public static Dictionary<int, double> DictionaryCachedBasePrices = new Dictionary<int, double>();
        public static Dictionary<int, double> DictionaryCapacity = new Dictionary<int, double>();
        public static Dictionary<int, int> DictionaryCategoryId = new Dictionary<int, int>();
        public static Dictionary<int, string> DictionaryCategoryName = new Dictionary<int, string>();
        public static Dictionary<int, double> DictionaryChanceOfDuplicating = new Dictionary<int, double>();
        public static Dictionary<int, int> DictionaryDataId = new Dictionary<int, int>();
        public static Dictionary<int, DirectInvType> InvTypeCache = new Dictionary<int, DirectInvType>();
    }
}