..\bin\OBODataConverter.exe psi-ms_4.1.25.obo
..\bin\OBODataConverter.exe psi-ms_4.1.25.obo  psi-ms_4.1.25_NoAncestors.txt /NoP
..\bin\OBODataConverter.exe psi-ms_4.1.25.obo  psi-ms_4.1.25_WithParents.txt /NoG
..\bin\OBODataConverter.exe psi-ms_4.1.25.obo  psi-ms_4.1.25_WithParentsAndGrandparents.txt

..\bin\OBODataConverter.exe psi-ms_4.1.25.obo  psi-ms_4.1.25_WithDefinition_StripQuotes.txt /Def /StripQuotes /Com /NoP /NoObsolete /PK:MS1

..\bin\OBODataConverter.exe psi-ms_4.1.25.obo  psi-ms_4.1.25_WithDefinition_StripQuotes_IncludeObsolete.txt /Def /StripQuotes /Com /PK:MS1

pause
