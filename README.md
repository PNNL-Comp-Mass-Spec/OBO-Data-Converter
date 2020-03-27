The OBO Data Converter reads an Ontology file in the OBO format and converts the data to a tab-delimited text file.

OBO files are described at https://oboformat.googlecode.com/svn/trunk/doc/GO.format.obo-1_2.html
and are typically edited / managed using OBO-Edit: http://www.oboedit.org/

## Downloads

Download a .zip file with the executable from:
https://github.com/PNNL-Comp-Mass-Spec/OBO-Data-Converter/releases

## Syntax

```
OBODataConverter.exe
 InputFilePath [/O:OutputFilePath] [/PK:Suffix] [/NoP] [/NoG] [/Def] [/StripQuotes] [/Com] [/NoObsolete]
```

The input file is the OBO file to convert

Optionally use `/O` to specify the output path
* If not provided the output file will have extension .txt or .txt.new

Use `/PK` to specify the string to append to the ontology term identifier 
when creating the primary key for the Term_PK column. 
* By default uses /PK:MS1

By default the output file includes parent terms
* Remove them with `/NoP`

By default the output file includes grandparent terms
* Remove them with `/NoG`
* Using `/NoP` auto-enables `/NoG`

By default the output file will not include the term definitions
* Include them with `/Def`

When using `/Def`, use `/StripQuotes` to look for definitions of the form
`"Description of term" [Ontology:Source]` and only include the text between the double quotes as the definition
* Using `/StripQuotes` auto-enables `/Def`

By default the output file will not include the term comments
* Include them with `/Com`

Use `/NoObsolete` to exclude obsolete terms

### Syntax Example

```
OBODataConverter.exe psi-ms_4.1.33.obo psi-ms_4.1.33_WithDefinition_StripQuotes_IncludeObsolete.txt /Def /StripQuotes /Com /PK:MS1
```


## Contacts

Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) in 2016 \
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov \
Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/ \
Source code: https://github.com/PNNL-Comp-Mass-Spec/OBO-Data-Converter

## License

Licensed under the Apache License, Version 2.0; you may not use this file except 
in compliance with the License.  You may obtain a copy of the License at 
http://www.apache.org/licenses/LICENSE-2.0
