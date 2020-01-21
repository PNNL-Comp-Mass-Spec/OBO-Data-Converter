..\bin\OBODataConverter.exe psi-ms_4.1.33.obo
..\bin\OBODataConverter.exe psi-ms_4.1.33.obo  psi-ms_4.1.33_NoAncestors.txt /NoP
..\bin\OBODataConverter.exe psi-ms_4.1.33.obo  psi-ms_4.1.33_WithParents.txt /NoG
..\bin\OBODataConverter.exe psi-ms_4.1.33.obo  psi-ms_4.1.33_WithParentsAndGrandparents.txt

..\bin\OBODataConverter.exe psi-ms_4.1.33.obo  psi-ms_4.1.33_WithDefinition_StripQuotes.txt /Def /StripQuotes /Com /NoP /NoObsolete /PK:MS1

rem Use this one for DMS
..\bin\OBODataConverter.exe psi-ms_4.1.33.obo  psi-ms_4.1.33_WithDefinition_StripQuotes_IncludeObsolete.txt /Def /StripQuotes /Com /PK:MS1

rem Use this in Visual Studio
rem ..\docs\psi-ms_4.1.33.obo psi-ms_4.1.33_WithDefinition_StripQuotes_IncludeObsolete.txt /Def /StripQuotes /Com /PK:MS1

pause
