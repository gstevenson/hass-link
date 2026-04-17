$ErrorActionPreference = "Stop"

$ResultsDir = "coverage-results"
$ReportDir  = "coverage-report"

Remove-Item -Recurse -Force $ResultsDir -ErrorAction SilentlyContinue

dotnet test tests/HassLink.Tests/HassLink.Tests.csproj `
    --collect:"XPlat Code Coverage" `
    --results-directory $ResultsDir `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet tool run reportgenerator `
    "-reports:$ResultsDir/**/coverage.cobertura.xml" `
    "-targetdir:$ReportDir" `
    "-reporttypes:Html;TextSummary"

Write-Host ""
Get-Content "$ReportDir/Summary.txt"
Write-Host ""
Write-Host "Full report: $ReportDir/index.html"
