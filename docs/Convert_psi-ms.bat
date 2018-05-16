..\bin\OBODataConverter.exe psi-ms_4.1.6.obo
..\bin\OBODataConverter.exe psi-ms_4.1.6.obo  psi-ms_4.1.6_NoAncestors.txt /NoP
..\bin\OBODataConverter.exe psi-ms_4.1.6.obo  psi-ms_4.1.6_WithParents.txt /NoG
..\bin\OBODataConverter.exe psi-ms_4.1.6.obo  psi-ms_4.1.6_WithParentsAndGrandparents.txt

..\bin\OBODataConverter.exe psi-ms_4.1.6.obo  psi-ms_4.1.6_WithDefinition_StripQuotes.txt /Def /StripQuotes /Com /NoP /NoObsolete /PK:MS1

..\bin\OBODataConverter.exe psi-ms_4.1.6.obo  psi-ms_4.1.6_WithDefinition_StripQuotes_IncludeObsolete.txt /Def /StripQuotes /Com /PK:MS1

pause
