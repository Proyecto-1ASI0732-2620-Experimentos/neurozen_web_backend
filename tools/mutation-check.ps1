<#
.SYNOPSIS
    Prueba de mutación manual de NeuroZen (especificación de pruebas, sección 6.2).

.DESCRIPTION
    Introduce, uno por uno, seis errores conocidos en el backend, ejecuta las pruebas que deben detectarlos y
    restaura el archivo original (siempre, incluso si algo falla). Una mutación queda "DETECTADA" cuando al menos
    una prueba falla. El resultado se guarda en docs/mutation-results.md como evidencia.

    Requisitos: .NET 9 SDK. Las mutaciones marcadas como "integración" necesitan MySQL: la variable
    NEUROZEN_TEST_MYSQL (cadena de conexión al servidor) o Docker en marcha (Testcontainers).

.PARAMETER SkipIntegration
    Ejecuta solo las pruebas unitarias de cada mutación (sin MySQL ni Docker). La mutación M5 se omite porque solo
    la detectan las pruebas de integración.

.EXAMPLE
    pwsh ./tools/mutation-check.ps1
    pwsh ./tools/mutation-check.ps1 -SkipIntegration
#>
param([switch]$SkipIntegration)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $root 'neurozen.API.Tests/neurozen.API.Tests.csproj'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$mutations = @(
    @{ Id = 'M1'; Name = 'Aceptar ADMIN en el registro'
       File = 'neurozen.API/IAM/Application/Internal/CommandServices/UserCommandService.cs'
       From = 'if (role == UserRole.Admin)'
       To = 'if (false && role == UserRole.Admin)'
       Unit = 'FullyQualifiedName~SignUp_WithAdminRole_ShouldThrowValidation'
       Integration = 'FullyQualifiedName~BusinessError_ShouldReturnJsonMessageNot500' },
    @{ Id = 'M2'; Name = 'Mostrar psicólogos PENDING en el directorio'
       File = 'neurozen.API/Professionals/Infrastructure/Repositories/ProfessionalRepository.cs'
       From = '.Where(p => p.VerificationStatus == VerificationStatus.Verified && p.DeletedAt == null)'
       To = '.Where(p => p.DeletedAt == null)'
       Unit = 'FullyQualifiedName~Directory_ShouldOnlyReturnVerifiedProfessionals'
       Integration = 'FullyQualifiedName~PsychologistLifecycle_RegisterVerifyAppearInDirectory' },
    @{ Id = 'M3'; Name = 'Permitir la doble reserva (sin comprobar el horario)'
       File = 'neurozen.API/Appointments/Application/Internal/CommandServices/AppointmentCommandService.cs'
       From = 'if (await appointmentRepository.IsSlotTakenAsync(professionalId, startUtc))'
       To = 'if (false && await appointmentRepository.IsSlotTakenAsync(professionalId, startUtc))'
       Unit = 'FullyQualifiedName~Book_AlreadyTakenSlot_ShouldThrowConflict'
       Integration = '' },
    @{ Id = 'M4'; Name = 'No invertir las preguntas 4, 5, 7 y 8 del test'
       File = 'neurozen.API/Stress/Domain/Model/ValueObjects/PerceivedStressScale.cs'
       From = 'ReversedIndexes.Contains(index) ? MaxAnswer - answer : answer'
       To = 'answer'
       Unit = 'FullyQualifiedName~Assessment_ShouldRecalculateScoreWithReversedQuestions4578'
       Integration = '' },
    @{ Id = 'M5'; Name = 'Quitar el control de dueño en GET /users/{id}'
       File = 'neurozen.API/IAM/Interfaces/REST/UsersController.cs'
       From = 'if (id != currentUser.Id)'
       To = 'if (false && id != currentUser.Id)'
       Unit = ''
       Integration = 'FullyQualifiedName~GetUser_OfAnotherUser_ShouldReturn403' },
    @{ Id = 'M6'; Name = 'Guardar el CVV (en la columna del vencimiento)'
       File = 'neurozen.API/Subscriptions/Application/Internal/CommandServices/SubscriptionCommandService.cs'
       From = 'var cardLast4 = PaymentCard.LastFour(cardNumber!);'
       To = 'var cardLast4 = PaymentCard.LastFour(cardNumber!); cardExpiry = command.Cvv!;'
       Unit = 'FullyQualifiedName~Subscription_ShouldStoreOnlyLast4AndNeverCvv'
       Integration = 'FullyQualifiedName~Subscription_CreateAndGet_ShouldOnlyExposeLast4' }
)

$results = @()
foreach ($m in $mutations) {
    $filters = @($m.Unit)
    if (-not $SkipIntegration) { $filters += $m.Integration }
    $filter = ($filters | Where-Object { $_ -ne '' }) -join '|'
    if ($filter -eq '') {
        Write-Host "[$($m.Id)] omitida: solo la detectan pruebas de integración (-SkipIntegration)" -ForegroundColor Yellow
        $results += [pscustomobject]@{ Id = $m.Id; Mutacion = $m.Name; Pruebas = '-'; Resultado = 'OMITIDA' }
        continue
    }

    $path = Join-Path $root $m.File
    $original = [System.IO.File]::ReadAllText($path, $utf8NoBom)
    if (-not $original.Contains($m.From)) { throw "[$($m.Id)] No se encontró el código a mutar en $($m.File)" }

    try {
        [System.IO.File]::WriteAllText($path, $original.Replace($m.From, $m.To), $utf8NoBom)
        Write-Host "[$($m.Id)] $($m.Name) → dotnet test --filter `"$filter`"" -ForegroundColor Cyan
        dotnet test $testProject --filter $filter --nologo --verbosity quiet | Out-Host
        $detected = $LASTEXITCODE -ne 0
    }
    finally {
        [System.IO.File]::WriteAllText($path, $original, $utf8NoBom)
    }

    $status = if ($detected) { 'DETECTADA' } else { 'NO DETECTADA' }
    $color = if ($detected) { 'Green' } else { 'Red' }
    Write-Host "[$($m.Id)] $status" -ForegroundColor $color
    $results += [pscustomobject]@{ Id = $m.Id; Mutacion = $m.Name; Pruebas = $filter; Resultado = $status }
}

$lines = @('# Resultado de la prueba de mutación', '',
    "Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm')  ", "Modo: $(if ($SkipIntegration) { 'solo unitarias' } else { 'unitarias + integración' })", '',
    '| Id | Mutación introducida | Pruebas ejecutadas | Resultado |', '|---|---|---|---|')
$lines += $results | ForEach-Object { "| $($_.Id) | $($_.Mutacion) | ``$($_.Pruebas)`` | $($_.Resultado) |" }
$output = Join-Path $root 'docs/mutation-results.md'
[System.IO.File]::WriteAllLines($output, $lines, $utf8NoBom)
$results | Format-Table -AutoSize | Out-Host
Write-Host "Evidencia guardada en $output"

if ($results.Resultado -contains 'NO DETECTADA') { exit 1 }
