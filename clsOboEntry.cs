﻿using System.Collections.Generic;

namespace OBODataConverter
{
    public class OboEntry
    {
        // Ignore Spelling: Obo

        public enum ParentTypes
        {
            Unknown = 0,
            IsA = 1,
            HasDomain = 2,
            HasOrder = 3,
            HasRegExp = 4,
            HasUnits = 5,
            PartOf = 6
        }

        public struct ParentTypeInfo
        {
            public ParentTypes ParentType;
            public string ParentTermName;

            public override string ToString()
            {
                return ParentType + ": " + ParentTermName;
            }
        }

        /// <summary>
        /// Ontology term identifier
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Ontology term name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Definition
        /// </summary>
        public string Definition { get; set; }

        /// <summary>
        /// Comment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Parent term identifiers
        /// </summary>
        /// <remarks>
        /// Keys are parent term ids and values are parent term names
        /// </remarks>
        public Dictionary<string, ParentTypeInfo> ParentTerms { get; }

        /// <summary>
        /// True if this entry does not have any children
        /// </summary>
        public bool IsLeaf { get; set; }

        /// <summary>
        /// True if this entry is obsolete
        /// </summary>
        public bool IsObsolete { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="identifier">Term identifier</param>
        /// <param name="name">Term name</param>
        /// <param name="isLeaf">True if this entry does not have any children</param>
        public OboEntry(string identifier, string name, bool isLeaf = false)
        {
            Identifier = identifier;
            Name = name;
            Definition = string.Empty;
            Comment = string.Empty;
            IsLeaf = isLeaf;

            ParentTerms = new Dictionary<string, ParentTypeInfo>();
        }

        /// <summary>
        /// Add a parent term
        /// </summary>
        /// <param name="parentTermID"></param>
        /// <param name="parentTermInfo"></param>
        public void AddParentTerm(string parentTermID, ParentTypeInfo parentTermInfo)
        {
            if (ParentTerms.ContainsKey(parentTermID))
            {
                // Parent term already defined
                return;
            }

            ParentTerms.Add(parentTermID, parentTermInfo);
        }

        /// <summary>
        /// Show term ID and name
        /// </summary>
        public override string ToString()
        {
            return Identifier + ": " + Name;
        }
    }
}
