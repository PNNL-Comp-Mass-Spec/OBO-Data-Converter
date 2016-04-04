using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OBODataConverter
{
    class clsOboConverter
    {
        #region "Constants"

        public const string DEFAULT_PRIMARY_KEY_SUFFIX = "MS1";

        #endregion

        #region "Structs"

        public struct udtOutputOptions
        {
            /// <summary>
            /// When true, include the ontology definition
            /// </summary>
            public bool IncludeDefinition;

            /// <summary>
            /// When true, if the definition is of the form "Description of term" [Ontology:Source]
            /// The definition will be written to the output file without the double quotes and without the text in the square brackets
            /// </summary>
            public bool StripQuotesFromDefinition;

            /// <summary>
            /// When true, include the ontology comment
            /// </summary>
            public bool IncludeComment;

            /// <summary>
            /// When true, include columns Parent_term_name and Parent_term_id in the output
            /// </summary>
            public bool IncludeParentTerms;

            /// <summary>
            /// When true, include columns GrandParent_term_name and GrandParent_term_id in the output
            /// </summary>
            /// <remarks>If this is true, IncludeParentTerms is assumed to be true</remarks>
            public bool IncludeGrandparentTerms;

            /// <summary>
            /// When true, exclude terms that have attribute is_obsolete: true
            /// </summary>
            public bool ExcludeObsolete;
        }

        #endregion

        #region "Classwide variables"

        private readonly string mPrimaryKeySuffix;

        private readonly Regex mQuotedDefinitionMatcher ;
        #endregion

        #region "Properties"

        /// <summary>
        /// Output file options
        /// </summary>
        public udtOutputOptions OutputOptions { get; set; }
        
        /// <summary>
        /// String appended to the ontology term identifier when creating the primary key for the Term_PK column
        /// </summary>
        public string PrimaryKeySuffix
        {
            get { return mPrimaryKeySuffix; }
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="primaryKeySuffix">String appended to the ontology term identifier when creating the primary key for the Term_PK column</param>
        public clsOboConverter(string primaryKeySuffix = DEFAULT_PRIMARY_KEY_SUFFIX)
        {
            if (string.IsNullOrWhiteSpace(primaryKeySuffix))
                mPrimaryKeySuffix = string.Empty;
            else
                mPrimaryKeySuffix = primaryKeySuffix;

            mQuotedDefinitionMatcher = new Regex(@"""(?<Definition>[^""]+)"" +\[.+\]", RegexOptions.Compiled);

            OutputOptions = DefaultOutputOptions();
        }

        /// <summary>
        /// Convert an Obo file to a tab-delimited text file
        /// </summary>
        /// <param name="oboFilePath"></param>
        /// <returns>True if success, otherwise false</returns>
        public bool ConvertOboFile(string oboFilePath)
        {
            var oboFile = new FileInfo(oboFilePath);

            var outputFilePath = ConstructOutputFilePath(oboFile);

            return ConvertOboFile(oboFilePath, outputFilePath);
        }

        /// <summary>
        /// Convert an Obo file to a tab-delimited text file
        /// </summary>
        /// <param name="oboFilePath"></param>
        /// <param name="outputFilePath"></param>
        /// <returns>True if success, otherwise false</returns>
        public bool ConvertOboFile(string oboFilePath, string outputFilePath)
        {
            var lineNumber = 0;

            try
            {
                if (string.IsNullOrWhiteSpace(oboFilePath))
                {
                    Console.WriteLine("oboFilePath is empty; nothing to convert");
                    return false;
                }

                var oboFile = new FileInfo(oboFilePath);

                if (!oboFile.Exists)
                {
                    Console.WriteLine("Source obo file not found: " + oboFilePath);
                    return false;
                }

                FileInfo outputFile;
                if (string.IsNullOrWhiteSpace(outputFilePath))
                    outputFile = new FileInfo(ConstructOutputFilePath(oboFile));
                else
                    outputFile = new FileInfo(outputFilePath);

                // Read the data from the Obo file
                // Track them using this list
                var ontologyEntries = new List<OboEntry>();

                using (var reader = new StreamReader(new FileStream(oboFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        lineNumber++;

                        if (string.IsNullOrWhiteSpace(dataLine))
                            continue;

                        if (dataLine == "[Term]")
                        {
                            ParseTerm(reader, ontologyEntries, ref lineNumber);
                        }
                    }


                    // Make a list of identifiers that are parents of other terms
                    var parentNodes = new SortedSet<string>();

                    /*
                    var timings = new List<KeyValuePair<TimeSpan, string>>();                    
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    */

                    foreach (var ontologyTerm in ontologyEntries)
                    {
                        foreach (var parentTerm in ontologyTerm.ParentTerms)
                        {
                            if (!parentNodes.Contains(parentTerm.Key))
                                parentNodes.Add(parentTerm.Key);
                        }
                    }

                    /*
                    stopwatch.Stop();                    
                    timings.Add(new KeyValuePair<TimeSpan, string>(stopwatch.Elapsed, "Populated parentNodes"));
                    
                    stopwatch.Reset();
                    stopwatch.Start();
                    */

                    // Update IsLeaf for the ontology entries
                    // An entry is a leaf node if no other nodes reference it as a parent
                    foreach (var ontologyTerm in ontologyEntries)
                    {
                        ontologyTerm.IsLeaf = !parentNodes.Contains(ontologyTerm.Identifier);
                    }

                    /*
                    stopwatch.Stop();
                    timings.Add(new KeyValuePair<TimeSpan, string>(stopwatch.Elapsed, "Updated IsLeaf using parentNodes"));

                    stopwatch.Reset();
                    stopwatch.Start();

                    // Update IsLeaf for the ontology entries
                    // An entry is a leaf node if no other nodes reference it as a parent
                    foreach (var ontologyTerm in ontologyEntries)
                    {
                        var currentTerm = ontologyTerm;

                        var query =
                            (from item in ontologyEntries
                             where item.ParentTerms.ContainsKey(currentTerm.Identifier)
                             select item);

                        var isLeaf = !query.Any();

                        if (currentTerm.IsLeaf != isLeaf)
                        {
                            Console.WriteLine("Logic error; isLeaf disageement");
                        }
                    }

                    stopwatch.Stop();
                    timings.Add(new KeyValuePair<TimeSpan, string>(stopwatch.Elapsed, "Updated IsLeaf using Linq"));

                    Console.WriteLine("{0,-15} {1}", "Msec for task", "Task");
                    
                    foreach (var timingEntry in timings)
                    {
                        Console.WriteLine("{0,10:0.000}      {1}", timingEntry.Key.TotalMilliseconds, timingEntry.Value);
                    }
                    */
                }

                var success = WriteOboInfoToFile(ontologyEntries, outputFile);

                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in ConvertOboFile at line " + lineNumber + ": " + ex.Message);
                return false;
            }
        }

        public static udtOutputOptions DefaultOutputOptions()
        {
            var outputOptions = new udtOutputOptions()
            {
                IncludeDefinition = false,
                StripQuotesFromDefinition = false,
                IncludeComment = false,
                IncludeParentTerms = true,
                IncludeGrandparentTerms = true,
                ExcludeObsolete = false
            };

            return outputOptions;

        }

        private string ConstructOutputFilePath(FileInfo oboFile)
        {

            if (string.IsNullOrWhiteSpace(oboFile.DirectoryName))
            {
                Console.WriteLine("Unable to determine parent directory of " + oboFile.FullName);
                return string.Empty;
            }

            var outputFilePath = Path.Combine(oboFile.DirectoryName, Path.GetFileNameWithoutExtension(oboFile.Name) + ".txt");

            if (outputFilePath.Equals(oboFile.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                return outputFilePath + ".new";
            }

            return outputFilePath;
        }

        private void AddParentTerm(IDictionary<string, OboEntry.udtParentTypeInfo> parentTerms, string parentTypeName, string parentTermId, string parentTermName, int lineNumber, string dataLine)
        {
            if (parentTerms.ContainsKey(parentTermId))
            {
                Console.WriteLine("Parent term specified twice; ignoring " + parentTermId + " for line " + lineNumber + ": " + dataLine);
                return;
            }

            var parentType = OboEntry.eParentType.Unknown;

            switch (parentTypeName)
            {
                case "is_a":
                    parentType = OboEntry.eParentType.IsA;
                    break;
                case "has_domain":
                    parentType = OboEntry.eParentType.HasDomain;
                    break;
                case "has_order":
                    parentType = OboEntry.eParentType.HasOrder;
                    break;
                case "has_regexp":
                    parentType = OboEntry.eParentType.HasRegExp;
                    break;
                case "has_units":
                    parentType = OboEntry.eParentType.HasUnits;
                    break;
                case "part_of":
                    parentType = OboEntry.eParentType.PartOf;
                    break;
            }

            var parentEntry = new OboEntry.udtParentTypeInfo
            {
                ParentType = parentType,
                ParentTermName = parentTermName
            };

            parentTerms.Add(parentTermId, parentEntry);

        }

        private static byte BoolToTinyInt(bool value)
        {
            if (value)
                return 1;

            return 0;
        }

        private static OboEntry GetAncestor(IEnumerable<OboEntry> ontologyEntries, string termIdentifier)
        {
            var query = (from item in ontologyEntries where item.Identifier == termIdentifier select item);
            return query.FirstOrDefault();
        }

        private List<string> OntologyTermNoParents(OboEntry ontologyTerm)
        {
            var suffix = string.IsNullOrWhiteSpace(mPrimaryKeySuffix) ? string.Empty : mPrimaryKeySuffix;

            var dataColumns = new List<string>
                                {
                                    ontologyTerm.Identifier + suffix,               // Term Primary Key
                                    ontologyTerm.Name,                              // Term Name
                                    ontologyTerm.Identifier,                        // Term Identifier
                                    BoolToTinyInt(ontologyTerm.IsLeaf).ToString()   // Is_Leaf
                                };

            if (OutputOptions.IncludeDefinition)
                dataColumns.Add(ontologyTerm.Definition);

            if (OutputOptions.IncludeComment)
                dataColumns.Add(ontologyTerm.Comment);

            return dataColumns;
        }

        private List<string> OntologyTermWithParents(OboEntry ontologyTerm, KeyValuePair<string, OboEntry.udtParentTypeInfo> parentTerm)
        {
            var dataColumns = OntologyTermNoParents(ontologyTerm);
            dataColumns.Add(parentTerm.Value.ParentTermName);             // Parent term name
            dataColumns.Add(parentTerm.Key);                              // Parent term Identifier

            return dataColumns;
        }

        private void ParseTerm(StreamReader reader, ICollection<OboEntry> ontologyEntries, ref int lineNumber)
        {
            try
            {
                var identifier = string.Empty;
                var name = string.Empty;
                var definition = string.Empty;
                var comment = string.Empty;

                var parentTerms = new Dictionary<string, OboEntry.udtParentTypeInfo>();
                var isObsolete = false;

                while (!reader.EndOfStream)
                {
                    var dataLine = reader.ReadLine();
                    lineNumber++;

                    if (string.IsNullOrWhiteSpace(dataLine))
                        break;

                    string key;
                    string value;

                    if (!SplitKeyValuePair(dataLine, ':', string.Empty, lineNumber, out key, out value))
                    {
                        continue;
                    }

                    switch (key)
                    {
                        case "id":
                            identifier = value;
                            break;
                        case "name":
                            name = value;
                            break;
                        case "comment":
                            comment = value;
                            break;
                        case "def":
                            if (OutputOptions.StripQuotesFromDefinition)
                            {
                                var match = mQuotedDefinitionMatcher.Match(value);
                                if (match.Success)
                                {
                                    definition = match.Groups["Definition"].Value;
                                    break;
                                }
                            }

                            definition = value;
                            break;
                        case "xref":
                            // Ignore xref
                            break;
                        case "is_a":

                            string parentTermId;
                            string parentTermName;

                            if (!SplitKeyValuePair(value.Trim(), '!', "is_a", lineNumber, out parentTermId, out parentTermName))
                            {
                                continue;
                            }

                            AddParentTerm(parentTerms, "is_a", parentTermId, parentTermName, lineNumber, dataLine);
                            break;

                        case "synonym":
                            // Ignore synonym
                            break;
                        case "replaced_by":
                            // Ignore replaced_by
                            break;
                        case "alt_id":
                            // Ignore ALT_ID
                            break;
                        case "relationship":

                            // relationship: part_of MS:1000458 ! source
                            // relationship: has_units UO:0000187 ! percent

                            string relationshipTypeAndParent;
                            string relationshipValue;

                            if (!SplitKeyValuePair(value.Trim(), '!', "relationshipDef", lineNumber, out relationshipTypeAndParent, out relationshipValue))
                            {
                                continue;
                            }

                            string relationshipType;
                            string relationshipParentTermName;

                            if (!SplitKeyValuePair(relationshipTypeAndParent.Trim(), ' ', "relationshipTypeAndParent", lineNumber, out relationshipType, out relationshipParentTermName))
                            {
                                continue;
                            }

                            switch (relationshipType)
                            {
                                case "has_domain":
                                    // Ignore this relationship type
                                    break;
                                case "has_order":
                                    break;
                                case "has_regexp":
                                    AddParentTerm(parentTerms, relationshipType, relationshipParentTermName, relationshipValue, lineNumber, dataLine);
                                    break;
                                case "has_units":
                                    AddParentTerm(parentTerms, relationshipType, relationshipParentTermName, relationshipValue, lineNumber, dataLine);
                                    break;
                                case "part_of":
                                    AddParentTerm(parentTerms, relationshipType, relationshipParentTermName, relationshipValue, lineNumber, dataLine);
                                    break;
                                default:
                                    Console.WriteLine("Unknown relationship type " + relationshipType + " at line " + lineNumber + ": " + dataLine);
                                    break;
                            }
                            break;

                        case "is_obsolete":
                            isObsolete = true;
                            break;

                        default:
                            Console.WriteLine("Unknown key " + key + " at line " + lineNumber + ": " + dataLine);
                            break;
                    }
                }

                var ontologyEntry = new OboEntry(identifier, name)
                {
                    Definition = definition,
                    IsObsolete = isObsolete,
                    Comment = comment
                };

                foreach (var parentEntry in parentTerms)
                {
                    ontologyEntry.AddParentTerm(parentEntry.Key, parentEntry.Value);
                }

                ontologyEntries.Add(ontologyEntry);

            }
            catch (Exception ex)
            {
                throw new Exception("Exception in ParseTerm at line " + lineNumber + ": " + ex.Message, ex);
            }
        }

        private static bool SplitKeyValuePair(string data, char delimiter, string dataDescription, int lineNumber, out string key, out string value)
        {
            var charIndex = data.IndexOf(delimiter);
            if (charIndex < 0)
            {
                string delimName;
                if (delimiter == ' ')
                    delimName = "space";
                else
                {
                    delimName = delimiter.ToString();
                }

                if (string.IsNullOrWhiteSpace(dataDescription))
                    Console.WriteLine(delimName + " not found in line " + lineNumber + ": " + data);
                else
                    Console.WriteLine(delimName + " not found in " + dataDescription + " for line " + lineNumber + ": " + data);

                key = null;
                value = null;

                return false;
            }

            key = data.Substring(0, charIndex).Trim();
            value = data.Substring(charIndex + 1).Trim();

            return true;
        }

        private bool WriteOboInfoToFile(List<OboEntry> ontologyEntries, FileInfo outputFile)
        {

            try
            {
                var purgatoryTermID = string.Empty;

                var purgatorySearch = (from item in ontologyEntries where string.Equals(item.Name, "purgatory", StringComparison.InvariantCultureIgnoreCase) select item.Identifier).ToList();
                if (purgatorySearch.Count > 0)
                    purgatoryTermID = purgatorySearch.FirstOrDefault();

                using (var writer = new StreamWriter(new FileStream(outputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    var columnHeaders = new List<string>
                    {
                        "Term_PK",
                        "Term_Name",
                        "Identifier",
                        "Is_Leaf"
                    };

                    if (OutputOptions.IncludeDefinition)
                        columnHeaders.Add("Definition");

                    if (OutputOptions.IncludeComment)
                        columnHeaders.Add("Comment");

                    if (OutputOptions.IncludeGrandparentTerms && !OutputOptions.IncludeParentTerms)
                    {
                        // Force-enable inclusion of parent terms because grandparent terms will be included
                        var updatedOptions = OutputOptions;
                        updatedOptions.IncludeParentTerms = true;
                        OutputOptions = updatedOptions;
                    }

                    if (OutputOptions.IncludeParentTerms)
                    {
                        columnHeaders.Add("Parent_term_name");
                        columnHeaders.Add("Parent_term_ID");
                    }

                    if (OutputOptions.IncludeGrandparentTerms)
                    {
                        columnHeaders.Add("GrandParent_term_name");
                        columnHeaders.Add("GrandParent_term_ID");
                    }

                    writer.WriteLine(string.Join("\t", columnHeaders));

                    foreach (var ontologyTerm in ontologyEntries)
                    {
                        if (OutputOptions.ExcludeObsolete && ontologyTerm.IsObsolete)
                        {
                            continue;
                        }

                        if (ontologyTerm.ParentTerms.Count == 0 || !OutputOptions.IncludeParentTerms)
                        {
                            var lineOut = OntologyTermNoParents(ontologyTerm);

                            if (OutputOptions.IncludeParentTerms)
                            {
                                if (ontologyTerm.IsObsolete && !string.IsNullOrWhiteSpace(purgatoryTermID))
                                {
                                    lineOut.Add(string.Empty);      // Parent term name
                                    lineOut.Add(string.Empty);      // Parent term ID
                                }
                                else
                                {
                                    lineOut.Add(string.Empty);      // Parent term name
                                    lineOut.Add(string.Empty);      // Parent term ID
                                }
                            }

                            writer.WriteLine(string.Join("\t", lineOut));
                            continue;
                        }
                        
                        foreach (var parentTerm in ontologyTerm.ParentTerms)
                        {
                            var ancestor = GetAncestor(ontologyEntries, parentTerm.Key);

                            if (ancestor == null || ancestor.ParentTerms.Count == 0 || !OutputOptions.IncludeGrandparentTerms)
                            {
                                // No grandparents (or grandparents are disabled)
                                var lineOut = OntologyTermWithParents(ontologyTerm, parentTerm);

                                if (OutputOptions.IncludeGrandparentTerms)
                                {

                                    if (ancestor != null && ancestor.IsObsolete && !string.IsNullOrWhiteSpace(purgatoryTermID))
                                    {
                                        lineOut.Add(string.Empty);      // Grandparent term name
                                        lineOut.Add(string.Empty);      // Grandparent term ID
                                    }
                                    else
                                    {
                                        lineOut.Add(string.Empty);      // Grandparent term name
                                        lineOut.Add(string.Empty);      // Grandparent term ID
                                    }

                                }

                                writer.WriteLine(string.Join("\t", lineOut));
                                continue;
                            }
                            
                            foreach (var grandParent in ancestor.ParentTerms)
                            {
                                var lineOut = OntologyTermWithParents(ontologyTerm, parentTerm);
                                lineOut.Add(grandParent.Value.ParentTermName);
                                lineOut.Add(grandParent.Key);

                                writer.WriteLine(string.Join("\t", lineOut));
                            }
                        } // ForEach
                    } // ForEach
                } // Using

                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to file " + outputFile.FullName + ": " + ex.Message);
                return false;
            }


        }
    }
}
