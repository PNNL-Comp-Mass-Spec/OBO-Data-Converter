using System;
using System.Collections.Generic;
using System.Linq;

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

        private readonly string mIdentifier;

        /// <summary>
        /// Parent Terms
        /// </summary>
        /// <remarks>
        /// Keys are parent term ids and values are parent term names
        /// </remarks>
        private readonly Dictionary<string, udtParentTypeInfo> mParentTerms;

        public string Identifier 
        {
            get 
            { 
                return mIdentifier; 
            }
        }

        public string Name { get; set; }
        
        public string Definition { get; set; }

        public string Comment { get; set; }

        /// <summary>
        /// Parent term identifiers
        /// </summary>
        /// <remarks>
        /// Keys are parent term ids and values are parent term names
        /// </remarks>
        public Dictionary<string, udtParentTypeInfo> ParentTerms
        { 
            get 
            {
                return mParentTerms; 
            }
        }

        public bool IsLeaf { get; set; }
        public bool IsObsolete { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="identifier"></param>
        public OboEntry(string identifier) : this(identifier, String.Empty)
        {            
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="name"></param>
        /// <param name="isLeaf"></param>
        public OboEntry(string identifier, string name, bool isLeaf = false)
        {
            mIdentifier = identifier;
            Name = name;
            Definition = String.Empty;
            Comment = string.Empty;
            IsLeaf = isLeaf;

            mParentTerms = new Dictionary<string, udtParentTypeInfo>();
        }

        public void AddParentTerm(string parentTermID, udtParentTypeInfo parentTermInfo)
        {
            if (mParentTerms.ContainsKey(parentTermID))
            {
                // Parent term already defined
                return;
            }

            mParentTerms.Add(parentTermID, parentTermInfo);
        }

        public override string ToString()
        {
            return mIdentifier + ": " + Name;
        }

    }
}
