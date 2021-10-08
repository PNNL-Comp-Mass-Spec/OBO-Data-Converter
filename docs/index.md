# __<span style="color:#D57500">OBO Data Converter</span>__
Utility for converting ontology OBO files to a tab-delimited text file

### Description
The OBO Data Converter reads an Ontology file in the OBO format and converts the data to a tab-delimited text file. OBO files are described at [https://owlcollab.github.io/oboformat/doc/GO.format.obo-1_2.html](https://owlcollab.github.io/oboformat/doc/GO.format.obo-1_2.html) and are typically edited / managed using OBO-Edit: [http://www.oboedit.org/](http://www.oboedit.org/)

### Downloads
* [Latest version](https://github.com/PNNL-Comp-Mass-Spec/OBO-Data-Converter/releases/latest)
* [Source code on GitHub](https://github.com/PNNL-Comp-Mass-Spec/OBO-Data-Converter)

#### Software Instructions
__Syntax__

`OBODataConverter.exe
 InputFilePath [/O:OutputFilePath] [/PK:Suffix] [/NoP] [/NoG] [/Def] [/StripQuotes] [/Com] [/NoObsolete]`

 The input file is the OBO file to convert

Optionally use /O to specify the output path
If not provided the output file will have extension .txt or .txt.new

Use /PK to specify the string to append to the ontology term identifier when creating the primary key for the Term_PK column. By default uses /PK:MS1

By default the output file includes parent terms; remove them with /NoP
By default the output file includes grandparent terms; remove them with /NoG
Using /NoP auto-enables /NoG

By default the output file will not include the term definitions; include them with /Def

When using /Def, use /StripQuotes to look for definitions of the form
`"Description of term" [Ontology:Source]`
and only include the text between the double quotes as the definition

Using /StripQuotes auto-enables /Def

By default the output file will not include the term comments; include them with /Com

Use /NoObsolete to exclude obsolete terms

### Acknowledgment

All publications that utilize this software should provide appropriate acknowledgement to PNNL and the OBO-Data-Converter GitHub repository. However, if the software is extended or modified, then any subsequent publications should include a more extensive statement, as shown in the Readme file for the given application or on the website that more fully describes the application.

### Disclaimer

These programs are primarily designed to run on Windows machines. Please use them at your own risk. This material was prepared as an account of work sponsored by an agency of the United States Government. Neither the United States Government nor the United States Department of Energy, nor Battelle, nor any of their employees, makes any warranty, express or implied, or assumes any legal liability or responsibility for the accuracy, completeness, or usefulness or any information, apparatus, product, or process disclosed, or represents that its use would not infringe privately owned rights.

Portions of this research were supported by the NIH National Center for Research Resources (Grant RR018522), the W.R. Wiley Environmental Molecular Science Laboratory (a national scientific user facility sponsored by the U.S. Department of Energy's Office of Biological and Environmental Research and located at PNNL), and the National Institute of Allergy and Infectious Diseases (NIH/DHHS through interagency agreement Y1-AI-4894-01). PNNL is operated by Battelle Memorial Institute for the U.S. Department of Energy under contract DE-AC05-76RL0 1830.

We would like your feedback about the usefulness of the tools and information provided by the Resource. Your suggestions on how to increase their value to you will be appreciated. Please e-mail any comments to proteomics@pnl.gov
