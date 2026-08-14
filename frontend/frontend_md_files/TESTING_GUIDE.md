# Frontend Integration Testing Guide

> **Status:** Dev server is running at `http://localhost:4200`  
> **Test Date:** August 11, 2026

---

## 🎯 Pre-Test Checklist

Before starting testing, verify:

- [ ] Frontend running at http://localhost:4200
- [ ] Backend running at http://localhost:5237
- [ ] Swagger docs accessible at http://localhost:5237/swagger
- [ ] Browser dev tools console open to watch for errors

---

## 📋 Test Scenarios

### Test 1: Register New Student

**Goal:** Verify registration wires to backend correctly

**Steps:**
1. Open http://localhost:4200
2. Click "Sign up" or navigate to http://localhost:4200/auth/register
3. Fill form:
   - Full Name: `Test User`
   - Email: `testuser@example.com`
   - Password: `password123` (8+ chars)
   - Confirm Password: `password123`
   - Role: Select "Student"
4. Click "Register"

**Expected Result:**
- ✅ No errors in browser console
- ✅ HTTP POST to `/api/auth/register` succeeds (201)
- ✅ Auto-login happens (HTTP POST to `/api/auth/login` succeeds)
- ✅ Redirects to `/problems` page
- ✅ Token stored in `localStorage['codify_token']`
- ✅ User object stored in `localStorage['codify_user']`
- ✅ Avatar initials display (e.g., "TU" for "Test User")

**Verification in browser console:**
```javascript
// Check localStorage
localStorage.getItem('codify_token')       // Should have JWT
localStorage.getItem('codify_user')        // Should have user JSON
```

**If it fails:**
- Check browser Network tab for HTTP errors
- Look for error message in UI
- Check backend console for validation errors

---

### Test 2: Login with Credentials

**Goal:** Verify login wires to backend correctly

**Steps:**
1. Log out (click logout button if visible, or clear localStorage)
   ```javascript
   localStorage.clear()
   ```
2. Navigate to http://localhost:4200/auth/login
3. Fill form:
   - Email: `testuser@example.com` (from Test 1)
   - Password: `password123`
4. Click "Sign in"

**Expected Result:**
- ✅ No errors in browser console
- ✅ HTTP POST to `/api/auth/login` succeeds (200)
- ✅ Redirects to `/problems` page
- ✅ Token stored in localStorage
- ✅ User object stored in localStorage

**Verification:**
```javascript
// Check localStorage has token
console.log(localStorage.getItem('codify_token').length > 50)  // true
```

**If it fails:**
- Wrong email/password → should show "Invalid email or password"
- Network error → check backend is running
- 401 error → check credentials are correct

---

### Test 3: Browse Problems List

**Goal:** Verify problems load from backend

**Steps:**
1. You should already be on `/problems` after login
2. Wait for page to load
3. Verify table shows problems
4. Try filtering by:
   - Difficulty (Easy, Medium, Hard)
   - Topic (Arrays, Graphs, etc.)

**Expected Result:**
- ✅ Problems table shows real data from database
- ✅ Difficulty badges show text (Easy/Medium/Hard), not numbers (0/1/2)
- ✅ Topic labels show joined tags (e.g., "Arrays · Hash Map")
- ✅ Filters work (client-side filtering)
- ✅ No loading spinner (data loaded successfully)

**Verification in Network tab:**
- Should see GET request to `/api/problems`
- Response should have real problem data with `title`, `difficulty`, `tags`, etc.

**Example Network Response:**
```json
{
  "data": [
    {
      "id": "uuid-here",
      "title": "Two Sum",
      "difficulty": 0,
      "tags": ["Arrays", "Hash Map"],
      "isActive": true
    }
  ]
}
```

**If it fails:**
- Shows "Loading problems..." forever → API call hanging or slow
- Shows error message → check error in Network tab
- Shows hardcoded mock problems → API call not being made
- Shows "0", "1", "2" for difficulty → enum mapping not working

---

### Test 4: View Problem Detail

**Goal:** Verify problem detail loads dynamically

