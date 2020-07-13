
# Also run:
# dotnet tool restore

if (test-path .\Trl.TermDataRepresentation.Tests\TestResults) {
    Remove-Item -r -force .\Trl.TermDataRepresentation.Tests\TestResults
}

if (test-path .\UnitTestCoverageReport) {
    Remove-Item -r -force .\UnitTestCoverageReport
}

dotnet test --collect:"XPlat Code Coverage"
dotnet tool run reportgenerator -reports:.\*Tests\TestResults\*\*.xml -targetdir:.\UnitTestCoverageReport -reporttypes:Html
