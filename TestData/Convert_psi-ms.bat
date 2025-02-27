..\bin\OBODataConverter.exe psi-ms_example.obo
..\bin\OBODataConverter.exe psi-ms_example.obo  psi-ms_example_NoAncestors.txt /NoP
..\bin\OBODataConverter.exe psi-ms_example.obo  psi-ms_example_WithParents.txt /NoG
..\bin\OBODataConverter.exe psi-ms_example.obo  psi-ms_example_WithParentsAndGrandparents.txt

..\bin\OBODataConverter.exe psi-ms_example.obo  psi-ms_example_WithDefinition_StripQuotes.txt /Def /StripQuotes /Com /NoP /NoObsolete /PK:MS1

rem Use this one for DMS
..\bin\OBODataConverter.exe psi-ms_example.obo  psi-ms_example_WithDefinition_StripQuotes_IncludeObsolete.txt /Def /StripQuotes /Com /PK:MS1 /Postgres

rem Use this in Visual Studio
rem ..\docs\psi-ms_4.1.191.obo /O:..\docs\psi-ms_4.1.191_WithDefinition_StripQuotes_IncludeObsolete.txt /Def /StripQuotes /Com /PK:MS1 /Postgres
pause
