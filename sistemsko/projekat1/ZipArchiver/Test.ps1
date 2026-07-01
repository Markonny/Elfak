$url = "http://localhost:5050/test1.txt&test2.txt"
Write-Host "--- POKREĆEM STRESS TEST ---" -ForegroundColor Cyan

1..15 | ForEach-Object {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $r = Invoke-WebRequest -Uri $url -Method Get -UseBasicParsing
        $sw.Stop()
        $ms = $sw.ElapsedMilliseconds
        
        $boja = if ($ms -gt 100) { "Yellow" } else { "Green" }
        
        Write-Host "Zahtev $_ : Status $($r.StatusCode) | Vreme: $($ms)ms" -ForegroundColor $boja
    } catch {
        Write-Host "Zahtev $_ : GREŠKA!" -ForegroundColor Red
    }
}

Write-Host "--- TEST ZAVRŠEN ---" -ForegroundColor Cyan
# Ova linija drži prozor otvorenim dok ne pritisneš taster
Read-Host "Pritisni Enter za izlaz"