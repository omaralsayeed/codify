#!/usr/bin/env pwsh

# Test script for Codify API - Complete end-to-end flow

$apiBase = "http://localhost:5237"
$results = @()

function Log-Test($title, $request, $response, $statusCode, $notes = "") {
    $result = @{
        Title = $title
        Request = $request
        Response = $response
        StatusCode = $statusCode
        Notes = $notes
    }
    $results += $result
    Write-Host "================================"
    Write-Host "TEST: $title"
    Write-Host "Status: $statusCode"
    Write-Host "Request: $request"
    Write-Host "Response: $($response | Out-String)"
    if ($notes) { Write-Host "Notes: $notes" }
    Write-Host ""
}

try {
    # Step 1: Register a student user
    Write-Host "Step 1: Registering a Student user..."
    $registerBody = @{
        email = "student$(Get-Random)@example.com"
        password = "StudentPass123!"
        firstName = "Test"
        lastName = "Student"
        userRole = "Student"
    } | ConvertTo-Json

    $registerResponse = Invoke-WebRequest -Uri "$apiBase/api/auth/register" `
        -Method POST `
        -Body $registerBody `
        -ContentType 'application/json' `
        -ErrorAction Stop

    $studentData = $registerResponse.Content | ConvertFrom-Json
    $studentId = $studentData.data.userId
    $studentToken = $studentData.data.token

    Log-Test "POST /api/auth/register (Student)" `
        $registerBody `
        ($studentData | ConvertTo-Json) `
        $registerResponse.StatusCode `
        "Student ID: $studentId"

    # Step 2: Register an instructor user
    Write-Host "Step 2: Registering an Instructor user..."
    $instructorBody = @{
        email = "instructor$(Get-Random)@example.com"
        password = "InstructorPass123!"
        firstName = "Test"
        lastName = "Instructor"
        userRole = "Instructor"
    } | ConvertTo-Json

    $instructorResponse = Invoke-WebRequest -Uri "$apiBase/api/auth/register" `
        -Method POST `
        -Body $instructorBody `
        -ContentType 'application/json' `
        -ErrorAction Stop

    $instructorData = $instructorResponse.Content | ConvertFrom-Json
    $instructorId = $instructorData.data.userId
    $instructorToken = $instructorData.data.token

    Log-Test "POST /api/auth/register (Instructor)" `
        $instructorBody `
        ($instructorData | ConvertTo-Json) `
        $instructorResponse.StatusCode `
        "Instructor ID: $instructorId"

    # Step 3: Create a problem (as instructor)
    Write-Host "Step 3: Creating a problem..."
    $problemBody = @{
        title = "Hello World"
        description = "Print hello to output"
        difficulty = "Easy"
        sampleInput = ""
        sampleOutput = "hello"
        conceptTagIds = @()
    } | ConvertTo-Json

    $problemResponse = Invoke-WebRequest -Uri "$apiBase/api/problems" `
        -Method POST `
        -Body $problemBody `
        -ContentType 'application/json' `
        -Headers @{ Authorization = "Bearer $instructorToken" } `
        -ErrorAction Stop

    $problemData = $problemResponse.Content | ConvertFrom-Json
    $problemId = $problemData.data.id

    Log-Test "POST /api/problems (Create)" `
        $problemBody `
        ($problemData | ConvertTo-Json) `
        $problemResponse.StatusCode `
        "Problem ID: $problemId"

    # Step 4: Create a test case (as instructor)
    Write-Host "Step 4: Creating a test case..."
    $testCaseBody = @{
        input = ""
        expectedOutput = "hello"
        isPublic = $true
        visibility = "Public"
        orderIndex = 0
    } | ConvertTo-Json

    $testCaseResponse = Invoke-WebRequest -Uri "$apiBase/api/problems/$problemId/testcases" `
        -Method POST `
        -Body $testCaseBody `
        -ContentType 'application/json' `
        -Headers @{ Authorization = "Bearer $instructorToken" } `
        -ErrorAction Stop

    $testCaseData = $testCaseResponse.Content | ConvertFrom-Json
    $testCaseId = $testCaseData.data.id

    Log-Test "POST /api/problems/{id}/testcases (Create)" `
        $testCaseBody `
        ($testCaseData | ConvertTo-Json) `
        $testCaseResponse.StatusCode `
        "TestCase ID: $testCaseId"

    # Step 5: Create a submission with code that prints "hello"
    Write-Host "Step 5: Creating a submission..."
    $submissionBody = @{
        problemId = $problemId
        code = "print('hello')"
        language = "Python"
    } | ConvertTo-Json

    $submissionResponse = Invoke-WebRequest -Uri "$apiBase/api/submissions" `
        -Method POST `
        -Body $submissionBody `
        -ContentType 'application/json' `
        -Headers @{ Authorization = "Bearer $studentToken" } `
        -ErrorAction Stop

    $submissionData = $submissionResponse.Content | ConvertFrom-Json
    $submissionId = $submissionData.data.id
    $submissionStatus = $submissionData.data.status

    Log-Test "POST /api/submissions (Create)" `
        $submissionBody `
        ($submissionData | ConvertTo-Json) `
        $submissionResponse.StatusCode `
        "Submission ID: $submissionId, Status: $submissionStatus"

    # Step 6: Check if response is 202 Accepted or 200 OK with Pending status
    if ($submissionResponse.StatusCode -eq 202 -or $submissionResponse.StatusCode -eq 201) {
        Write-Host "✓ Submission returned HTTP $($submissionResponse.StatusCode)"
    } else {
        Write-Host "⚠ Submission returned HTTP $($submissionResponse.StatusCode) instead of 202/201"
    }

    # Wait for evaluation
    Write-Host "Step 6: Waiting 5 seconds for Judge0 to evaluate..."
    Start-Sleep -Seconds 5

    # Step 7: Get submission details
    Write-Host "Step 7: Fetching submission details..."
    $detailsResponse = Invoke-WebRequest -Uri "$apiBase/api/submissions/$submissionId" `
        -Method GET `
        -Headers @{ Authorization = "Bearer $studentToken" } `
        -ErrorAction Stop

    $detailsData = $detailsResponse.Content | ConvertFrom-Json

    Log-Test "GET /api/submissions/{id} (Details)" `
        "N/A" `
        ($detailsData | ConvertTo-Json) `
        $detailsResponse.StatusCode `
        "Final Status: $($detailsData.data.status), TestCaseResults count: $($detailsData.data.testCaseResults.Count)"

}
catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    Log-Test "ERROR" "See above" $_.Exception.Message 0 "Exception occurred"
}

# Summary
Write-Host ""
Write-Host "========== FINAL TEST SUMMARY =========="
Write-Host "Total tests run: $($results.Count)"
foreach ($result in $results) {
    $icon = if ($result.StatusCode -eq 0) { "✗" } else { "✓" }
    Write-Host "$icon $($result.Title) - Status $($result.StatusCode)"
}
