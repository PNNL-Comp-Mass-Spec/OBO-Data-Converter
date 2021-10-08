using System.Collections.Generic;

namespace OBODataConverter
{
    public class OboEntry
    {
        public enum eParentType
        {
            Unknown = 0,
            IsA = 1,
            HasDomain = 2,
            HasOrder = 3,
            HasRegExp = 4,
            HasUnits = 5,
            PartOf = 6
        }

        public struct udtParentTypeInfo
        {
            public eParentType ParentType;
            public string ParentTermName;

            public override string ToString()
            {
                return ParentType + ": " + ParentTermName;
            }
        }

        public string Identifier { get; }

        public string Name { get; set; }

        public string Definition { get; set; }

        public string Comment { get; set; }

        /// <summary>
        /// Parent term identifiers
        /// </summary>
        /// <remarks>
        /// Keys are parent term ids and values are parent term names
        /// </remarks>
        public Dictionary<string, udtParentTypeInfo> ParentTerms { get; }

        public bool IsLeaf { get; set; }
        public bool IsObsolete { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="name"></param>
        /// <param name="isLeaf"></param>
        public OboEntry(string identifier, string name, bool isLeaf = false)
        {
            Identifier = identifier;
            Name = name;
            Definition = string.Empty;
            Comment = string.Empty;
            IsLeaf = isLeaf;

            ParentTerms = new Dictionary<string, udtParentTypeInfo>();
        }

        public void AddParentTerm(string parentTermID, udtParentTypeInfo parentTermInfo)
        {
            if (ParentTerms.ContainsKey(parentTermID))
            {
                // Parent term already defined
                return;
            }

            ParentTerms.Add(parentTermID, parentTermInfo);
        }

        public override string ToString()
        {
            return Identifier + ": " + Name;
        }
    }
}
