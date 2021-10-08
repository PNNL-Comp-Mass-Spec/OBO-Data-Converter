using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PRISM;

namespace OBODataConverter
{
    internal class OboConverter : EventNotifier
    {

        public const string DEFAULT_PRIMARY_KEY_SUFFIX = "MS1";

        private const int AUTO_REPLACE_MESSAGE_THRESHOLD = 5;

        public struct OutputFileOptions
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

        private readonly Regex mQuotedDefinitionMatcher;

        private readonly Dictionary<string, string> mNameReplacements;

        /// <summary>
        /// Tracks auto-replacement messages
        /// </summary>
        /// <remarks>Keys are the auto-replacement type and values are the number of times that replacement has been applied</remarks>
        private readonly Dictionary<string, int> mNameReplacementCountsByType;

        /// <summary>
        /// Output file options
        /// </summary>
        public OutputFileOptions OutputOptions { get; set; }

        /// <summary>
        /// String appended to the ontology term identifier when creating the primary key for the Term_PK column
        /// </summary>
        public string PrimaryKeySuffix { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="primaryKeySuffix">String appended to the ontology term identifier when creating the primary key for the Term_PK column</param>
        public OboConverter(string primaryKeySuffix = DEFAULT_PRIMARY_KEY_SUFFIX)
        {
            PrimaryKeySuffix = string.IsNullOrWhiteSpace(primaryKeySuffix) ? string.Empty : primaryKeySuffix;

            mQuotedDefinitionMatcher = new Regex(@"""(?<Definition>[^""]+)"" +\[.+\]", RegexOptions.Compiled);

            mNameReplacements = new Dictionary<string, string> {
                {@"\!", "!"}
            };

            mNameReplacementCountsByType = new Dictionary<string, int>();

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
                    OnErrorEvent("oboFilePath is empty; nothing to convert");
                    return false;
                }

                var oboFile = new FileInfo(oboFilePath);

                if (!oboFile.Exists)
                {
                    OnErrorEvent("Source obo file not found: " + oboFilePath);
                    return false;
                }

                OnStatusEvent("Parsing " + oboFile.FullName);

                FileInfo outputFile;
                if (string.IsNullOrWhiteSpace(outputFilePath))
                    outputFile = new FileInfo(ConstructOutputFilePath(oboFile));
                else
                    outputFile = new FileInfo(outputFilePath);

                // Read the data from the Obo file
                // Track them using this list
                var ontologyEntries = new List<OboEntry>();
                var leafNodeCount = 0;

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

                    foreach (var ontologyTerm in ontologyEntries)
                    {
                        foreach (var parentTerm in ontologyTerm.ParentTerms)
                        {
                            if (!parentNodes.Contains(parentTerm.Key))
                                parentNodes.Add(parentTerm.Key);
                        }
                    }

                    // Update IsLeaf for the ontology entries
                    // An entry is a leaf node if no other nodes reference it as a parent
                    foreach (var ontologyTerm in ontologyEntries)
                    {
                        if (parentNodes.Contains(ontologyTerm.Identifier))
                            continue;

                        ontologyTerm.IsLeaf = true;
                        leafNodeCount++;
                    }
                }

                var autoReplacementsOverThreshold =
                    (from item in mNameReplacementCountsByType
                     where item.Value > AUTO_REPLACE_MESSAGE_THRESHOLD
                     select item).ToList();

                if (autoReplacementsOverThreshold.Count > 0)
                {
                    Console.WriteLine();
                    foreach (var replacementItem in autoReplacementsOverThreshold)
                    {
                        OnStatusEvent(" ... " + replacementItem.Key + " " + replacementItem.Value + " times");
                    }
                }

                Console.WriteLine();
                OnStatusEvent(string.Format("Found {0:N0} terms, of which {1:N0} are leaf nodes", ontologyEntries.Count, leafNodeCount));
                Console.WriteLine();

                var success = WriteOboInfoToFile(ontologyEntries, outputFile);

                if (success)
                {
                    Console.WriteLine();
                    OnStatusEvent("Conversion is complete");
                }

                return success;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in ConvertOboFile at line " + lineNumber + ": " + ex.Message);
                return false;
            }
        }

        public static OutputFileOptions DefaultOutputOptions()
        {
            var outputOptions = new OutputFileOptions()
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
                OnErrorEvent("Unable to determine parent directory of " + oboFile.FullName);
                return string.Empty;
            }

            var outputFilePath = Path.Combine(oboFile.DirectoryName, Path.GetFileNameWithoutExtension(oboFile.Name) + ".txt");

            if (outputFilePath.Equals(oboFile.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                return outputFilePath + ".new";
            }

            return outputFilePath;
        }

        private void AddParentTerm(IDictionary<string, OboEntry.ParentTypeInfo> parentTerms, string parentTypeName, string parentTermId, string parentTermName, int lineNumber, string dataLine)
        {
            if (parentTerms.ContainsKey(parentTermId))
            {
                OnWarningEvent("Parent term specified twice; ignoring " + parentTermId + " for line " + lineNumber + ": " + dataLine);
                return;
            }

            var parentType = OboEntry.ParentTypes.Unknown;

            switch (parentTypeName)
            {
                case "is_a":
                    parentType = OboEntry.ParentTypes.IsA;
                    break;
                case "has_domain":
                    parentType = OboEntry.ParentTypes.HasDomain;
                    break;
                case "has_order":
                    parentType = OboEntry.ParentTypes.HasOrder;
                    break;
                case "has_regexp":
                    parentType = OboEntry.ParentTypes.HasRegExp;
                    break;
                case "has_units":
                    parentType = OboEntry.ParentTypes.HasUnits;
                    break;
                case "part_of":
                    parentType = OboEntry.ParentTypes.PartOf;
                    break;
            }

            var parentEntry = new OboEntry.ParentTypeInfo
            {
                ParentType = parentType,
                ParentTermName = parentTermName
            };

            parentTerms.Add(parentTermId, parentEntry);
        }

        private string AutoReplaceText
            (string value,
             Dictionary<string, string> replacementList,
             IDictionary<string, int> replacementCountsByType)
        {
            var updatedValue = value;

            foreach (var replacementItem in replacementList)
            {
                if (!updatedValue.Contains(replacementItem.Key))
                {
                    continue;
                }

                // Term contains text that we want to replace, for example replace \! with !
                // Replace the text and possibly inform the user that we made this change
                updatedValue = updatedValue.Replace(replacementItem.Key, replacementItem.Value);

                var replacementMsg = @"auto-replaced " + replacementItem.Key + " with " + replacementItem.Value;

                if (!replacementCountsByType.TryGetValue(replacementMsg, out var previousCount))
                {
                    previousCount = 0;
                    replacementCountsByType.Add(replacementMsg, 0);
                }

                replacementCountsByType[replacementMsg] = previousCount + 1;

                if (previousCount < AUTO_REPLACE_MESSAGE_THRESHOLD)
                    OnStatusEvent(@" ... " + replacementMsg + " in " + value);
            }

            return updatedValue;
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
            var suffix = string.IsNullOrWhiteSpace(PrimaryKeySuffix) ? string.Empty : PrimaryKeySuffix;

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

        private List<string> OntologyTermWithParents(OboEntry ontologyTerm, KeyValuePair<string, OboEntry.ParentTypeInfo> parentTerm)
        {
            var dataColumns = OntologyTermNoParents(ontologyTerm);
            dataColumns.Add(parentTerm.Value.ParentType.ToString());    // Parent term type
            dataColumns.Add(parentTerm.Value.ParentTermName);           // Parent term name
            dataColumns.Add(parentTerm.Key);                            // Parent term Identifier

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

                var parentTerms = new Dictionary<string, OboEntry.ParentTypeInfo>();
                var isObsolete = false;

                while (!reader.EndOfStream)
                {
                    var dataLine = reader.ReadLine();
                    lineNumber++;

                    if (string.IsNullOrWhiteSpace(dataLine))
                        break;

                    if (!SplitKeyValuePair(dataLine, ':', string.Empty, lineNumber, out var key, out var value))
                    {
                        continue;
                    }

                    switch (key)
                    {
                        case "id":
                            identifier = value;
                            break;

                        case "name":
                            name = AutoReplaceText(value, mNameReplacements, mNameReplacementCountsByType);
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
                            // Ignore
                            break;

                        case "is_a":

                            if (!SplitKeyValuePair(value.Trim(), '!', "is_a", lineNumber, out var parentTermId, out var parentTermName))
                            {
                                continue;
                            }

                            AddParentTerm(parentTerms, "is_a", parentTermId, parentTermName, lineNumber, dataLine);
                            break;

                        case "synonym":
                            // Ignore
                            break;

                        case "replaced_by":
                            // Ignore
                            break;

                        case "alt_id":
                            // Ignore
                            break;

                        case "relationship":

                            // relationship: part_of MS:1000458 ! source
                            // relationship: has_units UO:0000187 ! percent

                            if (!SplitKeyValuePair(value.Trim(), '!', "relationshipDef", lineNumber, out var relationshipTypeAndParent, out var relationshipValue))
                            {
                                continue;
                            }

                            if (!SplitKeyValuePair(relationshipTypeAndParent.Trim(), ' ', "relationshipTypeAndParent", lineNumber, out var relationshipType, out var relationshipParentTermName))
                            {
                                continue;
                            }

                            switch (relationshipType)
                            {
                                case "has_domain":
                                case "develops_from":
                                case "related_to":
                                    // Ignore these relationship types
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
                                    OnWarningEvent("Unknown relationship type " + relationshipType + " at line " + lineNumber + ": " + dataLine);
                                    break;
                            }
                            break;

                        case "is_obsolete":
                            isObsolete = true;
                            break;

                        case "property_value":
                        case "created_by":
                        case "creation_date":
                            // Ignore
                            break;

                        default:
                            OnWarningEvent("Unknown key " + key + " at line " + lineNumber + ": " + dataLine);
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

        private bool SplitKeyValuePair(string data, char delimiter, string dataDescription, int lineNumber, out string key, out string value)
        {
            var charIndex = data.IndexOf(delimiter);

            if (charIndex < 0)
            {
                var delimiterName = delimiter == ' ' ? "space" : delimiter.ToString();

                if (string.IsNullOrWhiteSpace(dataDescription))
                    OnWarningEvent(delimiterName + " not found in line " + lineNumber + ": " + data);
                else
                    OnWarningEvent(delimiterName + " not found in " + dataDescription + " for line " + lineNumber + ": " + data);

                key = null;
                value = null;

                return false;
            }

            key = data.Substring(0, charIndex).Trim();
            value = data.Substring(charIndex + 1).Trim();

            return true;
        }

        private bool WriteOboInfoToFile(IReadOnlyCollection<OboEntry> ontologyEntries, FileSystemInfo outputFile)
        {
            try
            {
                OnStatusEvent("Creating " + outputFile.FullName);

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
                        columnHeaders.Add("Parent_term_type");
                        columnHeaders.Add("Parent_term_name");
                        columnHeaders.Add("Parent_term_ID");
                    }

                    if (OutputOptions.IncludeGrandparentTerms)
                    {
                        columnHeaders.Add("GrandParent_term_type");
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
                                    lineOut.Add(string.Empty);      // Parent term type
                                    lineOut.Add(string.Empty);      // Parent term name
                                    lineOut.Add(string.Empty);      // Parent term ID
                                }
                                else
                                {
                                    lineOut.Add(string.Empty);      // Parent term type
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
                                        lineOut.Add(string.Empty);      // Grandparent term type
                                        lineOut.Add(string.Empty);      // Grandparent term name
                                        lineOut.Add(string.Empty);      // Grandparent term ID
                                    }
                                    else
                                    {
                                        lineOut.Add(string.Empty);      // Grandparent term type
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
                                lineOut.Add(grandParent.Value.ParentType.ToString());   // Grandparent term type
                                lineOut.Add(grandParent.Value.ParentTermName);          // Grandparent term name
                                lineOut.Add(grandParent.Key);                           // Grandparent term ID

                                writer.WriteLine(string.Join("\t", lineOut));
                            }
                        } // ForEach
                    } // ForEach
                } // Using

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error writing to file " + outputFile.FullName + ": " + ex.Message);
                return false;
            }
        }
    }
}
