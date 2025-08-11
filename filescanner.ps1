# Get tracked files according to .gitignore
$trackedFiles = git ls-files

# Search only in tracked files for sensitive patterns
$matches = $trackedFiles | ForEach-Object {
    Select-String -Path $_ -Pattern `
        'secret', `
        'password', `
        'apikey', `
        'api_key', `
        'token', `
        'connectionstring', `
        'connection_string', `
        'accountkey', `
        'account_key', `
        'Authorization\s*:\s*\S+', `
        'Endpoint\s*=.*?;.*?Key\s*=.*?;', `
        'DefaultEndpointsProtocol=.*?;AccountName=.*?;AccountKey=.*?;', `
        'eyJ[a-zA-Z0-9_-]+\.eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+' `
        -CaseSensitive:$false -SimpleMatch:$false -ErrorAction SilentlyContinue
}

# Group matches by file and include details (line number and matching text)
$filesWithMatches = $matches | Group-Object -Property Path

# Output detailed results
if ($filesWithMatches.Count -gt 0) {
    Write-Host "Files with potential sensitive information found:`n"
    foreach ($fileGroup in $filesWithMatches) {
        Write-Host "File: $($fileGroup.Name)"
        Write-Host "  Matches found:"
        foreach ($match in $fileGroup.Group) {
            Write-Host "    - Line $($match.LineNumber): '$($match.Line.Trim())'"
            Write-Host "      Pattern matched: $($match.Pattern)"
        }
        Write-Host ""
    }
} else {
    Write-Host "No files with potential sensitive information found."
}