**Steps:**
1. Click on any problem in the list
2. Wait for problem page to load
3. Verify title, description, examples, constraints

**Expected Result:**
- ✅ Problem title displays (not "1. Two Sum", but actual title)
- ✅ Difficulty badge shows text (Easy/Medium/Hard)
- ✅ Topic shows tags joined with ` · `
- ✅ Description loads (problem statement)
- ✅ Examples load (input, output, explanation)
- ✅ Constraints load as a list
- ✅ No loading spinner (data loaded successfully)

**Verification in Network tab:**
- Should see GET request to `/api/problems/{id}`
- Response should have problem details with `statement`, `constraints`, `sampleTestCases`, etc.

**Example Network Response:**
```json
{
  "data": {
    "id": "uuid-here",
    "title": "Two Sum",
    "difficulty": 0,
    "tags": ["Arrays", "Hash Map"],
    "statement": "Given an array of integers...",
    "constraints": "2 <= nums.length <= 10^4\n-10^9 <= nums[i] <= 10^9",
    "sampleTestCases": [
      {
        "input": "nums = [2,7,11,15], target = 9",
        "expectedOutput": "[0,1]"
      }
    ]
  }
}
```

**If it fails:**
- Shows loading spinner forever → API call hanging
- Shows error message → check error in Network tab
- Shows hardcoded "Two Sum" content → route param not being read
- Shows "0", "1", "2" for difficulty → enum mapping not working
- Examples don't show → field mapping issue (`sampleTestCases` → `examples`)

---

### Test 5: Verify Error Cases

**Goal:** Test error handling works correctly

#### Test 5a: Wrong Password

1. Navigate to login page
2. Enter correct email but wrong password
3. Click Sign In

**Expected Result:**
- ✅ Shows error message: "Invalid email or password"
- ✅ Does NOT redirect or log in
- ✅ No exception in console

#### Test 5b: Short Password at Register

1. Navigate to register page
2. Fill form with 7-character password
3. Click Register

**Expected Result:**
- ✅ Shows validation error: "Password must be at least 8 characters."
- ✅ Does NOT submit to backend
- ✅ Form stays on same page

#### Test 5c: Duplicate Email at Register

1. Try to register with email from Test 1
2. Fill form correctly
3. Click Register

**Expected Result:**
- ✅ Shows error: "Email is already registered."
- ✅ Form stays on same page
- ✅ User is NOT logged in

#### Test 5d: Unauthorized Access

1. Open browser console and run:
   ```javascript
   localStorage.removeItem('codify_token')
   localStorage.removeItem('codify_user')
   ```
2. Navigate to http://localhost:4200/problems

**Expected Result:**
- ✅ Redirects to `/auth/login`
- ✅ User cannot access protected pages without token

---

## 🔍 Browser Console Checks

Open the browser dev tools console and verify no errors appear during these actions:

```javascript
// These should all be truthy after login
localStorage.getItem('codify_token')           // JWT string
localStorage.getItem('codify_user')            // User JSON

// Parse user to verify structure
const user = JSON.parse(localStorage.getItem('codify_user'))
user.id                                        // UUID
user.name                                      // "Test User"
user.email                                     // "testuser@example.com"
user.role                                      // "student"
user.avatarInitials                            // "TU"
```

**Should NOT see:**
- ❌ CORS errors
- ❌ 401/403 Unauthorized
- ❌ 400 Bad Request
- ❌ Network errors
- ❌ JSON parsing errors
- ❌ Undefined is not a function

---

## 🌐 Network Tab Checks

Open DevTools Network tab and verify:

### Expected Requests

#### After Register
```
POST /api/auth/register          [201 Created]
POST /api/auth/login             [200 OK]
GET  /api/problems               [200 OK]
```

#### After Login
```
POST /api/auth/login             [200 OK]
GET  /api/problems               [200 OK]
```

#### After Clicking Problem
```
GET  /api/problems/{id}          [200 OK]
```

### All Requests Should Have

✅ **Status:** 200 or 201 (never 4xx or 5xx)  
✅ **Response type:** application/json  
✅ **Headers:** Include `Authorization: Bearer <token>` for protected routes  
✅ **Response body:** Valid JSON with `data` envelope

