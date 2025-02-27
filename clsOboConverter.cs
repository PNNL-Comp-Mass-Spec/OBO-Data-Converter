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
        // Ignore Spelling: Obo, Postgres

        /// <summary>
        /// Default primary key suffix
        /// </summary>
        /// <remarks>
        /// This suffix is appended to the ontology term identifier when creating the primary key for the Term_PK column
        /// </remarks>
        public const string DEFAULT_PRIMARY_KEY_SUFFIX = "MS1";

        private const int AUTO_REPLACE_MESSAGE_THRESHOLD = 5;

        /// <summary>
        /// Output file options struct
        /// </summary>
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

            // ReSharper disable once GrammarMistakeInComment

            /// <summary>
            /// When true, use \N for null values (empty columns in the output file),
            /// escape backslashes, and surround each column value with double quotes
            /// </summary>
            /// <remarks>
            /// <para>
            /// This allows the data file to be imported using the COPY command
            /// </para>
            /// <para>
            /// COPY ont.T_Tmp_PsiMS_2022Apr FROM '/tmp/psi-ms_4.1.80_WithDefinition_StripQuotes_IncludeObsolete.txt' CSV HEADER DELIMITER E'\t' QUOTE '"';
            /// </para>
            /// </remarks>
            public bool FormatForPostgres;
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

        // ReSharper disable once UnusedMember.Global

        /// <summary>
        /// Convert an OBO file to a tab-delimited text file
        /// </summary>
        /// <param name="oboFilePath">OBO file path</param>
        /// <returns>True if success, otherwise false</returns>
        public bool ConvertOboFile(string oboFilePath)
        {
            var oboFile = new FileInfo(oboFilePath);

            var outputFilePath = ConstructOutputFilePath(oboFile);

            return ConvertOboFile(oboFilePath, outputFilePath);
        }

        /// <summary>
        /// Convert an OBO file to a tab-delimited text file
        /// </summary>
        /// <param name="oboFilePath">Input file path</param>
        /// <param name="outputFilePath">Output file path</param>
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

                var outputFile = string.IsNullOrWhiteSpace(outputFilePath)
                    ? new FileInfo(ConstructOutputFilePath(oboFile))
                    : new FileInfo(outputFilePath);

                var nullValueFlag = GetNullValueFlag();

                // Read the data from the OBO file
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
                            ParseTerm(reader, ontologyEntries, nullValueFlag, ref lineNumber);
                        }
                    }

                    // Make a list of identifiers that are parents of other terms
                    var parentNodes = new SortedSet<string>();

                    foreach (var ontologyTerm in ontologyEntries)
                    {
                        foreach (var parentTerm in ontologyTerm.ParentTerms)
                        {
                            // Add the parent term if not yet in the sorted set
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
                        OnStatusEvent(" ... {0} {1} times", replacementItem.Key, replacementItem.Value);
                    }
                }

                Console.WriteLine();
                OnStatusEvent("Found {0:N0} terms, of which {1:N0} are leaf nodes", ontologyEntries.Count, leafNodeCount);
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
                OnErrorEvent("Exception in ConvertOboFile at line {0}: {1}", lineNumber, ex.Message);
                return false;
            }
        }

        private string GetNullValueFlag()
        {
            return OutputOptions.FormatForPostgres ? @"\N" : string.Empty;
        }

        /// <summary>
        /// Default output file options
        /// </summary>
        public static OutputFileOptions DefaultOutputOptions()
        {
            return new OutputFileOptions
            {
                IncludeDefinition = false,
                StripQuotesFromDefinition = false,
                IncludeComment = false,
                IncludeParentTerms = true,
                IncludeGrandparentTerms = true,
                ExcludeObsolete = false
            };
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
                OnWarningEvent("Parent term specified twice; ignoring {0} for line {1}: {2}", parentTermId, lineNumber, dataLine);
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

                var replacementMsg = string.Format("auto-replaced {0} with {1}", replacementItem.Key, replacementItem.Value);

                if (!replacementCountsByType.TryGetValue(replacementMsg, out var previousCount))
                {
                    previousCount = 0;
                    replacementCountsByType.Add(replacementMsg, 0);
                }

                replacementCountsByType[replacementMsg] = previousCount + 1;

                if (previousCount < AUTO_REPLACE_MESSAGE_THRESHOLD)
                    OnStatusEvent(" ... {0} in {1}", replacementMsg, value);
            }

            return updatedValue;
        }

        private static byte BoolToTinyInt(bool value)
        {
            return value ? (byte)1 : (byte)0;
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

        private void ParseTerm(StreamReader reader, ICollection<OboEntry> ontologyEntries, string nullValueFlag, ref int lineNumber)
        {
            try
            {
                var identifier = nullValueFlag;
                var name = nullValueFlag;
                var definition = nullValueFlag;
                var comment = nullValueFlag;

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
                                case "develops_from":
                                case "has_column":
                                case "has_domain":
                                case "has_optional_column":
                                case "has_relation":
                                case "has_structured_representation_in_format":
                                case "has_value_concept":
                                case "has_value_type":
                                case "related_to":
                                    // Ignore these relationship types
                                    break;
                                case "has_order":
                                    break;
                                case "has_metric_category":
                                case "has_regexp":
                                case "has_units":
                                case "part_of":
                                    AddParentTerm(parentTerms, relationshipType, relationshipParentTermName, relationshipValue, lineNumber, dataLine);
                                    break;
                                default:
                                    OnWarningEvent("Unknown relationship type {0} at line {1}: {2}", relationshipType, lineNumber, dataLine);
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
                            OnWarningEvent("Unknown key {0} at line {1}: {2}", key, lineNumber, dataLine);
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
                throw new Exception(string.Format("Exception in ParseTerm at line {0}: {1}", lineNumber, ex.Message), ex);
            }
        }

        private bool SplitKeyValuePair(string data, char delimiter, string dataDescription, int lineNumber, out string key, out string value)
        {
            var charIndex = data.IndexOf(delimiter);

            if (charIndex < 0)
            {
                var delimiterName = delimiter == ' ' ? "space" : delimiter.ToString();

                if (string.IsNullOrWhiteSpace(dataDescription))
                {
                    OnWarningEvent("{0} not found in line {1}: {2}", delimiterName, lineNumber, data);
                }
                else
                {
                    // Example message:
                    // ! not found in relationshipDef for line 6733: has_value_type xsd:dateTime
                    OnWarningEvent("{0} not found in {1} for line {2}: {3}", delimiterName, dataDescription, lineNumber, data);
                }

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

                var nullValueFlag = GetNullValueFlag();

                using var writer = new StreamWriter(new FileStream(outputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));

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

                // ReSharper disable once MergeIntoPattern
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

                var columnCount = columnHeaders.Count;

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
                                lineOut.Add(nullValueFlag); // Parent term type
                                lineOut.Add(nullValueFlag); // Parent term name
                                lineOut.Add(nullValueFlag); // Parent term ID
                            }
                            else
                            {
                                lineOut.Add(nullValueFlag); // Parent term type
                                lineOut.Add(nullValueFlag); // Parent term name
                                lineOut.Add(nullValueFlag); // Parent term ID
                            }

                            if (OutputOptions.IncludeGrandparentTerms)
                            {
                                lineOut.Add(nullValueFlag); // Grandparent term type
                                lineOut.Add(nullValueFlag); // Grandparent term name
                                lineOut.Add(nullValueFlag); // Grandparent term ID
                            }
                        }

                        WriteLine(writer, lineOut, columnCount, nullValueFlag);
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
                                if (ancestor?.IsObsolete == true && !string.IsNullOrWhiteSpace(purgatoryTermID))
                                {
                                    lineOut.Add(nullValueFlag); // Grandparent term type
                                    lineOut.Add(nullValueFlag); // Grandparent term name
                                    lineOut.Add(nullValueFlag); // Grandparent term ID
                                }
                                else
                                {
                                    lineOut.Add(nullValueFlag); // Grandparent term type
                                    lineOut.Add(nullValueFlag); // Grandparent term name
                                    lineOut.Add(nullValueFlag); // Grandparent term ID
                                }
                            }

                            WriteLine(writer, lineOut, columnCount, nullValueFlag);
                            continue;
                        }

                        foreach (var grandParent in ancestor.ParentTerms)
                        {
                            var lineOut = OntologyTermWithParents(ontologyTerm, parentTerm);
                            lineOut.Add(grandParent.Value.ParentType.ToString());   // Grandparent term type
                            lineOut.Add(grandParent.Value.ParentTermName);          // Grandparent term name
                            lineOut.Add(grandParent.Key);                           // Grandparent term ID

                            WriteLine(writer, lineOut, columnCount, nullValueFlag);
                        }
                    }   // ForEach
                }       // ForEach

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error writing to file {0}: {1}", outputFile.FullName, ex.Message);
                return false;
            }
        }

        private void WriteLine(TextWriter writer, IList<string> lineOut, int columnCount, string nullValueFlag)
        {
            if (!string.IsNullOrWhiteSpace(nullValueFlag))
            {
                while (lineOut.Count < columnCount)
                {
                    lineOut.Add(nullValueFlag);
                }
            }

            if (OutputOptions.FormatForPostgres)
            {
                for (var i = 0; i < lineOut.Count; i++)
                {
                    if (lineOut[i].Equals(@"\N"))
                        continue;

                    // Escape backslashes with \\
                    // Replace double quotes with ""
                    // Surround the entire field with double quotes
                    lineOut[i] = string.Format("\"{0}\"", lineOut[i].Replace(@"\", @"\\").Replace("\"", "\"\""));
                }
            }

            writer.WriteLine(string.Join("\t", lineOut));
        }
    }
}
