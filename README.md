The OBO Data Converter reads an Ontology file in the OBO format and converts the data to a tab-delimited text file.

OBO files are described at https://oboformat.googlecode.com/svn/trunk/doc/GO.format.obo-1_2.html
and are typically edited / managed using OBO-Edit: http://www.oboedit.org/

== Downloads ==

Download a .zip file with the executable from:
http://omics.pnl.gov/software/obo-data-converter

== Syntax ==

OBODataConverter.exe
 InputFilePath [/O:OutputFilePath] [/PK:Suffix] [/NoP] [/NoG] [/Def] [/StripQuotes] [/Com] [/NoObsolete]

The input file is the OBO file to convert

Optionally use /O to specify the output path
If not provided the output file will have extension .txt or .txt.new

Use /PK to specify the string to append to the ontology term identifier 
when creating the primary key for the Term_PK column. By default uses /PK:MS1

By default the output file includes parent terms; remove them with /NoP
By default the output file includes grandparent terms; remove them with /NoG
Using /NoP auto-enables /NoG

By default the output file will not include the term definitions; include them with /Def

When using /Def, use /StripQuotes to look for definitions of the form
  "Description of term" [Ontology:Source]
and only include the text between the double quotes as the definition

Using /StripQuotes auto-enables /Def

By default the output file will not include the term comments; include them with /Com

Use /NoObsolete to exclude obsolete terms

## Contacts

Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2016 \
E-mail: matthew.monroe@pnnl.gov or matt@alchemistmatt.com \
Website: http://panomics.pnnl.gov/ or http://omics.pnl.gov or http://www.sysbio.org/resources/staff/

## License

Licensed under the Apache License, Version 2.0; you may not use this file except 
in compliance with the License.  You may obtain a copy of the License at 
http://www.apache.org/licenses/LICENSE-2.0
