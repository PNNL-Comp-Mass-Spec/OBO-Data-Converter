The OBO Data Converter reads an Ontology file in the OBO format and converts the data to a tab-delimited text file.

OBO files are described at https://oboformat.googlecode.com/svn/trunk/doc/GO.format.obo-1_2.html
and are typically edited / managed using OBO-Edit: http://www.oboedit.org/

== Downloads ==

Download a .zip file with the executable from:
http://omics.pnl.gov/software/obo-data-converter

== Syntax ==

OBODataConverter.exe
 InputFilePath [/O:OutputFilePath] [/PK:Suffix] [/NoP] [/NoG] [/Def] [/StripQuotes] [/Com] [/NoObsolete]

<p>The input file is the OBO file to convert</p>

<p>Optionally use /O to specify the output path<br>
If not provided the output file will have extension .txt or .txt.new</p>

<p>Use /PK to specify the string to append to the ontology term identifier when creating the primary key for the Term_PK column. By default uses /PK:MS1</p>

<p>By default the output file includes parent terms; remove them with /NoP<br>
By default the output file includes grandparent terms; remove them with /NoG<br>
Using /NoP auto-enables /NoG</p>

<p>By default the output file will not include the term definitions; include them with /Def</p>

<p>When using /Def, use /StripQuotes to look for definitions of the form<br>
  <code>"Description of term" [Ontology:Source]</code><br>
and only include the text between the double quotes as the definition</p>

<p>Using /StripQuotes auto-enables /Def</p>

<p>By default the output file will not include the term comments; include them with /Com</p>

<p>Use /NoObsolete to exclude obsolete terms</p>

-------------------------------------------------------------------------------
Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2016

E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com
Website: http://panomics.pnnl.gov/ or http://omics.pnl.gov or http://www.sysbio.org/resources/staff/
-------------------------------------------------------------------------------

Licensed under the Apache License, Version 2.0; you may not use this file except 
in compliance with the License.  You may obtain a copy of the License at 
http://www.apache.org/licenses/LICENSE-2.0