**Example:**
```json
{ "data": { ... } }
```

---

## 📊 Data Validation Checklist

After each test, verify the data structure:

### User Object (after login)
```typescript
✅ id: string (UUID)
✅ name: string (from fullName)
✅ email: string
✅ role: 'student' | 'instructor' (NOT 0 or 1)
✅ avatarInitials: string (first+last letters uppercase)
✅ streak?: number (default 0)
```

### Problem Summary (from list)
```typescript
✅ id: string (UUID)
✅ title: string
✅ difficulty: 'easy' | 'medium' | 'hard' (NOT 0/1/2)
✅ topic: string (lowercase with hyphens)
✅ topicLabel: string (tags joined with ' · ')
✅ solvedCount?: number (0 in list, real count in detail)
```

### Problem Detail
```typescript
✅ id: string (UUID)
✅ title: string
✅ difficulty: string
✅ description: string (from statement)
✅ constraints: string[] (split by \n)
✅ examples: Array<{ input, output, explanation }>
✅ solvedCount: number (from acceptedSubmissionsCount)
```

---

## 🐛 Troubleshooting

### Problem: "Cannot GET /problems"

**Cause:** Frontend routing not working  
**Fix:** 
- Check that Angular is serving the app
- Try hard refresh (Ctrl+F5)
- Check browser console for errors

### Problem: "CORS error"

**Cause:** Backend not configured to accept requests from localhost:4200  
**Fix:**
- Check backend CORS settings
- Verify backend is running
- Try http://localhost:5237/api/problems directly in browser

### Problem: "Invalid token"

**Cause:** Token expired or corrupted  
**Fix:**
- Log out: `localStorage.clear()`
- Log back in
- Token should be fresh

### Problem: "400 Bad Request"

**Cause:** Frontend sending invalid data format  
**Fix:**
- Check Network tab Response for validation error message
- Verify enum values (role as number 0|1, not string)
- Verify field names match backend expectations

### Problem: Data shows but values wrong

**Cause:** Field mapping issue  
**Fix:**
- Difficulty shows 0/1/2 → Check `mapDifficulty()` called
- Topic shows raw tag string → Check `tags[0]` mapping
- Name shows `fullName` → Check `fullName` renamed to `name`

---

## ✅ Success Criteria

All tests pass if:

| Test | Status |
|---|---|
| Register → Auto-login → Redirect | ✅ |
| Login → Load problems | ✅ |
| Problems list shows real data | ✅ |
| Problem detail loads dynamically | ✅ |
| Difficulty shows text not numbers | ✅ |
| No errors in console | ✅ |
| No CORS errors | ✅ |
| Logout/clear token → Redirects to login | ✅ |

---

## 📝 Test Results Template

Copy and fill this out after testing:

```markdown
# Test Results — [Date]

## Environment
- Frontend: http://localhost:4200 ✅/❌
- Backend: http://localhost:5237 ✅/❌
- Build: npm start ✅/❌

## Test 1: Register New Student
- Result: ✅/❌
- Issues: [list any]

## Test 2: Login with Credentials
- Result: ✅/❌
- Issues: [list any]

## Test 3: Browse Problems List
- Result: ✅/❌
- Issues: [list any]

## Test 4: View Problem Detail
- Result: ✅/❌
- Issues: [list any]

## Test 5: Error Cases
- Wrong password: ✅/❌
- Short password: ✅/❌
- Duplicate email: ✅/❌
- Unauthorized access: ✅/❌

## Overall Result
- ✅ All tests passed
- ⚠️ Some issues found (list below)
- ❌ Critical issues (describe)

## Issues Found
- [Issue 1]
- [Issue 2]
```

---

## Next Steps After Testing

1. **If all tests pass:** ✅ Integration is working! Document results and proceed to Sprint 2
2. **If some tests fail:** ⚠️ Debug using troubleshooting guide above
3. **If critical issues:** ❌ Stop and review the integration code

---

**Need help?** Check the console errors and compare with the troubleshooting section.
